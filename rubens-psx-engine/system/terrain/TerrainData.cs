using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace rubens_psx_engine.system.terrain
{
    /// <summary>
    /// Core terrain data structure with mesh generation and serialization capabilities
    /// </summary>
    public class TerrainData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public float[,] HeightMap { get; set; }
        public VertexPositionNormalTexture[] Vertices { get; set; }
        public int[] Indices { get; set; }
        public float Scale { get; set; }
        public float HeightScale { get; set; }

        public TerrainData(int width, int height)
        {
            Width = width;
            Height = height;
            HeightMap = new float[width, height];
            Scale = 1.0f;
            HeightScale = 10.0f;
        }

        public void GenerateTerrain(int seed, float noiseScale, int octaves, float persistence)
        {
            PerlinNoise noise = new PerlinNoise(seed);

            // Generate base heightmap
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Height; z++)
                {
                    float xCoord = (float)x / Width * noiseScale;
                    float zCoord = (float)z / Height * noiseScale;
                    HeightMap[x, z] = (float)noise.OctaveNoise(xCoord, zCoord, 0, octaves, persistence);
                }
            }

            // Smooth the terrain
            SmoothTerrain(3);

            // Create river through the middle
            CreateRiver();

            GenerateMesh();
        }

        private void SmoothTerrain(int iterations)
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                float[,] smoothed = new float[Width, Height];

                for (int x = 0; x < Width; x++)
                {
                    for (int z = 0; z < Height; z++)
                    {
                        float sum = 0f;
                        int count = 0;

                        // Average with neighbors
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                int nx = x + dx;
                                int nz = z + dz;

                                if (nx >= 0 && nx < Width && nz >= 0 && nz < Height)
                                {
                                    sum += HeightMap[nx, nz];
                                    count++;
                                }
                            }
                        }

                        smoothed[x, z] = sum / count;
                    }
                }

                HeightMap = smoothed;
            }
        }

        private void CreateRiver()
        {
            float riverWidth = 6.0f;
            float riverDepth = 0.3f;
            int centerZ = Height / 2;

            // Create a meandering river through the center
            for (int x = 0; x < Width; x++)
            {
                // Create meandering pattern
                float meander = (float)Math.Sin((float)x / Width * Math.PI * 3) * 8f;
                int riverCenterZ = (int)(centerZ + meander);

                for (int z = 0; z < Height; z++)
                {
                    float distanceFromRiver = Math.Abs(z - riverCenterZ);

                    if (distanceFromRiver < riverWidth)
                    {
                        // Create smooth river banks
                        float riverInfluence = 1.0f - (distanceFromRiver / riverWidth);
                        riverInfluence = (float)Math.Pow(riverInfluence, 2); // Smooth curve

                        // Lower the terrain to create the river
                        HeightMap[x, z] -= riverDepth * riverInfluence;
                    }
                }
            }
        }

        public void GenerateMesh()
        {
            List<VertexPositionNormalTexture> vertexList = new List<VertexPositionNormalTexture>();
            List<int> indexList = new List<int>();

            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Height; z++)
                {
                    Vector3 position = new Vector3(x * Scale, HeightMap[x, z] * HeightScale, z * Scale);
                    Vector3 normal = CalculateNormal(x, z);
                    Vector2 texCoord = new Vector2((float)x / (Width - 1), (float)z / (Height - 1));

                    vertexList.Add(new VertexPositionNormalTexture(position, normal, texCoord));
                }
            }

            for (int x = 0; x < Width - 1; x++)
            {
                for (int z = 0; z < Height - 1; z++)
                {
                    int topLeft = x * Height + z;
                    int topRight = (x + 1) * Height + z;
                    int bottomLeft = x * Height + (z + 1);
                    int bottomRight = (x + 1) * Height + (z + 1);

                    indexList.Add(topLeft);
                    indexList.Add(topRight);
                    indexList.Add(bottomLeft);

                    indexList.Add(topRight);
                    indexList.Add(bottomRight);
                    indexList.Add(bottomLeft);
                }
            }

            Vertices = vertexList.ToArray();
            Indices = indexList.ToArray();
        }

        private Vector3 CalculateNormal(int x, int z)
        {
            float heightL = x > 0 ? HeightMap[x - 1, z] : HeightMap[x, z];
            float heightR = x < Width - 1 ? HeightMap[x + 1, z] : HeightMap[x, z];
            float heightD = z > 0 ? HeightMap[x, z - 1] : HeightMap[x, z];
            float heightU = z < Height - 1 ? HeightMap[x, z + 1] : HeightMap[x, z];

            Vector3 normal = new Vector3(heightL - heightR, 2.0f, heightD - heightU);
            normal.Normalize();
            return normal;
        }

        public float GetHeightAt(float x, float z)
        {
            int gridX = (int)(x / Scale);
            int gridZ = (int)(z / Scale);

            if (gridX < 0 || gridX >= Width - 1 || gridZ < 0 || gridZ >= Height - 1)
                return 0;

            float fractionalX = (x / Scale) - gridX;
            float fractionalZ = (z / Scale) - gridZ;

            float height1 = HeightMap[gridX, gridZ];
            float height2 = HeightMap[gridX + 1, gridZ];
            float height3 = HeightMap[gridX, gridZ + 1];
            float height4 = HeightMap[gridX + 1, gridZ + 1];

            float interpolatedHeight1 = height1 * (1 - fractionalX) + height2 * fractionalX;
            float interpolatedHeight2 = height3 * (1 - fractionalX) + height4 * fractionalX;

            return (interpolatedHeight1 * (1 - fractionalZ) + interpolatedHeight2 * fractionalZ) * HeightScale;
        }

        public void SaveToOBJ(string filePath)
        {
            using (StreamWriter file = new StreamWriter(filePath))
            {
                file.WriteLine("# Terrain OBJ File Generated by ProceduralTerrain");
                file.WriteLine($"# Vertices: {Vertices.Length}");
                file.WriteLine($"# Faces: {Indices.Length / 3}");
                file.WriteLine();

                foreach (var vertex in Vertices)
                {
                    file.WriteLine($"v {vertex.Position.X} {vertex.Position.Y} {vertex.Position.Z}");
                }

                foreach (var vertex in Vertices)
                {
                    file.WriteLine($"vt {vertex.TextureCoordinate.X} {vertex.TextureCoordinate.Y}");
                }

                foreach (var vertex in Vertices)
                {
                    file.WriteLine($"vn {vertex.Normal.X} {vertex.Normal.Y} {vertex.Normal.Z}");
                }

                for (int i = 0; i < Indices.Length; i += 3)
                {
                    int v1 = Indices[i] + 1;
                    int v2 = Indices[i + 1] + 1;
                    int v3 = Indices[i + 2] + 1;
                    file.WriteLine($"f {v1}/{v1}/{v1} {v2}/{v2}/{v2} {v3}/{v3}/{v3}");
                }
            }
        }

        public void SaveToFBX(string filePath)
        {
            var fbxContent = GenerateFBXContent();
            File.WriteAllText(filePath, fbxContent);
        }

        private string GenerateFBXContent()
        {
            var sb = new StringBuilder();

            sb.AppendLine("; FBX 7.3.0 project file");
            sb.AppendLine("; Generated by ProceduralTerrain");
            sb.AppendLine();
            sb.AppendLine("FBXHeaderExtension: {");
            sb.AppendLine("\tFBXHeaderVersion: 1003");
            sb.AppendLine("\tFBXVersion: 7300");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("Objects: {");
            sb.AppendLine("\tGeometry: \"Geometry::Terrain\", \"Mesh\" {");
            sb.AppendLine($"\t\tVertices: *{Vertices.Length * 3} {{");
            sb.Append("\t\t\ta: ");
            for (int i = 0; i < Vertices.Length; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"{Vertices[i].Position.X},{Vertices[i].Position.Y},{Vertices[i].Position.Z}");
            }
            sb.AppendLine();
            sb.AppendLine("\t\t}");

            sb.AppendLine($"\t\tPolygonVertexIndex: *{Indices.Length} {{");
            sb.Append("\t\t\ta: ");
            for (int i = 0; i < Indices.Length; i += 3)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"{Indices[i]},{Indices[i + 1]},{-Indices[i + 2] - 1}");
            }
            sb.AppendLine();
            sb.AppendLine("\t\t}");

            sb.AppendLine($"\t\tNormals: *{Vertices.Length * 3} {{");
            sb.Append("\t\t\ta: ");
            for (int i = 0; i < Vertices.Length; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"{Vertices[i].Normal.X},{Vertices[i].Normal.Y},{Vertices[i].Normal.Z}");
            }
            sb.AppendLine();
            sb.AppendLine("\t\t}");

            sb.AppendLine($"\t\tUV: *{Vertices.Length * 2} {{");
            sb.Append("\t\t\ta: ");
            for (int i = 0; i < Vertices.Length; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"{Vertices[i].TextureCoordinate.X},{Vertices[i].TextureCoordinate.Y}");
            }
            sb.AppendLine();
            sb.AppendLine("\t\t}");

            sb.AppendLine("\t}");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}