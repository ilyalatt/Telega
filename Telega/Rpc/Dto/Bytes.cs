using System;
using LanguageExt;

namespace Telega.Rpc.Dto
{
    public struct Bytes : IEquatable<Bytes>, IComparable<Bytes>
    {
        internal readonly byte[] Ref;
        internal Bytes(byte[] @ref) => Ref = @ref ?? throw new(nameof(@ref));


        static byte[] Copy(byte[] bts)
        {
            var res = new byte[bts.Length];
            Buffer.BlockCopy(bts, 0, res, 0, bts.Length);
            return res;
        }

        public static Bytes New(Some<byte[]> bytes) =>
            new(Copy(bytes));

        public byte[] ToArray() =>
            Copy(Ref);

        public static byte[] ToArray(Some<Bytes> bytes) =>
            bytes.Value.ToArray();


        public bool Equals(Bytes other)
        {
            var xs = Ref;
            var ys = other.Ref;
            var xsLen = xs.Length;
            var ysLen = ys.Length;

            if (xsLen != ysLen) return false;

            for (var i = 0; i < xsLen; i++)
            {
                if (xs[i] != ys[i]) return false;
            }

            return true;
        }

        public override bool Equals(object obj) => obj is Bytes x && Equals(x);

        public override int GetHashCode() =>
            (int) MurMur3.MurmurHash3_x86_32(Ref, (uint) Ref.Length, 16777619);

        public static bool operator ==(Bytes x, Bytes y) => x.Equals(y);
        public static bool operator !=(Bytes x, Bytes y) => !(x == y);

        public static bool operator <=(Bytes x, Bytes y) => x.CompareTo(y) <= 0;
        public static bool operator <(Bytes x, Bytes y) => x.CompareTo(y) < 0;
        public static bool operator >(Bytes x, Bytes y) => x.CompareTo(y) > 0;
        public static bool operator >=(Bytes x, Bytes y) => x.CompareTo(y) >= 0;

        public int CompareTo(Bytes other)
        {
            var xs = Ref;
            var ys = other.Ref;
            var xsLen = xs.Length;
            var ysLen = ys.Length;
            var minLen = Math.Min(xsLen, ysLen);

            for (var i = 0; i < minLen; i++)
            {
                if (xs[i] == ys[i]) continue;
                return xs[i] < ys[i] ? -1 : +1;
            }

            return xsLen == ysLen ? 0 : xsLen < ysLen ? -1 : +1;
        }


        public override string ToString() =>
            BitConverter.ToString(Ref);
    }

    public static class BytesExtensions
    {
        public static Bytes ToBytes(this byte[] bts) => Bytes.New(bts);
        public static Bytes ToBytesUnsafe(this byte[] bts) => new(bts);
        public static byte[] ToArrayUnsafe(this Bytes bytes) => bytes.Ref;
    }
}
