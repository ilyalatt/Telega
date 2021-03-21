using System;
using LanguageExt;

namespace Telega.Rpc.Dto.Generator.TextModel {
    class Text {
        public record String {
            public string Value { get; init; }

            public String(Some<string> value) =>
                Value = value;
        }

        public record Scope {
            public Arr<Text> Values { get; init; }
            public Text Separator { get; init; }

            public Scope(Some<Arr<Text>> values, Some<Text> separator) =>
                (Values, Separator) = (values, separator);
        }

        readonly object _tag;
        Text(object tag) => _tag = tag;

        public static Text CreateString(Some<string> value) => new(new String(value));

        public static Text CreateScope(Some<Arr<Text>> values, Some<Text> separator) =>
            new(new Scope(values, separator));


        public static implicit operator Text(string value) => CreateString(value);


        public T Match<T>(
            Func<T> _,
            Func<String, T> str = null,
            Func<Scope, T> scope = null
        ) {
            if (_ == null) {
                throw new ArgumentNullException(nameof(_));
            }

            return _tag switch
            {
                String x when str != null => str(x),
                Scope x when scope != null => scope(x),
                _ => _(),
            };
        }

        public T Match<T>(
            Func<String, T> str,
            Func<Scope, T> scope
        ) => Match(
            _: () => throw new Exception("WTF"),
            str: str ?? throw new ArgumentNullException(nameof(str)),
            scope: scope ?? throw new ArgumentNullException(nameof(scope))
        );
    }
}