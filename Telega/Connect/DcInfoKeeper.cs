using System.Collections.Generic;
using System.Linq;
using System.Net;
using Telega.Rpc.Dto.Types;

namespace Telega.Connect {
    sealed class DcInfoKeeper {
        volatile Dictionary<int, DcOption> _dcInfo = new();

        public void Update(Config cfg) =>
            _dcInfo = cfg.DcOptions
                .Where(x => !x.Ipv6)
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.First());

        public IPEndPoint FindEndpoint(int dcId) =>
            _dcInfo.TryGetValue(dcId, out var dcOpt)
                ? new IPEndPoint(IPAddress.Parse(dcOpt.IpAddress), dcOpt.Port)
                : throw new TgInternalException($"Can not find DC {dcId}", null);
    }
}