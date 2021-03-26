using System;
using System.Collections.Generic;
using Telega.Utils;

namespace Telega.Rpc.Dto {
    static class OptionCmp {
        public readonly struct Wrapper<T>
            : IEquatable<Wrapper<T>>, IComparable<Wrapper<T>>
            where T : IEquatable<T>, IComparable<T> {
            readonly (bool, T?) _v;

            public Wrapper((bool, T?) v) {
                _v = v;
            }

            public bool Equals(Wrapper<T> other) =>
                _v.Equals(other._v);

            public override bool Equals(object obj) =>
                obj is Wrapper<T> v && Equals(v);

            public int CompareTo(Wrapper<T> other) =>
                _v.CompareTo(other._v);

            public override int GetHashCode() =>
                _v.Item1 ? _v.Item2!.GetHashCode() : 0;

            public static bool operator ==(Wrapper<T> x, Wrapper<T> y) => x.Equals(y);
            public static bool operator !=(Wrapper<T> x, Wrapper<T> y) => !(x == y);

            public static bool operator <=(Wrapper<T> x, Wrapper<T> y) => x.CompareTo(y) <= 0;
            public static bool operator <(Wrapper<T> x, Wrapper<T> y) => x.CompareTo(y) < 0;
            public static bool operator >(Wrapper<T> x, Wrapper<T> y) => x.CompareTo(y) > 0;
            public static bool operator >=(Wrapper<T> x, Wrapper<T> y) => x.CompareTo(y) >= 0;
        }

        public static Wrapper<T> Wrap<T>(T? value) where T : class, IEquatable<T>, IComparable<T> =>
            new((value != null, value));

        public static Wrapper<T> Wrap<T>(T? value) where T : struct, IEquatable<T>, IComparable<T> =>
            new((value.HasValue, value.GetValueOrDefault()));

        public static Wrapper<ListCmp.Wrapper<T>> WrapListStruct<T>(IReadOnlyList<T>? value) where T : struct, IEquatable<T>, IComparable<T> =>
            Wrap(value.NMap(ListCmp.Wrap));
        
        public static Wrapper<ListCmp.Wrapper<T>> WrapListClass<T>(IReadOnlyList<T>? value) where T : class, IEquatable<T>, IComparable<T> =>
            Wrap(value.NMap(ListCmp.Wrap));
    }
}