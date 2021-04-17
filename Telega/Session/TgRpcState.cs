using Telega.Utils;

namespace Telega.Session {
    public sealed record TgRpcState(
        long Salt,
        long Id,
        int Sequence,
        long LastMessageId,
        int TimeOffset,
        TgAuthKey AuthKey
    ) {
        internal static TgRpcState NextMessageId(TgRpcState v) =>
            v with { LastMessageId = Helpers.GetNewMessageId(v.LastMessageId, v.TimeOffset) };
    }
}