using System;
using System.Security.Cryptography;

namespace Telega.Utils
{
    class Helpers
    {
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
            var part2 = ((long) time.Milliseconds << 22) | ((Rnd.NextUInt32() & 524287) << 2);
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
