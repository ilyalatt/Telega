using System.Collections.Generic;
using System.Linq;
using static LanguageExt.Prelude;

namespace Telega.Rpc.Dto.Generator.TextModel {
    static class TextAbbreviations {
        public static Text String(string s) => Text.CreateString(s);
        public static Text Join(Text separator, params Text[] xs) => Text.CreateScope(xs.ToArr(), separator);
        public static Text Concat(params Text[] xs) => Join("", xs);
        public static Text Join(Text separator, IEnumerable<Text> xs) => Join(separator, xs.ToArray());
        public static Text Concat(IEnumerable<Text> xs) => Join("", xs);
        public static Text Join(Text separator, params IEnumerable<Text>[] xs) => Join(separator, xs.Bind(identity));
        public static Text Concat(params IEnumerable<Text>[] xs) => Join("", xs);
    }
}