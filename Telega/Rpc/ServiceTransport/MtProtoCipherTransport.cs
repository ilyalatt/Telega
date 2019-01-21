using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Telega.Rpc.Dto;
using Telega.Utils;

namespace Telega.Rpc.ServiceTransport
{
    class MtProtoCipherTransport : IDisposable
    {
        readonly TcpTransport _transport;
        readonly Session _session;
        readonly ISessionStore _sessionStore;

        public MtProtoCipherTransport(TcpTransport transport, Session session, ISessionStore sessionStore)
        {
            _transport = transport;
            _session = session;
            _sessionStore = sessionStore;
        }

        public void Dispose() => _transport.Dispose();


        int GetSeqNum(bool inc) => inc ? _session.Sequence++ * 2 + 1 : _session.Sequence * 2;


        public static AesKeyData CalcKey(byte[] sharedKey, byte[] msgKey, bool client)
        {
            var x = client ? 0 : 8;
            var buffer = new byte[48];

            Array.Copy(msgKey, 0, buffer, 0, 16); // buffer[0:16] = msgKey
            Array.Copy(sharedKey, x, buffer, 16, 32); // buffer[16:48] = authKey[x:x+32]
            var sha1a = Helpers.Sha1(buffer); // sha1a = sha1(buffer)

            Array.Copy(sharedKey, 32 + x, buffer, 0, 16); // buffer[0:16] = authKey[x+32:x+48]
            Array.Copy(msgKey, 0, buffer, 16, 16); // buffer[16:32] = msgKey
            Array.Copy(sharedKey, 48 + x, buffer, 32, 16); // buffer[32:48] = authKey[x+48:x+64]
            var sha1b = Helpers.Sha1(buffer); // sha1b = sha1(buffer)

            Array.Copy(sharedKey, 64 + x, buffer, 0, 32); // buffer[0:32] = authKey[x+64:x+96]
            Array.Copy(msgKey, 0, buffer, 32, 16); // buffer[32:48] = msgKey
            var sha1c = Helpers.Sha1(buffer); // sha1c = sha1(buffer)

            Array.Copy(msgKey, 0, buffer, 0, 16); // buffer[0:16] = msgKey
            Array.Copy(sharedKey, 96 + x, buffer, 16, 32); // buffer[16:48] = authKey[x+96:x+128]
            var sha1d = Helpers.Sha1(buffer); // sha1d = sha1(buffer)

            var key = new byte[32]; // key = sha1a[0:8] + sha1b[8:20] + sha1c[4:16]
            Array.Copy(sha1a, 0, key, 0, 8);
            Array.Copy(sha1b, 8, key, 8, 12);
            Array.Copy(sha1c, 4, key, 20, 12);

            var iv = new byte[32]; // iv = sha1a[8:20] + sha1b[0:8] + sha1c[16:20] + sha1d[0:8]
            Array.Copy(sha1a, 8, iv, 0, 12);
            Array.Copy(sha1b, 0, iv, 12, 8);
            Array.Copy(sha1c, 16, iv, 20, 4);
            Array.Copy(sha1d, 0, iv, 24, 8);

            return new AesKeyData(key, iv);
        }

        async Task SendMsgBody(long messageId, bool incSeqNum, byte[] msg)
        {
            var msgSeqNum = GetSeqNum(incSeqNum);
            await _sessionStore.Save(_session);

            var plainText = BtHelpers.UsingMemBinWriter(bw =>
            {
                bw.Write(_session.Salt);
                bw.Write(_session.Id);
                bw.Write(messageId);
                bw.Write(msgSeqNum);

                bw.Write(msg.Length);
                bw.Write(msg);
            });

            var msgKey = Helpers.CalcMsgKey(plainText);
            var cipherText = Aes.EncryptAES(
                CalcKey(_session.AuthKey.Key.ToArray(), msgKey, true),
                plainText
            );

            await BtHelpers.UsingMemBinWriter(bw =>
            {
                bw.Write(_session.AuthKey.KeyId);
                bw.Write(msgKey);
                bw.Write(cipherText);
            }).Apply(_transport.Send);
        }

        public async Task Send(long messageId, bool incSeqNum, ITgSerializable dto)
        {
            var bts = BtHelpers.UsingMemBinWriter(dto.Serialize);
            await SendMsgBody(messageId, incSeqNum, bts);
        }


        async Task<byte[]> ReceivePlainText()
        {
            var body = await _transport.Receive();

            const uint protocolViolationCode = 0xfffffe6c;
            if (body.Length == 4 && BitConverter.ToUInt32(body, 0) ==  protocolViolationCode)
            {
                throw new TgProtocolViolation();
            }

            return body.Apply(BtHelpers.Deserialize(br =>
            {
                var authKeyId = br.ReadUInt64(); // TODO: check auth key id
                var msgKey = br.ReadBytes(16); // TODO: check msg_key correctness
                var keyData = CalcKey(_session.AuthKey.Key.ToArray(), msgKey, false);

                var cipherTextLen = (int) (br.BaseStream.Length - br.BaseStream.Position);
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
