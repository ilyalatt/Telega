using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
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
        readonly MtProtoCipherTransport _transport;
        readonly Session _session;
        readonly ISessionStore _sessionStore;
        readonly TaskQueue _rpcQueue = new TaskQueue();
        readonly ConcurrentStack<long> _unconfirmedMsgIds = new ConcurrentStack<long>(); // such a bad design

        readonly Task _receiveLoopTask;
        readonly ConcurrentDictionary<long, TaskCompletionSource<RpcResult>> _rpcFlow =
            new ConcurrentDictionary<long, TaskCompletionSource<RpcResult>>();
        async Task ReceiveLoop()
        {
            while (true)
            {
                var msgBody = await _transport.Receive();
                var msg = TgSystemMessageHandler.ReadMsg(msgBody);
                _unconfirmedMsgIds.Push(msg.Id);

                Option<TaskCompletionSource<RpcResult>> CaptureFlow(long id) =>
                    _rpcFlow.TryRemove(id, out var flow) ? Some(flow) : None;
                var callResults = msg.Apply(TgSystemMessageHandler.Handle(_session));
                await _sessionStore.Save(_session);
                callResults.Iter(res => CaptureFlow(res.Id).Match(
                    flow => flow.SetResult(res),
                    () => TgTrace.Trace($"TgTransport: Unexpected RPC result, the message id is {res.Id}")
                ));
            }
        }

        public TgTransport(MtProtoCipherTransport transport, Session session, ISessionStore sessionStore)
        {
            _transport = transport;
            _session = session;
            _sessionStore = sessionStore;
            _receiveLoopTask = Task.Run(ReceiveLoop);
        }

        public void Dispose() => _transport.Dispose();


        // it is not supported at least for the layer 82
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


        int GetSeqNum(bool inc) => inc ? _session.Sequence++ * 2 + 1 : _session.Sequence * 2;
        byte[] CreateMsg(byte[] msg, bool isContentRelated, long? msgId = null) => BtHelpers.UsingMemBinWriter(bw =>
        {
            bw.Write(msgId ?? _session.GetNewMessageId());
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
            var shouldAck = unconfirmedIds.Count != 0;
            if (!shouldAck)
            {
                var singleDtoMsgId = _session.GetNewMessageId();
                return (CreateMsg(dto, isContentRelated: true, msgId: singleDtoMsgId), singleDtoMsgId);
            }

            var ack = CreateMsg(new MsgsAck(unconfirmedIds.ToArr()), isContentRelated: false);
            var msgId = _session.GetNewMessageId();
            var dtoBts = CreateMsg(dto, isContentRelated: true, msgId: msgId);

            return BtHelpers.UsingMemBinWriter(bw =>
            {
                bw.Write(MsgContainerTypeNumber);
                bw.Write(2);
                bw.Write(ack);
                bw.Write(dtoBts);
            }).Apply(bts => CreateMsg(bts, isContentRelated: false)).Apply(bts => (bts, msgId));
        }


        public async Task<T> Call<T>(ITgFunc<T> func)
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

                    await _transport.Send(container);
                    return tcs.Task;
                });

                await Task.WhenAny(_receiveLoopTask, respTask);
                await CheckReceiveLoop();


                var resp = await respTask;
                if (resp.IsSuccess) return resp.Body.Apply(func.DeserializeResult);

                if (!resp.IsFail) throw new Exception("WTF");
                var exc = resp.Exception;

                if (exc is TgBadSalt) continue;
                throw exc;
            }
        }
    }
}
