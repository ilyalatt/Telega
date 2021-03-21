using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telega.CallMiddleware;
using Telega.Rpc.Dto.Functions.Auth;
using Telega.Utils;

namespace Telega.Connect {
    sealed class TgConnectionPool : IDisposable {
        readonly ILogger _logger;
        readonly TgCallMiddlewareChain _callMiddlewareChain;
        readonly TcpClientConnectionHandler? _connHandler;

        static async Task TryExportAuth(TgConnection src, TgConnection dst) {
            if (!src.Session.Get().IsAuthorized) {
                return;
            }

            try {
                var auth = await src.Transport.Call(new ExportAuthorization(dst.Config.ThisDc)).ConfigureAwait(false);
                await dst.Transport.Call(new ImportAuthorization(auth.Id, auth.Bytes)).ConfigureAwait(false);
            }
            catch (TgNotAuthenticatedException) { }
        }

        async Task<TgConnection> EstablishForkConnection(TgConnection srcConn, int dcId) {
            var ep = DcInfoKeeper.FindEndpoint(dcId);
            var connectInfo = ConnectInfo.FromInfo(srcConn.Session.Get().ApiId, ep);
            var dstConn = await TgConnectionEstablisher.EstablishConnection(_logger, connectInfo, _callMiddlewareChain, _connHandler).ConfigureAwait(false);
            await TryExportAuth(srcConn, dstConn).ConfigureAwait(false);
            return dstConn;
        }

        readonly ConcurrentDictionary<int, Task<TgConnection>> _connTasks = new();

        readonly ConcurrentDictionary<int, TgConnection> _conns = new();

        // TODO: refactor the common part of Connect & ReConnect

        public async Task<TgConnection> Connect(TgConnection src, int dstDcId) {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(TgConnectionPool));
            }

            // it is not perfect but it should work right almost every time
            if (_connTasks.TryGetValue(dstDcId, out var connTask)) {
                return await connTask.ConfigureAwait(false);
            }

            if (_conns.TryGetValue(dstDcId, out var conn)) {
                return conn;
            }

            var task = _connTasks[dstDcId] = EstablishForkConnection(src, dstDcId);

            try {
                return _conns[dstDcId] = await task.ConfigureAwait(false);
            }
            finally {
                _connTasks.TryRemove(dstDcId, out _);
            }
        }

        public async Task<TgConnection> ReConnect(int dcId) {
            if (_isDisposed) {
                throw new ObjectDisposedException(nameof(TgConnectionPool));
            }

            // it is not perfect but it should work right almost every time
            if (_connTasks.TryGetValue(dcId, out var connTask)) {
                return await connTask.ConfigureAwait(false);
            }

            if (!_conns.TryGetValue(dcId, out var conn)) {
                throw Helpers.FailedAssertion($"TgConnectionPool.Reconnect: DC {dcId} not found.");
            }

            var connectInfo = ConnectInfo.FromSession(conn.Session.Get());
            var newConnTask = _connTasks[dcId] = TgConnectionEstablisher.EstablishConnection(_logger, connectInfo, _callMiddlewareChain, _connHandler);

            try {
                return _conns[dcId] = await newConnTask.ConfigureAwait(false);
            }
            finally {
                _connTasks.TryRemove(dcId, out _);
            }
        }

        bool _isDisposed;

        public void Dispose() {
            _isDisposed = true;
            _conns.Values.Iter(x => x.Dispose());
        }

        public TgConnectionPool(
            ILogger logger,
            TgConnection mainConn,
            TgCallMiddlewareChain callMiddlewareChain,
            TcpClientConnectionHandler? connHandler = null
        ) {
            _logger = logger;
            _conns[mainConn.Config.ThisDc] = mainConn;
            _callMiddlewareChain = callMiddlewareChain;
            _connHandler = connHandler;
        }
    }
}