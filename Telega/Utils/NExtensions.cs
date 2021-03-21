using System;
using System.Collections.Generic;
using System.Linq;

namespace Telega.Utils {
    public static class NExtensions {
        public static IEnumerable<Y> NChoose<X, Y>(this IEnumerable<X> seq, Func<X, Y?> mapper) =>
            seq.Select(x => mapper(x)!).Where(y => y != null);

        public static Y? NMap<X, Y>(this X? x, Func<X, Y> mapper) where X : class =>
            x != null ? mapper(x) : default;
        
        public static Y? NMap<X, Y>(this X? x, Func<X, Y> mapper) where X : struct =>
            x.HasValue ? mapper(x.Value) : default;
    }
}