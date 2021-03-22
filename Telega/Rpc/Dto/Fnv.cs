using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Telega.Rpc.Dto {
    static class Fnv32 {
        const int OffsetBasis = -2128831035;
        const int Prime = 16777619;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Next(int hashA, int hashB) =>
            unchecked((hashA ^ hashB) * Prime);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Hash<T>(IReadOnlyList<T> items, int offsetBasis = OffsetBasis) {
            var hash = offsetBasis;
            foreach (var item in items) {
                hash = Next(hash, item?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }
}