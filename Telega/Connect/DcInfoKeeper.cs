using System.Collections.Generic;
using System.Linq;
using System.Net;
using Telega.Rpc.Dto.Types;
using Telega.Utils;

namespace Telega.Connect {
    static class DcInfoKeeper {
        static volatile Dictionary<int, DcOption>? _dcInfo;

        public static void Update(Config cfg) {
            _dcInfo = cfg.DcOptions
               .Where(x => !x.Ipv6)
               .GroupBy(x => x.Id)
               .ToDictionary(x => x.Key, x => x.First());
        }

        public static IPEndPoint FindEndpoint(int dcId) {
            Helpers.Assert(_dcInfo! != null, "DcInfo == null");

            if (!_dcInfo!.TryGetValue(dcId, out var dcOpt)) {
                throw new TgInternalException($"Can not find DC {dcId}", null);
            }

            return new IPEndPoint(IPAddress.Parse(dcOpt.IpAddress), dcOpt.Port);
        }
    }
}