using System;
using BigMath;
using Telega.Utils;

namespace Telega.Session {
    public sealed class TgAuthKey {
        public byte[] Key { get; }
        internal ulong KeyId { get; }
        internal ulong AuxHash { get; }

        public TgAuthKey(byte[] key) {
            var sha = Helpers.Sha1(key);
            AuxHash = BitConverter.ToUInt64(sha, 0);
            KeyId = BitConverter.ToUInt64(sha, 12);
            Key = key;
        }

        internal static TgAuthKey FromGab(BigInteger gab) =>
            new(gab.ToByteArrayUnsigned());

        internal byte[] CalcNewNonceHash(byte[] newNonce, byte number) => BtHelpers
           .UsingMemBinWriter(bw => {
                bw.Write(newNonce);
                bw.Write(number);
                bw.Write(AuxHash);
            })
           .Apply(Helpers.Sha1)
           .Apply(hash => {
                var newNonceHash = new byte[16];
                Array.Copy(hash, 4, newNonceHash, 0, 16);
                return newNonceHash;
            });

        public override string ToString() => $"{BitConverter.ToString(Key)}";
    }
}