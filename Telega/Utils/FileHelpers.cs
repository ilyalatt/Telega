using System.IO;
using System.Threading.Tasks;

namespace Telega.Utils {
    static class FileHelpers {
        public static async Task<byte[]> ReadFileBytes(string fileName) {
            using var fs = File.OpenRead(fileName);
            return await fs.ReadToEndAsync().ConfigureAwait(false);
        }

        public static async Task WriteFileBytes(string fileName, byte[] bytes) {
            using var fs = File.OpenWrite(fileName);
            await fs.WriteAsync(bytes).ConfigureAwait(false);
        }
    }
}