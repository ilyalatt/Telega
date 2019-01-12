using System;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Telega.Rpc
{
    public abstract class TgRpcException : TgException
    {
        internal TgRpcException(Some<string> message, Option<Exception> innerException) : base(
            message,
            innerException
        ) { }
    }

    public class TgRpcDeserializeException : TgRpcException
    {
        internal TgRpcDeserializeException(Some<string> message) : base(message, None) { }

        static string TypeNumber(uint n) => "0x" + n.ToString("x8");

        internal static TgRpcDeserializeException UnexpectedTypeNumber(uint actual, uint[] expected) => new TgRpcDeserializeException(
            $"Unexpected type number, got {TypeNumber(actual)}, "+
            $"expected {expected.Map(TypeNumber).Apply(xs => string.Join(" or ", xs))}."
        );

        internal static TgRpcDeserializeException UnexpectedBoolTypeNumber(uint actual) => new TgRpcDeserializeException(
            $"Unexpected 'Bool' type number {TypeNumber(actual)}"
        );

        internal static TgRpcDeserializeException UnexpectedVectorTypeNumber(uint actual) => new TgRpcDeserializeException(
            $"Unexpected 'Vector' type number {TypeNumber(actual)}"
        );
    }
}
