using LanguageExt;
using Telega.Rpc.Dto.Types;
using static LanguageExt.Prelude;

namespace Telega.Rpc
{
    class TgRpcBadMsgException : TgRpcException
    {
        public int ErrorCode { get; }

        public TgRpcBadMsgException(int errorCode, Some<string> msg) : base(msg, None)
        {
            ErrorCode = errorCode;
        }
    }

    static class RpcBadMsgNotificationHandler
    {
        public static TgRpcBadMsgException ToException(BadMsgNotification.Tag error)
        {
            var code = error.ErrorCode;
            TgRpcBadMsgException Ex(string msg) => new TgRpcBadMsgException(code, msg);

            switch (code)
            {
                case 16:
                    return Ex(
                        "msg_id too low (most likely, client time is wrong; it would be worthwhile to synchronize it using msg_id notifications and re-send the original message with the “correct” msg_id or wrap it in a container with a new msg_id if the original message had waited too long on the client to be transmitted)"
                    );
                case 17:
                    return Ex(
                        "msg_id too high (similar to the previous case, the client time has to be synchronized, and the message re-sent with the correct msg_id)"
                    );
                case 18:
                    return Ex(
                        "incorrect two lower order msg_id bits (the server expects client message msg_id to be divisible by 4)"
                    );
                case 19:
                    return Ex(
                        "container msg_id is the same as msg_id of a previously received message (this must never happen)"
                    );
                case 20:
                    return Ex(
                        "message too old, and it cannot be verified whether the server has received a message with this msg_id or not"
                    );
                case 32:
                    return Ex(
                        "msg_seqno too low (the server has already received a message with a lower msg_id but with either a higher or an equal and odd seqno)"
                    );
                case 33:
                    return Ex(
                        " msg_seqno too high (similarly, there is a message with a higher msg_id but with either a lower or an equal and odd seqno)"
                    );
                case 34:
                    return Ex(
                        "an even msg_seqno expected (irrelevant message), but odd received"
                    );
                case 35:
                    return Ex(
                        "odd msg_seqno expected (relevant message), but even received"
                    );
                case 48:
                    return Ex(
                        "incorrect server salt (in this case, the bad_server_salt response is received with the correct salt, and the message is to be re-sent with it)"
                    );
                case 64:
                    return Ex("invalid container");
                default:
                    return Ex("Unknown code");
            }
        }
    }
}
