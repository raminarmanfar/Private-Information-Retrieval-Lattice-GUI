using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace PIRLatticeGUI
{
    static class MathClass
    {
        public static void printMatrix(double[,] mat)
        {
            for (long i = 0; i < mat.GetLongLength(0); i++)
            {
                Console.WriteLine();
                for (long j = 0; j < mat.GetLongLength(1); j++)
                {
                    Console.Write("{0} ", mat[i, j]);
                }
            }
        }

        public static BigInteger mod(BigInteger k, BigInteger p) { return ((k %= p) < 0) ? k + p : k; }

        public static BigInteger EEA(BigInteger a, BigInteger p)
        {
            BigInteger b = p;
            BigInteger x = 0;
            BigInteger y = 1;
            BigInteger u = 1;
            BigInteger v = 0;
            while (a != 0)
            {
                BigInteger q = b / a;
                BigInteger r = b % a;
                BigInteger m = x - u * q;
                BigInteger n = y - v * q;
                b = a;
                a = r;
                x = u;
                y = v;
                u = m;
                v = n;
            }
            return mod(x, p);
        }
        static Random rnd = new Random(DateTime.Now.Millisecond);

        public static BigInteger GenerateLargePrime(int l0)
        {
            //static Random random = new Random(DateTime.Now.Millisecond);
            Primality primality = new Primality();
            string numbers = string.Empty;

            BigInteger number;
            BigInteger v = new BigInteger(Math.Pow(2, 3 * l0));
            while (true)
            {
                numbers += rnd.Next(0, 10);
                number = BigInteger.Parse(numbers);
                if (BigInteger.Compare(number, v) > 0) break;
            }

            if (primality.IsPrimeMillerRabin(number))
            {
                return number;
            }
            else
            {
                return GenerateLargePrime(l0);
            }
        }

    }
}
