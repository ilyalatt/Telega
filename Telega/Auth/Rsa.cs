using System;
using BigMath;
using Telega.Utils;

namespace Telega.Auth
{
    static class Rsa
    {
        public static byte[] Encrypt(
            (BigInteger, BigInteger) key,
            byte[] data
        ) {
            var (m, e) = key;

            var msg = BtHelpers.UsingMemBinWriter(bw =>
            {
                bw.Write(Helpers.Sha1(data));
                bw.Write(data);

                if (data.Length >= 235) return;

                var padding1 = new byte[235 - data.Length];
                new Random().NextBytes(padding1);
                bw.Write(padding1);
            });

            var cipherText = new BigInteger(1, msg).ModPow(e, m).ToByteArrayUnsigned();
            if (cipherText.Length == 256) return cipherText;

            var paddedCipherText = new byte[256];
            var padding = 256 - cipherText.Length;
            cipherText.CopyTo(paddedCipherText, padding);

            return paddedCipherText;
        }
    }
}
