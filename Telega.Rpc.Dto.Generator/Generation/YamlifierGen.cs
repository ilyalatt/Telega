using System;
using LanguageExt;
using Telega.Rpc.Dto.Generator.TextModel;
using Telega.Rpc.Dto.Generator.TgScheme;
using static LanguageExt.Prelude;
using static Telega.Rpc.Dto.Generator.TextModel.TextAbbreviations;
using static Telega.Rpc.Dto.Generator.TextModel.NestedTextAbbreviations;

namespace Telega.Rpc.Dto.Generator.Generation {
    static class YamlifierGen {
        static readonly NestedText ToStringDef =
            Scope(
                Line("public override string ToString() =>"),
                IndentedScope(1,
                    Line("Yamlifier.Yamlify(Yamlifier.Stringify(Yamlify(tagOnly: false), this));")
                )
            );
        
        public static NestedText GenTagYamlifier(Arr<Arg> args, string typeName) {
            Text GenYamlifier(bool tagOnly, TgType type) =>
                type.Match(
                    primitive: x => $"Yamlifier.Write{x.Type}",
                    typeRef: x => $"{TgTypeConverter.ConvertType(type, cmpWrapper: false)}.Yamlify(tagOnly: {(tagOnly ? "true" : "false")})",
                    vector: x => Concat(
                        $"Yamlifier.StringifyVector<{TgTypeConverter.ConvertType(x.Type, cmpWrapper: false)}>(",
                        GenYamlifier(tagOnly, x.Type),
                        ")"
                    )
                );

            static bool IsTypeRef(Arg x) =>
                x.Type.Match(typeRef: _ => true, _: () => false);

            static bool IsOptional(Arg x) =>
                x.Kind.Match(optional: _ => true, _: () => false);

            Option<Text> GenNonFlagArgYamlifier(Arg arg) =>
                arg.Kind.Match(
                    _: () => throw new("WTF"),
                    required: _ => GenYamlifier(tagOnly: IsTypeRef(arg), arg.Type).Apply(Some),
                    optional: x => arg.Type == TgType.OfPrimitive(PrimitiveType.True)
                        ? None
                        : Concat(
                            $"Yamlifier.StringifyOption<{TgTypeConverter.ConvertType(arg.Type, cmpWrapper: false)}>(",
                            GenYamlifier(tagOnly: IsTypeRef(arg), arg.Type),
                            ")"
                        ).Apply(Some)
                ).Map(s =>
                    Concat("Yamlifier.Stringify(", s, $", v.{arg.Name})")
                );

            Option<Text> GenArgYamlifier(Arg arg) => arg.Kind.Match(
                _: () => GenNonFlagArgYamlifier(arg),
                flags: _ => None
            );

            var stringifierBody = args
               .Choose(x => GenArgYamlifier(x).Map(y => {
                   var isTypeRef = IsTypeRef(x);
                   var isOptional = IsOptional(x);
                   var argName = x.Name;
                   var tagName = !isOptional
                       ? $"v.{argName}._TagName"
                       : $"v.{argName}.IfNoneUnsafe(() => null!)?._TagName";
                   var nameofFieldName = $"nameof(v.{argName})";
                   return (
                       arg: !isTypeRef
                           ? nameofFieldName
                           : Concat(
                               $"{tagName} != null ",
                               $"? {nameofFieldName} + '.' + {tagName} ",
                               $": {nameofFieldName}"
                            ),
                       yamlifier: y
                   );
               }))
               .Map(t => Concat("(", t.arg, ", ", t.yamlifier, ")"))
               .Map(Line)
               .Scope($",{Environment.NewLine}");
            var def = Scope(
                Line("[System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]"),
                Line("internal string? _TagName => null;"),
                Line(""),
                Line($"internal static Yamlifier.Stringifier<{typeName}> Yamlify(bool tagOnly) =>"),
                IndentedScope(1,
                    Line("v => Yamlifier.WriteMapping("),
                    Indent(1, stringifierBody),
                    Line(");")
                ),
                Line(""),
                ToStringDef
            );
            return def;
        }

        public static NestedText GenUnionYamlifier(Arr<Signature> tags, string typeName) {
            return Scope(
                Line("[System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]"),
                Line("internal string? _TagName =>"),
                IndentedScope(1,
                    Line("_tag switch {"),
                    IndentedScope(1,
                        tags.Map(x => $"{x.Name} => nameof({x.Name}),").Map(Line).Scope(),
                        Line("_ => throw new(\"WTF\"),")
                    ),
                    Line("};")
                ),
                Line(""),
                Line($"internal static Yamlifier.Stringifier<{typeName}> Yamlify(bool tagOnly) =>"),
                IndentedScope(1,
                    Line("v => v._tag switch {"),
                    IndentedScope(1,
                        tags
                            .Map(x => Concat(
                                $"{x.Name} x => Yamlifier.WriteUnion(",
                                Concat(
                                    $"!tagOnly ? nameof({x.Name}) : null, ",
                                    $"Yamlifier.Stringify({x.Name}.Yamlify(tagOnly: true), x)"
                                ),
                                "),"
                            ))
                            .Map(Line)
                            .Scope(),
                        Line("_ => throw new(\"WTF\"),")
                    ),
                    Line("};")
                ),
                Line(""),
                ToStringDef
            );
        }
    }
}