using System;
using LanguageExt;
using Telega.CallMiddleware;
using Telega.Rpc.Dto.Types;
using Telega.Utils;

namespace Telega.Connect
{
    sealed class TgConnection : IDisposable
    {
        public readonly Var<Session> Session;
        public readonly TgCustomizedTransport Transport;
        public readonly Config Config;

        public TgConnection(Some<Var<Session>> session, Some<TgCustomizedTransport> transport, Some<Config> config)
        {
            Session = session;
            Transport = transport;
            Config = config;
        }

        public void Dispose() => Transport.Dispose();
    }
}
