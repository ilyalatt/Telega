using System;
using LanguageExt;
using Telega.CallMiddleware;
using Telega.Rpc.Dto.Types;
using Telega.Utils;

namespace Telega.Connect {
    sealed class TgConnection : IDisposable {
        public Var<Session> Session { get; }
        public TgCustomizedTransport Transport { get; }
        public Config Config { get; }

        public TgConnection(Some<Var<Session>> session, Some<TgCustomizedTransport> transport, Some<Config> config) {
            Session = session;
            Transport = transport;
            Config = config;
        }

        public void Dispose() => Transport.Dispose();
    }
}