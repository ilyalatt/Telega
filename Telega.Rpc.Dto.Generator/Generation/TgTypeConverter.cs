using LanguageExt;
using Telega.Rpc.Dto.Generator.TgScheme;

namespace Telega.Rpc.Dto.Generator.Generation {
    static class TgTypeConverter {
        public static string ConvertType(TgType type, bool cmpWrapper) => type.Match(
            primitive: x => {
                return x.Type switch
                {
                    PrimitiveType.Bytes => "Bytes",
                    PrimitiveType.Int128 => "BigMath.Int128",
                    PrimitiveType.Int256 => "BigMath.Int256",
                    _ => x.Type.ToString().ToLower(),
                };
            },
            vector: x => cmpWrapper
                ? $"ListCmp.Wrapper<{ConvertType(x.Type, true)}>"
                : $"IReadOnlyList<{ConvertType(x.Type, false)}>",
            typeRef: x => x.Name == "X" || x.Name == "!X" ? "TFunc" : $"T.{x.Name}"
        );

        public static bool IsRefType(TgType type) => type.Match(
            primitive: x => x.Type == PrimitiveType.String,
            vector: _ => false,
            typeRef: _ => true
        );

        public static string ConvertArgType(Arg arg, bool cmpWrapper) => arg.Kind.Match(
            required: x => ConvertType(arg.Type, cmpWrapper),
            optional: x => arg.Type == TgType.OfPrimitive(PrimitiveType.True) ? "bool" : $"Option<{ConvertType(arg.Type, cmpWrapper)}>",
            flags: _ => "int"
        );

        public static bool IsRefArgType(Arg arg) => arg.Kind.Match(
            required: x => IsRefType(arg.Type),
            optional: x => false,
            flags: _ => false
        );

        public static string WrapArgTypeWithNullable(Arg arg, bool cmpWrapper) => ConvertArgType(arg, cmpWrapper)
           .Apply(x => $"{x}?");
    }
}