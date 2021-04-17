using System;
using System.Threading.Tasks;
using Telega.Utils;

namespace Telega.Session.Store {
    public class FileSessionStore : ISessionStore {
        readonly string _fileName;
        readonly string _backupFileName;
        
        readonly Var<TgSession> _session = new(TgSession.Default);
        readonly SessionDebouncedSync _sync;

        public FileSessionStore(string name) {
            _fileName = name;
            _backupFileName = _fileName + ".backup";
            _sync = SessionDebouncedSync.Run(
                session: _session,
                triggerSave: SaveImpl
            );
        }

        async Task<TgSession> LoadImpl() {
            var bytes = await BackupFileIo.Load(_backupFileName, _fileName).ConfigureAwait(false);
            // TODO:
            // return bytes != null ? BtHelpers.Deserialize(TgSession.Deserialize)(bytes) : TgSession.Default;
            return TgSession.Default;
        }

        public async Task<TgSession> Get() {
            var session = _session.Get();
            if (session != null) {
                return session;
            }
            
            session = await LoadImpl().ConfigureAwait(false);
            return _session.Update(_ => session);
        }

        async Task SaveImpl(TgSession tgSession) {
            // var bytes = BtHelpers.UsingMemBinWriter(tgSession.Serialize);
            // await BackupFileIo.Save(_backupFileName, _fileName, bytes).ConfigureAwait(false);
        }

        public void Update(TgSession session) =>
            _session.Update(_ => session);

        public void Dispose() => 
            _sync.Dispose();
    }
}