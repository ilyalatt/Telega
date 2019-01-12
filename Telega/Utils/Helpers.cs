using System;
using System.Security.Cryptography;
using LanguageExt;
using Telega.Rpc.ServiceTransport;

namespace Telega.Utils
{
    class Helpers
    {
        static readonly Random Random = new Random();

        static ulong GenerateRandomULong() =>
            ((ulong) Random.Next() << 32) | (uint) Random.Next();

        public static long GenerateRandomLong() =>
            (long) GenerateRandomULong();

        public static AesKeyData CalcKey(byte[] sharedKey, byte[] msgKey, bool client)
        {
            var x = client ? 0 : 8;
            var buffer = new byte[48];

            Array.Copy(msgKey, 0, buffer, 0, 16);            // buffer[0:16] = msgKey
            Array.Copy(sharedKey, x, buffer, 16, 32);     // buffer[16:48] = authKey[x:x+32]
            var sha1a = Sha1(buffer);                     // sha1a = sha1(buffer)

            Array.Copy(sharedKey, 32 + x, buffer, 0, 16);   // buffer[0:16] = authKey[x+32:x+48]
            Array.Copy(msgKey, 0, buffer, 16, 16);           // buffer[16:32] = msgKey
            Array.Copy(sharedKey, 48 + x, buffer, 32, 16);  // buffer[32:48] = authKey[x+48:x+64]
            var sha1b = Sha1(buffer);                     // sha1b = sha1(buffer)

            Array.Copy(sharedKey, 64 + x, buffer, 0, 32);   // buffer[0:32] = authKey[x+64:x+96]
            Array.Copy(msgKey, 0, buffer, 32, 16);           // buffer[32:48] = msgKey
            var sha1c = Sha1(buffer);                     // sha1c = sha1(buffer)

            Array.Copy(msgKey, 0, buffer, 0, 16);            // buffer[0:16] = msgKey
            Array.Copy(sharedKey, 96 + x, buffer, 16, 32);  // buffer[16:48] = authKey[x+96:x+128]
            var sha1d = Sha1(buffer);                     // sha1d = sha1(buffer)

            var key = new byte[32];                       // key = sha1a[0:8] + sha1b[8:20] + sha1c[4:16]
            Array.Copy(sha1a, 0, key, 0, 8);
            Array.Copy(sha1b, 8, key, 8, 12);
            Array.Copy(sha1c, 4, key, 20, 12);

            var iv = new byte[32];                        // iv = sha1a[8:20] + sha1b[0:8] + sha1c[16:20] + sha1d[0:8]
            Array.Copy(sha1a, 8, iv, 0, 12);
            Array.Copy(sha1b, 0, iv, 12, 8);
            Array.Copy(sha1c, 16, iv, 20, 4);
            Array.Copy(sha1d, 0, iv, 24, 8);

            return new AesKeyData(key, iv);
        }

        public static byte[] CalcMsgKey(byte[] data)
        {
            var msgKey = new byte[16];
            Array.Copy(Sha1(data), 4, msgKey, 0, 16);
            return msgKey;
        }

        public static byte[] Sha1(byte[] data)
        {
            using (SHA1 sha1 = new SHA1Managed())
            {
                return sha1.ComputeHash(data);
            }
        }

        static TimeSpan EpochTime =>
            DateTime.UtcNow - new DateTime(1970, 1, 1);

        public static int GetCurrentEpochTime() =>
            (int) EpochTime.TotalSeconds;

        public static long GetNewMessageId(long lastMessageId, int timeOffset)
        {
            var time = EpochTime;

            // [ unix timestamp : 32 bit]
            // [ milliseconds : 10 bit ]
            // [ buffer space : 1 bit ]
            // [ random : 19 bit ]
            // [ msg_id type : 2 bit ]
            // = [ msg_id : 64 bit ]
            var part1 = time.TotalSeconds + timeOffset;
            var part2 = ((long) time.Milliseconds << 22) | ((uint) Random.Next(524288) << 2);
            var newMessageId = ((long) part1 << 32) | part2;

            return lastMessageId >= newMessageId ? lastMessageId + 4 : newMessageId;
        }

        public static TgFailedAssertionException FailedAssertion(string message) =>
            throw new TgFailedAssertionException(message);

        public static void Assert(bool condition, Func<string> message)
        {
            if (!condition) throw FailedAssertion(message());
        }

        public static void Assert(bool condition, string message) =>
            Assert(condition, () => message);
    }
}
