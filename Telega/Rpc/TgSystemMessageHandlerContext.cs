using System.Collections.Generic;
using LanguageExt;
using Telega.Rpc.Dto.Types;

namespace Telega.Rpc
{
    // immutability?
    sealed class TgSystemMessageHandlerContext
    {
        public List<long> Ack { get; } = new List<long>();
        public List<RpcResult> RpcResults { get; } = new List<RpcResult>();
        public List<UpdatesType> Updates { get; } = new List<UpdatesType>();
        public Option<long> NewSalt;
    }
}
