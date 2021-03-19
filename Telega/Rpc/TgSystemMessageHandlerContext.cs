using System.Collections.Generic;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Telega.Rpc.Dto.Types;

namespace Telega.Rpc
{
    // immutability?
    sealed record TgSystemMessageHandlerContext(ILogger Logger)
    {
        public List<long> Ack { get; } = new();
        public List<RpcResult> RpcResults { get; } = new();
        public List<UpdatesType> Updates { get; } = new();
        public Option<long> NewSalt { get; set; }
    }
}
