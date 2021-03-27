using System;
using Telega.CallMiddleware;
using Telega.Rpc.Dto.Types;
using Telega.Utils;

namespace Telega.Connect {
    sealed class TgConnection : IDisposable {
        public Var<Session> Session { get; }
        public TgCustomizedTransport Transport { get; }
        public Config Config { get; }

        public TgConnection(Var<Session> session, TgCustomizedTransport transport, Config config) {
            Session = session;
            Transport = transport;
            Config = config;
        }

        public void Dispose() => Transport.Dispose();
    }
}