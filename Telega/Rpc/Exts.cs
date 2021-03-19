using System;
using System.Collections.Generic;
using LanguageExt;

namespace Telega.Rpc {
    static class Exts {
        public static Unit Iter<T>(this IEnumerable<T> seq, Func<T, Unit> action) =>
            seq.Iter(x => { action(x); }); // so the signature is Action<T> instead of Func<T, Unit>
    }
}