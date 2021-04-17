namespace Telega.Session {
    // Refactoring of Session
    // As we see config and context are similar
    // But there is a difference
    // We should not use config in internals because it just messes things
    // Except ConnectionConfig maybe
    //
    // Also LastMessageId bug!
    // Also remove fucking padding!
    // Also resync time!
    //
    // Consider MessagePack!
    // Check if it can generate a runtime code
    //

    public sealed record TgSession(
         TgRpcConfig? RpcConfig,
         TgRpcState? RpcState
        ) {
        public static TgSession Default => new(
            RpcConfig: null,
            RpcState: null
        );
    }
}