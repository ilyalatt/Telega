using System;
using LanguageExt;
using Telega.Rpc.Dto.Generator.TextModel;
using Telega.Rpc.Dto.Generator.TgScheme;
using static Telega.Rpc.Dto.Generator.TextModel.NestedTextAbbreviations;

namespace Telega.Rpc.Dto.Generator.Generation {
    static class WithGen {
        public static NestedText GenWith(Arr<Arg> args, string typeName) => Scope(
            Line($"public {typeName} With("),
            IndentedScope(1, "," + Environment.NewLine, args
               .Map(arg => $"{TgTypeConverter.WrapArgTypeWithNullable(arg)} {Helpers.LowerFirst(arg.Name)} = null")
               .Map(Line)
            ),
            Line($") => new {typeName}("),
            IndentedScope(1, "," + Environment.NewLine, args
               .Map(arg => arg.Name).Map(argName => $"{Helpers.LowerFirst(argName)} ?? {argName}")
               .Map(Line)
            ),
            Line(");")
        );
    }
}