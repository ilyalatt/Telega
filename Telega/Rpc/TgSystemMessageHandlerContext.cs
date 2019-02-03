using System.Collections.Generic;
using LanguageExt;

namespace Telega.Rpc
{
    // immutability?
    sealed class TgSystemMessageHandlerContext
    {
        public readonly List<long> Ack = new List<long>();
        public readonly List<RpcResult> RpcResults = new List<RpcResult>();
        public Option<long> NewSalt;
    }
}
