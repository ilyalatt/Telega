using System.IO;
using System.Net;
using LanguageExt;
using Telega.Auth;
using Telega.Rpc;
using Telega.Rpc.Dto;
using Telega.Utils;

namespace Telega
{
    // will be changed soon
    public sealed class Session
    {
        public long Id { get; set; }
        public AuthKey AuthKey { get; set; }
        public bool IsAuthorized { get; set; }
        public int Sequence { get; set; }
        public long Salt { get; set; }
        public int TimeOffset { get; set; }
        public long LastMessageId { get; set; }
        public IPEndPoint Endpoint { get; set; }


        public static Session New() => new Session
        {
            Id = Rnd.NextInt64()
        };

        public long GetNewMessageId() =>
            LastMessageId = Helpers.GetNewMessageId(LastMessageId, TimeOffset);


        public void Serialize(BinaryWriter bw)
        {
            TgMarshal.WriteLong(bw, Id);
            TgMarshal.WriteInt(bw, Sequence);
            TgMarshal.WriteLong(bw, Salt);
            TgMarshal.WriteLong(bw, LastMessageId);
            TgMarshal.WriteInt(bw, TimeOffset);
            TgMarshal.WriteBytes(bw, Endpoint.Address.GetAddressBytes().ToBytesUnsafe());
            TgMarshal.WriteInt(bw, Endpoint.Port);
            TgMarshal.WriteBytes(bw, AuthKey.Key);
            TgMarshal.WriteBool(bw, IsAuthorized);
        }

        public static Session Deserialize(BinaryReader br)
        {
            var id = TgMarshal.ReadLong(br);
            var sequence = TgMarshal.ReadInt(br);
            var salt = TgMarshal.ReadLong(br);
            var lastMessageId = TgMarshal.ReadLong(br);
            var timeOffset = TgMarshal.ReadInt(br);

            var serverAddress = TgMarshal.ReadBytes(br).ToArrayUnsafe().Apply(bts => new IPAddress(bts));
            var port = TgMarshal.ReadInt(br);
            var ep = new IPEndPoint(serverAddress, port);

            var authData = TgMarshal.ReadBytes(br);
            var isAuthenticated = TgMarshal.ReadBool(br);

            return new Session
            {
                Id = id,
                Salt = salt,
                Sequence = sequence,
                LastMessageId = lastMessageId,
                TimeOffset = timeOffset,
                Endpoint = ep,
                AuthKey = AuthKey.Deserialize(authData),
                IsAuthorized = isAuthenticated
            };
        }
    }
}
