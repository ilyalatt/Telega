using System;
using System.Net;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.SomeHelp;
using Telega.CallMiddleware;
using Telega.Connect;
using Telega.Rpc.Dto;

namespace Telega
{
    public sealed class TelegramClient : IDisposable
    {
        private const string DefaultTelegramIp = "149.154.167.50";
        private const int DefaultTelegramPort = 443;
        private const string DefaultSessionName = "session.dat";

        readonly TgBellhop _bellhop;
        readonly SessionStoreSync _storeSync;

        public TelegramClientAuth Auth { get; }
        public TelegramClientContacts Contacts { get; }
        public TelegramClientChannels Channels { get; }
        public TelegramClientMessages Messages { get; }
        public TelegramClientUpload Upload { get; }
        public TelegramClientUpdates Updates { get; }

        static readonly IPEndPoint DefaultEndpoint = new(IPAddress.Parse(DefaultTelegramIp), DefaultTelegramPort);

        TelegramClient(
            TgBellhop bellhop,
            ISessionStore sessionStore
        ) {
            _bellhop = bellhop;
            _storeSync = SessionStoreSync.Init(_bellhop.SessionVar.ToSome(), sessionStore.ToSome());

            Auth = new TelegramClientAuth(_bellhop);
            Contacts = new TelegramClientContacts(_bellhop);
            Channels = new TelegramClientChannels(_bellhop);
            Messages = new TelegramClientMessages(_bellhop);
            Upload = new TelegramClientUpload(_bellhop);
            Updates = new TelegramClientUpdates(_bellhop);
        }

        public void Dispose() {
            _bellhop.ConnectionPool.Dispose();
            _storeSync.Stop();
        }


        static async Task<TelegramClient> Connect(
            ConnectInfo connectInfo,
            ISessionStore store,
            TgCallMiddlewareChain? callMiddlewareChain = null,
            TcpClientConnectionHandler? tcpClientConnectionHandler = null
        ) {
            var bellhop = await TgBellhop.Connect(
                connectInfo,
                callMiddlewareChain,
                tcpClientConnectionHandler
            ).ConfigureAwait(false);
            return new TelegramClient(bellhop, store);
        }

        public static async Task<TelegramClient> Connect(
            int apiId,
            ISessionStore? store = null,
            IPEndPoint? endpoint = null,
            TgCallMiddlewareChain? callMiddlewareChain = null,
            TcpClientConnectionHandler? tcpClientConnectionHandler = null
        ) {
            store ??= new FileSessionStore(DefaultSessionName);
            var ep = endpoint ?? DefaultEndpoint;
            var connectInfo = (await store.Load().ConfigureAwait(false))
                .Map(SomeExt.ToSome).Map(ConnectInfo.FromSession)
                .IfNone(ConnectInfo.FromInfo(apiId, ep));

            return await Connect(connectInfo, store, callMiddlewareChain, tcpClientConnectionHandler);
        }

        public static async Task<TelegramClient> Connect(
            Some<Session> session,
            ISessionStore? store = null,
            TgCallMiddlewareChain? callMiddlewareChain = null,
            TcpClientConnectionHandler? tcpClientConnectionHandler = null
        ) {
            store ??= new FileSessionStore(DefaultSessionName);
            var connectInfo = ConnectInfo.FromSession(session);

            return await Connect(connectInfo, store, callMiddlewareChain, tcpClientConnectionHandler);
        }

        public Task<T> Call<T>(ITgFunc<T> func) =>
            _bellhop.Call(func);
    }
}
