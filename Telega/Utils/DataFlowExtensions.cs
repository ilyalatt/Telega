using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Telega.Utils {
    public static class DataFlowExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T With<T>(this T v, Action<T> mutator) {
            mutator(v);
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<T> With<T>(this T v, Func<T, Task> mutator) {
            await mutator(v).ConfigureAwait(false);
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Y Apply<X, Y>(this X x, Func<X, Y> mapper) =>
            mapper(x);
    }
}