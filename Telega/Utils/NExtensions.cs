using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Telega.Utils {
    public static class NExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NIsSome<X>(X? x) where X : struct =>
            x.HasValue;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NIsSome<X>(X? x) where X : class =>
            x != null;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NIsNone<X>(this X? x) where X : struct =>
            !x.HasValue;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NIsNone<X>(this X? x) where X : class =>
            x == null;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<Y> NChoose<X, Y>(this IEnumerable<X> seq, Func<X, Y?> mapper) where Y : struct =>
            seq.Select(mapper).Where(y => y.HasValue).Select(x => x!.Value);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<Y> NChoose<X, Y>(this IEnumerable<X> seq, Func<X, Y?> mapper) where Y : class =>
            seq.Select(x => mapper(x)!).Where(y => y != null);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NIter<X>(this X? x, Action<X> iterator) where X : struct {
            if (x.HasValue) {
                iterator(x.Value);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NIter<X>(this X? x, Action<X> iterator) where X : class {
            if (x != null) {
                iterator(x);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NMatch<X>(this X? x, Action<X> some, Action none) where X : struct {
            if (x.HasValue) {
                some(x.Value);
            }
            else {
                none();
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NMatch<X>(this X? x, Action<X> some, Action none) where X : class {
            if (x != null) {
                some(x);
            }
            else {
                none();
            }
        }
    }

    public static class NMap1 {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Y? NMap<X, Y>(this X? x, Func<X, Y> mapper) where X : struct where Y : struct =>
            x.HasValue ? mapper(x.Value) : default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Y? NMap<X, Y>(this X? x, Func<X, Y> mapper) where X : class where Y : class =>
            x != null ? mapper(x) : default;
    }

    public static class NMap2 {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Y? NMap<X, Y>(this X? x, Func<X, Y> mapper) where X : struct where Y : class =>
            x.HasValue ? mapper(x.Value) : default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Y? NMap<X, Y>(this X? x, Func<X, Y> mapper) where X : class where Y : struct =>
            x != null ? mapper(x) : default;
    }
}