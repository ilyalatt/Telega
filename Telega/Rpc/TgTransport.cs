using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageExt;
using Telega.Connect;
using Telega.Internal;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.ServiceTransport;
using Telega.Utils;
using static LanguageExt.Prelude;

namespace Telega.Rpc
{
    class TgTransport : IDisposable
    {
        readonly Var<Session> _session;
        readonly MtProtoCipherTransport _transport;
        readonly TaskQueue _rpcQueue = new TaskQueue();
        readonly ConcurrentStack<long> _unconfirmedMsgIds = new ConcurrentStack<long>(); // such a bad design

        readonly Task _receiveLoopTask;
        readonly ConcurrentDictionary<long, TaskCompletionSource<RpcResult>> _rpcFlow =
            new ConcurrentDictionary<long, TaskCompletionSource<RpcResult>>();

        public readonly CustomObservable<UpdatesType> Updates = new CustomObservable<UpdatesType>();

        async Task ReceiveLoopImpl()
        {
            while (true)
            {
                var msgBody = await _transport.Receive();
                var msg = TgSystemMessageHandler.ReadMsg(msgBody);
                var ctx = new TgSystemMessageHandlerContext();
                msg.Apply(TgSystemMessageHandler.Handle(ctx));

                ctx.NewSalt.Iter(salt =>
                    _session.SetWith(x => x.With(salt: salt))
                );
                ctx.Ack.Iter(_unconfirmedMsgIds.Push);

                Option<TaskCompletionSource<RpcResult>> CaptureFlow(long id) =>
                    _rpcFlow.TryRemove(id, out var flow) ? Some(flow) : None;
                ctx.RpcResults.Iter(res => CaptureFlow(res.Id).Match(
                    flow => flow.SetResult(res),
                    () => TgTrace.Trace($"TgTransport: Unexpected RPC result, the message id is {res.Id}")
                ));

                ctx.Updates.Iter(Updates.OnNext);
            }
        }

        async Task ReceiveLoop()
        {
//            try
//            {
            await ReceiveLoopImpl();
//            }
//            catch (TgTransportException e)
//            {
//                // Updates.OnError(e);
//            }
        }

        public TgTransport(MtProtoCipherTransport transport, Var<Session> session)
        {
            _transport = transport;
            _session = session;
            _receiveLoopTask = Task.Run(ReceiveLoop);
        }

        public void Dispose() => _transport.Dispose();


        // it is not supported
        /*
        const uint GZipPackedTypeNumber = 0x3072cfa1;
        static byte[] GZip(byte[] data)
        {
            var ms = new MemoryStream();
            var gzip = new GZipStream(ms, CompressionLevel.Fastest);
            gzip.Write(data, 0, data.Length);
            return ms.ToArray();
        }
        static byte[] TryPack(byte[] data)
        {
            return data;
            var gzip = BtHelpers.UsingMemBinWriter(bw =>
            {
                TgMarshal.WriteUint(bw, GZipPackedTypeNumber);
                TgMarshal.WriteBytes(bw, GZip(data).ToBytesUnsafe());
            });

            return gzip.Length < data.Length ? gzip : data;
        }
        */


        Arr<long> PopUnconfirmedMsgIds()
        {
            const int magic = 3;
            var ids = new List<long>(_unconfirmedMsgIds.Count + magic);
            while (_unconfirmedMsgIds.TryPop(out var id)) ids.Add(id);
            return ids.ToArr();
        }


        int GetSeqNum(bool inc)
        {
            var seqNum = _session.Get().Sequence * 2 + (inc ? 1 : 0);
            if (inc) _session.SetWith(x => x.With(sequence: x.Sequence + 1));
            return seqNum;
        }

        byte[] CreateMsg(byte[] msg, bool isContentRelated, long? msgId = null) => BtHelpers.UsingMemBinWriter(bw =>
        {
            bw.Write(msgId ?? Session.GetNewMessageId(_session));
            bw.Write(GetSeqNum(isContentRelated));
            bw.Write(msg.Length);
            bw.Write(msg);
        });
        byte[] CreateMsg(ITgSerializable dto, bool isContentRelated, long? msgId = null) =>
            CreateMsg(BtHelpers.UsingMemBinWriter(dto.Serialize), isContentRelated, msgId);


        const uint MsgContainerTypeNumber = 0x73f1f8dc;
        (byte[], long) WithAck(ITgSerializable dto)
        {
            var unconfirmedIds = PopUnconfirmedMsgIds();
            var shouldNotAck = unconfirmedIds.Count == 0;
            if (shouldNotAck)
            {
                var singleDtoMsgId = Session.GetNewMessageId(_session);
                return (CreateMsg(dto, isContentRelated: true, msgId: singleDtoMsgId), singleDtoMsgId);
            }

            var ackBts = CreateMsg(new MsgsAck(unconfirmedIds.ToArr()), isContentRelated: false);
            var msgId = Session.GetNewMessageId(_session);
            var dtoBts = CreateMsg(dto, isContentRelated: true, msgId: msgId);

            return BtHelpers.UsingMemBinWriter(bw =>
            {
                bw.Write(MsgContainerTypeNumber);
                bw.Write(2);
                bw.Write(ackBts);
                bw.Write(dtoBts);
            }).Apply(bts => CreateMsg(bts, isContentRelated: false)).Apply(bts => (bts, msgId));
        }


        public async Task<Task<T>> Call<T>(ITgFunc<T> func)
        {
            async Task CheckReceiveLoop()
            {
                if (_receiveLoopTask.IsFaulted) await _receiveLoopTask;
            }

            while (true)
            {
                await CheckReceiveLoop();

                var respTask = await _rpcQueue.Put(async () =>
                {
                    var (container, msgId) = WithAck(func);
                    var tcs = new TaskCompletionSource<RpcResult>();
                    _rpcFlow[msgId] = tcs;

//                    try
//                    {
                    await _transport.Send(container);
//                    }
//                    catch (TgTransportException e)
//                    {
//                        Updates.OnError(e);
//                    }

                    return tcs.Task;
                });

                async Task<T> AwaitResult()
                {
                    await Task.WhenAny(_receiveLoopTask, respTask);
                    await CheckReceiveLoop();

                    var resp = await respTask;
                    if (resp.IsSuccess) return resp.Body!.Apply(func.DeserializeResult);

                    if (!resp.IsFail) throw new Exception("WTF");
                    throw resp.Exception!;
                }

                return AwaitResult();
            }
        }
    }
}
