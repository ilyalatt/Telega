using System.Net;
using System.Threading.Tasks;
using Telega.Auth;
using Telega.CallMiddleware;
using Telega.Rpc;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Functions;
using Telega.Rpc.Dto.Functions.Help;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.ServiceTransport;
using Telega.Session;
using Telega.Utils;

namespace Telega.Connect {
    static class TgConnectionInitializer {
        static async Task<System.Net.Sockets.TcpClient> CreateTcpClient(
            IPEndPoint endpoint
        ) {
            var res = new System.Net.Sockets.TcpClient(endpoint.AddressFamily);
            await res.ConnectAsync(endpoint.Address, endpoint.Port).ConfigureAwait(false);
            return res;
        }
        
        static async Task<Step3Res> Authenticate(TcpTransport tcpTransport) {
            var mtPlainTransport = new MtProtoPlainTransport(tcpTransport);
            return await Authenticator.DoAuthentication(mtPlainTransport).ConfigureAwait(false);
        }

        static async Task AuthenticateIfNeeded(
            TgConnectionContext tgConnectionContext,
            TcpTransport tcpTransport
        ) {
            var stateVar = tgConnectionContext.RpcState;
            var state = stateVar.Get();
            if (state != null) {
                return;
            }

            var (authKey, timeOffset) = await Authenticate(tcpTransport).ConfigureAwait(false);
            stateVar.Update(_ => new TgRpcState(
                Salt: 0,
                Id: 0,
                Sequence: 0,
                LastMessageId: 0,
                AuthKey: authKey,
                TimeOffset: timeOffset
            ));
        }

        static async Task<Config> CallInitConnection(
            TgRpcConfig config,
            TgTransport transport
        ) {
            var app = config.AppInfo;
            var ep = config.Endpoint;
            var proxy = config.IsMtProtoProxy ? new InputClientProxy(ep.Address.ToString(), ep.Port) : null;
            var request = new InitConnection<GetConfig, Config>(
                apiId: config.Credentials.Id,
                appVersion: app.AppVersion,
                deviceModel: app.DeviceModel,
                langCode: app.LangCode,
                query: new GetConfig(),
                systemVersion: app.SystemVersion,
                systemLangCode: app.SystemLangCode,
                langPack: app.LangPack,
                proxy: proxy,
                @params: null
            );
            var invokeWithLayer = new InvokeWithLayer<InitConnection<GetConfig, Config>, Config>(
                layer: SchemeInfo.LayerVersion,
                query: request
            );
            var call = await transport.Call(invokeWithLayer).ConfigureAwait(false);
            return await call.ConfigureAwait(false);
        }

        public static async Task<TgConnection> InitConnection(
            TgConnectionContext tgConnectionContext
        ) {
            var connectConfig = tgConnectionContext.ConnectConfig;
            var rpcStateVar = (Var<TgRpcState>) tgConnectionContext.RpcState;
            var tcpClient = await CreateTcpClient(connectConfig.Endpoint).ConfigureAwait(false);
            var tcpTransport = new TcpTransport(tcpClient);
            await AuthenticateIfNeeded(
                tgConnectionContext,
                tcpTransport
            ).ConfigureAwait(false);
            var mtCipherTransport = new MtProtoCipherTransport(tcpTransport, rpcStateVar);
            var tgTransport = new TgTransport(
                tgConnectionContext.Logger,
                mtCipherTransport,
                rpcStateVar
            );
            var cfg = await CallInitConnection(connectConfig, tgTransport).ConfigureAwait(false);
            var transport = new TgCustomizedTransport(tgTransport, tgConnectionContext.Middleware);
            return new TgConnection(rpcStateVar, transport, cfg);
        }
    }
}