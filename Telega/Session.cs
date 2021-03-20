using System.IO;
using System.Net;
using LanguageExt;
using Telega.Auth;
using Telega.Rpc;
using Telega.Rpc.Dto;
using Telega.Utils;
using static LanguageExt.Prelude;

namespace Telega {
    public sealed class Session {
        const int Version = 1;

        public int ApiId { get; }
        public long Id { get; }
        public AuthKey AuthKey { get; }
        public bool IsAuthorized { get; }
        public int Sequence { get; }
        public long Salt { get; }
        public int TimeOffset { get; }
        public long LastMessageId { get; }
        public IPEndPoint Endpoint { get; }

        public Session(
            int apiId,
            long id,
            Some<AuthKey> authKey,
            bool isAuthorized,
            int sequence,
            long salt,
            int timeOffset,
            long lastMessageId,
            Some<IPEndPoint> endpoint
        ) {
            ApiId = apiId;
            Id = id;
            AuthKey = authKey;
            IsAuthorized = isAuthorized;
            Sequence = sequence;
            Salt = salt;
            TimeOffset = timeOffset;
            LastMessageId = lastMessageId;
            Endpoint = endpoint;
        }

        public Session With(
            int? apiId = null,
            long? id = null,
            AuthKey? authKey = null,
            bool? isAuthorized = null,
            int? sequence = null,
            long? salt = null,
            int? timeOffset = null,
            long? lastMessageId = null,
            IPEndPoint? endpoint = null
        ) => new(
            apiId ?? ApiId,
            id ?? Id,
            authKey ?? AuthKey,
            isAuthorized ?? IsAuthorized,
            sequence ?? Sequence,
            salt ?? Salt,
            timeOffset ?? TimeOffset,
            lastMessageId ?? LastMessageId,
            endpoint ?? Endpoint
        );

        public static Session New(int apiId, Some<IPEndPoint> endpoint, Some<AuthKey> authKey, int timeOffset) => new(
            apiId: apiId,
            id: Rnd.NextInt64(),
            authKey: authKey,
            isAuthorized: false,
            sequence: 0,
            salt: 0,
            timeOffset: timeOffset,
            lastMessageId: 0,
            endpoint
        );

        internal static long GetNewMessageId(Var<Session> sessionVar) {
            return sessionVar.SetWith(x => x.With(
                lastMessageId: Helpers.GetNewMessageId(x.LastMessageId, x.TimeOffset)
            )).LastMessageId;
        }


        public void Serialize(BinaryWriter bw) {
            TgMarshal.WriteInt(bw, Version);
            TgMarshal.WriteInt(bw, ApiId);
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

        public static Session Deserialize(BinaryReader br) {
            var version = TgMarshal.ReadInt(br);
            if (version != Version) {
                throw new TgInternalException($"Invalid session file version, got {version}, expected {Version}.", None);
            }

            var apiId = TgMarshal.ReadInt(br);
            var id = TgMarshal.ReadLong(br);
            var sequence = TgMarshal.ReadInt(br);
            var salt = TgMarshal.ReadLong(br);
            var lastMessageId = TgMarshal.ReadLong(br);
            var timeOffset = TgMarshal.ReadInt(br);

            var serverAddress = TgMarshal.ReadBytes(br).ToArrayUnsafe().Apply(bts => new IPAddress(bts));
            var port = TgMarshal.ReadInt(br);
            var ep = new IPEndPoint(serverAddress, port);

            var authData = TgMarshal.ReadBytes(br);
            var isAuthorized = TgMarshal.ReadBool(br);

            return new(
                apiId: apiId,
                id: id,
                salt: salt,
                sequence: sequence,
                lastMessageId: lastMessageId,
                timeOffset: timeOffset,
                endpoint: ep,
                authKey: AuthKey.Deserialize(authData),
                isAuthorized: isAuthorized
            );
        }
    }
}