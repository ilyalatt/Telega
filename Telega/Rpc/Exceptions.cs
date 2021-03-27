using System;
using System.Linq;
using Telega.Utils;

namespace Telega.Rpc {
    abstract class TgRpcException : TgInternalException {
        internal TgRpcException(string message, Exception? innerException) : base(
            message,
            innerException
        ) { }
    }

    class TgRpcDeserializeException : TgRpcException {
        TgRpcDeserializeException(string message) : base(message, null) { }

        static string TypeNumber(uint n) => "0x" + n.ToString("x8");

        internal static TgRpcDeserializeException UnexpectedTypeNumber(uint actual, uint[] expected) => new(
            $"Unexpected type number, got {TypeNumber(actual)}, " +
            $"expected {expected.Select(TypeNumber).Apply(xs => string.Join(" or ", xs))}."
        );

        internal static TgRpcDeserializeException UnexpectedBoolTypeNumber(uint actual) => new(
            $"Unexpected 'Bool' type number {TypeNumber(actual)}"
        );

        internal static TgRpcDeserializeException UnexpectedVectorTypeNumber(uint actual) => new(
            $"Unexpected 'Vector' type number {TypeNumber(actual)}"
        );
    }
}