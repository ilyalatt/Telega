using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BigMath;
using BigMath.Utils;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Functions;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.ServiceTransport;
using Telega.Utils;
using static Telega.Utils.BtHelpers;

namespace Telega.Auth {
    record Step3Res(
        AuthKey AuthKey,
        int TimeOffset
    );

    static class Step3 {
        static Func<BinaryReader, T> WithHashSumCheck<T>(Func<BinaryReader, T> func) => br => {
            var hash = br.ReadBytes(20);
            var bs = br.BaseStream;

            var firstPos = bs.Position;
            var res = func(br);
            var secondPos = bs.Position;

            bs.Position = firstPos;
            var body = br.ReadBytes((int) (secondPos - firstPos));
            var computedHash = Helpers.Sha1(body);

            Helpers.Assert(hash.SequenceEqual(computedHash), "auth step3: invalid hash of encrypted answer");

            return res;
        };

        static byte[] WithHashAndPadding(byte[] bts) => UsingMemBinWriter(bw => {
            bw.Write(Helpers.Sha1(bts));
            bw.Write(bts);
            var padding = (16 - (int) bw.BaseStream.Position % 16).Apply(x => x == 16 ? 0 : x);
            bw.Write(Rnd.NextBytes(padding));
        });

        public static async Task<Step3Res> Do(
            ServerDhParams.Ok_Tag dhParams,
            Int256 newNonce,
            MtProtoPlainTransport transport
        ) {
            var key = Aes.GenerateKeyDataFromNonces(dhParams.ServerNonce.ToBytes(true), newNonce.ToBytes(true));
            var plaintextAnswer = Aes.DecryptAES(key, dhParams.EncryptedAnswer.ToArrayUnsafe());
            var dh = plaintextAnswer.Apply(Deserialize(WithHashSumCheck(ServerDhInnerData.Deserialize)));

            Helpers.Assert(dh.Nonce == dhParams.Nonce, "auth step3: invalid nonce in encrypted answer");
            Helpers.Assert(dh.ServerNonce == dhParams.ServerNonce, "auth step3: invalid server nonce in encrypted answer");

            var currentEpochTime = Helpers.GetCurrentEpochTime();
            var timeOffset = dh.ServerTime - currentEpochTime;

            var g = dh.G;
            var dhPrime = new BigInteger(1, dh.DhPrime.ToArrayUnsafe());
            var ga = new BigInteger(1, dh.Ga.ToArrayUnsafe());

            var b = new BigInteger(Rnd.NextBytes(2048));
            var gb = BigInteger.ValueOf(g).ModPow(b, dhPrime);
            var gab = ga.ModPow(b, dhPrime);

            var dhInnerData = new ClientDhInnerData(
                nonce: dh.Nonce,
                serverNonce: dh.ServerNonce,
                retryId: 0,
                gb: gb.ToByteArrayUnsigned().ToBytesUnsafe()
            );
            var dhInnerDataBts = Serialize(dhInnerData);

            var dhInnerDataHashedBts = WithHashAndPadding(dhInnerDataBts);
            var dhInnerDataHashedEncryptedBytes = Aes.EncryptAES(key, dhInnerDataHashedBts);

            var resp = await transport.Call(new SetClientDhParams(
                nonce: dh.Nonce,
                serverNonce: dh.ServerNonce,
                encryptedData: dhInnerDataHashedEncryptedBytes.ToBytesUnsafe()
            )).ConfigureAwait(false);
            var res = resp.Match(
                dhGenOk_Tag: x => x,
                dhGenFail_Tag: _ => throw Helpers.FailedAssertion("auth step3: dh_gen_fail"),
                dhGenRetry_Tag: _ => throw Helpers.FailedAssertion("auth step3: dh_gen_retry")
            );

            var authKey = AuthKey.FromGab(gab);
            var newNonceHash = authKey.CalcNewNonceHash(newNonce.ToBytes(true), 1).ToInt128();
            Helpers.Assert(res.Nonce == dh.Nonce, "auth step3: invalid nonce");
            Helpers.Assert(res.ServerNonce == dh.ServerNonce, "auth step3: invalid server nonce");
            Helpers.Assert(res.NewNonceHash1 == newNonceHash, "auth step3: invalid new nonce hash");

            return new Step3Res(authKey, timeOffset);
        }
    }
}