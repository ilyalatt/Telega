using System;
using Telega.CallMiddleware;
using Telega.Rpc.Dto.Types;
using Telega.Session;
using Telega.Utils;

namespace Telega.Connect {
    sealed record TgConnection : IDisposable {
        public Var<TgSession> Session { get; }
        public TgCustomizedTransport Transport { get; }
        public Config Config { get; }

        public TgConnection(Var<TgSession> session, TgCustomizedTransport transport, Config config) {
            Session = session;
            Transport = transport;
            Config = config;
        }

        public void Dispose() => Transport.Dispose();
    }
}