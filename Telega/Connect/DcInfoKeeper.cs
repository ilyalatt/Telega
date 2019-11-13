using System.Collections.Generic;
using System.Linq;
using System.Net;
using LanguageExt;
using Telega.Rpc.Dto.Types;
using Telega.Utils;
using static LanguageExt.Prelude;

namespace Telega.Connect
{
    static class DcInfoKeeper
    {
        static volatile Dictionary<int, DcOption>? _dcInfo;

        public static void Update(Some<Config> cfg)
        {
            _dcInfo = cfg.Value.DcOptions
                .Filter(x => !x.Ipv6)
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.Head());
        }

        public static IPEndPoint FindEndpoint(int dcId)
        {
            Helpers.Assert(_dcInfo! != null, "DcInfo == null");

            if (!_dcInfo!.TryGetValue(dcId, out var dcOpt)) throw new TgInternalException($"Can not find DC {dcId}", None);
            return new IPEndPoint(IPAddress.Parse(dcOpt.IpAddress), dcOpt.Port);
        }
    }
}
