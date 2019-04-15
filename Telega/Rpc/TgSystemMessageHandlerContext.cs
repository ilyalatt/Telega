using System.Collections.Generic;
using LanguageExt;
using Telega.Rpc.Dto.Types;

namespace Telega.Rpc
{
    // immutability?
    sealed class TgSystemMessageHandlerContext
    {
        public readonly List<long> Ack = new List<long>();
        public readonly List<RpcResult> RpcResults = new List<RpcResult>();
        public readonly List<UpdatesType> Updates = new List<UpdatesType>();
        public Option<long> NewSalt;
    }
}
