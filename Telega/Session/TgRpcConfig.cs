using System.Net;

namespace Telega.Session {
    public sealed record TgRpcConfig(
        TgApiCredentials Credentials,
        IPEndPoint Endpoint,
        bool IsMtProtoProxy,
        TgAppInfo AppInfo
    ) {
        static IPEndPoint DefaultEndpoint =>
            new(IPAddress.Parse("149.154.167.50"), 443);

        public static TgRpcConfig Default => new(
            Credentials: TgApiCredentials.Test,
            Endpoint: DefaultEndpoint,
            IsMtProtoProxy: false,
            AppInfo: TgAppInfo.Default
        );
    }
}