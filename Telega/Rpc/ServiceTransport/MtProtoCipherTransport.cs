using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LanguageExt;
using Telega.Rpc.Dto;
using Telega.Utils;

namespace Telega.Rpc.ServiceTransport
{
    class MtProtoCipherTransport : IDisposable
    {
        readonly TcpTransport _transport;
        readonly IVarGetter<Session> _session;

        public MtProtoCipherTransport(TcpTransport transport, IVarGetter<Session> session)
        {
            _transport = transport;
            _session = session;
        }

        public void Dispose() => _transport.Dispose();


        static byte[] Sha256(params ArraySegment<byte>[] btsArr)
        {
            using (var sha = SHA256.Create())
            {
                btsArr.SkipLast(1).Iter(bts => sha.TransformBlock(bts.Array, bts.Offset, bts.Count, null, 0));
                btsArr.Last().Apply(bts => sha.TransformFinalBlock(bts.Array, bts.Offset, bts.Count));
                return sha.Hash;
            }
        }

        static ArraySegment<byte> Slice(byte[] buffer, int offset, int count) =>
            new(buffer, offset, count);

        static ArraySegment<byte> AsSlice(byte[] buffer) =>
            new(buffer, 0, buffer.Length);

        static byte[] Concat(params ArraySegment<byte>[] btsArr)
        {
            var res = new byte[btsArr.Sum(x => x.Count)];
            btsArr.Scan(0, (a, x) => a + x.Count).Zip(btsArr).Iter(t =>
                Buffer.BlockCopy(t.Item2.Array, t.Item2.Offset, res, t.Item1, t.Item2.Count)
            );
            return res;
        }

        static int Offset(bool isClient) =>
            isClient ? 0 : 8;

        public static byte[] CalcMsgKey(byte[] authKey, byte[] plainText, bool isClient)
        {
            var x = Offset(isClient);
            var msgKeyLarge = Sha256(Slice(authKey, 88 + x, 32), AsSlice(plainText));
            return Concat(Slice(msgKeyLarge, 8, 16));
        }

        public static AesKeyData CalcAesKey(byte[] authKey, byte[] msgKey, bool isClient)
        {
            var x = Offset(isClient);
            var sha256A = Sha256(AsSlice(msgKey), Slice(authKey, x, 36));
            var sha256B = Sha256(Slice(authKey, 40 + x, 36), AsSlice(msgKey));
            var aesKey = Concat(Slice(sha256A, 0, 8), Slice(sha256B, 8, 16), Slice(sha256A, 24, 8));
            var aesIv = Concat(Slice(sha256B, 0, 8), Slice(sha256A, 8, 16), Slice(sha256B, 24, 8));
            return new AesKeyData(aesKey, aesIv);
        }

        public async Task Send(byte[] msg)
        {
            var session = _session.Get();

            var plainText = BtHelpers.UsingMemBinWriter(bw =>
            {
                bw.Write(session.Salt);
                bw.Write(session.Id);
                bw.Write(msg);

                var bs = bw.BaseStream;
                var requiredPadding = (16 - (int) bs.Position % 16).Apply(x => x == 16 ? 0 : x);
                var randomPadding = (Rnd.NextInt32() & 15) * 16;
                var padding = 16 + requiredPadding + randomPadding;
                bw.Write(Rnd.NextBytes(padding));
            });

            var authKey = session.AuthKey.Key.ToArrayUnsafe();
            var msgKey = CalcMsgKey(authKey, plainText, true);
            var aesKey = CalcAesKey(authKey, msgKey, true);
            var cipherText = Aes.EncryptAES(aesKey, plainText);

            await BtHelpers.UsingMemBinWriter(bw =>
            {
                bw.Write(session.AuthKey.KeyId);
                bw.Write(msgKey);
                bw.Write(cipherText);
            }).Apply(_transport.Send);
        }


        async Task<byte[]> ReceivePlainText()
        {
            var body = await _transport.Receive();
            return body.Apply(BtHelpers.Deserialize(br =>
            {
                var authKeyId = br.ReadUInt64(); // TODO: check auth key id
                var msgKey = br.ReadBytes(16); // TODO: check msg_key correctness
                var keyData = CalcAesKey(_session.Get().AuthKey.Key.ToArrayUnsafe(), msgKey, false);

                var bs = br.BaseStream;
                var cipherTextLen = (int) (bs.Length - bs.Position);
                var cipherText = br.ReadBytes(cipherTextLen);
                var plainText = Aes.DecryptAES(keyData, cipherText);

                return plainText;
            }));
        }

        public async Task<BinaryReader> Receive()
        {
            var plainText = await ReceivePlainText();
            return plainText.Apply(BtHelpers.Deserialize(br =>
            {
                var remoteSalt = br.ReadUInt64();
                var remoteSessionId = br.ReadUInt64();

                return br;
            }));
        }
    }
}
