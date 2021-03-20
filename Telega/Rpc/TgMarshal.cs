using System;
using System.IO;
using System.Runtime.CompilerServices;
using BigMath;
using BigMath.Utils;
using LanguageExt;
using Telega.Rpc.Dto;
using static LanguageExt.Prelude;

namespace Telega.Rpc {
    static class TgMarshal {
        const uint VectorNum = 0x1cb5c415;
        const uint FalseNum = 0xbc799737;
        const uint TrueNum = 0x997275b5;
        const int BytesMagic = 254;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt(BinaryReader br) =>
            br.ReadInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt(BinaryWriter bw, int value) =>
            bw.Write(value);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUint(BinaryReader br) =>
            br.ReadUInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUint(BinaryWriter bw, uint value) =>
            bw.Write(value);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadLong(BinaryReader br) =>
            br.ReadInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLong(BinaryWriter bw, long value) =>
            bw.Write(value);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDouble(BinaryReader br) =>
            br.ReadDouble();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(BinaryWriter bw, double value) =>
            bw.Write(value);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CalculateBtsPadding(int length) {
            var tail4Bts = (length + (length < BytesMagic ? 1 : 0)) % 4;
            var padding = tail4Bts == 0 ? 0 : 4 - tail4Bts;
            return padding;
        }

        public static Bytes ReadBytes(BinaryReader br) {
            var firstByte = br.ReadByte();

            var len = firstByte == BytesMagic ? br.ReadByte() | (br.ReadByte() << 8) | (br.ReadByte() << 16) : firstByte;
            var data = br.ReadBytes(len);

            var padding = CalculateBtsPadding(len);
            if (padding > 0) {
                br.ReadBytes(padding);
            }

            return data.ToBytesUnsafe();
        }

        public static void WriteBytes(BinaryWriter bw, Bytes bytes) {
            var bts = bytes.ToArrayUnsafe();

            if (bts.Length < BytesMagic) {
                bw.Write((byte) bts.Length);
            }
            else {
                bw.Write((byte) BytesMagic);
                bw.Write((byte) bts.Length);
                bw.Write((byte) (bts.Length >> 8));
                bw.Write((byte) (bts.Length >> 16));
            }

            bw.Write(bts);

            var padding = CalculateBtsPadding(bts.Length);
            for (var i = 0; i < padding; i++) {
                bw.Write((byte) 0);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadString(BinaryReader br) =>
            System.Text.Encoding.UTF8.GetString(ReadBytes(br).ToArrayUnsafe());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteString(BinaryWriter bw, string value) =>
            WriteBytes(bw, System.Text.Encoding.UTF8.GetBytes(value).ToBytesUnsafe());


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBool(BinaryReader br) {
            var n = br.ReadUInt32();
            return n switch
            {
                TrueNum => true,
                FalseNum => false,
                _ => throw TgRpcDeserializeException.UnexpectedBoolTypeNumber(n),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBool(BinaryWriter bw, bool value) =>
            bw.Write(value ? TrueNum : FalseNum);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 ReadInt128(BinaryReader br) =>
            br.ReadBytes(16).ToInt128(0, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt128(BinaryWriter bw, Int128 value) =>
            bw.Write(value.ToBytes(true));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int256 ReadInt256(BinaryReader br) =>
            br.ReadBytes(32).ToInt256(0, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt256(BinaryWriter bw, Int256 value) =>
            bw.Write(value.ToBytes(true));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<BinaryReader, Arr<T>> ReadVector<T>(
            Func<BinaryReader, T> deserializer
        ) => br => {
            var typeNumber = ReadUint(br);
            if (typeNumber != VectorNum) {
                throw TgRpcDeserializeException.UnexpectedVectorTypeNumber(typeNumber);
            }

            var count = ReadInt(br);
            var arr = new T[count];
            for (var i = 0; i < count; i++) {
                arr[i] = deserializer(br);
            }

            return arr.ToArr();
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Action<BinaryWriter, Arr<T>> WriteVector<T>(
            Action<BinaryWriter, T> serializer
        ) => (bw, vector) => {
            WriteUint(bw, VectorNum);
            WriteInt(bw, vector.Count);
            vector.Iter(x => serializer(bw, x));
        };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<BinaryReader, Option<T>> ReadOption<T>(
            int mask,
            int bit,
            Func<BinaryReader, T> deserializer
        ) => br => (mask & (1 << bit)) == 0 ? None : Some(deserializer(br));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<BinaryReader, bool> ReadOption(
            int mask,
            int bit
        ) => _ => (mask & (1 << bit)) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaskBit<T>(int bit, Option<T> option) =>
            option.IsNone ? 0 : 1 << bit;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaskBit(int bit, bool value) =>
            value ? 1 << bit : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Action<BinaryWriter, Option<T>> WriteOption<T>(
            Action<BinaryWriter, T> serializer
        ) => (bw, option) => option.Iter(x => serializer(bw, x));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSerializable<T>(BinaryWriter bw, T value) where T : ITgSerializable =>
            value.Serialize(bw);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(
            BinaryReader br,
            Func<BinaryReader, T> deserializer
        ) => deserializer(br);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(
            T value,
            BinaryWriter bw,
            Action<BinaryWriter, T> serializer
        ) => serializer(bw, value);
    }
}