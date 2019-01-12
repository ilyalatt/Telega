using System.Threading.Tasks;
using LanguageExt;
using Telega.Rpc.Dto;
using Telega.Utils;

namespace Telega.Rpc.ServiceTransport
{
    class MtProtoPlainTransport
    {
        long _lastMessageId;
        readonly TcpTransport _transport;

        public MtProtoPlainTransport(Some<TcpTransport> transport) => _transport = transport;

        long GetNewMessageId() =>
            _lastMessageId = Helpers.GetNewMessageId(_lastMessageId, timeOffset: 0);

        async Task Send(byte[] msg) =>
            await BtHelpers.UsingMemBinWriter(bw =>
            {
                bw.Write((long) 0);
                bw.Write(GetNewMessageId());

                bw.Write(msg.Length);
                bw.Write(msg);
            }).Apply(_transport.Send);

        async Task<byte[]> Receive()
        {
            var body = await _transport.Receive();
            return body.Apply(BtHelpers.Deserialize(br =>
            {
                var authKeyId = br.ReadInt64(); // 0
                var messageId = br.ReadInt64();

                var msgLen = br.ReadInt32();
                var msg = br.ReadBytes(msgLen);
                return msg;
            }));
        }

        public async Task<T> Call<T>(ITgFunc<T> func)
        {
            await BtHelpers.UsingMemBinWriter(func.Serialize).Apply(Send);
            return await Receive().Map(BtHelpers.Deserialize(func.DeserializeResult));
        }
    }
}
