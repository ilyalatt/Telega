using System;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;

namespace Telega.Rpc.Dto.Generator.TgScheme
{
    enum PrimitiveType
    {
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

    class TgType
    {
        public class Primitive : Record<Primitive>
        {
            public PrimitiveType Type { get; }
            public Primitive(PrimitiveType type) => Type = type;
        }

        public class Vector : Record<Vector>
        {
            public TgType Type { get; }
            public Vector(Some<TgType> type) => Type = type;
        }

        public class TypeRef : Record<TypeRef>
        {
            public string Name { get; }
            public TypeRef(Some<string> name) => Name = name;
        }


        readonly object _tag;
        TgType(object tag) => _tag = tag;

        bool Equals(TgType other) => !ReferenceEquals(other, null) && _tag.Equals(other._tag);
        public override bool Equals(object obj) => obj is TgType x && Equals(x);
        public static bool operator ==(TgType a, TgType b) => a?.Equals(b) ?? ReferenceEquals(b, null);
        public static bool operator !=(TgType a, TgType b) => !(a == b);
        public override int GetHashCode() => _tag.GetHashCode();
        public override string ToString() => _tag.ToString();

        public static TgType OfPrimitive(PrimitiveType type) => new TgType(new Primitive(type));
        public static TgType OfVector(Some<TgType> type) => new TgType(new Vector(type));
        public static TgType OfTypeRef(Some<string> name) => new TgType(new TypeRef(name));


        public T Match<T>(
            Func<T> _,
            Func<Primitive, T> primitive = null,
            Func<Vector, T> vector = null,
            Func<TypeRef, T> typeRef = null
        ) {
            if (_ == null) throw new ArgumentNullException(nameof(_));
            switch (_tag)
            {
                case Primitive x when primitive != null: return primitive(x);
                case Vector x when vector != null: return vector(x);
                case TypeRef x when typeRef != null: return typeRef(x);
                default: return _();
            }
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

    struct Flag
    {
        public readonly string ArgName;
        public readonly int Bit;

        public Flag(Some<string> argName, int bit)
        {
            ArgName = argName;
            Bit = bit;
        }
    }

    // TODO: enhance the model
    class ArgKind
    {
        public class Required : Record<Required> { }

        public class Optional : Record<Optional>
        {
            public Flag Flag { get; }
            public Optional(Some<Flag> flag) => Flag = flag;
        }

        public class Flags : Record<Flags> { }

        readonly object _tag;
        ArgKind(object tag) => _tag = tag;

        bool Equals(ArgKind other) => !ReferenceEquals(other, null) && _tag.Equals(other._tag);
        public override bool Equals(object obj) => obj is ArgKind x && Equals(x);
        public override int GetHashCode() => _tag.GetHashCode();
        public override string ToString() => _tag.ToString();

        public static ArgKind OfRequired() => new ArgKind(new Required());
        public static ArgKind OfOptional(Some<Flag> flag) => new ArgKind(new Optional(flag));
        public static ArgKind OfFlags() => new ArgKind(new Flags());


        public T Match<T>(
            Func<T> _,
            Func<Required, T> required = null,
            Func<Optional, T> optional = null,
            Func<Flags, T> flags = null
        ) {
            if (_ == null) throw new ArgumentNullException(nameof(_));
            switch (_tag)
            {
                case Required x when required != null: return required(x);
                case Optional x when optional != null: return optional(x);
                case Flags x when flags != null: return flags(x);
                default: return _();
            }
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

    class Arg : Record<Arg>
    {
        public string Name { get; }
        public TgType Type { get; }
        public ArgKind Kind { get; }

        public Arg(Some<string> name, Some<TgType> type, Some<ArgKind> kind)
        {
            Name = name;
            Type = type;
            Kind = kind;
        }
    }

    struct Signature
    {
        public readonly string Name;
        public readonly int TypeNumber;
        public readonly Arr<Arg> Args;
        public readonly TgType ResultType;

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

    class Scheme
    {
        public Option<int> LayerVersion { get; }
        public Arr<Signature> Types { get; }
        public Arr<Signature> Functions { get; }

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
