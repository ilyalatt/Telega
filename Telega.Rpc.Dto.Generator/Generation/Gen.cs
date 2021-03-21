using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using LanguageExt.SomeHelp;
using Telega.Rpc.Dto.Generator.TextModel;
using Telega.Rpc.Dto.Generator.TgScheme;
using static LanguageExt.Prelude;
using static Telega.Rpc.Dto.Generator.TextModel.TextAbbreviations;
using static Telega.Rpc.Dto.Generator.TextModel.NestedTextAbbreviations;

namespace Telega.Rpc.Dto.Generator.Generation {
    // TODO: refactor it, add CsModel over TextModel
    static class Gen {
        static readonly NestedText Header = Scope(
            Line("using System;"),
            Line("using System.IO;"),
            Line("using LanguageExt;"),
            Line("using static Telega.Rpc.TgMarshal;"),
            Line("using T = Telega.Rpc.Dto.Types;")
        );

        static NestedText GenTypeTagBody(string typeName, string tagName, Signature tag) {
            var modifiedArgs = tag.Args
               .Map(x => x with {
                   Name = x.Name == tagName ? $"{x.Name}Value" : x.Name, // TODO: move to normalizer
               })
               .ToArr();
            var argsWithoutFlags = modifiedArgs
               .Filter(x => x.Kind.Match(_: () => true, flags: _ => false))
               .ToArr();
            var tagArgs = argsWithoutFlags
               .Map(x => (
                    name: x.Name,
                    lowerName: Helpers.LowerFirst(x.Name),
                    type: TgTypeConverter.ConvertArgType(x),
                    isRef: TgTypeConverter.IsRefArgType(x)
                )).ToArray();

            return Scope(
                Scope(
                    Line($"internal const uint TypeNumber = {Helpers.TypeNumber(tag.TypeNumber)};"),
                    Line("[System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]"),
                    Line("uint ITgTypeTag.TypeNumber => TypeNumber;"),
                    Line("")
                ),
                tagArgs.Map(arg => Line($"public {arg.type} {arg.name} {{ get; }}")).Scope(),
                Line(""),
                Scope(
                    Line($"public {tagName}("),
                    IndentedScope(1, $",{Environment.NewLine}",
                        tagArgs.Map(arg => Line($"{arg.type} {arg.lowerName}"))
                    ),
                    Line(") {"),
                    IndentedScope(1,
                        tagArgs.Map(arg => Line(
                            $"{arg.name} = {arg.lowerName}" +
                            $"{(arg.isRef ? $" ?? throw new ArgumentNullException(nameof({arg.lowerName}))" : "")};"
                        ))
                    ),
                    Line("}")
                ),
                Line(""),
                typeName != tagName
                    ? Scope(
                        Line($"public static implicit operator {typeName}({tagName} tag) => new(tag);"),
                        Line($"public static implicit operator Some<{typeName}>({tagName} tag) => new(tag);"),
                        Line("")
                    )
                    : EmptyScope(),
                RelationsGen.GenRelations(tagName, isRecord: true, argsWithoutFlags),
                Line(""),
                Line(""),
                SerializerGen.GenSerializer(modifiedArgs, typeNumber: None, "ITgTypeTag.SerializeTag"),
                Line(""),
                SerializerGen.GenTypeTagDeserialize(tagName, modifiedArgs),
                Line(""),
                Line(""),
                YamlifierGen.GenTagYamlifier(modifiedArgs, tagName)
            );
        }

        static NestedText GenTypeTag(string typeName, Signature tag) {
            var tagName = tag.Name;

            return Scope(
                Line($"public sealed record {tagName}"),
                Line($": ITgTypeTag, IComparable<{tagName}>, IComparable"),
                Line("{"),
                Indent(1, GenTypeTagBody(typeName, tagName, tag)),
                Line("}")
            );
        }

        static NestedText GenFunc(Signature func, string funcName) {
            var argsWithoutFlags = func.Args
               .Filter(x => x.Kind.Match(_: () => true, flags: _ => false))
               .ToArr();
            var funcArgs = argsWithoutFlags
               .Map(x => (
                    name: x.Name,
                    lowerName: Helpers.LowerFirst(x.Name),
                    type: TgTypeConverter.ConvertArgType(x),
                    isRef: TgTypeConverter.IsRefArgType(x)
                )).ToArray();

            // usually it is a higher-order function, i am too lazy to modify the scheme just for this case
            var isWrapper = func.Args.Exists(x => x.Type == TgType.OfTypeRef("X") || x.Type == TgType.OfTypeRef("!X"));
            var resType = isWrapper ? "TFuncRes" : TgTypeConverter.ConvertType(func.ResultType);
            var classAccess = isWrapper ? "" : "public ";
            var classTemplates = isWrapper ? "<TFunc, TFuncRes>" : "";
            var classAnnotations = isWrapper
                ? $": ITgFunc<{resType}> where TFunc : class, ITgFunc<{resType}>"
                : $": ITgFunc<{resType}>, IComparable<{funcName}>, IComparable";

            var resDes = isWrapper
                ? "Query.DeserializeResult(br);" // it is 'Query' all the time, i am too lazy
                : Concat("Read(br, ", SerializerGen.GenTypeDeserializer(func.ResultType), ");");
            var resultDeserializer = Scope(
                Line($"{resType} ITgFunc<{resType}>.DeserializeResult(BinaryReader br) =>"),
                Indent(1, Line(resDes))
            );

            return Scope(
                Line($"{classAccess}sealed record {funcName}{classTemplates} {classAnnotations} {{"),
                IndentedScope(1,
                    funcArgs.Map(arg => Line($"public {arg.type} {arg.name} {{ get; init; }}")).Scope(),
                    Scope(
                        Line(""),
                        Scope(
                            Line($"public {funcName}("),
                            IndentedScope(1, $",{Environment.NewLine}",
                                funcArgs.Map(arg => Line($"{arg.type} {arg.lowerName}"))
                            ),
                            Line(") {"),
                            IndentedScope(1,
                                funcArgs.Map(arg => Line(
                                    $"{arg.name} = {arg.lowerName}" +
                                    $"{(arg.isRef ? $" ?? throw new ArgumentNullException(nameof({arg.lowerName}))" : "")};"
                                ))
                            ),
                            Line("}")
                        ),
                        Line(""),
                        Line(""),
                        isWrapper
                            ? EmptyScope()
                            : Scope(
                                RelationsGen.GenRelations(funcName, isRecord: true, argsWithoutFlags),
                                Line("")
                            ),
                        SerializerGen.GenSerializer(func.Args, typeNumber: func.TypeNumber, "ITgSerializable.Serialize"),
                        Line(""),
                        resultDeserializer,
                        Line(""),
                        Line(""),
                        isWrapper
                            ? EmptyScope()
                            : YamlifierGen.GenTagYamlifier(func.Args, funcName)
                    )
                ),
                Line("}")
            );
        }

        static NestedText GenTypeWithManyTags(string typeName, Arr<Signature> typeTags) {
            var tagsDefs = typeTags.Map(x => GenTypeTag(typeName, x)).Scope(Environment.NewLine + Environment.NewLine);

            var tagDef = Scope(
                Line("readonly ITgTypeTag _tag;"),
                Line($"{typeName}(ITgTypeTag tag) => _tag = tag ?? throw new ArgumentNullException(nameof(tag));")
            );

            var serializeRef = Scope(
                Line("void ITgSerializable.Serialize(BinaryWriter bw) {"),
                IndentedScope(1,
                    Line("WriteUint(bw, _tag.TypeNumber);"),
                    Line("_tag.SerializeTag(bw);")
                ),
                Line("}")
            );

            var staticTryDeserializeDef = Scope(
                Line($"internal static Option<{typeName}> TryDeserialize(uint typeNumber, BinaryReader br) {{"),
                IndentedScope(1,
                    Line("return typeNumber switch {"),
                    IndentedScope(1,
                        typeTags.Map(x => Line($"{x.Name}.TypeNumber => ({typeName}) {x.Name}.DeserializeTag(br),")).Scope(),
                        Line("_ => Prelude.None,")
                    ),
                    Line("};")
                ),
                Line("}")
            );

            var staticDeserializeDef = Scope(
                Line($"internal static {typeName} Deserialize(BinaryReader br) {{"),
                IndentedScope(1,
                    Line("var typeNumber = ReadUint(br);"),
                    Line(Concat(
                        "return TryDeserialize(typeNumber, br).IfNone(() => ",
                        "throw TgRpcDeserializeException.UnexpectedTypeNumber(actual: typeNumber, expected: new[] { ",
                        typeTags.Map(x => $"{x.Name}.TypeNumber").Map(String).Apply(xs => Join(", ", xs)),
                        " })",
                        ");"
                    ))
                ),
                Line("}")
            );

            var matchArgFns =
                typeTags.Map(tag => tag.Name).Map(tagName =>
                    $"Func<{tagName}, T>? {Helpers.LowerFirst(tagName)}"
                );

            var matchOptDef = Scope(
                Line($"{(typeTags.Count <= 1 ? "" : "public ")}T Match<T>("),
                IndentedScope(1, $",{Environment.NewLine}",
                    Line("Func<T> _").Singleton(),
                    matchArgFns.Map(x => Concat(x, " = null")).Map(Line)
                ),
                Line(") {"),
                IndentedScope(1,
                    Line("if (_ == null) throw new ArgumentNullException(nameof(_));"),
                    Line("return _tag switch {"),
                    IndentedScope(1,
                        typeTags.Map(tag => {
                            var tagName = tag.Name;
                            var tagNameLower = Helpers.LowerFirst(tagName);
                            return Line($"{tagName} x when {tagNameLower} != null => {tagNameLower}(x),");
                        }).Scope(),
                        Line("_ => _(),")
                    ),
                    Line("};")
                ),
                Line("}")
            );

            var matchDef = Scope(
                Line("public T Match<T>("),
                IndentedScope(1, $",{Environment.NewLine}", matchArgFns.Map(Line)),
                Line(") => Match("),
                IndentedScope(1, $",{Environment.NewLine}",
                    Line(@"() => throw new(""WTF"")").Singleton(),
                    typeTags.Map(tag => {
                        var tagName = tag.Name;
                        var tagNameLower = Helpers.LowerFirst(tagName);
                        return Line($"{tagNameLower} ?? throw new ArgumentNullException(nameof({tagNameLower}))");
                    })
                ),
                Line(");")
            );

            static string TrimTagSuffix(string s) => s.EndsWith("Tag") ? s[..^3] : s;
            var castHelpersDef = Scope(
                typeTags.Map(tag => $"public {tag.Name}? {TrimTagSuffix(tag.Name)} => _tag as {tag.Name};").Map(Line)
            );

            var cmpPairName = String("CmpPair");
            var helpersDef = Scope(
                Line("int GetTagOrder() {"),
                IndentedScope(1,
                    Line("return _tag switch {"),
                    IndentedScope(1,
                        typeTags.Map((idx, x) => $"{x.Name} => {idx},").Map(Line).Scope(),
                        Line("_ => throw new(\"WTF\"),")
                    ),
                    Line("};")
                ),
                Line("}"),
                Line("[System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]"),
                Line(Concat("(int, object) ", cmpPairName, " => (GetTagOrder(), _tag);"))
            );

            var bodyDef = Scope(Environment.NewLine + Environment.NewLine,
                tagsDefs,
                tagDef,
                serializeRef,
                staticTryDeserializeDef,
                staticDeserializeDef,
                matchOptDef,
                matchDef,
                castHelpersDef,
                helpersDef,
                RelationsGen.GenEqRelations(typeName, isRecord: false, cmpPairName),
                RelationsGen.GenCmpRelations(typeName, cmpPairName),
                RelationsGen.GenGetHashCode(cmpPairName),
                YamlifierGen.GenUnionYamlifier(typeTags, typeName)
            );

            var def = Scope(
                Line($"public sealed class {typeName} : ITgType, IEquatable<{typeName}>, IComparable<{typeName}>, IComparable {{"),
                Indent(1, bodyDef),
                Line("}")
            );

            return def;
        }

        static NestedText GenTypeWithOneTag(string typeName, Signature tag) {
            var tagBody = GenTypeTagBody(typeName, typeName, tag);

            var serializeRef = Scope(
                Line("void ITgSerializable.Serialize(BinaryWriter bw) {"),
                IndentedScope(1,
                    Line("WriteUint(bw, TypeNumber);"),
                    Line("((ITgTypeTag) this).SerializeTag(bw);")
                ),
                Line("}")
            );

            var staticTryDeserializeDef = Scope(
                Line($"internal static Option<{typeName}> TryDeserialize(uint typeNumber, BinaryReader br) =>"),
                Indent(1, Line(Concat(
                    "typeNumber == TypeNumber",
                    " ? Prelude.Some(DeserializeTag(br))",
                    " : Prelude.None;"
                )))
            );

            var staticDeserializeDef = Scope(
                Line($"internal static {typeName} Deserialize(BinaryReader br) {{"),
                IndentedScope(1,
                    Line("var typeNumber = ReadUint(br);"),
                    Line(Concat(
                        "return TryDeserialize(typeNumber, br).IfNone(() => ",
                        Concat(
                            "throw TgRpcDeserializeException.UnexpectedTypeNumber(actual: typeNumber, expected: new[] { ",
                            "TypeNumber",
                            " })"
                        ),
                        ");"
                    ))
                ),
                Line("}")
            );

            var bodyDef = Scope(Environment.NewLine + Environment.NewLine,
                tagBody,
                serializeRef,
                staticTryDeserializeDef,
                staticDeserializeDef
            );

            return Scope(
                Line($"public sealed record {typeName} : ITgType, ITgTypeTag, IComparable<{typeName}>, IComparable {{"),
                Indent(1, bodyDef),
                Line("}")
            );
        }

        static NestedText GenType(string typeName, Arr<Signature> tags) {
            return tags.Count > 1 ? GenTypeWithManyTags(typeName, tags) : GenTypeWithOneTag(typeName, tags[0]);
        }

        public static Func<NestedText, NestedText> WrapIntoNamespace(string nameSpace) => def => Scope(
            Header,
            Line(""),
            Line($"namespace {nameSpace} {{"),
            Indent(1, def),
            Line("}")
        );

        public static IEnumerable<GenFile> GenTypes(Scheme scheme) => scheme.Types
            .GroupBy(x => x.ResultType)
            .Choose(x => x
                .Key.Match(_: () => None, typeRef: Some)
                .Map(custom => (name: custom.Name, tags: x.ToArr()))
            )
            .AsParallel()
            .Map(type => {
                var (rawNameSpace, name) = TgSchemeNormalizer.SplitName(type.name);
                var nameSpace = string.Join(".", new[] { "Telega.Rpc.Dto.Types", rawNameSpace }.Choose(identity));
                var def = GenType(name, type.tags).Apply(WrapIntoNamespace(nameSpace));
                var content = def.ToSome().Apply(xs => NestedTextStringifier.Stringify(xs));
                return new GenFile(nameSpace, name, content);
            });

        public static IEnumerable<GenFile> GenFunctions(Scheme scheme) => scheme.Functions
            .AsParallel()
            .Map(func => {
                var (rawNameSpace, name) = TgSchemeNormalizer.SplitName(func.Name);
                var nameSpace = string.Join(".", new[] { "Telega.Rpc.Dto.Functions", rawNameSpace }.Choose(identity));
                var def = GenFunc(func, name).Apply(WrapIntoNamespace(nameSpace));
                var content = def.ToSome().Apply(xs => NestedTextStringifier.Stringify(xs));
                return new GenFile(nameSpace, name, content);
            });

        public static GenFile GenSchemeInfo(Scheme scheme) {
            var layerVersion = scheme.LayerVersion.GetOrThrow(() => new Exception("Scheme does not have layer version"));
            const string nameSpace = "Telega.Rpc.Dto";
            const string name = "SchemeInfo";
            var infoDef = Scope(
                Line($"static class {name} {{"),
                IndentedScope(1,
                    Line($"public const int LayerVersion = {layerVersion};")
                ),
                Line("}")
            ).Apply(WrapIntoNamespace(nameSpace));
            var content = NestedTextStringifier.Stringify(infoDef);

            return new GenFile(nameSpace, name, content);
        }
    }
}