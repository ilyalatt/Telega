using System.Threading.Tasks;
using Telega.Rpc.ServiceTransport;
using Telega.Utils;

namespace Telega.Auth {
    static class Authenticator {
        public static async Task<Step3Res> DoAuthentication(MtProtoPlainTransport transport) {
            var step1Res = await Step1.Do(BtHelpers.GenNonce16(), transport);
            var step2Res = await Step2.Do(step1Res, BtHelpers.GenNonce32(), transport);
            var step3Res = await Step3.Do(step2Res.ServerDhParams, step2Res.NewNonce, transport);
            return step3Res;
        }
    }
}