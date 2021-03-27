using System.Collections.Generic;
using System.Linq;

namespace Telega.CallMiddleware {
    public sealed record TgCallMiddlewareChain(
        IReadOnlyList<ITgCallMiddleware> Middleware
    ) {
        public TgCallMiddlewareChain Add(ITgCallMiddleware middleware) => this with {
            Middleware = Middleware.Append(middleware).ToList()
        };

        public static TgCallMiddlewareChain Empty =>
            new(new ITgCallMiddleware[0]);

        public static TgCallMiddlewareChain Default => Empty
           .Add(new FloodMiddleware())
           .Add(new DelayMiddleware());

        public TgCallHandler<T> Apply<T>(TgCallHandler<T> handler) =>
            Middleware.Aggregate(handler, (a, x) => x.Handle(a));
    }
}