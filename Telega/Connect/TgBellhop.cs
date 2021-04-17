using System;
using System.Threading.Tasks;
using Telega.Rpc;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Types;
using Telega.Session;
using Telega.Utils;

namespace Telega.Connect {
    sealed class TgBellhop : IDisposable {
        public TgConnectionPool ConnectionPool { get; }
        public Var<TgConnection?> CurrentConnection { get; private set; }
        public CustomObservable<UpdatesType> Updates { get; } = new();

        public TgSession? Session =>
            CurrentConnection.Get()?.Session.Get();

        public void UpdateSession(Func<TgSession, TgSession> func) =>
            CurrentConnection.Get()?.Session.Update(func);

        void MirrorUpdates(TgConnection conn) {
            conn.Transport.Transport.Updates.Subscribe(
                onNext: Updates.OnNext,
                onError: Updates.OnError,
                onCompleted: Updates.OnCompleted
            );
        }

        async Task<TgConnection> MigrateCurrentConnection(Func<TgConnection?, Task<TgConnection>> f) {
            var oldConn = CurrentConnection;
            oldConn.Session.
            var newConn = await f(oldConn).ConfigureAwait(false);
            CurrentConnection = newConn;
            MirrorUpdates(newConn);
            return newConn;
        }

        public TgBellhop(TgConnectionPool connectionPool) {
            ConnectionPool = connectionPool;
        }

        public TgBellhop Fork() =>
            new(ConnectionPool, CurrentConnection);

        public async Task Connect(
            TgRpcConfig config
        ) {
            // TODO: reset old connections
            var pool = new TgConnectionPool(config, conn);
            return new TgBellhop(pool, conn);
        }

        async Task<TgConnection> ConnectIfNeeded() {
            var conn = CurrentConnection;
            if (conn != null) {
                return conn;
            }
            
            conn = await TaskExceptionWrapper.Wrap(
                () => TgConnectionInitializer.InitConnection(_config, null)
            ).ConfigureAwait(false);
            return conn;
        }

        async Task<T> CallWithReConnect<T>(ITgFunc<T> func) {
            var conn = await ConnectIfNeeded().ConfigureAwait(false);
            try {
                return await conn.Transport.Call(func).ConfigureAwait(false);
            }
            catch (TgTransportException) {
                var oldConn = CurrentConnection;
                oldConn?.Dispose();

                conn = await MigrateCurrentConnection(x => ConnectionPool.ReConnect(x.Config.ThisDc)).ConfigureAwait(false);
                return await conn.Transport.Call(func).ConfigureAwait(false);
            }
        }

        async Task<T> CallWithMigration<T>(ITgFunc<T> func) {
            try {
                return await CallWithReConnect(func).ConfigureAwait(false);
            }
            catch (TgDataCenterMigrationException e) {
                await MigrateCurrentConnection(x => ConnectionPool.Connect(x, e.Dc)).ConfigureAwait(false);
                return await CallWithReConnect(func).ConfigureAwait(false);
            }
        }

        public Task<T> Call<T>(ITgFunc<T> func) =>
            TaskExceptionWrapper.Wrap(() => CallWithMigration(func));

        public void Dispose() {
            ConnectionPool.Dispose();
            // todo: dispose store!
        }
    }
}