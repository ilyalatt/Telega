using System.Threading.Tasks;
using BigMath;
using LanguageExt;
using Telega.Rpc.Dto.Functions;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.ServiceTransport;
using Telega.Utils;

namespace Telega.Auth {
    static class Step1 {
        public static async Task<ResPq> Do(Int128 nonce, Some<MtProtoPlainTransport> transport) {
            var res = await transport.Value.Call(new ReqPq(nonce)).ConfigureAwait(false);
            Helpers.Assert(res.Nonce == nonce, "auth step1: invalid nonce");
            return res;
        }
    }
}