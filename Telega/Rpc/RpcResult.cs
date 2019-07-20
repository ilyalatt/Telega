using System.IO;

namespace Telega.Rpc
{
    struct RpcResult
    {
        public readonly long Id;
        public readonly BinaryReader? Body;
        public readonly TgException? Exception;

        public RpcResult(long id, BinaryReader? body, TgException? exception)
        {
            Id = id;
            Body = body;
            Exception = exception;
        }

        public static RpcResult OfSuccess(long msgId, BinaryReader msgBody) =>
            new RpcResult(msgId, msgBody, null);

        public static RpcResult OfFail(long msgId, TgException exception) =>
            new RpcResult(msgId, null, exception);

        public bool IsSuccess => Body != null;
        public bool IsFail => Exception != null;
    }
}
