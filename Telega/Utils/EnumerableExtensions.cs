using System;
using System.Collections.Generic;
using System.Linq;

namespace Telega.Utils {
    static class EnumerableExtensions {
        public static T? TryFind<T>(this IReadOnlyList<T> list, Predicate<T> predicate) where T : struct {
            foreach (var x in list) {
                if (predicate(x)) {
                    return x;
                }
            }

            return default(T?);
        }

        public static IEnumerable<T> SkipLast<T>(this IReadOnlyList<T> seq, int count) {
            return seq.Take(Math.Max(0, seq.Count - count));
        }

        public static void Iter<T>(this IEnumerable<T> seq, Action<T> mutator) {
            foreach (var x in seq) {
                mutator(x);
            }
        }
        
        public static void Iter<T>(this IEnumerable<T> seq, Action<T, int> mutator) {
            var idx = 0;
            foreach (var x in seq) {
                mutator(x, idx++);
            }
        }
    }
}