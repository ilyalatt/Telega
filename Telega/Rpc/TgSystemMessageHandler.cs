using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Types;
using Telega.Utils;
using static LanguageExt.Prelude;

namespace Telega.Rpc {
    static class TgSystemMessageHandler {
        const uint MsgContainerTypeNumber = 0x73f1f8dc;
        const uint RpcResultTypeNumber = 0xf35c6d01;
        const uint GZipPackedTypeNumber = 0x3072cfa1;

        static Func<BinaryReader, T> Peek<T>(Func<BinaryReader, T> func) => br => {
            var bs = br.BaseStream;
            var pos = bs.Position;

            var res = func(br);

            bs.Position = pos;
            return res;
        };

        static uint PeekTypeNumber(BinaryReader br) =>
            br.Apply(Peek(x => x.ReadUInt32()));

        static void EnsureTypeNumber(BinaryReader br, uint expectedTypeNumber) {
            if (br.ReadUInt32() != expectedTypeNumber) {
                throw new Exception("WTF");
            }
        }


        public struct Message {
            public long Id { get; }
            public int SeqNo { get; }
            public BinaryReader Body { get; }

            public Message(long id, int seqNo, BinaryReader body) {
                Id = id;
                SeqNo = seqNo;
                Body = body;
            }

            public static Func<Message, Message> WithBody(BinaryReader body) =>
                msg => new Message(msg.Id, msg.SeqNo, body);
        }

        public static Message ReadMsg(BinaryReader br) {
            var id = br.ReadInt64();
            var seqNo = br.ReadInt32();

            var bodyLength = br.ReadInt32();
            var body = br.ReadBytes(bodyLength);

            return new Message(id, seqNo, body.Apply(BtHelpers.Deserialize(identity)));
        }

        static IEnumerable<Message> ReadContainer(BinaryReader br) {
            EnsureTypeNumber(br, MsgContainerTypeNumber);
            var count = br.ReadInt32();
            return Range(0, count).Map(_ => ReadMsg(br));
        }

        static BinaryReader ReadGZipPacked(BinaryReader br) {
            EnsureTypeNumber(br, GZipPackedTypeNumber);
            var packedData = TgMarshal.ReadBytes(br).ToArrayUnsafe();
            // we need to unpack data to byte array because position and length is used sometimes
            var unpackedData = new GZipStream(new MemoryStream(packedData), CompressionMode.Decompress).ReadToEnd();
            return new BinaryReader(new MemoryStream(unpackedData));
        }


        static RpcResult HandleRpcResult(BinaryReader br) {
            EnsureTypeNumber(br, RpcResultTypeNumber);
            var reqMsgId = br.ReadInt64();
            var innerCode = PeekTypeNumber(br);

            switch (innerCode) {
                case RpcError.TypeNumber:
                    EnsureTypeNumber(br, RpcError.TypeNumber);
                    return RpcError.DeserializeTag(br)
                       .Apply(RpcResultErrorHandler.ToException)
                       .Apply(exc => RpcResult.OfFail(reqMsgId, exc));
                case GZipPackedTypeNumber:
                    return ReadGZipPacked(br).Apply(msgBr => RpcResult.OfSuccess(reqMsgId, msgBr));
                default:
                    return RpcResult.OfSuccess(reqMsgId, br);
            }
        }

        static RpcResult HandleBadMsgNotification(BinaryReader br) {
            EnsureTypeNumber(br, BadMsgNotification.DefaultTag.TypeNumber);
            var badMsg = BadMsgNotification.DefaultTag.DeserializeTag(br);
            return badMsg
               .Apply(RpcBadMsgNotificationHandler.ToException)
               .Apply(exc => RpcResult.OfFail(badMsg.BadMsgId, exc));
        }

        static RpcResult HandleBadServerSalt(BinaryReader br, TgSystemMessageHandlerContext ctx) {
            br.ReadInt32();
            var msg = BadMsgNotification.ServerSaltTag.DeserializeTag(br);

            ctx.NewSalt = msg.NewServerSalt;

            return RpcResult.OfFail(msg.BadMsgId, new TgBadSaltException());
        }

        static RpcResult HandlePong(BinaryReader br) {
            var msg = br.Apply(Peek(Pong.Deserialize));
            var msgId = msg.MsgId;
            return RpcResult.OfSuccess(msgId, br);
        }

        static void HandleNewSessionCreated(BinaryReader messageReader, TgSystemMessageHandlerContext ctx) {
            messageReader.ReadInt32();
            var newSession = NewSession.DeserializeTag(messageReader);

            ctx.NewSalt = newSession.ServerSalt;

            ctx.Logger.LogTrace("NewSession: " + newSession);
        }


        public static Func<Message, Unit> Handle(TgSystemMessageHandlerContext ctx) => message => {
            var br = message.Body;
            var msgId = message.Id;
            var typeNumber = PeekTypeNumber(br);

            switch (typeNumber) {
                case GZipPackedTypeNumber:
                    return message.Apply(ReadGZipPacked(br).Apply(Message.WithBody)).Apply(Handle(ctx));
                case MsgContainerTypeNumber:
                    return ReadContainer(br).Iter(Handle(ctx));

                case RpcResultTypeNumber:
                    ctx.Ack.Add(msgId);
                    ctx.RpcResults.Add(HandleRpcResult(br));
                    return unit;
                case BadMsgNotification.DefaultTag.TypeNumber:
                    ctx.RpcResults.Add(HandleBadMsgNotification(br));
                    return unit;
                case BadMsgNotification.ServerSaltTag.TypeNumber:
                    ctx.RpcResults.Add(HandleBadServerSalt(br, ctx));
                    return unit;
                case Pong.TypeNumber:
                    ctx.RpcResults.Add(HandlePong(br));
                    return unit;

                case NewSession.TypeNumber:
                    ctx.Ack.Add(msgId);
                    HandleNewSessionCreated(br, ctx);
                    return unit;

                case MsgsAck.TypeNumber:
                    // var msg = br.Apply(MsgsAck.Deserialize);
                    // var ids = msg.MsgIds.Apply(xs => string.Join(", ", xs));
                    // TlTrace.Trace("Ack: " + ids);
                    return unit;

                case FutureSalts.TypeNumber:
                    return unit;

                case MsgDetailedInfo.DefaultTag.TypeNumber:
                    return unit;

                case MsgDetailedInfo.NewTag.TypeNumber:
                    EnsureTypeNumber(br, typeNumber);
                    MsgDetailedInfo.NewTag.DeserializeTag(br).AnswerMsgId.With(ctx.Ack.Add);
                    return unit;


                default:
                    EnsureTypeNumber(br, typeNumber);

                    UpdatesType.TryDeserialize(typeNumber, br).NMatch(
                        updates => {
                            ctx.Ack.Add(msgId);
                            ctx.Updates.Add(updates);
                        },
                        () => { ctx.Logger.LogTrace("TgSystemMessageHandler: Unhandled msg " + typeNumber.ToString("x8")); }
                    );

                    return unit;
            }
        };
    }
}