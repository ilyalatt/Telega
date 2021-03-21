using System.Threading.Tasks;
using BigMath;
using LanguageExt;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Functions;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.ServiceTransport;
using Telega.Utils;
using static Telega.Utils.BtHelpers;

namespace Telega.Auth {
    struct Step2Result {
        public ServerDhParams.OkTag ServerDhParams { get; }
        public Int256 NewNonce { get; }

        public Step2Result(Some<ServerDhParams.OkTag> serverDhParams, Int256 newNonce) {
            ServerDhParams = serverDhParams;
            NewNonce = newNonce;
        }
    }

    static class Step2 {
        public static async Task<Step2Result> Do(Some<ResPq> someResPq, Int256 newNonce, Some<MtProtoPlainTransport> transport) {
            var resPq = someResPq.Value;

            var pqBts = resPq.Pq.ToArrayUnsafe();
            Helpers.Assert(pqBts.Length <= 8, "auth step2: pq is too big");
            var pq = new BigInteger(1, pqBts);
            var (pLong, qLong) = Factorizer.Factorize((ulong) pq.LongValue);
            var p = new BigInteger(pLong);
            var q = new BigInteger(qLong);

            var pqInnerData = new PqInnerData.DefaultTag(
                pq: resPq.Pq,
                p: p.ToByteArrayUnsigned().ToBytesUnsafe(),
                q: q.ToByteArrayUnsigned().ToBytesUnsafe(),
                nonce: resPq.Nonce,
                serverNonce: resPq.ServerNonce,
                newNonce: newNonce
            );
            var pqInnerDataBts = Serialize((PqInnerData) pqInnerData);

            var fingerprint = resPq.ServerPublicKeyFingerprints.Find(x => x == TgServerRsaKey.Fingerprint)
               .IfNone(() => throw Helpers.FailedAssertion(
                    $"auth step2: can not find a key for fingerprints: {string.Join(", ", resPq.ServerPublicKeyFingerprints.Map(x => x.ToString("x16")))}"
                ));
            var cipherText = Rsa.Encrypt(TgServerRsaKey.Key, pqInnerDataBts);

            var resp = await transport.Value.Call(new ReqDhParams(
                nonce: pqInnerData.Nonce,
                serverNonce: pqInnerData.ServerNonce,
                p: pqInnerData.P,
                q: pqInnerData.Q,
                publicKeyFingerprint: fingerprint,
                encryptedData: cipherText.ToBytesUnsafe()
            ));
            var res = resp.Match(
                okTag: x => x,
                failTag: x => throw Helpers.FailedAssertion("auth step2: server_DH_params_fail")
            );

            Helpers.Assert(res.Nonce == pqInnerData.Nonce, "auth step2: invalid nonce");
            Helpers.Assert(res.ServerNonce == pqInnerData.ServerNonce, "auth step2: invalid server nonce");

            return new Step2Result(res, newNonce);
        }
    }
}