using System;
using LanguageExt;

namespace Telega.Rpc.Dto.Generator.TextModel {
    class NestedText {
        public class Indent {
            public int Offset { get; }
            public NestedText Text { get; }

            public Indent(int offset, Some<NestedText> text) {
                Offset = offset;
                Text = text;
            }
        }

        public class Line {
            public Text Value { get; }

            public Line(Some<Text> value) => Value = value;
        }

        public class Scope {
            public Arr<NestedText> Values { get; }
            public Text Separator { get; }

            public Scope(Some<Arr<NestedText>> values, Some<Text> separator) {
                Values = values;
                Separator = separator;
            }
        }

        readonly object _tag;
        NestedText(object tag) => _tag = tag;

        public static NestedText CreateIndent(int offset, Some<NestedText> text) => new(new Indent(offset, text));
        public static NestedText CreateLine(Some<Text> value) => new(new Line(value));

        public static NestedText CreateScope(Some<Arr<NestedText>> values, Some<Text> separator) =>
            new(new Scope(values, separator));


        public T Match<T>(
            Func<T> _,
            Func<Indent, T> indent = null,
            Func<Line, T> line = null,
            Func<Scope, T> scope = null
        ) {
            if (_ == null) {
                throw new ArgumentNullException(nameof(_));
            }

            return _tag switch
            {
                Indent x when indent != null => indent(x),
                Line x when line != null => line(x),
                Scope x when scope != null => scope(x),
                _ => _(),
            };
        }

        public T Match<T>(
            Func<Indent, T> indent,
            Func<Line, T> line,
            Func<Scope, T> scope
        ) => Match(
            _: () => throw new Exception("WTF"),
            indent: indent ?? throw new ArgumentNullException(nameof(indent)),
            line: line ?? throw new ArgumentNullException(nameof(line)),
            scope: scope ?? throw new ArgumentNullException(nameof(scope))
        );
    }
}