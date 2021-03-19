using System;
using LanguageExt;

namespace Telega.Rpc.Dto.Generator.TgScheme {
    static class Exts {
        public static T GetOrThrow<T>(this Option<T> opt, Func<Exception> exception) =>
            opt.IfNone(() => throw exception());
    }
}