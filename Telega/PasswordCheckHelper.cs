using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using BigMath;
using LanguageExt;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Functions.Auth;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Account;
using Telega.Utils;
using Algo = Telega.Rpc.Dto.Types.PasswordKdfAlgo.Sha256Sha256Pbkdf2Hmacsha512Iter100000Sha256ModPowTag;

namespace Telega {
    static class PasswordCheckHelper {
        static byte[] Sha256(params byte[][] bts) {
            using var sha = SHA256.Create();
            bts.SkipLast(1).Iter(x => sha.TransformBlock(x, 0, x.Length, null, 0));
            bts.Last().With(x => sha.TransformFinalBlock(x, 0, x.Length));
            return sha.Hash;
        }
        
        static byte[] Sha256(params ArraySegment<byte>[] bts) {
            using var sha = SHA256.Create();
            bts.SkipLast(1).Iter(x => sha.TransformBlock(x.Array, 0, x.Count, null, 0));
            bts.Last().With(x => sha.TransformFinalBlock(x.Array, 0, x.Count));
            return sha.Hash;
        }

        // https://stackoverflow.com/a/18649357
        static byte[] Pbkdf2Sha512(int dkLen, byte[] password, byte[] salt, int iterationCount) {
            using var hmac = new HMACSHA512(password);
            var hashLength = hmac.HashSize / 8;
            if ((hmac.HashSize & 7) != 0) {
                hashLength++;
            }

            var keyLength = dkLen / hashLength;
            if ((long) dkLen > (0xFFFFFFFFL * hashLength) || dkLen < 0) {
                throw new ArgumentOutOfRangeException(nameof(dkLen));
            }

            if (dkLen % hashLength != 0) {
                keyLength++;
            }

            var extendedKey = new byte[salt.Length + 4];
            Buffer.BlockCopy(salt, 0, extendedKey, 0, salt.Length);
            using var ms = new System.IO.MemoryStream();
            for (var i = 0; i < keyLength; i++) {
                extendedKey[salt.Length] = (byte) (((i + 1) >> 24) & 0xFF);
                extendedKey[salt.Length + 1] = (byte) (((i + 1) >> 16) & 0xFF);
                extendedKey[salt.Length + 2] = (byte) (((i + 1) >> 8) & 0xFF);
                extendedKey[salt.Length + 3] = (byte) (((i + 1)) & 0xFF);
                var u = hmac.ComputeHash(extendedKey);
                Array.Clear(extendedKey, salt.Length, 4);
                var f = u;
                for (var j = 1; j < iterationCount; j++) {
                    u = hmac.ComputeHash(u);
                    for (var k = 0; k < f.Length; k++) {
                        f[k] ^= u[k];
                    }
                }

                ms.Write(f, 0, f.Length);
                Array.Clear(u, 0, u.Length);
                Array.Clear(f, 0, f.Length);
            }

            var dk = new byte[dkLen];
            ms.Position = 0;
            ms.Read(dk, 0, dkLen);
            ms.Position = 0;
            for (long i = 0; i < ms.Length; i++) {
                ms.WriteByte(0);
            }

            Array.Clear(extendedKey, 0, extendedKey.Length);
            return dk;
        }

        static T UseSecureStringUtf8Representation<T>(SecureString s, Func<ArraySegment<byte>, T> mapper) {
            var chars = new char[s.Length];
            var charsHandle = GCHandle.Alloc(chars, GCHandleType.Pinned);
            var bytes = new byte[Encoding.UTF8.GetMaxByteCount(s.Length)];
            var bytesHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var bstr = IntPtr.Zero;
            try {
                bstr = Marshal.SecureStringToBSTR(s);
                Marshal.Copy(bstr, chars, 0, chars.Length);
                var bytesCount = Encoding.UTF8.GetBytes(chars, 0, chars.Length, bytes, 0);
                var segment = new ArraySegment<byte>(bytes, 0, bytesCount);
                return mapper(segment);
            }
            finally {
                Array.Clear(chars, 0, chars.Length);
                charsHandle.Free();
                Array.Clear(bytes, 0, bytes.Length);
                bytesHandle.Free();
                Marshal.ZeroFreeBSTR(bstr);
            }
        }

        static byte[] ComputeHash(Algo algo, SecureString passwordStr) {
            var salt1 = algo.Salt1.ToArrayUnsafe();
            var salt1Segment = new ArraySegment<byte>(salt1);
            var salt2 = algo.Salt2.ToArrayUnsafe();

            var hash1 = UseSecureStringUtf8Representation(
                passwordStr,
                pwd => Sha256(salt1Segment, pwd, salt1Segment)
            );
            var hash2 = Sha256(salt2, hash1, salt2);
            var hash3 = Pbkdf2Sha512(64, hash2, salt1, 100000);
            return Sha256(salt2, hash3, salt2);
        }

        static byte[] WithHashPadding(byte[] bts) {
            const int sizeForHash = 256;
            var fill = sizeForHash - bts.Length;
            if (fill == 0) {
                return bts;
            }

            var res = new byte[sizeForHash];
            Buffer.BlockCopy(bts, 0, res, fill, bts.Length);
            return res;
        }

        static byte[] ToBytes(BigInteger number) =>
            WithHashPadding(number.ToByteArrayUnsigned());

        static byte[] Xor(byte[] a, byte[] b) {
            var res = new byte[a.Length];
            for (var i = 0; i < a.Length; i++) {
                res[i] = (byte) (a[i] ^ b[i]);
            }

            return res;
        }

        static bool IsGoodModExpFirst(
            BigInteger modExp,
            BigInteger prime
        ) {
            var diff = prime - modExp;
            /*if (modexp.failed() || prime.failed() || diff.failed()) {
                return false;
            }*/
            const int minDiffBitsCount = 2048 - 64;
            const int maxModExpSize = 256;

            return
                diff >= 0 &&
                diff.BitLength >= minDiffBitsCount &&
                modExp.BitLength >= minDiffBitsCount &&
                modExp.BitLength <= maxModExpSize * 8;
        }

        static BigInteger UnsignedNum(byte[] bts) =>
            new(1, bts);

        static (BigInteger, byte[], BigInteger) GenerateAndCheckRandom(BigInteger g, byte[] bigB, BigInteger p) {
            const int randomSize = 256;
            while (true) {
                var random = Rnd.NextBytes(randomSize);
                var a = UnsignedNum(random);
                var bigA = g.ModPow(a, p);
                if (!IsGoodModExpFirst(bigA, p)) {
                    continue;
                }

                var bigABts = ToBytes(bigA);
                var u = UnsignedNum(Sha256(bigABts, bigB));
                if (u > 0) {
                    return (a, bigABts, u);
                }
            }
        }

        public static CheckPassword GenRequest(Password pwdInfo, Algo algo, SecureString passwordStr) {
            var hash = ComputeHash(algo, passwordStr);

            var pBytes = algo.P.ToArrayUnsafe().Apply(WithHashPadding);
            var p = UnsignedNum(pBytes);
            var g = new BigInteger(algo.G);
            var bigBBytes = pwdInfo.SrpB.Map(bts => bts.ToArrayUnsafe()).IfNone(() => new byte[0]).Apply(WithHashPadding);
            var bigB = UnsignedNum(bigBBytes);
            /*
            if (!MTP::IsPrimeAndGood(algo.p, algo.g)) {
		        LOG(("API Error: Bad p/g in cloud password creation!"));
	            return failed();
	        } else if (!IsGoodLarge(B, p)) {
		        LOG(("API Error: Bad B in cloud password check!"));
		        return failed();
	        }
            */

            var x = UnsignedNum(hash);
            var gBytes = ToBytes(g);
            var gX = g.ModPow(x, p);
            var k = UnsignedNum(Sha256(pBytes, gBytes));
            var kgX = (k * gX).Remainder(p);

            var (a, bigABytes, u) = GenerateAndCheckRandom(g, bigBBytes, p);
            var gB = (bigB - kgX).Remainder(p);
            Helpers.Assert(IsGoodModExpFirst(gB, p), "Bad g_b in cloud password check!");

            var ux = u * x;
            var aUx = a + ux;
            var bigS = gB.ModPow(aUx, p);
            var bigK = Sha256(ToBytes(bigS));
            var bigM1 = Sha256(
                Xor(Sha256(pBytes), Sha256(gBytes)),
                Sha256(algo.Salt1.ToArrayUnsafe()),
                Sha256(algo.Salt2.ToArrayUnsafe()),
                bigABytes,
                bigBBytes,
                bigK
            );

            return new CheckPassword(password: new InputCheckPasswordSrp.DefaultTag(
                srpId: pwdInfo.SrpId.IfNone(0),
                a: bigABytes.ToBytesUnsafe(),
                m1: bigM1.ToBytesUnsafe()
            ));
        }
    }
}