using System.Threading.Tasks;
using LanguageExt;
using Telega.Rpc;
using Telega.Rpc.Dto;

namespace Telega.CallMiddleware
{
    sealed class TgCustomizedTransport
    {
        public readonly TgTransport Transport;
        public readonly TgCallMiddlewareChain CallMiddlewareChain;

        public TgCustomizedTransport(Some<TgTransport> transport, Some<TgCallMiddlewareChain> callMiddlewareChain)
        {
            Transport = transport;
            CallMiddlewareChain = callMiddlewareChain;
        }

        public async Task<T> Call<T>(ITgFunc<T> func)
        {
            TgCallHandler<T> handler = f => Transport.Call(f);
            handler = CallMiddlewareChain.Apply(handler);

            // TODO: refactor it
            const int attemptsCount = 10;
            var currentAttempt = 1;
            while (true)
            {
                try
                {
                    var respTask = await handler(func);
                    return await respTask;
                }
                catch (TgBadSaltException) when(currentAttempt < attemptsCount) { }
                catch (TgRpcBadMsgException e) when (currentAttempt < attemptsCount && e.ErrorCode == TgRpcBadMsgCodes.MsgSeqNoLow) { }

                currentAttempt++;
            }
        }
    }
}
