using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BigMath;
using LanguageExt;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Functions.Account;
using Telega.Rpc.Dto.Functions.Auth;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Account;
using Telega.Utils;
using static LanguageExt.Prelude;

namespace Telega
{
    public sealed class TelegramClientAuth
    {
        readonly TelegramClient _tg;
        internal TelegramClientAuth(Some<TelegramClient> tg) => _tg = tg;


        public async Task<string> SendCode(Some<string> phoneNumber)
        {
            var resp = await _tg.Call(new SendCode(
                phoneNumber: phoneNumber,
                apiId: _tg._apiId,
                apiHash: _tg._apiHash,
                allowFlashcall: false,
                currentNumber: None
            ));
            return resp.PhoneCodeHash;
        }

        public async Task<User> SignIn(Some<string> phoneNumber, Some<string> phoneCodeHash, Some<string> code)
        {
            var resp = await _tg.Call(new SignIn(phoneNumber: phoneNumber, phoneCodeHash: phoneCodeHash, phoneCode: code));
            var user = resp.User;
            await _tg.SetAuthorized(user);
            return user;
        }

        public async Task<Password> GetPasswordInfo() =>
            await _tg.Call(new GetPassword());


        static byte[] Sha256(params byte[][] bts)
        {
            var sha = SHA256.Create();
            bts.SkipLast(1).Iter(x => sha.TransformBlock(x, 0, x.Length, null, 0));
            bts.Last().With(x => sha.TransformFinalBlock(x, 0, x.Length));
            return sha.Hash;
        }

        // https://stackoverflow.com/a/18649357
        static byte[] Pbkdf2Sha512(int dkLen, byte[] password, byte[] salt, int iterationCount)
        {
            using (var hmac = new HMACSHA512(password))
            {
                var hashLength = hmac.HashSize / 8;
                if ((hmac.HashSize & 7) != 0)
                    hashLength++;
                var keyLength = dkLen / hashLength;
                if ((long) dkLen > (0xFFFFFFFFL * hashLength) || dkLen < 0)
                    throw new ArgumentOutOfRangeException(nameof(dkLen));
                if (dkLen % hashLength != 0)
                    keyLength++;
                var extendedKey = new byte[salt.Length + 4];
                Buffer.BlockCopy(salt, 0, extendedKey, 0, salt.Length);
                using (var ms = new System.IO.MemoryStream())
                {
                    for (var i = 0; i < keyLength; i++)
                    {
                        extendedKey[salt.Length] = (byte) (((i + 1) >> 24) & 0xFF);
                        extendedKey[salt.Length + 1] = (byte) (((i + 1) >> 16) & 0xFF);
                        extendedKey[salt.Length + 2] = (byte) (((i + 1) >> 8) & 0xFF);
                        extendedKey[salt.Length + 3] = (byte) (((i + 1)) & 0xFF);
                        var u = hmac.ComputeHash(extendedKey);
                        System.Array.Clear(extendedKey, salt.Length, 4);
                        var f = u;
                        for (var j = 1; j < iterationCount; j++)
                        {
                            u = hmac.ComputeHash(u);
                            for (var k = 0; k < f.Length; k++)
                            {
                                f[k] ^= u[k];
                            }
                        }

                        ms.Write(f, 0, f.Length);
                        System.Array.Clear(u, 0, u.Length);
                        System.Array.Clear(f, 0, f.Length);
                    }

                    var dk = new byte[dkLen];
                    ms.Position = 0;
                    ms.Read(dk, 0, dkLen);
                    ms.Position = 0;
                    for (long i = 0; i < ms.Length; i++)
                    {
                        ms.WriteByte(0);
                    }

                    System.Array.Clear(extendedKey, 0, extendedKey.Length);
                    return dk;
                }
            }
        }

        static byte[] ComputeHash(PasswordKdfAlgo.Sha256Sha256Pbkdf2Hmacsha512Iter100000Sha256ModPowTag algo, string passwordStr)
        {
            var salt1 = algo.Salt1.ToArrayUnsafe();
            var salt2 = algo.Salt2.ToArrayUnsafe();
            var passwordBytes = Encoding.UTF8.GetBytes(passwordStr);

            var hash1 = Sha256(salt1, passwordBytes, salt1);
            var hash2 = Sha256(salt2, hash1, salt2);
            var hash3 = Pbkdf2Sha512(64, hash2, salt1, 100000);
            return Sha256(salt2, hash3, salt2);
        }

        static byte[] NumBytesForHash(BigInteger number)
        {
            var bts = number.ToByteArrayUnsigned();
            const int kSizeForHash = 256;
            var fill = kSizeForHash - bts.Length;
            if (fill == 0) return bts;

            var res = new byte[kSizeForHash];
            Buffer.BlockCopy(bts, 0, res, fill, bts.Length);
            return res;
        }

        static byte[] Xor(byte[] a, byte[] b)
        {
            var res = new byte[a.Length];
            for (var i = 0; i < a.Length; i++)
            {
                res[i] = (byte) (a[i] ^ b[i]);
            }
            return res;
        }

        static uint[] GetBitsProp(BigInteger num)
        {
            var bitsProp = typeof(BigInteger).GetField("_bits", BindingFlags.Instance | BindingFlags.NonPublic);
            return (uint[]) bitsProp.GetValue(num);
        }

        static int GetBytesCount(BigInteger num)
        {
            return GetBitsProp(num).Length * 4;
        }

        bool IsGoodModExpFirst(
            BigInteger modexp,
            BigInteger prime
        ) {
            var diff = prime - modexp;
            /*if (modexp.failed() || prime.failed() || diff.failed()) {
                return false;
            }*/
            const int kMinDiffBitsCount = 2048 - 64;
            const int kMaxModExpSize = 256;

            return
                diff >= 0 &&
                diff.BitLength >= kMinDiffBitsCount &&
                modexp.BitLength >= kMinDiffBitsCount &&
                modexp.BitLength <= kMaxModExpSize * 8;
        }

        static BigInteger UnsignedNum(byte[] bts) =>
            new BigInteger(1, bts);


        public async Task<User> CheckPassword(Some<Password> passwordInfo, Some<string> passwordStr)
        {
            // TODO: check new salt
            var pwdInfo = passwordInfo.Value;
            var algo = pwdInfo.CurrentAlgo.IfNone(() => null)
                .AsSha256Sha256Pbkdf2Hmacsha512Iter100000Sha256ModPowTag() .IfNone(() => null);

            var hash = ComputeHash(algo, passwordStr);

            var p = UnsignedNum(algo.P.ToArrayUnsafe());
            var g = new BigInteger(algo.G);
            var B = UnsignedNum(pwdInfo.SrpB.Map(bts => bts.ToArrayUnsafe()).IfNone(() => new byte[0]));

            var x = UnsignedNum(hash);
            var pForHash = NumBytesForHash(p); // TODO
            var gForHash = NumBytesForHash(g);
            var BForHash = NumBytesForHash(B); // TODO
            var g_x = g.ModPow(x, p);
            var k = UnsignedNum(Sha256(pForHash, gForHash));
            var kg_x = (k * g_x).Remainder(p);

            (BigInteger, byte[], BigInteger) GenerateAndCheckRandom()
            {
                const int kRandomSize = 256;
                while (true)
                {
                    var random = Rnd.NextBytes(kRandomSize);
                    var a1 = UnsignedNum(random);
                    var A = g.ModPow(a1, p);
                    if (IsGoodModExpFirst(A, p)) {
                        var AForHash1 = NumBytesForHash(A);
                        var u1 = UnsignedNum(Sha256(AForHash1, BForHash));
                        if (u1 > 0) {
                            return (a1, AForHash1, u1);
                        }
                    }
                }
            };

            var (a, AForHash, u) = GenerateAndCheckRandom();
            var g_b = (B - kg_x).Remainder(p);
            /*if (!MTP::IsGoodModExpFirst(g_b, p)) {
                LOG(("API Error: Bad g_b in cloud password check!"));
                return failed();
            }*/

            var ux = u * x;
            var a_ux = a + ux;
            var S = g_b.ModPow(a_ux, p);
            /*if (S.failed()) {
                LOG(("API Error: Failed to count S in cloud password check!"));
                return failed();
            }*/
            var K = Sha256(NumBytesForHash(S));
            var M1 = Sha256(
                Xor(Sha256(pForHash), Sha256(gForHash)),
                Sha256(algo.Salt1.ToArrayUnsafe()),
                Sha256(algo.Salt2.ToArrayUnsafe()),
                AForHash,
                BForHash,
                K
            );

            var request = new CheckPassword(password: new InputCheckPasswordSrp.Tag(
                srpId: pwdInfo.SrpId.IfNone(0),
                a: AForHash.ToBytesUnsafe(),
                m1: M1.ToBytesUnsafe()
            ));

            var res = await _tg.Call(request);
            var user = res.User;

            await _tg.SetAuthorized(user);

            return user;
        }

        public async Task<User> SignUp(
            Some<string> phoneNumber,
            Some<string> phoneCodeHash,
            Some<string> code,
            Some<string> firstName,
            Some<string> lastName
        ) {
            var res = await _tg.Call(new SignUp(
                phoneNumber: phoneNumber,
                phoneCode: code,
                phoneCodeHash: phoneCodeHash,
                firstName: firstName,
                lastName: lastName
            ));
            var user = res.User;
            await _tg.SetAuthorized(user);
            return user;
        }
    }
}
