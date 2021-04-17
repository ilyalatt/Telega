using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Telega.Rpc.Dto.Functions.Auth;
using Telega.Utils;

namespace Telega.Connect {
    sealed class TgConnectionPool : IDisposable {
        readonly TgConnectionContext _context;
        readonly DcInfoKeeper _dcInfoKeeper = new();
        readonly ConcurrentDictionary<int, Task<TgConnection>> _connectTasks = new();
        readonly ConcurrentDictionary<int, TgConnection> _connections = new();
        bool _isDisposed;

        public TgConnectionPool(
            TgConnectionContext context
        ) {
            _context = context;
        }
        
        public void Dispose() {
            _isDisposed = true;
            _connections.Values.Iter(x => x.Dispose());
        }


        static async Task TryExportAuth(TgConnection src, TgConnection dst) {
            try {
                var auth = await src.Transport.Call(new ExportAuthorization(dst.Config.ThisDc)).ConfigureAwait(false);
                await dst.Transport.Call(new ImportAuthorization(auth.Id, auth.Bytes)).ConfigureAwait(false);
            }
            catch (TgNotAuthenticatedException) { }
        }

        async Task<TgConnection> ForkConnection(TgConnection srcConn, int dcId) {
            var ep = _dcInfoKeeper.FindEndpoint(dcId);
            var context = _context with {
                ConnectConfig = _context.ConnectConfig with {
                    Endpoint = ep
                }
            };
            var dstConn = await TgConnectionInitializer.InitConnection(context).ConfigureAwait(false);
            _dcInfoKeeper.Update(dstConn.Config);
            await TryExportAuth(srcConn, dstConn).ConfigureAwait(false);
            return dstConn;
        }

        // TODO: refactor the common part of Connect & ReConnect

        public async Task<TgConnection> Connect(TgConnection src, int dstDcId) {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(TgConnectionPool));
            }

            // it is not perfect but it should work right almost every time
            if (_connectTasks.TryGetValue(dstDcId, out var connTask)) {
                return await connTask.ConfigureAwait(false);
            }

            if (_connections.TryGetValue(dstDcId, out var conn)) {
                return conn;
            }

            var task = _connectTasks[dstDcId] = ForkConnection(src, dstDcId);

            try {
                return _connections[dstDcId] = await task.ConfigureAwait(false);
            }
            finally {
                _connectTasks.TryRemove(dstDcId, out _);
            }
        }

        public async Task<TgConnection> ReConnect(int dcId) {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(TgConnectionPool));
            }

            // it is not perfect but it should work right almost every time
            if (_connectTasks.TryGetValue(dcId, out var connTask)) {
                return await connTask.ConfigureAwait(false);
            }

            if (!_connections.ContainsKey(dcId)) {
                throw Helpers.FailedAssertion($"TgConnectionPool.Reconnect: DC {dcId} not found.");
            }

            var newConnTask = _connectTasks[dcId] = TgConnectionInitializer.InitConnection(_context);

            try {
                return _connections[dcId] = await newConnTask.ConfigureAwait(false);
            }
            finally {
                _connectTasks.TryRemove(dcId, out _);
            }
        }

    }
}