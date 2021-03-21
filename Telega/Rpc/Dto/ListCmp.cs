using System;
using System.Collections.Generic;

namespace Telega.Rpc.Dto {
    static class ListCmp {
        public readonly struct Wrapper<T>
            : IEquatable<Wrapper<T>>, IComparable<Wrapper<T>>
            where T : IEquatable<T>, IComparable<T> {
            readonly IReadOnlyList<T> _v;

            public Wrapper(IReadOnlyList<T> v) {
                _v = v;
            }

            public bool Equals(Wrapper<T> other) {
                var x = _v;
                var y = other._v;

                if (ReferenceEquals(x, y)) {
                    return true;
                }
                
                if (x.Count != y.Count) {
                    return false;
                }

                for (var i = 0; i < x.Count; i++) {
                    if (!x[i].Equals(y[i])) {
                        return false;
                    }
                }

                return true;
            }

            public int CompareTo(Wrapper<T> other) {
                var x = _v;
                var y = other._v;
                
                if (ReferenceEquals(x, y)) {
                    return 0;
                }
                
                var minCount = Math.Min(x.Count, y.Count);
                for (var i = 0; i < minCount; i++) {
                    var cmp = x[i].CompareTo(y[i]);
                    if (cmp != 0) {
                        return cmp;
                    }
                }

                return x.Count.CompareTo(y.Count);
            }
        }

        public static Wrapper<T> Wrap<T>(IReadOnlyList<T> value) where T : IEquatable<T>, IComparable<T> =>
            new(value);
    }
}