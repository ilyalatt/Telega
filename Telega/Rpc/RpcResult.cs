using System.IO;

namespace Telega.Rpc
{
    struct RpcResult
    {
        public long Id { get; }
        public BinaryReader? Body { get; }
        public TgException? Exception { get; }

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
