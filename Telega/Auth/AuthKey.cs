using System;
using BigMath;
using LanguageExt;
using Telega.Rpc.Dto;
using Telega.Utils;

namespace Telega.Auth {
    public class AuthKey {
        public Bytes Key { get; }
        public ulong KeyId { get; }
        public ulong AuxHash { get; }

        AuthKey(Bytes key, ulong keyId, ulong auxHash) {
            Key = key;
            KeyId = keyId;
            AuxHash = auxHash;
        }

        public static AuthKey Deserialize(Bytes key) =>
            key.ToArrayUnsafe().Apply(Helpers.Sha1).Apply(BtHelpers.Deserialize(br => {
                var auxHash = br.ReadUInt64();
                br.ReadBytes(4);
                var keyId = br.ReadUInt64();
                return new AuthKey(key, keyId, auxHash);
            }));

        internal static AuthKey FromGab(BigInteger gab) =>
            gab.ToByteArrayUnsigned().ToBytesUnsafe().Apply(Deserialize);

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

        public override string ToString() => $"(KeyId: {KeyId:x16}, AuxHash: {AuxHash:x16}, Key: {Key})";
    }
}