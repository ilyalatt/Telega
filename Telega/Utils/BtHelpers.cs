using System;
using System.IO;
using System.Threading.Tasks;
using BigMath;
using BigMath.Utils;
using Telega.Rpc.Dto;

namespace Telega.Utils {
    static class BtHelpers {
        public static Int128 GenNonce16() =>
            Rnd.NextBytes(16).ToInt128();

        public static Int256 GenNonce32() =>
            Rnd.NextBytes(32).ToInt256();

        public static byte[] WithMemStream(Action<MemoryStream> writer) =>
            new MemoryStream().With(writer).ToArray();

        public static async Task<byte[]> WithMemStream(Func<MemoryStream, Task> writer) {
            var ms = new MemoryStream();
            await writer(ms).ConfigureAwait(false);
            return ms.ToArray();
        }

        public static byte[] UsingMemBinWriter(Action<BinaryWriter> serializer) =>
            WithMemStream(ms => serializer(new BinaryWriter(ms)));

        public static byte[] Serialize(ITgType dto) =>
            UsingMemBinWriter(dto.Serialize);

        public static Func<byte[], T> Deserialize<T>(Func<BinaryReader, T> deserializer) => bts => {
            var ms = new MemoryStream(bts);
            var br = new BinaryReader(ms);
            return deserializer(br);
        };
    }
}