using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using LanguageExt.SomeHelp;
using Telega.Rpc.Dto.Generator.TgScheme;
using static LanguageExt.Prelude;
using StringHashSet = System.Collections.Generic.HashSet<string>;

namespace Telega.Rpc.Dto.Generator.Generation
{
    static class TgSchemeNormalizer
    {
        static string UpperFirst(string s) => s[0].Apply(char.ToUpper).Apply(fc => fc + s.Substring(1));

        static string LowerCapsAndUpperSomeCases(string origStr) =>
            (str: origStr, subs: List<string>()).Generate(s =>
            {
                var len = s.str.Length;
                if (len == 0) return None;
                if (len == 1) return ("", s.subs.Add(s.str));
                var subLen =
                    char.IsDigit(s.str[0])
                    ? s.str.TakeWhile(char.IsDigit).Count()
                    : char.IsUpper(s.str[0]) && char.IsUpper(s.str[1])
                        ? s.str.TakeWhile(char.IsUpper).Count().Apply(x => x == len || char.IsDigit(s.str[x]) ? x : x - 1)
                        : s.str.Map((i, x) => (i, x)).Skip(1).Find(t => char.IsUpper(t.x) || char.IsDigit(t.x)).Map(t => t.i).IfNone(len);
                var sub = s.str.Substring(0, subLen);
                var rest = s.str.Substring(subLen);
                return (rest, s.subs.Add(sub));
            })
            .Last().Apply(s => s.subs)
            .Map(sub => sub.Length > 1 ? char.ToUpper(sub[0]) + sub.Substring(1).ToLower() : sub).AsEnumerable().Apply(string.Concat);

        static string NormalizeName(string name) => name
            .Split('_').Map(UpperFirst).Apply(string.Concat)
            .Split('.').Map(UpperFirst).Apply(xs => string.Join(".", xs))
            .Apply(LowerCapsAndUpperSomeCases);

        public static (Option<string>, string) SplitName(string typeName)
        {
            var nameSpace = typeName.LastIndexOf('.').Apply(Some).Filter(x => x != -1).Map(x => typeName.Substring(0, x));
            var name = nameSpace.Map(x => x.Length).Map(x => x + 1).Map(typeName.Substring).IfNone(typeName);
            return (nameSpace, name);
        }


        // TODO: compute it from a scheme
        static readonly StringHashSet NamespaceConflicts = new()
        {
            "Updates"
        };

        static TgType Normalize(TgType type) => type.Match(
            primitive: _ => type,
            typeRef: x => x.Name
                .Apply(NormalizeName)
                .Apply(s => NamespaceConflicts.Contains(s) ? s + "Type" : s)
                .Apply(SomeExt.ToSome).Apply(TgType.OfTypeRef),
            vector: x => x.Type.Apply(Normalize).Apply(SomeExt.ToSome).Apply(TgType.OfVector)
        );

        static Flag Normalize(Flag flag) => new(
            argName: flag.ArgName.Apply(NormalizeName),
            bit: flag.Bit
        );

        static ArgKind Normalize(ArgKind argKind) => argKind.Match(
            required: _ => argKind,
            optional: x => x.Flag.Apply(Normalize).Apply(SomeExt.ToSome).Apply(ArgKind.OfOptional),
            flags: _ => argKind
        );

        static Arg Normalize(Arg arg) => new(
            name: arg.Name.Apply(NormalizeName),
            type: arg.Type.Apply(Normalize),
            kind: arg.Kind.Apply(Normalize)
        );

        static Signature Normalize(
            Signature signature,
            Func<string, Func<string, string>> nameProcessor = null // typeName -> argName -> newArgName
        ) {
            var resultType = signature.ResultType.Apply(Normalize);
            var resultTypeStrProvider = fun(() =>
                resultType.Match(_: () => throw new Exception("WTF"), typeRef: identity).Name
                .Apply(SplitName).Item2
            );
            var name = signature.Name.Apply(NormalizeName).Apply(s =>
                nameProcessor.Apply(Optional)
                .Map(f => resultTypeStrProvider()
                .Apply(f))
                .Map(f => f(s)).IfNone(s)
            );
            return new Signature(
                name: name,
                typeNumber: signature.TypeNumber,
                args: signature.Args.Map(Normalize),
                resultType: resultType
            );
        }

        static IEnumerable<int> UpperChars(string s) =>
            s.Map((i, x) => (i, x)).Filter(t => char.IsUpper(t.x)).Map(t => t.i);

        static IEnumerable<string> SplitByUpperChars(string s) =>
            new[] { 0 }.Concat(UpperChars(s)).Concat(new[] { s.Length })
            .Pairwise()
            .Filter(t => t.Item1 != t.Item2)
            .Map(t => s.Substring(t.Item1, t.Item2 - t.Item1));


        static int CasedLcpLen(string test, string s) => test
            .Apply(SplitByUpperChars)
            .Zip(SplitByUpperChars(s)).TakeWhile(t => t.Item1 == t.Item2)
            .Sum(t => t.Item1.Length);

        static int CasedLcsLen(string test, string s) => test
            .Apply(SplitByUpperChars)
            .Reverse()
            .Zip(SplitByUpperChars(s).Reverse()).TakeWhile(t => t.Item1 == t.Item2)
            .Sum(t => t.Item1.Length);

        static Func<string, string> RemoveLcp(string test) => s =>
            s.Substring(CasedLcpLen(test, s));
        static Func<string, string> RemoveLcs(string test) => s =>
            s.Substring(0, s.Length - CasedLcsLen(test, s));

        static Signature NormalizeType(Signature signature) => Normalize(
            signature,
            typeName => rawName => rawName
                .Apply(SplitName).Apply(t => t.Item2)
                .Apply(name =>
                    name.Apply(RemoveLcp(typeName)).Apply(RemoveLcs(typeName))
                    .Apply(Optional).Filter(x => x.Length != name.Length)
                    .IfNone(() =>
                        name.IndexOf(typeName, StringComparison.Ordinal).Apply(Optional).Filter(x => x != -1)
                        .Map(idx => name.Remove(idx, typeName.Length))
                        .IfNone(name)
                    )
                )
                .Apply(name => name + "Tag")
        );

        static Signature NormalizeFunc(Signature signature) =>
            Normalize(signature);

        static readonly StringHashSet IgnoredTypes = new()
        {
            "Null",
            "True",
            "Bool"
            // "vector" is ignored by the parser
        };

        static Scheme Normalize(Scheme scheme) => new(
            layerVersion: scheme.LayerVersion,
            types: scheme.Types
                .Filter(x => x.ResultType.Match(
                    _: () => true,
                    primitive: _ => false,
                    typeRef: c => !IgnoredTypes.Contains(c.Name)
                ))
                .Map(NormalizeType),
            functions: scheme.Functions.Map(NormalizeFunc)
        );

        public static Scheme Normalize(Some<Scheme> someScheme) =>
            Normalize(someScheme.Value);
    }
}
