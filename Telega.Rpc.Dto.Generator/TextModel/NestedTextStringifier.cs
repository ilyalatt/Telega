using System.Text;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Telega.Rpc.Dto.Generator.TextModel
{
    static class NestedTextStringifier
    {
        public static string Stringify(Some<NestedText> someText, int spacesPerIndent = 4)
        {
            var sb = new StringBuilder();

            void TextRec(Text text) => text.Match(
                str: x =>
                {
                    sb.Append(x.Value);
                    return unit;
                },
                scope: x =>
                {
                    x.Values.HeadOrNone().Iter(TextRec);
                    x.Values.Skip(1).Iter(t =>
                    {
                        TextRec(x.Separator);
                        TextRec(t);
                    });
                    return unit;
                }
            );

            Unit Rec(NestedText nestedText, int indentation) => nestedText.Match(
                indent: x => Rec(x.Text, indentation + x.Offset),
                line: x =>
                {
                    sb.Append(' ', spacesPerIndent * indentation);
                    TextRec(x.Value);
                    return unit;
                },
                scope: x =>
                {
                    x.Values.HeadOrNone().Iter(head => Rec(head, indentation));
                    x.Values.Skip(1).Iter(t =>
                    {
                        TextRec(x.Separator);
                        Rec(t, indentation);
                    });
                    return unit;
                }
            );

            Rec(someText, 0);
            return sb.ToString();
        }
    }
}
