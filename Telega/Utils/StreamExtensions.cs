using System.IO;
using System.Threading.Tasks;

namespace Telega.Utils {
    static class StreamExtensions {
        public static byte[] ReadToEnd(this Stream stream) {
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        public static async Task<byte[]> ReadToEndAsync(this Stream stream) {
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }

        public static async Task WriteAsync(this Stream stream, byte[] bytes) {
            var ms = new MemoryStream(bytes);
            await ms.CopyToAsync(stream);
        }
    }
}