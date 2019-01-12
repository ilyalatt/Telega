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


        // TODO: send in a container
        async Task SendConfirmations()
        {
            var cnt = _unconfirmedMsgIds.Count;
            if (cnt == 0) return;

            const int magic = 3;
            var ids = new List<long>(cnt + magic);
            while (_unconfirmedMsgIds.TryPop(out var id)) ids.Add(id);

            try
            {
                var ack = (MsgsAck) new MsgsAck.Tag(ids.ToArr());
                var msgId = _session.GetNewMessageId();
                await _transport.Send(messageId: msgId, incSeqNum: false, dto: ack);
            }
            finally
            {
                _unconfirmedMsgIds.PushRange(ids.ToArray());
            }
        }

        public async Task<T> Call<T>(ITgFunc<T> request)
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
                    await SendConfirmations();

                    var msgId = _session.GetNewMessageId();
                    var tcs = new TaskCompletionSource<RpcResult>();
                    _rpcFlow[msgId] = tcs;

                    await _transport.Send(messageId: msgId, incSeqNum: true, dto: request);
                    return tcs.Task;
                });

                await Task.WhenAny(_receiveLoopTask, respTask);
                await CheckReceiveLoop();


                var resp = await respTask;
                if (resp.IsSuccess) return resp.Body.Apply(request.DeserializeResult);

                if (!resp.IsFail) throw new Exception("WTF");
                var exc = resp.Exception;

                if (exc is TgBadSalt) continue;
                throw exc;
            }
        }
    }
}
