using System.Threading.Tasks;
using BigMath;
using LanguageExt;
using Telega.Rpc.Dto.Functions;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.ServiceTransport;
using Telega.Utils;
using static LanguageExt.Prelude;

namespace Telega.Auth
{
    static class Step1
    {
        public static async Task<ResPq.Tag> Do(Int128 nonce, Some<MtProtoPlainTransport> transport)
        {
            var resp = await transport.Value.Call(new ReqPq(nonce));
            var res = resp.Match(identity);

            Helpers.Assert(res.Nonce == nonce, "auth step1: invalid nonce");
            return res;
        }
    }
}
