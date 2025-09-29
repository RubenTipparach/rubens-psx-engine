using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine.system.procedural
{
    public class PlanetGenerationParams
    {
        public float ContinentFrequency { get; set; } = 8.0f;   // Will be scaled down in GetPlanetHeight
        public float MountainFrequency { get; set; } = 15.0f;     // Will be scaled down in GetPlanetHeight
        public float DetailFrequency { get; set; } = 25.0f;      // Will be scaled down in GetPlanetHeight
        public float OceanLevel { get; set; } = 0.3f;  // Lower ocean level for more land
        public float OceanDepth { get; set; } = -0.1f;
        public float ContinentHeight { get; set; } = 0.2f;  // Higher continent elevation
        public float MountainHeight { get; set; } = 0.5f;   // Higher mountain elevation
        public float PolarCutoff { get; set; } = 0.7f;
        public int Seed { get; set; } = 42;
    }

    public class AdvancedPlanetRenderer : IDisposable
    {
        private GraphicsDevice graphicsDevice;
        private VertexBuffer vertexBuffer;
        private VertexBuffer coloredVertexBuffer;
        private IndexBuffer indexBuffer;
        private int primitiveCount;

        // Textures
        private Texture2D heightmapTexture;
        private Texture2D normalTexture;
        private Texture2D oceanMaskTexture;
        private Texture2D colorTexture;

        // Properties
        public float Radius { get; private set; }
        public int SubdivisionLevel { get; private set; }
        public PlanetGenerationParams Parameters { get; set; }
        public bool UseVertexColoring { get; set; } = false;

        // Texture data
        private float[,] heightmapData;
        private Color[,] colorData;
        private bool[,] oceanMaskData;

        private Random random;


        public AdvancedPlanetRenderer(GraphicsDevice device, float radius = 10f, int subdivisionLevel = 128)
        {
            graphicsDevice = device;
            Radius = radius;
            SubdivisionLevel = subdivisionLevel;
            Parameters = new PlanetGenerationParams();

            GeneratePlanet();
        }

        public void RegenerateWithParams(PlanetGenerationParams newParams)
        {
            Parameters = newParams;
            GeneratePlanet();
        }

        private void GeneratePlanet()
        {
            random = new Random(Parameters.Seed);

            // Generate heightmap data
            GenerateHeightmapData();

            // Generate textures from data
            GenerateTextures();

            // Generate mesh
            GenerateMesh();
        }

        private void GenerateHeightmapData()
        {
            int textureSize = 512; // Higher resolution for better detail
            heightmapData = new float[textureSize, textureSize];
            colorData = new Color[textureSize, textureSize];
            oceanMaskData = new bool[textureSize, textureSize];

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    // Convert to spherical coordinates
                    float u = x / (float)(textureSize - 1);
                    float v = y / (float)(textureSize - 1);

                    float theta = u * MathF.PI * 2f; // Longitude
                    float phi = v * MathF.PI; // Latitude

                    // Convert to 3D position on sphere
                    Vector3 spherePos = new Vector3(
                        MathF.Sin(phi) * MathF.Cos(theta),
                        MathF.Cos(phi),
                        MathF.Sin(phi) * MathF.Sin(theta)
                    );

                    // Generate elevation using single height function
                    float elevation = GetPlanetHeight(spherePos);
                    heightmapData[x, y] = elevation;

                    // Determine if ocean
                    oceanMaskData[x, y] = elevation < Parameters.OceanLevel;

                    // Generate color
                    colorData[x, y] = GetBiomeColor(elevation, spherePos);
                }
            }
        }


        private void GenerateTextures()
        {
            int size = heightmapData.GetLength(0);

            // Create heightmap texture
            heightmapTexture?.Dispose();
            heightmapTexture = new Texture2D(graphicsDevice, size, size);
            Color[] heightColors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float height = (heightmapData[x, y] + 1f) * 0.5f; // Normalize to 0-1
                    byte value = (byte)(height * 255);
                    heightColors[y * size + x] = new Color(value, value, value, (byte)255);
                }
            }
            heightmapTexture.SetData(heightColors);

            // Create normal map from heightmap
            normalTexture?.Dispose();
            normalTexture = new Texture2D(graphicsDevice, size, size);
            Color[] normalColors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector3 normal = CalculateNormalFromHeightmap(x, y, size);
                    normalColors[y * size + x] = new Color(
                        (byte)((normal.X + 1f) * 0.5f * 255),
                        (byte)((normal.Y + 1f) * 0.5f * 255),
                        (byte)((normal.Z + 1f) * 0.5f * 255),
                        (byte)255
                    );
                }
            }
            normalTexture.SetData(normalColors);

            // Create ocean mask texture
            oceanMaskTexture?.Dispose();
            oceanMaskTexture = new Texture2D(graphicsDevice, size, size);
            Color[] maskColors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    byte maskValue = oceanMaskData[x, y] ? (byte)255 : (byte)0;
                    maskColors[y * size + x] = new Color(maskValue, maskValue, maskValue, (byte)255);
                }
            }
            oceanMaskTexture.SetData(maskColors);

            // Create color texture
            colorTexture?.Dispose();
            colorTexture = new Texture2D(graphicsDevice, size, size);
            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    colors[y * size + x] = colorData[x, y];
                }
            }
            colorTexture.SetData(colors);
        }

        private Vector3 CalculateNormalFromHeightmap(int x, int y, int size)
        {
            float scale = 10f; // Adjust for normal strength

            // Sample neighboring heights
            float left = x > 0 ? heightmapData[x - 1, y] : heightmapData[x, y];
            float right = x < size - 1 ? heightmapData[x + 1, y] : heightmapData[x, y];
            float up = y > 0 ? heightmapData[x, y - 1] : heightmapData[x, y];
            float down = y < size - 1 ? heightmapData[x, y + 1] : heightmapData[x, y];

            // Calculate normal using central difference
            Vector3 normal = new Vector3(
                (left - right) * scale,
                2f,
                (up - down) * scale
            );

            return Vector3.Normalize(normal);
        }

        private void GenerateMesh()
        {
            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
            List<VertexPositionColor> coloredVertices = new List<VertexPositionColor>();
            List<int> indices = new List<int>();

            // Generate icosahedron-based sphere with proper subdivision
            GenerateIcosahedronSphere(vertices, coloredVertices, indices);

            // Create regular textured vertex buffer
            vertexBuffer?.Dispose();
            vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture),
                vertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices.ToArray());

            // Create vertex-colored buffer
            coloredVertexBuffer?.Dispose();
            coloredVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor),
                coloredVertices.Count, BufferUsage.WriteOnly);
            coloredVertexBuffer.SetData(coloredVertices.ToArray());

            indexBuffer?.Dispose();
            indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits,
                indices.Count, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices.ToArray());

            primitiveCount = indices.Count / 3;
        }

        private void GenerateIcosahedronSphere(List<VertexPositionNormalTexture> vertices, List<VertexPositionColor> coloredVertices, List<int> indices)
        {
            // Create initial icosahedron vertices
            var icosahedronVertices = CreateIcosahedronVertices();
            var icosahedronIndices = CreateIcosahedronIndices();

            // Subdivide the icosahedron
            for (int i = 0; i < SubdivisionLevel; i++)
            {
                SubdivideIcosahedron(ref icosahedronVertices, ref icosahedronIndices);
            }

            // Convert to final vertices - no deduplication to avoid holes
            for (int i = 0; i < icosahedronVertices.Count; i++)
            {
                Vector3 normalizedPos = Vector3.Normalize(icosahedronVertices[i]);

                // Sample height using single height function approach
                float elevation = GetPlanetHeight(normalizedPos);
                Vector3 position = normalizedPos * (Radius + elevation * Radius * 0.5f);  // Increased displacement for visible features
                Vector2 texCoord = GetUVForSpherePosition(normalizedPos);

                // Add textured vertex
                vertices.Add(new VertexPositionNormalTexture(position, normalizedPos, texCoord));

                // Add colored vertex with height-based color
                Color heightColor = GetHeightColor(elevation);
                coloredVertices.Add(new VertexPositionColor(position, heightColor));
            }

            // Use indices directly without remapping
            indices.AddRange(icosahedronIndices);
        }

        private List<Vector3> CreateIcosahedronVertices()
        {
            float phi = (1.0f + MathF.Sqrt(5.0f)) / 2.0f; // Golden ratio
            float invPhi = 1.0f / phi;

            var vertices = new List<Vector3>
            {
                // Vertices of icosahedron (12 vertices)
                Vector3.Normalize(new Vector3(-invPhi, phi, 0)),
                Vector3.Normalize(new Vector3(invPhi, phi, 0)),
                Vector3.Normalize(new Vector3(-invPhi, -phi, 0)),
                Vector3.Normalize(new Vector3(invPhi, -phi, 0)),
                Vector3.Normalize(new Vector3(0, -invPhi, phi)),
                Vector3.Normalize(new Vector3(0, invPhi, phi)),
                Vector3.Normalize(new Vector3(0, -invPhi, -phi)),
                Vector3.Normalize(new Vector3(0, invPhi, -phi)),
                Vector3.Normalize(new Vector3(phi, 0, -invPhi)),
                Vector3.Normalize(new Vector3(phi, 0, invPhi)),
                Vector3.Normalize(new Vector3(-phi, 0, -invPhi)),
                Vector3.Normalize(new Vector3(-phi, 0, invPhi))
            };

            return vertices;
        }

        private List<int> CreateIcosahedronIndices()
        {
            return new List<int>
            {
                // 20 triangular faces of icosahedron - corrected winding order for outward normals
                0, 5, 11, 0, 1, 5, 0, 7, 1, 0, 10, 7, 0, 11, 10,
                1, 9, 5, 5, 4, 11, 11, 2, 10, 10, 6, 7, 7, 8, 1,
                3, 4, 9, 3, 2, 4, 3, 6, 2, 3, 8, 6, 3, 9, 8,
                4, 5, 9, 2, 11, 4, 6, 10, 2, 8, 7, 6, 9, 1, 8
            };
        }

        private void SubdivideIcosahedron(ref List<Vector3> vertices, ref List<int> indices)
        {
            var newVertices = new List<Vector3>(vertices);
            var newIndices = new List<int>();
            var midpointCache = new Dictionary<(int, int), int>();

            for (int i = 0; i < indices.Count; i += 3)
            {
                int v1 = indices[i];
                int v2 = indices[i + 1];
                int v3 = indices[i + 2];

                // Get midpoints
                int m1 = GetMidpoint(v1, v2, newVertices, midpointCache);
                int m2 = GetMidpoint(v2, v3, newVertices, midpointCache);
                int m3 = GetMidpoint(v3, v1, newVertices, midpointCache);

                // Create 4 new triangles - maintain correct winding order
                newIndices.AddRange(new[] { v1, m3, m1 });
                newIndices.AddRange(new[] { v2, m1, m2 });
                newIndices.AddRange(new[] { v3, m2, m3 });
                newIndices.AddRange(new[] { m1, m3, m2 });
            }

            vertices = newVertices;
            indices = newIndices;
        }

        private int GetMidpoint(int i1, int i2, List<Vector3> vertices, Dictionary<(int, int), int> cache)
        {
            var key = i1 < i2 ? (i1, i2) : (i2, i1);

            if (cache.TryGetValue(key, out int cachedIndex))
                return cachedIndex;

            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 midpoint = Vector3.Normalize((v1 + v2) / 2.0f);

            int newIndex = vertices.Count;
            vertices.Add(midpoint);
            cache[key] = newIndex;

            return newIndex;
        }

        private float GetPlanetHeight(Vector3 directionFromPlanetCenter)
        {
            // Single height function with MUCH lower frequencies for realistic planetary features
            float elevation = 0f;

            // Continental shelf - low frequency for continent-scale features
            float continents = 0f;
            continents += SimplexNoise3D(directionFromPlanetCenter * (Parameters.ContinentFrequency * 0.1f) + new Vector3(Parameters.Seed)) * 1.0f;
            continents += SimplexNoise3D(directionFromPlanetCenter * (Parameters.ContinentFrequency * 0.2f) + new Vector3(Parameters.Seed * 1.3f)) * 0.5f;
            continents = (continents + 1.0f) * 0.5f;
            continents = MathF.Pow(continents, 2.5f); // More contrast for distinct continents

            // Check if this is ocean
            bool isOcean = continents < Parameters.OceanLevel;

            if (isOcean)
            {
                // Oceans are below sea level for proper color coding
                return -0.05f;
            }

            // Base continental elevation
            elevation = continents * Parameters.ContinentHeight;

            // Mountain ranges - medium frequency for realistic mountain chains
            float mountains = 0f;
            mountains += SimplexNoise3D(directionFromPlanetCenter * (Parameters.MountainFrequency * 0.15f) + new Vector3(Parameters.Seed * 2)) * 0.7f;
            mountains += SimplexNoise3D(directionFromPlanetCenter * (Parameters.MountainFrequency * 0.3f) + new Vector3(Parameters.Seed * 2.7f)) * 0.3f;
            mountains = Math.Max(0, mountains);
            // Apply mountains only to land areas
            float landMask = (continents - Parameters.OceanLevel) / (1f - Parameters.OceanLevel);
            mountains *= landMask;
            elevation += mountains * Parameters.MountainHeight;

            // Hills and details - higher frequency for surface details
            float details = 0f;
            details += SimplexNoise3D(directionFromPlanetCenter * (Parameters.DetailFrequency * 0.2f) + new Vector3(Parameters.Seed * 3)) * 0.05f;
            details += SimplexNoise3D(directionFromPlanetCenter * (Parameters.DetailFrequency * 0.4f) + new Vector3(Parameters.Seed * 3.3f)) * 0.025f;
            elevation += details * landMask; // Only on land

            // Polar ice caps
            float polarFactor = Math.Abs(directionFromPlanetCenter.Y);
            if (polarFactor > Parameters.PolarCutoff)
            {
                float iceFactor = (polarFactor - Parameters.PolarCutoff) / (1f - Parameters.PolarCutoff);
                elevation = Math.Max(elevation, 0.02f * iceFactor);
            }

            return elevation;
        }

        private class Vector3EqualityComparer : IEqualityComparer<Vector3>
        {
            private const float Epsilon = 0.0001f;

            public bool Equals(Vector3 x, Vector3 y)
            {
                return Math.Abs(x.X - y.X) < Epsilon &&
                       Math.Abs(x.Y - y.Y) < Epsilon &&
                       Math.Abs(x.Z - y.Z) < Epsilon;
            }

            public int GetHashCode(Vector3 obj)
            {
                return HashCode.Combine(
                    Math.Round(obj.X / Epsilon),
                    Math.Round(obj.Y / Epsilon),
                    Math.Round(obj.Z / Epsilon)
                );
            }
        }

        private float SampleHeightmapForPosition(Vector3 spherePos)
        {
            // Use the single height function directly for consistency
            return GetPlanetHeight(spherePos);
        }

        private Vector2 GetUVForSpherePosition(Vector3 spherePos)
        {
            float theta = MathF.Atan2(spherePos.Z, spherePos.X);
            float phi = MathF.Acos(spherePos.Y / spherePos.Length());

            float u = (theta + MathF.PI) / (2f * MathF.PI);
            float v = phi / MathF.PI;

            return new Vector2(u, v);
        }


        private Color GetHeightColor(float elevation)
        {
            // Create a color gradient based on elevation for height visualization
            if (elevation < -0.1f)
                return Color.DarkBlue;  // Deep water
            else if (elevation < 0.0f)
                return Color.Blue;      // Shallow water
            else if (elevation < 0.02f)
                return Color.SandyBrown;  // Beach/sand
            else if (elevation < 0.1f)
                return Color.Green;     // Low land
            else if (elevation < 0.2f)
                return Color.DarkGreen; // Hills
            else if (elevation < 0.4f)
                return Color.Brown;     // Mountains
            else
                return Color.White;     // Snow peaks
        }

        private Color GetBiomeColor(float elevation, Vector3 sphereNormal)
        {
            float polarFactor = Math.Abs(sphereNormal.Y);

            if (elevation < 0.0f)
            {
                return elevation < -0.05f ? new Color(10, 30, 80) : new Color(20, 60, 120);
            }
            else if (elevation < 0.02f)
            {
                return new Color(220, 200, 130);
            }
            else if (elevation < 0.1f)
            {
                if (polarFactor > Parameters.PolarCutoff)
                    return new Color(240, 240, 250);
                else if (polarFactor > 0.5f)
                    return new Color(100, 140, 100);
                else
                    return new Color(80, 160, 60);
            }
            else if (elevation < 0.25f)
            {
                return polarFactor > 0.6f ? new Color(230, 230, 240) : new Color(50, 100, 40);
            }
            else if (elevation < 0.4f)
            {
                return new Color(120, 100, 80);
            }
            else
            {
                return new Color(245, 245, 250);
            }
        }

        // Proper 3D Perlin noise implementation
        private float SimplexNoise3D(Vector3 pos)
        {
            // Use multiple octaves of noise for more natural results
            float noise = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f;

            // 4 octaves of noise
            for (int i = 0; i < 4; i++)
            {
                noise += Perlin3D(pos * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return noise / maxValue;
        }

        private float Perlin3D(Vector3 pos)
        {
            // Simple 3D Perlin noise implementation
            int X = (int)MathF.Floor(pos.X) & 255;
            int Y = (int)MathF.Floor(pos.Y) & 255;
            int Z = (int)MathF.Floor(pos.Z) & 255;

            pos.X -= MathF.Floor(pos.X);
            pos.Y -= MathF.Floor(pos.Y);
            pos.Z -= MathF.Floor(pos.Z);

            float u = Fade(pos.X);
            float v = Fade(pos.Y);
            float w = Fade(pos.Z);

            int A = Hash(X) + Y;
            int AA = Hash(A) + Z;
            int AB = Hash(A + 1) + Z;
            int B = Hash(X + 1) + Y;
            int BA = Hash(B) + Z;
            int BB = Hash(B + 1) + Z;

            return Lerp(w, Lerp(v, Lerp(u, Grad(Hash(AA), pos.X, pos.Y, pos.Z),
                                           Grad(Hash(BA), pos.X - 1, pos.Y, pos.Z)),
                                   Lerp(u, Grad(Hash(AB), pos.X, pos.Y - 1, pos.Z),
                                           Grad(Hash(BB), pos.X - 1, pos.Y - 1, pos.Z))),
                           Lerp(v, Lerp(u, Grad(Hash(AA + 1), pos.X, pos.Y, pos.Z - 1),
                                           Grad(Hash(BA + 1), pos.X - 1, pos.Y, pos.Z - 1)),
                                   Lerp(u, Grad(Hash(AB + 1), pos.X, pos.Y - 1, pos.Z - 1),
                                           Grad(Hash(BB + 1), pos.X - 1, pos.Y - 1, pos.Z - 1))));
        }

        private float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

        private float Lerp(float t, float a, float b) => a + t * (b - a);

        private float Grad(int hash, float x, float y, float z)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : h == 12 || h == 14 ? x : z;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        private int Hash(int x)
        {
            x = ((x >> 16) ^ x) * 0x45d9f3b;
            x = ((x >> 16) ^ x) * 0x45d9f3b;
            x = (x >> 16) ^ x;
            return x & 255;
        }

        public void Draw(GraphicsDevice device, Matrix world, Matrix view, Matrix projection, Effect effect)
        {
            if (UseVertexColoring)
            {
                // Use vertex-colored buffer for height visualization
                device.SetVertexBuffer(coloredVertexBuffer);
            }
            else
            {
                // Use textured buffer for planet shader
                device.SetVertexBuffer(vertexBuffer);

                // Set textures for planet shader
                effect.Parameters["HeightmapTexture"]?.SetValue(heightmapTexture);
                effect.Parameters["NormalTexture"]?.SetValue(normalTexture);
                effect.Parameters["OceanMaskTexture"]?.SetValue(oceanMaskTexture);
                effect.Parameters["ColorTexture"]?.SetValue(colorTexture);
            }

            device.Indices = indexBuffer;

            // Set matrices
            effect.Parameters["World"]?.SetValue(world);
            effect.Parameters["View"]?.SetValue(view);
            effect.Parameters["Projection"]?.SetValue(projection);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, primitiveCount);
            }
        }

        public Texture2D HeightmapTexture => heightmapTexture;
        public Texture2D NormalTexture => normalTexture;
        public Texture2D OceanMaskTexture => oceanMaskTexture;
        public Texture2D ColorTexture => colorTexture;

        public void Dispose()
        {
            vertexBuffer?.Dispose();
            coloredVertexBuffer?.Dispose();
            indexBuffer?.Dispose();
            heightmapTexture?.Dispose();
            normalTexture?.Dispose();
            oceanMaskTexture?.Dispose();
            colorTexture?.Dispose();
        }
    }
}