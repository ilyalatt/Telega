using LanguageExt;

namespace Telega.Rpc.Dto.Generator.Generation
{
    static class Helpers
    {
        static readonly System.Collections.Generic.HashSet<string> CsKeywords = new System.Collections.Generic.HashSet<string>() {
            "null",
            "out",
            "long",
            "private",
            "public",
            "static",
            "true",
            "params",
            "default"
        };

        public static string LowerFirst(string s) => s[0]
            .Apply(char.ToLower).Apply(fc => fc + s.Substring(1))
            .Apply(x => CsKeywords.Contains(x) ? "@" + x : x);

        public static string TypeNumber(int typeNumber) =>
            $"0x{typeNumber:x8}";
    }
}
