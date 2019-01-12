using System;

namespace Telega.Rpc.Dto.Generator.Generation
{
    class GenException : Exception
    {
        public GenException(string message) : base(message) { }
    }
}
