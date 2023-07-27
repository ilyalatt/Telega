using System.Linq;
using System.Threading.Tasks;
using BigMath;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Functions;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.ServiceTransport;
using Telega.Utils;
using static Telega.Utils.BtHelpers;

namespace Telega.Auth {
    record Step2Result(
        ServerDhParams.Ok_Tag ServerDhParams,
        Int256 NewNonce
    );

    static class Step2 {
        public static async Task<Step2Result> Do(ResPq resPq, Int256 newNonce, MtProtoPlainTransport transport) {
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

            var fingerprint = resPq.ServerPublicKeyFingerprints.TryFind(x => x == TgServerRsaKey.Fingerprint)
                ?? throw Helpers.FailedAssertion(
                    $"auth step2: can not find a key for fingerprints: {string.Join(", ", resPq.ServerPublicKeyFingerprints.Select(x => x.ToString("x16")))}"
                );
            var cipherText = Rsa.Encrypt(TgServerRsaKey.Key, pqInnerDataBts);

            var resp = await transport.Call(new ReqDhParams(
                nonce: pqInnerData.Nonce,
                serverNonce: pqInnerData.ServerNonce,
                p: pqInnerData.P,
                q: pqInnerData.Q,
                publicKeyFingerprint: fingerprint,
                encryptedData: cipherText.ToBytesUnsafe()
            )).ConfigureAwait(false);
            var res = resp.Match(
                ok_Tag: x => x,
                fail_Tag: _ => throw Helpers.FailedAssertion("auth step2: server_DH_params_fail")
            );

            Helpers.Assert(res.Nonce == pqInnerData.Nonce, "auth step2: invalid nonce");
            Helpers.Assert(res.ServerNonce == pqInnerData.ServerNonce, "auth step2: invalid server nonce");

            return new Step2Result(res, newNonce);
        }
    }
}