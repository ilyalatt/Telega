using System;
using System.Collections.Generic;
using LanguageExt;
using Telega.Rpc.Dto.Generator.TextModel;
using Telega.Rpc.Dto.Generator.TgScheme;
using static Telega.Rpc.Dto.Generator.TextModel.TextAbbreviations;
using static Telega.Rpc.Dto.Generator.TextModel.NestedTextAbbreviations;
using static LanguageExt.Prelude;

namespace Telega.Rpc.Dto.Generator.Generation
{
    static class SerializerGen
    {
        public static NestedText GenSerializer(Arr<Arg> args, Option<int> typeNumber, string funcName)
        {
            Text GenSerializer(TgType type) => type.Match(
                primitive: x => $"Write{x.Type}",
                typeRef: x => "WriteSerializable",
                vector: x => Concat(
                    $"WriteVector<{TgTypeConverter.ConvertType(x.Type)}>(",
                    GenSerializer(x.Type),
                    ")"
                )
            );

            Option<Text> GenNonFlagArgSerializer(Arg arg) =>
                arg.Kind.Match(
                    _: () => throw new("WTF"),
                    required: _ => GenSerializer(arg.Type).Apply(Some),
                    optional: x => arg.Type == TgType.OfPrimitive(PrimitiveType.True) ? None : Concat(
                        $"WriteOption<{TgTypeConverter.ConvertType(arg.Type)}>(",
                        GenSerializer(arg.Type),
                        ")"
                    ).Apply(Some)
                ).Map(s =>
                    Concat($"Write({arg.Name}, bw, ", s, ");")
                );

            Text GenMaskSerializer(IEnumerable<(string, int)> maskArgs) =>
                maskArgs.ToArr()
                .Apply(Optional).Filter(xs => xs.Count > 0)
                .Map(xs => xs
                    .Map(x => $"MaskBit({x.Item2}, {x.Item1})").Map(String).Reduce((x, y) => Concat(x, " | ", y))
                )
                .IfNone("0")
                .Apply(mask => Concat("Write(", mask, ", bw, WriteInt);"));

            Option<Text> GenArgSerializer(Arg arg) => arg.Kind.Match(
                _: () => GenNonFlagArgSerializer(arg),
                flags: _ =>
                    args.Choose(x => x.Kind.Match(
                        _: () => None,
                        optional: optional => ($"{x.Name}", optional.Flag.Bit).Apply(Some)
                    )).Apply(GenMaskSerializer)
            );

            var body = args.Choose(GenArgSerializer).Map(Line).Scope().Apply(s => typeNumber
                .Map(Helpers.TypeNumber).Map(x => Line($"WriteUint(bw, {x});")).Map(typeNumSer => Scope(typeNumSer, s))
                .IfNone(s)
            );
            var def = Scope(
                Line($"void {funcName}(BinaryWriter bw)"),
                Line("{"),
                Indent(1, body),
                Line("}")
            );
            return def;
        }

        public static Text GenTypeDeserializer(TgType type) =>
            type.Match(
                primitive: x => Concat("Read", x.Type.ToString()),
                typeRef: x => Concat("T.", x.Name, ".Deserialize"),
                vector: x => Concat(
                    "ReadVector(",
                    GenTypeDeserializer(x.Type),
                    ")"
                )
            );

        public static NestedText GenTypeTagDeserialize(string tagName, Arr<Arg> args)
        {
            Text GenArgDeserializer(Arg arg) =>
                arg.Kind.Match(
                    required: _ => GenTypeDeserializer(arg.Type),
                    flags: _ => GenTypeDeserializer(arg.Type),
                    optional: x => Concat(
                        "ReadOption(",
                        Join(", ",
                            new Text[]
                            {
                                Helpers.LowerFirst(x.Flag.ArgName),
                                x.Flag.Bit.ToString()
                            },
                            arg.Type == TgType.OfPrimitive(PrimitiveType.True) ? None : Some(GenTypeDeserializer(arg.Type))
                        ),
                        ")"
                    )
                ).Apply(s =>
                    Concat($"var {Helpers.LowerFirst(arg.Name)} = Read(br, ", s, ");")
                );

            var argsWithoutFlags = args.Filter(x => x.Kind.Match(_: () => true, flags: _ => false));
            var body = Scope(
                args.Map(GenArgDeserializer).Map(Line).Scope(),
                Line(Concat(
                    $"return new {tagName}(",
                    argsWithoutFlags.Map(x => x.Name).Map(Helpers.LowerFirst).Map(String).Apply(xs => Join(", ", xs)),
                    ");"
                ))
            );
            var def = Scope(
                Line($"internal static {tagName} DeserializeTag(BinaryReader br)"),
                Line("{"),
                Indent(1, body),
                Line("}")
            );
            return def;
        }
    }
}
