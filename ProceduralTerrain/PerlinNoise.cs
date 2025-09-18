using System;

namespace ProceduralTerrain
{
    /// <summary>
    /// Perlin noise generator for procedural terrain generation
    /// </summary>
    public class PerlinNoise
    {
        private readonly Random random;
        private readonly int[] permutation;

        public PerlinNoise(int seed = 0)
        {
            random = new Random(seed);
            permutation = new int[512];

            int[] p = new int[256];
            for (int i = 0; i < 256; i++)
                p[i] = i;

            for (int i = 255; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }

            for (int i = 0; i < 256; i++)
            {
                permutation[i] = p[i];
                permutation[i + 256] = p[i];
            }
        }

        private double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);

        private double Lerp(double t, double a, double b) => a + t * (b - a);

        private double Grad(int hash, double x, double y, double z)
        {
            int h = hash & 15;
            double u = h < 8 ? x : y;
            double v = h < 4 ? y : h == 12 || h == 14 ? x : z;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        public double Noise(double x, double y, double z)
        {
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;
            int Z = (int)Math.Floor(z) & 255;

            x -= Math.Floor(x);
            y -= Math.Floor(y);
            z -= Math.Floor(z);

            double u = Fade(x);
            double v = Fade(y);
            double w = Fade(z);

            int A = permutation[X] + Y;
            int AA = permutation[A] + Z;
            int AB = permutation[A + 1] + Z;
            int B = permutation[X + 1] + Y;
            int BA = permutation[B] + Z;
            int BB = permutation[B + 1] + Z;

            return Lerp(w, Lerp(v, Lerp(u, Grad(permutation[AA], x, y, z),
                                         Grad(permutation[BA], x - 1, y, z)),
                                Lerp(u, Grad(permutation[AB], x, y - 1, z),
                                         Grad(permutation[BB], x - 1, y - 1, z))),
                        Lerp(v, Lerp(u, Grad(permutation[AA + 1], x, y, z - 1),
                                         Grad(permutation[BA + 1], x - 1, y, z - 1)),
                                Lerp(u, Grad(permutation[AB + 1], x, y - 1, z - 1),
                                         Grad(permutation[BB + 1], x - 1, y - 1, z - 1))));
        }

        public double OctaveNoise(double x, double y, double z, int octaves, double persistence)
        {
            double total = 0;
            double frequency = 1;
            double amplitude = 1;
            double maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                total += Noise(x * frequency, y * frequency, z * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }
    }
}