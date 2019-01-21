using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LanguageExt;
using Telega.Auth;
using Telega.Internal;
using Telega.Rpc;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Functions;
using Telega.Rpc.Dto.Functions.Auth;
using Telega.Rpc.Dto.Functions.Help;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Auth;
using Telega.Rpc.ServiceTransport;
using Helpers = Telega.Utils.Helpers;
using static LanguageExt.Prelude;

namespace Telega
{
    public sealed class TelegramClient : IDisposable
    {
        TgTransport _transport;
        internal string _apiHash; // TODO: fix
        internal int _apiId; // TODO: fix
        readonly Session _session;
        readonly ISessionStore _sessionSessionStore;
        Arr<DcOption> _dcOptions;
        readonly TcpClientConnectionHandler _handler;

        public readonly TelegramClientAuth Auth;
        public readonly TelegramClientContacts Contacts;
        public readonly TelegramClientMessages Messages;

        async Task<System.Net.Sockets.TcpClient> CreateTcpClient()
        {
            var ep = _session.Endpoint;
            if (_handler != null) return await _handler(ep);

            var res = new System.Net.Sockets.TcpClient();
            await res.ConnectAsync(ep.Address, ep.Port);
            return res;
        }

        async Task<TcpTransport> CreateTcpTransport() =>
            new TcpTransport(await CreateTcpClient());

        static readonly IPEndPoint DefaultEndpoint = new IPEndPoint(IPAddress.Parse("149.154.167.50"), 443);

        TelegramClient(
            int apiId,
            string apiHash,
            Session session,
            ISessionStore sessionStore,
            TcpClientConnectionHandler handler
        ) {
            _apiHash = apiHash;
            _apiId = apiId;
            _session = session;
            _sessionSessionStore = sessionStore;
            _handler = handler;

            Auth = new TelegramClientAuth(this);
            Contacts = new TelegramClientContacts(this);
            Messages = new TelegramClientMessages(this);
        }

        public void Dispose() => _transport.Dispose();


        static async Task<T> Wrap<T>(Func<Task<T>> wrapper)
        {
            try
            {
                return await Task.Run(wrapper).ConfigureAwait(false);
            }
            // TODO: a separate class handling this stuff
            catch (Exception exc) when (!(exc is TgException) && !(exc is OutOfMemoryException))
            {
                throw new TgInternalException("Unhandled exception. See an inner exception.", exc);
            }
        }

        async Task<Unit> Connect()
        {
            _transport?.Dispose();
            var tcpTransport = await CreateTcpTransport();

            if (_session.AuthKey == null)
            {
                var mtPlainTransport = new MtProtoPlainTransport(tcpTransport);
                var result = await Authenticator.DoAuthentication(mtPlainTransport);
                _session.AuthKey = result.AuthKey;
                _session.TimeOffset = result.TimeOffset;
            }

            var mtCipherTransport = new MtProtoCipherTransport(tcpTransport, _session, _sessionSessionStore);
            _transport = new TgTransport(mtCipherTransport, _session, _sessionSessionStore);

            //set-up layer
            var config = new GetConfig();
            var request = new InitConnection<GetConfig, Config>(
                apiId: _apiId,
                appVersion: "1.0.0",
                deviceModel: "PC",
                langCode: "en",
                query: config,
                systemVersion: "Win 10.0",
                systemLangCode: "en",
                langPack: "tdesktop",
                proxy: None
            );
            var invokeWithLayer = new InvokeWithLayer<InitConnection<GetConfig, Config>, Config>(layer: SchemeInfo.LayerVersion, query: request);
            var cfg = await _transport.Call(invokeWithLayer);

            _dcOptions = cfg.DcOptions;
            return unit;
        }

        public static async Task<TelegramClient> Connect(
            int apiId,
            Some<string> apiHash,
            ISessionStore store = null,
            TcpClientConnectionHandler handler = null,
            IPEndPoint endpoint = null
        ) {
            if (apiId == default) throw new ArgumentNullException(nameof(apiId));

            store = store ?? new FileSessionStore("session.dat");
            var session = (await store.Load()).IfNone(Session.New);
            session.Endpoint = endpoint ?? DefaultEndpoint;

            var client = new TelegramClient(
                apiId,
                apiHash,
                session,
                store,
                handler
            );
            await Wrap(client.Connect);
            return client;
        }


        // TODO: fix
        internal async Task SetAuthorized(User user)
        {
            TgTrace.Trace("Authorized: " + user);
            _session.IsAuthorized = true;
            await _sessionSessionStore.Save(_session);
        }

        public bool IsAuthorized =>
            _session.IsAuthorized;

        async Task ReconnectToDc(int dcId)
        {
            Helpers.Assert(_dcOptions != null && _dcOptions.Count > 0, "bad dc options");

            var exported = !IsAuthorized ? null : await _transport.Call(new ExportAuthorization(dcId: dcId));

            var dc = _dcOptions.First(d => d.Id == dcId);
            _session.Endpoint = new IPEndPoint(IPAddress.Parse(dc.IpAddress), dc.Port);
            _session.AuthKey = null;
            await Connect();

            if (exported != null)
            {
                var resp = await _transport.Call(new ImportAuthorization(id: exported.Id, bytes: exported.Bytes));
                await SetAuthorized(resp.User);
            }
        }

        async Task<T> CallWithDcMigration<T>(ITgFunc<T> func)
        {
            while (true)
            {
                try
                {
                    return await _transport.Call(func);
                }
                catch (TgDataCenterMigrationException e)
                {
                    await ReconnectToDc(e.Dc);
                }
            }
        }

        // TODO: a separate class for all this stuff
        public Task<T> Call<T>(ITgFunc<T> func) =>
            Wrap(() => CallWithDcMigration(func ?? throw new ArgumentNullException(nameof(func))));
    }
}
