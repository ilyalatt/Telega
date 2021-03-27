using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.ServiceTransport;
using Telega.Utils;

namespace Telega.Rpc {
    class TgTransport : IDisposable {
        readonly Var<Session> _session;
        readonly MtProtoCipherTransport _transport;
        readonly TaskQueue _rpcQueue = new();
        readonly ConcurrentStack<long> _unconfirmedMsgIds = new(); // such a bad design

        readonly Task _receiveLoopTask;
        readonly ConcurrentDictionary<long, TaskCompletionSource<RpcResult>> _rpcFlow = new();

        public CustomObservable<UpdatesType> Updates { get; } = new();

        async Task ReceiveLoopImpl(ILogger logger) {
            while (true) {
                var msgBody = await _transport.Receive().ConfigureAwait(false);
                var msg = TgSystemMessageHandler.ReadMsg(msgBody);
                var ctx = new TgSystemMessageHandlerContext(logger);
                msg.With(TgSystemMessageHandler.Handle(ctx));

                ctx.NewSalt.NIter(salt =>
                    _session.SetWith(x => x.With(salt: salt))
                );
                ctx.Ack.Iter(_unconfirmedMsgIds.Push);

                TaskCompletionSource<RpcResult>? CaptureFlow(long id) =>
                    _rpcFlow.TryRemove(id, out var flow) ? flow : null;

                ctx.RpcResults.Iter(res => CaptureFlow(res.Id).NMatch(
                    flow => flow.SetResult(res),
                    () => ctx.Logger.LogTrace($"TgTransport: Unexpected RPC result, the message id is {res.Id}")
                ));

                ctx.Updates.Iter(Updates.OnNext);
            }
        }

        async Task ReceiveLoop(ILogger logger) {
            //            try //            {
            await ReceiveLoopImpl(logger).ConfigureAwait(false);
            //            }
            //            catch (TgTransportException e) //            {
            //                // Updates.OnError(e);
            //            }
        }

        public TgTransport(ILogger logger, MtProtoCipherTransport transport, Var<Session> session) {
            _transport = transport;
            _session = session;
            _receiveLoopTask = Task.Run(() => ReceiveLoop(logger));
        }

        public void Dispose() => _transport.Dispose();


        // it is not supported
        /*
        const uint GZipPackedTypeNumber = 0x3072cfa1;
        static byte[] GZip(byte[] data) {
            var ms = new MemoryStream();
            var gzip = new GZipStream(ms, CompressionLevel.Fastest);
            gzip.Write(data, 0, data.Length);
            return ms.ToArray();
        }
        static byte[] TryPack(byte[] data) {
            return data;
            var gzip = BtHelpers.UsingMemBinWriter(bw => {
                TgMarshal.WriteUint(bw, GZipPackedTypeNumber);
                TgMarshal.WriteBytes(bw, GZip(data).ToBytesUnsafe());
            });

            return gzip.Length < data.Length ? gzip : data;
        }
        */


        IReadOnlyList<long> PopUnconfirmedMsgIds() {
            const int magic = 3;
            var ids = new List<long>(_unconfirmedMsgIds.Count + magic);
            while (_unconfirmedMsgIds.TryPop(out var id)) {
                ids.Add(id);
            }

            return ids;
        }


        int GetSeqNum(bool inc) {
            var seqNum = _session.Get().Sequence * 2 + (inc ? 1 : 0);
            if (inc) {
                _session.SetWith(x => x.With(sequence: x.Sequence + 1));
            }

            return seqNum;
        }

        byte[] CreateMsg(byte[] msg, bool isContentRelated, long? msgId = null) => BtHelpers.UsingMemBinWriter(bw => {
            bw.Write(msgId ?? Session.GetNewMessageId(_session));
            bw.Write(GetSeqNum(isContentRelated));
            bw.Write(msg.Length);
            bw.Write(msg);
        });

        byte[] CreateMsg(ITgSerializable dto, bool isContentRelated, long? msgId = null) =>
            CreateMsg(BtHelpers.UsingMemBinWriter(dto.Serialize), isContentRelated, msgId);


        const uint MsgContainerTypeNumber = 0x73f1f8dc;

        (byte[], long) WithAck(ITgSerializable dto) {
            var unconfirmedIds = PopUnconfirmedMsgIds();
            var shouldNotAck = unconfirmedIds.Count == 0;
            if (shouldNotAck) {
                var singleDtoMsgId = Session.GetNewMessageId(_session);
                return (CreateMsg(dto, isContentRelated: true, msgId: singleDtoMsgId), singleDtoMsgId);
            }

            var ackBts = CreateMsg(new MsgsAck(unconfirmedIds), isContentRelated: false);
            var msgId = Session.GetNewMessageId(_session);
            var dtoBts = CreateMsg(dto, isContentRelated: true, msgId: msgId);

            return BtHelpers.UsingMemBinWriter(bw => {
                bw.Write(MsgContainerTypeNumber);
                bw.Write(2);
                bw.Write(ackBts);
                bw.Write(dtoBts);
            }).Apply(bts => CreateMsg(bts, isContentRelated: false)).Apply(bts => (bts, msgId));
        }


        public async Task<Task<T>> Call<T>(ITgFunc<T> func) {
            async Task CheckReceiveLoop() {
                if (_receiveLoopTask.IsFaulted) {
                    await _receiveLoopTask.ConfigureAwait(false);
                }
            }

            while (true) {
                await CheckReceiveLoop().ConfigureAwait(false);

                var respTask = await _rpcQueue.Put(async () => {
                    var (container, msgId) = WithAck(func);
                    var tcs = new TaskCompletionSource<RpcResult>();
                    _rpcFlow[msgId] = tcs;

                    //                    try //                    {
                    await _transport.Send(container).ConfigureAwait(false);
                    //                    }
                    //                    catch (TgTransportException e) //                    {
                    //                        Updates.OnError(e);
                    //                    }

                    return tcs.Task;
                }).ConfigureAwait(false);

                async Task<T> AwaitResult() {
                    await Task.WhenAny(_receiveLoopTask, respTask).ConfigureAwait(false);
                    await CheckReceiveLoop().ConfigureAwait(false);

                    var resp = await respTask.ConfigureAwait(false);
                    if (resp.IsSuccess) {
                        return resp.Body!.Apply(func.DeserializeResult);
                    }

                    if (!resp.IsFail) {
                        throw new Exception("WTF");
                    }

                    throw resp.Exception!;
                }

                return AwaitResult();
            }
        }
    }
}