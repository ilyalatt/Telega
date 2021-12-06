using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telega.Auth;
using Telega.CallMiddleware;
using Telega.Rpc;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Functions;
using Telega.Rpc.Dto.Functions.Help;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.ServiceTransport;
using Telega.Utils;

namespace Telega.Connect {
    static class TgConnectionEstablisher {
        static async Task<TcpClient> CreateTcpClient(
            IPEndPoint endpoint,
            TcpClientConnectionHandler? connHandler = null
        ) {
            if (connHandler != null) {
                return await connHandler(endpoint).ConfigureAwait(false);
            }

            var res = new TcpClient(endpoint.AddressFamily);
            try {
                await res.ConnectAsync(endpoint.Address, endpoint.Port).ConfigureAwait(false);
            }
            catch (SocketException) {
                // TODO
                throw new TgBrokenConnectionException();
            }
            return res;
        }

        public static async Task<TgConnection> EstablishConnection(
            ILogger logger,
            ConnectInfo connectInfo,
            TgCallMiddlewareChain callMiddlewareChain,
            TcpClientConnectionHandler? connHandler = null
        ) {
            var endpoint = connectInfo.Endpoint;
            Helpers.Assert(endpoint != null, "endpoint == null");
            var tcpClient = await CreateTcpClient(endpoint!, connHandler).ConfigureAwait(false);
            var tcpTransport = new TcpTransport(tcpClient);

            if (connectInfo.NeedsInAuth) {
                var mtPlainTransport = new MtProtoPlainTransport(tcpTransport);
                var result = await Authenticator.DoAuthentication(mtPlainTransport).ConfigureAwait(false);
                connectInfo.SetAuth(result);
            }

            var session = connectInfo.ToSession().AsVar();
            var mtCipherTransport = new MtProtoCipherTransport(tcpTransport, session);
            var transport = new TgCustomizedTransport(new TgTransport(logger, mtCipherTransport, session), callMiddlewareChain);

            // TODO: separate Config
            var config = new GetConfig();
            var request = new InitConnection<GetConfig, Config>(
                apiId: session.Get().ApiId,
                appVersion: "1.0.0",
                deviceModel: "PC",
                langCode: "en",
                query: config,
                systemVersion: "Win 10.0",
                systemLangCode: "en",
                langPack: "tdesktop",
                proxy: null,
                @params: null
            );
            var invokeWithLayer = new InvokeWithLayer<InitConnection<GetConfig, Config>, Config>(
                layer: SchemeInfo.LayerVersion,
                query: request
            );
            var cfg = await transport.Call(invokeWithLayer).ConfigureAwait(false);

            DcInfoKeeper.Update(cfg);
            return new TgConnection(session, transport, cfg);
        }
    }
}