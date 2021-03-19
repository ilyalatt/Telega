using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace Telega.Rpc
{
    sealed class CustomObservable<T> : IObservable<T>
    {
        readonly List<IObserver<T>> _observers = new();

        public void OnCompleted() => _observers.ForEach(x => x.OnCompleted());
        public void OnError(Exception error) => _observers.ForEach(x => x.OnError(error));
        public void OnNext(T value) => _observers.ForEach(x => x.OnNext(value));

        public IDisposable Subscribe(IObserver<T> observer)
        {
            _observers.Add(observer);
            return Disposable.Create(() => _observers.Remove(observer));
        }
    }
}
