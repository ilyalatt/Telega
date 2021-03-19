using System.Net;
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
using static LanguageExt.Prelude;

namespace Telega.Connect {
    static class TgConnectionEstablisher {
        static async Task<System.Net.Sockets.TcpClient> CreateTcpClient(
            IPEndPoint endpoint,
            TcpClientConnectionHandler? connHandler = null
        ) {
            if (connHandler != null) {
                return await connHandler(endpoint);
            }

            var res = new System.Net.Sockets.TcpClient(endpoint.AddressFamily);
            await res.ConnectAsync(endpoint.Address, endpoint.Port);
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
            var tcpClient = await CreateTcpClient(endpoint!, connHandler);
            var tcpTransport = new TcpTransport(tcpClient);

            if (connectInfo.NeedsInAuth) {
                var mtPlainTransport = new MtProtoPlainTransport(tcpTransport);
                var result = await Authenticator.DoAuthentication(mtPlainTransport);
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
                proxy: None,
                @params: None
            );
            var invokeWithLayer = new InvokeWithLayer<InitConnection<GetConfig, Config>, Config>(
                layer: SchemeInfo.LayerVersion,
                query: request
            );
            var cfg = await transport.Call(invokeWithLayer);

            DcInfoKeeper.Update(cfg);
            return new TgConnection(session, transport, cfg);
        }
    }
}