using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using LanguageExt;
using LanguageExt.SomeHelp;
using static LanguageExt.Prelude;

namespace Telega.Rpc.Dto.Generator.TgScheme {
    static class TgSchemeParser {
        static TgSchemeParserException Exception(string msg)
            => new(msg);

        static Func<Exception> Ex(string msg) =>
            () => Exception(msg);

        static Option<int> ParseHexInt(string s) =>
            int.TryParse(s, NumberStyles.HexNumber, null, out var v) ? Some(v) : None;

        enum SectionType {
            Types,
            Functions
        }

        static IEnumerable<(SectionType, string)> SplitBySections(string s) {
            var res = new List<(SectionType, string)>();

            var lastIdx = 0;
            var lastType = SectionType.Types;
            while (true) {
                const string sectionMark = "---";
                var sectionStartIdx = s.IndexOf(sectionMark, lastIdx, StringComparison.Ordinal);
                if (sectionStartIdx == -1) {
                    res.Add((lastType, s[lastIdx..]));
                    break;
                }

                var sectionStartShiftedIdx = sectionStartIdx + sectionMark.Length;

                var sectionEndIdx = s.IndexOf(
                    sectionMark,
                    sectionStartShiftedIdx,
                    StringComparison.Ordinal
                );
                if (sectionEndIdx == -1) {
                    throw Exception("can not find a closing section mark");
                }

                var sectionEndShiftedIdx = sectionEndIdx + sectionMark.Length;

                var sectionName = s[sectionStartShiftedIdx..sectionEndIdx];
                var sectionType = sectionName ==
                    "types" ? SectionType.Types
                    : sectionName == "functions" ? SectionType.Functions
                    : throw Exception("unknown section type");

                res.Add((lastType, s[lastIdx..sectionStartIdx]));

                lastIdx = sectionEndShiftedIdx;
                lastType = sectionType;
            }

            return res;
        }
        
        static Option<int> ExtractLayerVersion(string s) =>
            new Regex(@"\/\/ LAYER (.+)$").Match(s).Groups[1].Value
                .Apply(Optional).Filter(x => x.Length > 0)
                .Map(parseInt)
                .Map(x => x.GetOrThrow(Ex("can not parse a version of '// LAYER={version}'")));

        const string FlagMarker = @"^([\w\d]+)\.(\d+)\?(.+)$";

        static Option<(string, Flag)> ParseFlag(string s) =>
            s.Apply(Optional)
                .Map(x => Regex.Match(x, FlagMarker))
                .Filter(m => m.Success)
                .Map(m => parseInt(m.Groups[2].Value)
                        .Map(idx => (m.Groups[3].Value, new Flag(m.Groups[1].Value, idx)))
                        .GetOrThrow(Ex("can not parse a flag bit")));

        static TgType ParseType(string s) {
            const string vector = "vector";
            string GetVectorType(string ss) => ss[vector.Length..].Apply(x => x[1..^1]);
            if (s.StartsWith(vector, true, null)) {
                return s.Apply(GetVectorType).Apply(ParseType).ToSome().Apply(TgType.OfVector);
            }

            PrimitiveType? TryParsePrimitive() {
                return s.ToLower() switch {
                    "#" or "int" => PrimitiveType.Int,
                    "uint" => PrimitiveType.Uint,
                    "long" => PrimitiveType.Long,
                    "double" => PrimitiveType.Double,
                    "string" => PrimitiveType.String,
                    "bytes" => PrimitiveType.Bytes,
                    "true" => PrimitiveType.True,
                    "bool" => PrimitiveType.Bool,
                    "int128" => PrimitiveType.Int128,
                    "int256" => PrimitiveType.Int256,
                    _ => null,
                };
            }

            return TryParsePrimitive().ToOption().Map(TgType.OfPrimitive).IfNone(() => TgType.OfTypeRef(s));
        }

        static Arg ParseArg(string s) {
            var spl = s.Split(':')
                .Apply(Optional).Filter(x => x.Length == 2).GetOrThrow(Ex("bad signature"));
            var name = spl[0];
            var typeStr = spl[1];

            var flag = ParseFlag(typeStr);
            var type = flag.Map(t => t.Item1).IfNone(typeStr).Apply(ParseType);
            var argKind = flag.Map(t => t.Item2)
                .Map(SomeExt.ToSome).Map(ArgKind.OfOptional)
                .IfNone(typeStr == "#" ? ArgKind.OfFlags(name) : ArgKind.OfRequired());
            return new Arg(name, type, argKind);
        }

        static Signature ParseSignature(string s) =>
            s.Split(new[] { '#', ' ' }, 3, StringSplitOptions.RemoveEmptyEntries)
                .Apply(Optional).Filter(x => x.Length == 3).GetOrThrow(Ex("bad signature"))
                .Apply(spl => {
                    var name = spl[0];
                    var typeNumber = spl[1].Apply(ParseHexInt).GetOrThrow(Ex("bad signature code"));
                    var spl2 = spl[2].Split('=')
                        .Map(x => x.Trim()).ToArray()
                        .Apply(Optional).Filter(x => x.Length == 2).GetOrThrow(Ex("bad signature"));
                    var argsStr = spl2[0];
                    var resultType = spl2[1].Split(';')
                        .Apply(Optional).Filter(x => x.Length == 2).GetOrThrow(Ex(""))
                        .Head()
                        .Apply(ParseType);

                    var args = argsStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Filter(x => x != "{X:Type}") // meh
                        .Map(ParseArg)
                        .ToArr();

                    return new Signature(name, typeNumber, args, resultType);
                });


        static readonly string[] IgnoredLines = {
            "int ? = Int;",
            "long ? = Long;",
            "double ? = Double;",
            "string ? = String;",
            "vector {t:Type} # [ t ] = Vector t;",
            "vector#1cb5c415 {t:Type} # [ t ] = Vector t;",
            "int128 4*[ int ] = Int128;",
            "int256 8*[ int ] = Int256;"
        };

        public static Scheme Parse(Some<string> tgScheme) {
            var sections = SplitBySections(tgScheme.Value);
            var version = ExtractLayerVersion(tgScheme);
            var signatures = sections
                .Map(t => (t.Item1, t.Item2
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Map(x => x.Trim())
                    .Filter(x => !string.IsNullOrEmpty(x))
                    .Filter(x => !x.StartsWith("//"))
                    .Filter(x => !IgnoredLines.Contains(x))
                    .Filter(x => !x.StartsWith("tls"))
                    .Map(ParseSignature)
                ))
                .GroupBy(t => t.Item1).ToDictionary(g => g.Key, g => g.Bind(x => x.Item2).ToArr());
            return new Scheme(
                version,
                signatures.TryGetValue(SectionType.Types, out var types) ? types : new(),
                signatures.TryGetValue(SectionType.Functions, out var functions) ? functions : new()
            );
        }
    }
}