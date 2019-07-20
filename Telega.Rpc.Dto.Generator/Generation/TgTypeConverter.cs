using LanguageExt;
using Telega.Rpc.Dto.Generator.TgScheme;

namespace Telega.Rpc.Dto.Generator.Generation
{
    static class TgTypeConverter
    {
        public static string ConvertType(TgType type) => type.Match(
            primitive: x =>
            {
                switch (x.Type)
                {
                    case PrimitiveType.Bytes: return "Bytes";
                    case PrimitiveType.Int128: return "BigMath.Int128";
                    case PrimitiveType.Int256: return "BigMath.Int256";
                    default: return x.Type.ToString().ToLower();
                }
            },
            vector: x => $"Arr<{ConvertType(x.Type)}>",
            typeRef: x => x.Name == "X" || x.Name == "!X" ? "TFunc" : $"T.{x.Name}"
        );

        public static bool IsRefType(TgType type) => type.Match(
            primitive: x => x.Type == PrimitiveType.String,
            vector: _ => false,
            typeRef: _ => true
        );

        public static string ConvertArgType(Arg arg) => arg.Kind.Match(
            required: x => ConvertType(arg.Type),
            optional: x => arg.Type == TgType.OfPrimitive(PrimitiveType.True) ? "bool" : $"Option<{ConvertType(arg.Type)}>",
            flags: _ => "int"
        );

        public static bool IsRefArgType(Arg arg) => arg.Kind.Match(
            required: x => IsRefType(arg.Type),
            optional: x => false,
            flags: _ => false
        );

        public static string WrapArgTypeWithNullable(Arg arg) => ConvertArgType(arg)
            .Apply(x => $"{x}?");
    }
}
