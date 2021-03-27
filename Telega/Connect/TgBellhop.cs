using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telega.CallMiddleware;
using Telega.Rpc;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Types;
using Telega.Utils;

namespace Telega.Connect {
    sealed class TgBellhop {
        public TgConnectionPool ConnectionPool { get; }
        public Var<TgConnection> CurrentConnection { get; }
        public CustomObservable<UpdatesType> Updates { get; } = new();

        public IVarGetter<Session> SessionVar =>
            CurrentConnection.Bind(x => x.Session);

        public Session Session =>
            SessionVar.Get();

        public void SetSession(Func<Session, Session> func) =>
            CurrentConnection.Get().Session.SetWith(func);

        void MirrorUpdates(TgConnection conn) {
            conn.Transport.Transport.Updates.Subscribe(
                onNext: Updates.OnNext,
                onError: Updates.OnError,
                onCompleted: Updates.OnCompleted
            );
        }

        async Task<TgConnection> ChangeConn(Func<TgConnection, Task<TgConnection>> f) {
            var oldConn = CurrentConnection.Get();
            var newConn = await f(oldConn).ConfigureAwait(false);
            CurrentConnection.Set(newConn);
            MirrorUpdates(newConn);
            return newConn;
        }

        public TgBellhop(TgConnectionPool connectionPool, TgConnection currentConnection) {
            ConnectionPool = connectionPool;
            CurrentConnection = currentConnection.AsVar();
            MirrorUpdates(currentConnection);
        }

        public TgBellhop Fork() =>
            new(ConnectionPool, CurrentConnection.Get());

        public static async Task<TgBellhop> Connect(
            ILogger logger,
            ConnectInfo connectInfo,
            TgCallMiddlewareChain? callMiddlewareChain = null,
            TcpClientConnectionHandler? connHandler = null
        ) {
            callMiddlewareChain ??= TgCallMiddlewareChain.Default;
            var conn = await TaskWrapper.Wrap(() =>
                TgConnectionEstablisher.EstablishConnection(logger, connectInfo, callMiddlewareChain, connHandler)
            ).ConfigureAwait(false);
            var pool = new TgConnectionPool(logger, conn, callMiddlewareChain, connHandler);
            return new TgBellhop(pool, conn);
        }


        async Task<T> CallWithReConnect<T>(ITgFunc<T> func) {
            try {
                var conn = CurrentConnection.Get();
                return await conn.Transport.Call(func).ConfigureAwait(false);
            }
            catch (TgTransportException) {
                var oldConn = CurrentConnection.Get();
                oldConn.Dispose();

                var conn = await ChangeConn(x => ConnectionPool.ReConnect(x.Config.ThisDc)).ConfigureAwait(false);
                return await conn.Transport.Call(func).ConfigureAwait(false);
            }
        }

        async Task<T> CallWithMigration<T>(ITgFunc<T> func) {
            try {
                return await CallWithReConnect(func).ConfigureAwait(false);
            }
            catch (TgDataCenterMigrationException e) {
                await ChangeConn(x => ConnectionPool.Connect(x, e.Dc)).ConfigureAwait(false);
                return await CallWithReConnect(func).ConfigureAwait(false);
            }
        }


        public Task<T> Call<T>(ITgFunc<T> func) =>
            TaskWrapper.Wrap(() => CallWithMigration(func));
    }
}