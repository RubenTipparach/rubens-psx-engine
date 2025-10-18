using Microsoft.Xna.Framework;
using System;

namespace rubens_psx_engine.Extensions
{
    public static class ColorExtensions
    {
        /// <summary>
        /// Creates a Color from a hex string (e.g., "#FF5733" or "FF5733")
        /// </summary>
        public static Color FromHex(string hex)
        {
            // Remove # if present
            hex = hex.TrimStart('#');

            if (hex.Length != 6)
            {
                throw new ArgumentException("Hex color must be 6 characters (RRGGBB)");
            }

            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);

            return new Color(r, g, b);
        }

        /// <summary>
        /// Gets the RGB values as floating point 0-1 range
        /// </summary>
        public static Vector3 ToVector3(this Color color)
        {
            return new Vector3(
                color.R / 255f,
                color.G / 255f,
                color.B / 255f
            );
        }

        /// <summary>
        /// Creates a Color from floating point RGB values (0-1 range)
        /// </summary>
        public static Color FromVector3(Vector3 rgb)
        {
            return new Color(
                (byte)(rgb.X * 255f),
                (byte)(rgb.Y * 255f),
                (byte)(rgb.Z * 255f)
            );
        }
    }
}
