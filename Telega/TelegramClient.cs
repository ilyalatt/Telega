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
        private const string TelegramIP = "149.154.167.50";
        private const string DefaultSessoinName = "session.dat";

        private readonly TgBellhop bellhop;
        private readonly SessionStoreSync storeSync;

        public TelegramClientAuth Auth { get; }
        public TelegramClientContacts Contacts { get; }
        public TelegramClientChannels Channels { get; }
        public TelegramClientMessages Messages { get; }
        public TelegramClientUpload Upload { get; }
        public TelegramClientUpdates Updates { get; }

        static readonly IPEndPoint DefaultEndpoint = new(IPAddress.Parse(TelegramIP), 443);

        TelegramClient(
            TgBellhop bellhop,
            ISessionStore sessionStore
        )
        {
            this.bellhop = bellhop;
            storeSync = SessionStoreSync.Init(bellhop.SessionVar.ToSome(), sessionStore.ToSome());

            Auth = new TelegramClientAuth(bellhop);
            Contacts = new TelegramClientContacts(bellhop);
            Channels = new TelegramClientChannels(bellhop);
            Messages = new TelegramClientMessages(bellhop);
            Upload = new TelegramClientUpload(bellhop);
            Updates = new TelegramClientUpdates(bellhop);
        }

        public void Dispose()
        {
            bellhop.ConnectionPool.Dispose();
            storeSync.Stop();
        }

        static async Task<TelegramClient> Connect(
            ConnectInfo connectInfo,
            ISessionStore store,
            TgCallMiddlewareChain? callMiddlewareChain = null,
            TcpClientConnectionHandler? tcpClientConnectionHandler = null
        )
        {
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
        )
        {
            store ??= new FileSessionStore(DefaultSessoinName);
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
        )
        {
            store ??= new FileSessionStore(DefaultSessoinName);
            var connectInfo = ConnectInfo.FromSession(session);

            return await Connect(connectInfo, store, callMiddlewareChain, tcpClientConnectionHandler);
        }

        public Task<T> Call<T>(ITgFunc<T> func) =>
            bellhop.Call(func);
    }
}
