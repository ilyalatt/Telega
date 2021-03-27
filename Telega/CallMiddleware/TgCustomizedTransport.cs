using System;
using System.Threading.Tasks;
using Telega.Rpc;
using Telega.Rpc.Dto;

namespace Telega.CallMiddleware {
    sealed class TgCustomizedTransport : IDisposable {
        public TgTransport Transport { get; }
        public TgCallMiddlewareChain CallMiddlewareChain { get; }

        public TgCustomizedTransport(TgTransport transport, TgCallMiddlewareChain callMiddlewareChain) {
            Transport = transport;
            CallMiddlewareChain = callMiddlewareChain;
        }

        public void Dispose() => Transport.Dispose();

        public async Task<T> Call<T>(ITgFunc<T> func) {
            TgCallHandler<T> handler = f => Transport.Call(f);
            handler = CallMiddlewareChain.Apply(handler);

            // TODO: refactor it
            const int attemptsCount = 10;
            var currentAttempt = 1;
            while (true) {
                try {
                    var respTask = await handler(func).ConfigureAwait(false);
                    return await respTask.ConfigureAwait(false);
                }
                catch (TgBadSaltException) when (currentAttempt < attemptsCount) { }
                catch (TgRpcBadMsgException e) when (currentAttempt < attemptsCount && e.ErrorCode == TgRpcBadMsgCodes.MsgSeqNoLow) { }

                currentAttempt++;
            }
        }
    }
}