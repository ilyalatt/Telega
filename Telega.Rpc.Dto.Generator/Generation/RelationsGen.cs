using System;
using LanguageExt;
using Telega.Rpc.Dto.Generator.TextModel;
using Telega.Rpc.Dto.Generator.TgScheme;
using static Telega.Rpc.Dto.Generator.TextModel.TextAbbreviations;
using static Telega.Rpc.Dto.Generator.TextModel.NestedTextAbbreviations;

namespace Telega.Rpc.Dto.Generator.Generation
{
    static class RelationsGen
    {
        public static NestedText GenEqRelations(string typeName, Text cmpBy)
        {
            var equality = Concat(
                "public bool Equals(",
                typeName,
                " other) => !ReferenceEquals(other, null) && (ReferenceEquals(this, other) || ",
                cmpBy, " == other.", cmpBy, ");"
            ).Apply(Line);
            var equalityLegacy = Concat(
                "public override bool Equals(object other) => other is ",
                typeName,
                " x && Equals(x);"
            ).Apply(Line);
            var equalityOps = Scope(
                Line($"public static bool operator ==({typeName} x, {typeName} y) => x?.Equals(y) ?? ReferenceEquals(y, null);"),
                Line($"public static bool operator !=({typeName} x, {typeName} y) => !(x == y);")
            );
            return Scope(equality, equalityLegacy, equalityOps);
        }

        public static NestedText GenCmpRelations(string typeName, Text cmpBy)
        {
            var cmp = Concat(
                "public int CompareTo(",
                typeName,
                " other) => ReferenceEquals(other, null)",
                " ? throw new ArgumentNullException(nameof(other))",
                " : ReferenceEquals(this, other)",
                " ? 0",
                " : ", cmpBy, ".CompareTo(other.", cmpBy, ")", ";"
            ).Apply(Line);
            var cmpLegacy = Concat(
                "int IComparable.CompareTo(object other) => other is ",
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

        public static NestedText GenToString(Text by) =>
            Concat("public override string ToString() => ", by, ";").Apply(Line);

        public static NestedText GenRelations(string typeName, Arr<Arg> args)
        {
            var cmpTupleName = String("CmpTuple");


            Text EmPt(Text text) => Concat("(", text, ")");
            Func<Arr<Text>, Text> Tuple(bool type) => xs =>
                xs.Count == 0 ? (type ? "Unit" : "Unit.Default") :
                xs.Count == 1 ? xs[0]
                : Join(", ", xs).Apply(EmPt);

            Text ArgsTuple(bool type, Func<Arg, Text> argStr) => args
                .Map(argStr)
                .Apply(Tuple(type));

            var argsTuple = ArgsTuple(false, x => x.Name);
            var argsTupleType = ArgsTuple(true, x => TgTypeConverter.ConvertArgType(x));

            var cmpTuple = Scope(
                Line(Concat(argsTupleType, " ", cmpTupleName, " =>")),
                Indent(1, Line(Concat(argsTuple, ";")))
            );


            var argsInterpolationStr = args
                .Map(x => Concat(x.Name, ": ", "{", x.Name, "}"))
                .Apply(xs => Join(", ", xs))
                .Apply(x => Concat("$\"(", x, ")\""));

            return Scope(Environment.NewLine + Environment.NewLine,
                cmpTuple,
                GenEqRelations(typeName, cmpTupleName),
                GenCmpRelations(typeName, cmpTupleName),
                GenGetHashCode(cmpTupleName),
                GenToString(argsInterpolationStr)
            );
        }
    }
}
