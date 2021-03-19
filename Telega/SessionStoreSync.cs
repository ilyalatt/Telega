using System;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Telega.Utils;

namespace Telega
{
    sealed class SessionStoreSync
    {
        readonly ISessionStore _store;
        readonly IVarGetter<Session> _session;
        readonly Task _task;
        readonly CancellationTokenSource _cts = new();

        static readonly TimeSpan Period = TimeSpan.FromSeconds(1);

        async Task SaveLoop()
        {
            var ct = _cts.Token;

            var prevSession = default(Session);
            while (!ct.IsCancellationRequested)
            {
                var session = _session.Get();
                if (!ReferenceEquals(prevSession, session))
                {
                    prevSession = session;
                    await _store.Save(_session.Get()).ConfigureAwait(false);
                }

                try
                {
                    await Task.Delay(Period, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            await _store.Save(_session.Get()).ConfigureAwait(false);
        }

        SessionStoreSync(Some<IVarGetter<Session>> session, Some<ISessionStore> store)
        {
            _session = session.Value;
            _store = store.Value;

            _task = SaveLoop();
        }

        public static SessionStoreSync Init(Some<IVarGetter<Session>> session, Some<ISessionStore> store) =>
            new(session, store);

        public void Stop()
        {
            _cts.Cancel();
            _task.Wait();
        }
    }
}
