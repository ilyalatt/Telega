using System.IO;
using System.Threading.Tasks;
using Telega.Utils;

namespace Telega {
    public class FileSessionStore : ISessionStore {
        readonly string _fileName;
        readonly string _backupFileName;
        readonly TaskQueue _taskQueue = new();

        public FileSessionStore(string name) {
            _fileName = name;
            _backupFileName = _fileName + ".backup";
        }


        static async Task<Session?> Read(string fileName) {
            if (!File.Exists(fileName)) {
                return null;
            }

            var bts = await FileHelpers.ReadFileBytes(fileName).ConfigureAwait(false);
            return bts.Apply(BtHelpers.Deserialize(Session.Deserialize));
        }

        void RestoreBackup() {
            if (!File.Exists(_backupFileName)) {
                return;
            }

            if (File.Exists(_fileName)) {
                File.Delete(_fileName);
            }

            File.Move(_backupFileName, _fileName);
        }

        async Task<Session?> LoadImpl() {
            RestoreBackup();
            return await Read(_fileName).ConfigureAwait(false);
        }

        public Task<Session?> Load() =>
            _taskQueue.Put(LoadImpl);


        void CreateBackup() {
            RestoreBackup();
            if (File.Exists(_fileName)) {
                File.Move(_fileName, _backupFileName);
            }
        }

        void DeleteBackup() {
            if (File.Exists(_fileName) && File.Exists(_backupFileName)) {
                File.Delete(_backupFileName);
            }
        }

        static async Task Save(string fileName, Session session) {
            var bts = BtHelpers.UsingMemBinWriter(session.Serialize);
            await FileHelpers.WriteFileBytes(fileName, bts).ConfigureAwait(false);
        }

        async Task SaveImpl(Session session) {
            CreateBackup();
            await Save(_fileName, session).ConfigureAwait(false);
            DeleteBackup();
        }

        public async Task Save(Session session) =>
            await _taskQueue.Put(() => SaveImpl(session)).ConfigureAwait(false);
    }
}