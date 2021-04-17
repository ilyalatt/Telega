using System.IO;
using System.Threading.Tasks;
using Telega.Utils;

namespace Telega.Session.Store {
    static class BackupFileIo {
        static void RestoreBackup(string backupFileName, string fileName) {
            if (!File.Exists(backupFileName)) {
                return;
            }

            if (File.Exists(fileName)) {
                File.Delete(fileName);
            }

            File.Move(backupFileName, fileName);
        }

        static void CreateBackup(string backupFileName, string fileName) {
            RestoreBackup(backupFileName, fileName);
            if (File.Exists(fileName)) {
                File.Move(fileName, backupFileName);
            }
        }

        static void DeleteBackup(string backupFileName, string fileName) {
            if (File.Exists(fileName) && File.Exists(backupFileName)) {
                File.Delete(backupFileName);
            }
        }

        static async Task<byte[]?> Read(string fileName) => File.Exists(fileName)
            ? await FileHelpers.ReadFileBytes(fileName).ConfigureAwait(false)
            : null;
        
        static async Task Save(string fileName, byte[] bytes) {
            await FileHelpers.WriteFileBytes(fileName, bytes).ConfigureAwait(false);
        }

        public static async Task<byte[]?> Load(string backupFileName, string fileName) {
            RestoreBackup(backupFileName, fileName);
            return await Read(fileName).ConfigureAwait(false);
        }

        public static async Task Save(string backupFileName, string fileName, byte[] bytes) {
            CreateBackup(backupFileName, fileName);
            await Save(fileName, bytes).ConfigureAwait(false);
            DeleteBackup(backupFileName, fileName);
        }
    }
}