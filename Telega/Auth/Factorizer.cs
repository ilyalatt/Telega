using System.Linq;

namespace Telega.Auth {
    static class Factorizer {
        // copypasted from https://github.com/ricksladkey/dirichlet-numerics
        struct UInt128 {
            ulong s0;
            ulong s1;

            uint r0 => (uint) s0;
            uint r1 => (uint) (s0 >> 32);
            uint r2 => (uint) s1;
            uint r3 => (uint) (s1 >> 32);

            static byte[] bitLength = Enumerable.Range(0, byte.MaxValue + 1)
               .Select(value => {
                    int count;
                    for (count = 0; value != 0; count++) {
                        value >>= 1;
                    }

                    return (byte) count;
                }).ToArray();

            static int GetBitLength(uint value) {
                var tt = value >> 16;
                if (tt != 0) {
                    var t = tt >> 8;
                    if (t != 0) {
                        return bitLength[t] + 24;
                    }

                    return bitLength[tt] + 16;
                }
                else {
                    var t = value >> 8;
                    if (t != 0) {
                        return bitLength[t] + 8;
                    }

                    return bitLength[value];
                }
            }

            static ulong Q(uint u0, uint u1, uint u2, uint v1, uint v2) {
                var u0u1 = (ulong) u0 << 32 | u1;
                var qhat = u0 == v1 ? uint.MaxValue : u0u1 / v1;
                var r = u0u1 - qhat * v1;
                if (r == (uint) r && v2 * qhat > (r << 32 | u2)) {
                    --qhat;
                    r += v1;
                    if (r == (uint) r && v2 * qhat > (r << 32 | u2)) {
                        --qhat;
                        r += v1;
                    }
                }

                return qhat;
            }

            static uint DivRem(uint u0, ref uint u1, ref uint u2, uint v1, uint v2) {
                var qhat = Q(u0, u1, u2, v1, v2);
                var carry = qhat * v2;
                var borrow = (long) u2 - (uint) carry;
                carry >>= 32;
                u2 = (uint) borrow;
                borrow >>= 32;
                carry += qhat * v1;
                borrow += (long) u1 - (uint) carry;
                carry >>= 32;
                u1 = (uint) borrow;
                borrow >>= 32;
                borrow += (long) u0 - (uint) carry;
                if (borrow != 0) {
                    --qhat;
                    carry = (ulong) u2 + v2;
                    u2 = (uint) carry;
                    carry >>= 32;
                    carry += (ulong) u1 + v1;
                    u1 = (uint) carry;
                }

                return (uint) qhat;
            }

            static ulong Remainder96(ref UInt128 u, ulong v) {
                var dneg = GetBitLength((uint) (v >> 32));
                var d = 32 - dneg;
                var vPrime = v << d;
                var v1 = (uint) (vPrime >> 32);
                var v2 = (uint) vPrime;
                var r0 = u.r0;
                var r1 = u.r1;
                var r2 = u.r2;
                var r3 = (uint) 0;
                if (d != 0) {
                    r3 = r2 >> dneg;
                    r2 = r2 << d | r1 >> dneg;
                    r1 = r1 << d | r0 >> dneg;
                    r0 <<= d;
                }

                DivRem(r3, ref r2, ref r1, v1, v2);
                DivRem(r2, ref r1, ref r0, v1, v2);
                return ((ulong) r1 << 32 | r0) >> d;
            }

            static ulong Remainder128(ref UInt128 u, ulong v) {
                var dneg = GetBitLength((uint) (v >> 32));
                var d = 32 - dneg;
                var vPrime = v << d;
                var v1 = (uint) (vPrime >> 32);
                var v2 = (uint) vPrime;
                var r0 = u.r0;
                var r1 = u.r1;
                var r2 = u.r2;
                var r3 = u.r3;
                var r4 = (uint) 0;
                if (d != 0) {
                    r4 = r3 >> dneg;
                    r3 = r3 << d | r2 >> dneg;
                    r2 = r2 << d | r1 >> dneg;
                    r1 = r1 << d | r0 >> dneg;
                    r0 <<= d;
                }

                DivRem(r4, ref r3, ref r2, v1, v2);
                DivRem(r3, ref r2, ref r1, v1, v2);
                DivRem(r2, ref r1, ref r0, v1, v2);
                return ((ulong) r1 << 32 | r0) >> d;
            }

            public static ulong Remainder(ref UInt128 u, ulong v) {
                if (u.s1 == 0) {
                    return u.s0 % v;
                }

                var v0 = (uint) v;
                if (v == v0) {
                    if (u.s1 <= uint.MaxValue) {
                        return Remainder96(ref u, v0);
                    }

                    return Remainder128(ref u, v0);
                }

                if (u.s1 <= uint.MaxValue) {
                    return Remainder96(ref u, v);
                }

                return Remainder128(ref u, v);
            }

            static void Multiply64(out UInt128 w, ulong u, ulong v) {
                var u0 = (ulong) (uint) u;
                var u1 = u >> 32;
                var v0 = (ulong) (uint) v;
                var v1 = v >> 32;
                var carry = u0 * v0;
                var r0 = (uint) carry;
                carry = (carry >> 32) + u0 * v1;
                var r2 = carry >> 32;
                carry = (uint) carry + u1 * v0;
                w.s0 = carry << 32 | r0;
                w.s1 = (carry >> 32) + r2 + u1 * v1;
            }

            public static void Multiply(out UInt128 c, ulong a, ulong b) => Multiply64(out c, a, b);
        }

        static ulong MulMod(ulong a, ulong b, ulong m) {
            UInt128.Multiply(out var tmp, a, b);
            return UInt128.Remainder(ref tmp, m);
        }

        static ulong Gcd(ulong a, ulong b) {
            while (b != 0) {
                a %= b;

                var t = a;
                a = b;
                b = t;
            }

            return a;
        }

        static ulong PollardsRho(ulong num) {
            const int maxBatchSize = 128;
            var x = 2UL;
            var fixedX = x;
            var power = 2;

            while (true) {
                var itersCnt = power;
                while (true) {
                    var isEndOfCycle = maxBatchSize >= itersCnt;
                    var batchSize = isEndOfCycle ? itersCnt : maxBatchSize;

                    var z = 1UL;
                    for (var j = 0; j < batchSize; j++) {
                        x = MulMod(x, x, num);
                        x = x + 1 == num ? 0 : x + 1;

                        var dx = x > fixedX ? x - fixedX : fixedX - x;
                        z = MulMod(z, dx, num);
                    }

                    var factor = Gcd(z, num);
                    if (factor != 1) {
                        return factor;
                    }

                    itersCnt -= batchSize;
                    if (isEndOfCycle) {
                        break;
                    }
                }

                fixedX = x;
                power <<= 1;
            }
        }


        public static (ulong, ulong) Factorize(ulong pq) {
            var divisor = PollardsRho(pq);
            var p = divisor;
            var q = pq / divisor;
            return p < q ? (p, q) : (q, p);
        }
    }
}