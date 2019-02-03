using LanguageExt;

namespace Telega.CallMiddleware
{
    public sealed class TgCallMiddlewareChain
    {
        public readonly Arr<ITgCallMiddleware> Middleware;

        public TgCallMiddlewareChain(Arr<ITgCallMiddleware> middleware)
        {
            Middleware = middleware;
        }

        public TgCallMiddlewareChain With(
            Arr<ITgCallMiddleware>? middleware = null
        ) => new TgCallMiddlewareChain(
            middleware: middleware ?? Middleware
        );

        public TgCallMiddlewareChain Add(ITgCallMiddleware middleware) => With(
            middleware: Middleware.Add(middleware)
        );


        public static readonly TgCallMiddlewareChain Empty = new TgCallMiddlewareChain(
            Arr<ITgCallMiddleware>.Empty
        );

        public static TgCallMiddlewareChain Default => Empty
            .Add(new DelayCallMiddleware()); // mutable


        public TgCallHandler<T> Apply<T>(TgCallHandler<T> handler) =>
            Middleware.Fold(handler, (a, x) => x.Handle(a));
    }
}
