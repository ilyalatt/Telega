using Microsoft.Extensions.Logging;
using Telega.CallMiddleware;
using Telega.Session;
using Telega.Utils;

namespace Telega.Connect {
    sealed record TgConnectionContext(
        ILogger Logger,
        Var<TgRpcState?> RpcState,
        TgRpcConfig ConnectConfig,
        TgCallMiddlewareChain Middleware
    );
}