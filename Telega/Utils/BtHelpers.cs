using System;
using System.IO;
using System.Threading.Tasks;
using BigMath;
using BigMath.Utils;
using LanguageExt;
using Telega.Rpc.Dto;

namespace Telega.Utils
{
    static class BtHelpers
    {
        // TODO: enhance random

        public static Int128 GenNonce16()
        {
            var bts = new byte[16];
            new Random().NextBytes(bts);
            return bts.ToInt128();
        }

        public static Int256 GenNonce32()
        {
            var bts = new byte[32];
            new Random().NextBytes(bts);
            return bts.ToInt256();
        }

        public static byte[] WithMemStream(Action<MemoryStream> writer) =>
            new MemoryStream().With(writer).ToArray();

        public static Task<byte[]> WithMemStream(Func<MemoryStream, Task> writer) =>
            new MemoryStream().With(writer).Map(ms => ms.ToArray());

        public static byte[] UsingMemBinWriter(Action<BinaryWriter> serializer) =>
            WithMemStream(ms => serializer(new BinaryWriter(ms)));

        public static byte[] Serialize(ITgType dto) =>
            UsingMemBinWriter(dto.Serialize);

        public static Func<byte[], T> Deserialize<T>(Func<BinaryReader, T> deserializer) => bts =>
        {
            var ms = new MemoryStream(bts);
            var br = new BinaryReader(ms);
            return deserializer(br);
        };
    }
}
