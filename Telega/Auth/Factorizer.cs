using System;
using BigMath;

namespace Telega.Auth
{
    static class Factorizer
    {
        static readonly Random Random = new Random();

        static long Gcd(long a, long b)
        {
            while (a != 0 && b != 0)
            {
                while ((b & 1) == 0)
                {
                    b >>= 1;
                }
                while ((a & 1) == 0)
                {
                    a >>= 1;
                }
                if (a > b)
                {
                    a -= b;
                }
                else {
                    b -= a;
                }
            }
            return b == 0 ? a : b;
        }

        static long FindSmallMultiplierLopatin(long what)
        {
            long g = 0;
            for (int i = 0; i < 3; i++)
            {
                int q = (Random.Next(128) & 15) + 17;
                long x = Random.Next(1000000000) + 1, y = x;
                int lim = 1 << (i + 18);
                for (int j = 1; j < lim; j++)
                {
                    long a = x, b = x, c = q;
                    while (b != 0)
                    {
                        if ((b & 1) != 0)
                        {
                            c += a;
                            if (c >= what)
                            {
                                c -= what;
                            }
                        }
                        a += a;
                        if (a >= what)
                        {
                            a -= what;
                        }
                        b >>= 1;
                    }
                    x = c;
                    long z = x < y ? y - x : x - y;
                    g = Gcd(z, what);
                    if (g != 1)
                    {
                        break;
                    }
                    if ((j & (j - 1)) == 0)
                    {
                        y = x;
                    }
                }
                if (g > 1)
                {
                    break;
                }
            }

            long p = what / g;
            return Math.Min(p, g);
        }

        public static (long, long) Factorize(long pq)
        {
            var divisor = FindSmallMultiplierLopatin(pq);

            var p = divisor;
            var q = pq / divisor;

            return p < q ? (p, q) : (q, p);
        }
    }
}
