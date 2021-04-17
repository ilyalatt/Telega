using System;
using System.Threading;
using System.Threading.Tasks;
using Telega.Utils;

namespace Telega.Session.Store {
    sealed class SessionDebouncedSync : IDisposable {
        static readonly TimeSpan SyncPeriod = TimeSpan.FromSeconds(1);
        
        readonly IVarGetter<TgSession> _session;
        readonly Func<TgSession, Task> _triggerSave;

        readonly IDisposable _watcher;
        readonly CancellationTokenSource _loopCts;
        Task? _loopTask;

        async Task Loop() {
            var ct = _loopCts.Token;
            var prevSession = TgSession.Default;

            async Task<bool> SaveIfNeeded() {
                var session = _session.Get();
                if (ReferenceEquals(session, prevSession)) {
                    return false;
                }

                await _triggerSave(session).ConfigureAwait(false);
                prevSession = session;
                return true;
            }

            while (true) {
                try {
                    var isUpdated = await SaveIfNeeded().ConfigureAwait(false);
                    if (!isUpdated) {
                        return;
                    }
                    await Task.Delay(SyncPeriod, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    break;
                }
            }

            await SaveIfNeeded().ConfigureAwait(false);
        }

        IDisposable SetupWatcher() =>
            _session.Subscribe(_ => 
                _loopTask ??= Loop().ContinueWith(_ => _loopTask = null)
            );

        SessionDebouncedSync(
            IVarGetter<TgSession> session, 
            Func<TgSession, Task> triggerSave
        ) {
            _session = session;
            _triggerSave = triggerSave;

            _loopCts = new CancellationTokenSource();
            _watcher = SetupWatcher();
        }

        public static SessionDebouncedSync Run(
            IVarGetter<TgSession> session,
            Func<TgSession, Task> triggerSave
        ) => new(session, triggerSave);

        async Task DisposeAsync() {
            _watcher.Dispose();
            _loopCts.Cancel();
            var loopTask = _loopTask;
            if (loopTask != null) {
                await loopTask.ConfigureAwait(false);
            }
        }

        public void Dispose() =>
            DisposeAsync().Wait();
    }
}