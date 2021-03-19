using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;

namespace Telega.Rpc.Dto.Generator.Generation {
    static class Extensions {
        public static IEnumerable<T> Generate<T>(this T seed, Func<T, Option<T>> generator) {
            var obj = seed;
            while (true) {
                yield return obj;

                var newObj = generator(obj);
                if (newObj.IsSome) {
                    obj = newObj.ValueUnsafe();
                }
                else {
                    yield break;
                }
            }
        }

        public static IEnumerable<(T, T)> Pairwise<T>(this IEnumerable<T> seq) =>
            seq.Scan(default((T, T)), (a, x) => (a.Item2, x)).Skip(2);
    }
}