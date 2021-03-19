using System;
using LanguageExt;

namespace Telega.Utils {
    interface IVarGetter<out T> {
        T Get();
    }

    interface IVarSetter<in T> {
        void Set(T value);
    }

    sealed class Var<T> : IVarGetter<T>, IVarSetter<T> where T : class {
        volatile T _value;
        Var(T value) => _value = value;

        public static Var<T> Create(T value) => new(value);

        public T Get() => _value;
        public void Set(T value) => _value = value;
    }

    static class VarExtensions {
        public static Var<T> AsVar<T>(this T value) where T : class =>
            Var<T>.Create(value);

        public static T SetWith<T>(this Var<T> var, Func<T, T> func) where T : class =>
            var.Get().Apply(func).With(var.Set);

        sealed class VarGetterMapping<X, Y> : IVarGetter<Y> {
            readonly IVarGetter<X> _var;
            readonly Func<X, Y> _mapper;

            public VarGetterMapping(IVarGetter<X> @var, Func<X, Y> mapper) {
                _var = var;
                _mapper = mapper;
            }

            public Y Get() =>
                _var.Get().Apply(_mapper);
        }

        public static IVarGetter<Y> Map<X, Y>(this IVarGetter<X> var, Func<X, Y> mapper) =>
            new VarGetterMapping<X, Y>(var, mapper);

        public static IVarGetter<Y> Bind<X, Y>(this IVarGetter<X> var, Func<X, IVarGetter<Y>> binder) =>
            var.Map(binder).Map(x => x.Get());
    }
}