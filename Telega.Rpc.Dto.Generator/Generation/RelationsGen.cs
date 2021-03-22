using System;
using LanguageExt;
using Telega.Rpc.Dto.Generator.TextModel;
using Telega.Rpc.Dto.Generator.TgScheme;
using static Telega.Rpc.Dto.Generator.TextModel.TextAbbreviations;
using static Telega.Rpc.Dto.Generator.TextModel.NestedTextAbbreviations;

namespace Telega.Rpc.Dto.Generator.Generation {
    static class RelationsGen {
        public static NestedText GenEqRelations(string typeName, bool isRecord, Text cmpBy) {
            var equality = Concat(
                "public bool Equals(",
                $"{typeName}?",
                " other) => other is not null && (ReferenceEquals(this, other) || ",
                cmpBy, " == other!.", cmpBy, ");"
            ).Apply(Line);
            var equalityLegacy = Concat(
                "public override bool Equals(object? other) => other is ",
                $"{typeName}",
                " x && Equals(x);"
            ).Apply(Line);
            var equalityOps = Scope(
                Line($"public static bool operator ==({typeName}? x, {typeName}? y) => x?.Equals(y) ?? y is null;"),
                Line($"public static bool operator !=({typeName}? x, {typeName}? y) => !(x == y);")
            );
            return isRecord
                ? equality
                : Scope(equality, equalityLegacy, equalityOps);
        }

        public static NestedText GenCmpRelations(string typeName, Text cmpBy) {
            var cmp = Concat(
                "public int CompareTo(",
                typeName,
                " other) => other is null",
                " ? throw new ArgumentNullException(nameof(other))",
                " : ReferenceEquals(this, other)",
                " ? 0",
                " : ", cmpBy, ".CompareTo(other.", cmpBy, ")", ";"
            ).Apply(Line);
            var cmpLegacy = Concat(
                "int IComparable.CompareTo(object? other) => other is ",
                typeName,
                " x ? CompareTo(x) : throw new ArgumentException(\"bad type\", nameof(other));"
            ).Apply(Line);
            var cmpOps = Scope(new[] { "<=", "<", ">", ">=" }
               .Map(op => Concat(
                    "public static bool operator ", op, "(", typeName, " x, ", typeName, " y) => ",
                    "x.CompareTo(y) ", op, " 0;"
                ))
               .Map(Line)
            );

            return Scope(cmp, cmpLegacy, cmpOps);
        }

        public static NestedText GenGetHashCode(Text by) =>
            Concat("public override int GetHashCode() => ", by, ".GetHashCode();").Apply(Line);

        public static NestedText GenRelations(string typeName, bool isRecord, Arr<Arg> args) {
            var cmpTupleName = String("CmpTuple");


            Text EmPt(Text text) => Concat("(", text, ")");
            
            Func<Arr<Text>, Text> Tuple(bool type) => xs =>
                xs.Count == 0 ? (type ? "Unit" : "Unit.Default") :
                xs.Count == 1 ? xs[0] :
                Join(", ", xs).Apply(EmPt);

            Text ArgsTuple(bool type, Func<Arg, Text> argStr) => args
               .Map(argStr)
               .Apply(Tuple(type));

            var argsTuple = ArgsTuple(false, x =>
                x.Type.Match(vector: _ => true, _: () => false)
                ? x.Kind.Match(optional: _ => true, _: () => false)
                    ? $"{x.Name}.Map(ListCmp.Wrap)"
                    : $"ListCmp.Wrap({x.Name})"
                : $"{x.Name}"
            );
            var argsTupleType = ArgsTuple(true, x => TgTypeConverter.ConvertArgType(x, cmpWrapper: true));

            var cmpTuple = Scope(
                Line("[System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]"),
                Line(Concat(argsTupleType, " ", cmpTupleName, " =>")),
                Indent(1, Line(Concat(argsTuple, ";")))
            );

            return Scope(Environment.NewLine + Environment.NewLine,
                cmpTuple,
                GenEqRelations(typeName, isRecord, cmpTupleName),
                GenCmpRelations(typeName, cmpTupleName),
                GenGetHashCode(cmpTupleName)
            );
        }
    }
}