using System;
using System.Threading.Tasks;
using LanguageExt;
using Telega.CallMiddleware;
using Telega.Rpc;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Types;
using Telega.Utils;

namespace Telega.Connect
{
    sealed class TgBellhop
    {
        public readonly TgConnectionPool ConnectionPool;
        public readonly Var<TgConnection> CurrentConnection;
        public readonly CustomObservable<UpdatesType> Updates = new CustomObservable<UpdatesType>();

        public IVarGetter<Session> SessionVar =>
            CurrentConnection.Bind(x => x.Session);
        public Session Session =>
            SessionVar.Get();
        public void SetSession(Func<Session, Session> func) =>
            CurrentConnection.Get().Session.SetWith(func);

        void MirrorUpdates(TgConnection conn)
        {
            conn.Transport.Transport.Updates.Subscribe(
                onNext: Updates.OnNext,
                onError: Updates.OnError,
                onCompleted: Updates.OnCompleted
            );
        }

        async Task<TgConnection> ChangeConn(Func<TgConnection, Task<TgConnection>> f)
        {
            var oldConn = CurrentConnection.Get();
            var newConn = await f(oldConn);
            CurrentConnection.Set(newConn);
            MirrorUpdates(newConn);
            return newConn;
        }

        public TgBellhop(Some<TgConnectionPool> connectionPool, Some<TgConnection> currentConnection)
        {
            ConnectionPool = connectionPool;
            CurrentConnection = currentConnection.Value.AsVar();
            MirrorUpdates(currentConnection);
        }

        public TgBellhop Fork() =>
            new TgBellhop(ConnectionPool, CurrentConnection.Get());

        public static async Task<TgBellhop> Connect(
            ConnectInfo connectInfo,
            TgCallMiddlewareChain? callMiddlewareChain = null,
            TcpClientConnectionHandler? connHandler = null
        ) {
            callMiddlewareChain ??= TgCallMiddlewareChain.Default;
            var conn = await TaskWrapper.Wrap(() =>
                TgConnectionEstablisher.EstablishConnection(connectInfo, callMiddlewareChain, connHandler)
            ).ConfigureAwait(false);
            var pool = new TgConnectionPool(conn, callMiddlewareChain, connHandler);
            return new TgBellhop(pool, conn);
        }


        async Task<T> CallWithReConnect<T>(ITgFunc<T> func)
        {
            try
            {
                var conn = CurrentConnection.Get();
                return await conn.Transport.Call(func);
            }
            catch (TgTransportException)
            {
                var oldConn = CurrentConnection.Get();
                oldConn.Dispose();

                var conn = await ChangeConn(x => ConnectionPool.ReConnect(x.Config.ThisDc));
                return await conn.Transport.Call(func);
            }
        }

        async Task<T> CallWithMigration<T>(ITgFunc<T> func)
        {
            try
            {
                return await CallWithReConnect(func);
            }
            catch (TgDataCenterMigrationException e)
            {
                await ChangeConn(x => ConnectionPool.Connect(x, e.Dc));
                return await CallWithReConnect(func);
            }
        }


        public Task<T> Call<T>(ITgFunc<T> func) =>
            TaskWrapper.Wrap(() => CallWithMigration(func));
    }
}
