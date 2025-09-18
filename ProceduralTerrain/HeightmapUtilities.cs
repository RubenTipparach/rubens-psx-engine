using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ProceduralTerrain
{
    /// <summary>
    /// Utilities for working with heightmaps, including image generation
    /// </summary>
    public static class HeightmapUtilities
    {
        public static Bitmap GenerateHeightmapImage(float[,] heightMap)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            var bitmap = new Bitmap(width, height);

            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;

            // Find min and max heights
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (heightMap[x, y] < minHeight) minHeight = heightMap[x, y];
                    if (heightMap[x, y] > maxHeight) maxHeight = heightMap[x, y];
                }
            }

            float range = maxHeight - minHeight;
            if (range == 0) range = 1;

            // Generate bitmap
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float normalizedHeight = (heightMap[x, y] - minHeight) / range;
                    int grayValue = (int)(normalizedHeight * 255);
                    bitmap.SetPixel(x, y, Color.FromArgb(grayValue, grayValue, grayValue));
                }
            }

            return bitmap;
        }

        public static void SaveHeightmapTexture(float[,] heightMap, string filePath)
        {
            using (var bitmap = GenerateHeightmapImage(heightMap))
            {
                bitmap.Save(filePath, ImageFormat.Png);
            }
        }

        public static void SaveHeightmapTexture(TerrainData terrainData, string filePath)
        {
            SaveHeightmapTexture(terrainData.HeightMap, filePath);
        }
    }
}