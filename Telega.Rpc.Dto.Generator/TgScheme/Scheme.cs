using System;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;

namespace Telega.Rpc.Dto.Generator.TgScheme {
    enum PrimitiveType {
        Int,
        Uint,
        Long,
        Double,
        String,
        Bytes,
        True,
        Bool,
        Int128,
        Int256
    }

    class TgType {
        public record Primitive {
            public PrimitiveType Type { get; init; }
            public Primitive(PrimitiveType type) => Type = type;
        }

        public record Vector {
            public TgType Type { get; init; }
            public Vector(Some<TgType> type) => Type = type;
        }

        public record TypeRef {
            public string Name { get; init; }
            public TypeRef(Some<string> name) => Name = name;
        }


        readonly object _tag;
        TgType(object tag) => _tag = tag;

        bool Equals(TgType other) => other is not null && _tag.Equals(other._tag);
        public override bool Equals(object obj) => obj is TgType x && Equals(x);
        public static bool operator ==(TgType a, TgType b) => a?.Equals(b) ?? b is null;
        public static bool operator !=(TgType a, TgType b) => !(a == b);
        public override int GetHashCode() => _tag.GetHashCode();
        public override string ToString() => _tag.ToString();

        public static TgType OfPrimitive(PrimitiveType type) => new(new Primitive(type));
        public static TgType OfVector(Some<TgType> type) => new(new Vector(type));
        public static TgType OfTypeRef(Some<string> name) => new(new TypeRef(name));


        public T Match<T>(
            Func<T> _,
            Func<Primitive, T> primitive = null,
            Func<Vector, T> vector = null,
            Func<TypeRef, T> typeRef = null
        ) {
            if (_ == null) {
                throw new ArgumentNullException(nameof(_));
            }

            return _tag switch
            {
                Primitive x when primitive != null => primitive(x),
                Vector x when vector != null => vector(x),
                TypeRef x when typeRef != null => typeRef(x),
                _ => _(),
            };
        }

        public T Match<T>(
            Func<Primitive, T> primitive,
            Func<Vector, T> vector,
            Func<TypeRef, T> typeRef
        ) => Match(
            _: () => throw new Exception("WTF"),
            primitive: primitive ?? throw new ArgumentNullException(nameof(primitive)),
            vector: vector ?? throw new ArgumentNullException(nameof(vector)),
            typeRef: typeRef ?? throw new ArgumentNullException(nameof(typeRef))
        );
    }

    record Flag {
        public string ArgName { get; init; }
        public int Bit { get; init; }

        public Flag(Some<string> argName, int bit) {
            ArgName = argName;
            Bit = bit;
        }
    }

    // TODO: enhance the model
    class ArgKind {
        public record Required { }

        public record Optional {
            public Flag Flag { get; init; }
            public Optional(Some<Flag> flag) => Flag = flag;
        }

        public record Flags { }

        readonly object _tag;
        ArgKind(object tag) => _tag = tag;

        bool Equals(ArgKind other) => other is not null && _tag.Equals(other._tag);
        public override bool Equals(object obj) => obj is ArgKind x && Equals(x);
        public override int GetHashCode() => _tag.GetHashCode();
        public override string ToString() => _tag.ToString();

        public static ArgKind OfRequired() => new(new Required());
        public static ArgKind OfOptional(Some<Flag> flag) => new(new Optional(flag));
        public static ArgKind OfFlags() => new(new Flags());


        public T Match<T>(
            Func<T> _,
            Func<Required, T> required = null,
            Func<Optional, T> optional = null,
            Func<Flags, T> flags = null
        ) {
            if (_ == null) {
                throw new ArgumentNullException(nameof(_));
            }

            return _tag switch
            {
                Required x when required != null => required(x),
                Optional x when optional != null => optional(x),
                Flags x when flags != null => flags(x),
                _ => _(),
            };
        }

        public T Match<T>(
            Func<Required, T> required,
            Func<Optional, T> optional,
            Func<Flags, T> flags
        ) => Match(
            _: () => throw new Exception("WTF"),
            required: required ?? throw new ArgumentNullException(nameof(required)),
            optional: optional ?? throw new ArgumentNullException(nameof(optional)),
            flags: flags ?? throw new ArgumentNullException(nameof(flags))
        );
    }

    record Arg {
        public string Name { get; init; }
        public TgType Type { get; init; }
        public ArgKind Kind { get; init; }

        public Arg(Some<string> name, Some<TgType> type, Some<ArgKind> kind) {
            Name = name;
            Type = type;
            Kind = kind;
        }
    }

    record Signature {
        public string Name { get; init; }
        public int TypeNumber { get; init; }
        public Arr<Arg> Args { get; init; }
        public TgType ResultType { get; init; }

        public Signature(
            Some<string> name,
            Some<int> typeNumber,
            Some<Arr<Arg>> args,
            Some<TgType> resultType
        ) {
            Name = name;
            TypeNumber = typeNumber;
            Args = args.Value;
            ResultType = resultType.Value;
        }
    }

    record Scheme {
        public Option<int> LayerVersion { get; init; }
        public Arr<Signature> Types { get; init; }
        public Arr<Signature> Functions { get; init; }

        public Scheme(
            Option<int> layerVersion,
            Some<Arr<Signature>> types,
            Some<Arr<Signature>> functions
        ) {
            LayerVersion = layerVersion;
            Types = types.Value;
            Functions = functions.Value;
        }

        public static Scheme Merge(Scheme a, Scheme b) {
            if (a.LayerVersion.IsSome && b.LayerVersion.IsSome) {
                if (a.LayerVersion.ValueUnsafe() != b.LayerVersion.ValueUnsafe()) {
                    throw new Exception("Can not merge schemas with different layer versions");
                }
            }

            return new Scheme(
                a.LayerVersion || b.LayerVersion,
                a.Types + b.Types,
                a.Functions + b.Functions
            );
        }
    }
}