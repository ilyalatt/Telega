using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Telega.CallMiddleware;
using Telega.Session;
using Telega.Session.Store;

namespace Telega.Client {
    public sealed record TelegramClientConfig(
        ILogger Logger,
        ISessionStore SessionStore,
        TgCallMiddlewareChain Middleware,
        TgRpcConfig DefaultRpcConfig
    ) {
        static FileSessionStore DefaultSessionStore =>
            new("session.dat");

        public static TelegramClientConfig Default => new(
            Logger: NullLogger.Instance,
            SessionStore: DefaultSessionStore,
            Middleware: TgCallMiddlewareChain.Default,
            DefaultRpcConfig: TgRpcConfig.Default
        );
    }
}