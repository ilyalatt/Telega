using System;
using System.Collections.Generic;

namespace Telega.Utils {
    interface IVarGetter<out T> {
        T Get();
        IDisposable Subscribe(Action<T> subscription);
    }

    interface IVarUpdater<T> {
        T Update(Func<T, T> mapper);
    }

    sealed class Disposable : IDisposable {
        readonly Action _onDispose;

        public Disposable(Action onDispose) {
            _onDispose = onDispose;
        }

        public void Dispose() => _onDispose();
    }

    sealed class Var<T> : IVarGetter<T>, IVarUpdater<T> {
        T _value;
        readonly List<Action<T>> _subscriptions = new();

        public Var(T value) => _value = value;

        public T Get() => _value;
        
        public T Update(Func<T, T> mapper) {
            lock (this) {
                _value = mapper(_value);
                _subscriptions.ForEach(x => x(_value));
                return _value;
            }
        }

        public IDisposable Subscribe(Action<T> subscription) {
            _subscriptions.Add(subscription);
            return new Disposable(() => _subscriptions.Remove(subscription));
        }
    }

    static class VarExtensions {
        sealed class VarGetterMapping<X, Y> : IVarGetter<Y> {
            readonly IVarGetter<X> _var;
            readonly Func<X, Y> _mapper;

            public VarGetterMapping(IVarGetter<X> @var, Func<X, Y> mapper) {
                _var = var;
                _mapper = mapper;
            }

            public Y Get() =>
                _var.Get().Apply(_mapper);

            public IDisposable Subscribe(Action<Y> subscription) =>
                _var.Subscribe(x => subscription(_mapper(x)));
        }

        public static IVarGetter<Y> Map<X, Y>(this IVarGetter<X> var, Func<X, Y> mapper) =>
            new VarGetterMapping<X, Y>(var, mapper);

        public static IVarGetter<Y> Bind<X, Y>(this IVarGetter<X> var, Func<X, IVarGetter<Y>> binder) =>
            var.Map(binder).Map(x => x.Get());
    }
}