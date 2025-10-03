using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace rubens_psx_engine.system.procedural
{
    public class PlanetParameters
    {
        public float ContinentFrequency { get; set; } = 1.5f;
        public float MountainFrequency { get; set; } = 4.0f;
        public float DetailFrequency { get; set; } = 8.0f;
        public float WorleyFrequency { get; set; } = 2.0f;
        public float OceanLevel { get; set; } = 7.5f;
        public float ContinentHeight { get; set; } = 0.2f;
        public float MountainHeight { get; set; } = 0.5f;
        public float WorleyStrength { get; set; } = 0.3f;
        public int Seed { get; set; } = 42;
    }

    public class ProceduralPlanetGenerator : IDisposable
    {
        private GraphicsDevice graphicsDevice;
        private float radius;
        public int HeightmapResolution { get; private set; }
        public PlanetParameters Parameters { get; set; }

        // Heightmap data and texture
        private float[,] heightmapData;
        private Texture2D heightmapTexture;
        public Texture2D HeightmapTexture => heightmapTexture;

        // Normal map data and texture
        private Vector3[,] normalMapData;
        private Texture2D normalMapTexture;
        public Texture2D NormalMapTexture => normalMapTexture;

        // Mesh data
        private VertexBuffer texturedVertexBuffer;
        private VertexBuffer coloredVertexBuffer;
        private IndexBuffer indexBuffer;
        private int primitiveCount;

        // Icosahedron base data
        private List<Vector3> icosahedronVertices;
        private List<int> icosahedronIndices;

        private Random random;

        public ProceduralPlanetGenerator(GraphicsDevice device, float radius = 20f, int heightmapResolution = 1024)
        {
            this.graphicsDevice = device;
            this.radius = radius;
            this.HeightmapResolution = heightmapResolution;
            this.Parameters = new PlanetParameters();

            // Initialize icosahedron base geometry
            CreateIcosahedronBase();
        }

        public void GeneratePlanet()
        {
            random = new Random(Parameters.Seed);

            // Generate high-resolution heightmap
            GenerateHeightmapData();

            // Generate normal map from heightmap
            GenerateNormalMapData();

            // Create texture from heightmap data
            CreateHeightmapTexture();

            // Create normal map texture
            CreateNormalMapTexture();

            // Generate mesh geometry from heightmap
            GenerateMeshFromHeightmap();
        }

        private void CreateIcosahedronBase()
        {
            icosahedronVertices = new List<Vector3>();
            icosahedronIndices = new List<int>();

            // Create icosahedron vertices (12 vertices)
            float phi = (1.0f + MathF.Sqrt(5.0f)) / 2.0f; // Golden ratio
            float invPhi = 1.0f / phi;

            // 12 vertices of icosahedron
            icosahedronVertices.AddRange(new Vector3[]
            {
                new Vector3(-invPhi, phi, 0), new Vector3(invPhi, phi, 0), new Vector3(-invPhi, -phi, 0), new Vector3(invPhi, -phi, 0),
                new Vector3(0, -invPhi, phi), new Vector3(0, invPhi, phi), new Vector3(0, -invPhi, -phi), new Vector3(0, invPhi, -phi),
                new Vector3(phi, 0, -invPhi), new Vector3(phi, 0, invPhi), new Vector3(-phi, 0, -invPhi), new Vector3(-phi, 0, invPhi)
            });

            // Normalize vertices to unit sphere
            for (int i = 0; i < icosahedronVertices.Count; i++)
            {
                icosahedronVertices[i] = Vector3.Normalize(icosahedronVertices[i]);
            }

            // 20 triangular faces of icosahedron (counter-clockwise winding for outward-facing normals)
            icosahedronIndices.AddRange(new int[]
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            });
        }

        private void GenerateHeightmapData()
        {
            heightmapData = new float[HeightmapResolution, HeightmapResolution];

            for (int y = 0; y < HeightmapResolution; y++)
            {
                for (int x = 0; x < HeightmapResolution; x++)
                {
                    // Convert 2D coordinates to sphere coordinates
                    float u = x / (float)(HeightmapResolution - 1);
                    float v = y / (float)(HeightmapResolution - 1);

                    // Convert to spherical coordinates
                    float theta = u * MathF.PI * 2f; // Longitude
                    float phi = v * MathF.PI; // Latitude

                    // Convert to 3D position on unit sphere
                    Vector3 spherePos = new Vector3(
                        MathF.Sin(phi) * MathF.Cos(theta),
                        MathF.Cos(phi),
                        MathF.Sin(phi) * MathF.Sin(theta)
                    );

                    // Calculate height using enhanced height function
                    float height = GetPlanetHeight(spherePos);
                    heightmapData[x, y] = height;
                }
            }
        }

        private float GetPlanetHeight(Vector3 directionFromPlanetCenter)
        {
            Vector3 pos = directionFromPlanetCenter;

            // Domain warping for more organic, archipelago-like shapes
            float warpStrength = 0.25f;
            Vector3 warp = new Vector3(
                PerlinNoise3D(pos * 1.2f + new Vector3(Parameters.Seed)),
                PerlinNoise3D(pos * 1.2f + new Vector3(Parameters.Seed + 100)),
                PerlinNoise3D(pos * 1.2f + new Vector3(Parameters.Seed + 200))
            ) * warpStrength;
            pos += warp;

            // Continental base - more frequent for islands
            float continents = FractalNoise(pos, Parameters.ContinentFrequency * 1.5f, 5, 2.2f, 0.55f, Parameters.Seed);
            continents = (continents + 1.0f) * 0.5f; // Normalize to [0,1]

            // Adjust for 60% land coverage - gentle power curve with significant boost
            continents = MathF.Pow(continents, 0.9f) * 1.9f;
            continents = MathHelper.Clamp(continents, 0f, 1f);

            // Worley noise for island/archipelago patterns
            float worley = WorleyNoise3D(pos * Parameters.WorleyFrequency * 1.8f + new Vector3(Parameters.Seed * 2));
            continents = continents * (0.6f + worley * Parameters.WorleyStrength * 0.8f);

            // Base elevation with more variation
            float elevation = continents * Parameters.ContinentHeight;

            // More permissive terrain mask for mountains everywhere
            float terrainMask = MathHelper.Clamp((continents - 0.2f) / 0.8f, 0f, 1f);
            terrainMask = MathF.Pow(terrainMask, 0.8f);

            // Dramatic mountain ranges using ridged multi-fractal
            float mountains = RidgedNoise(pos, Parameters.MountainFrequency * 1.2f, 4, 2.3f, 0.5f, Parameters.Seed + 1000);
            mountains = MathF.Pow(mountains, 1.8f); // Very sharp peaks

            // Mountain detail layer
            float mountainDetail1 = RidgedNoise(pos, Parameters.MountainFrequency * 3.5f, 3, 2.1f, 0.5f, Parameters.Seed + 2000);
            mountains = mountains * (0.8f + mountainDetail1 * 0.2f);

            // Significant mountain contribution
            elevation += mountains * Parameters.MountainHeight * 1.5f * terrainMask;

            // Rolling hills using billowy noise
            float hills = FractalNoise(pos, Parameters.DetailFrequency * 0.7f, 3, 2.1f, 0.6f, Parameters.Seed + 3000);
            hills = MathF.Abs(hills); // Billow effect
            elevation += hills * 0.25f * terrainMask;

            // Fine surface details for texture
            float details = FractalNoise(pos, Parameters.DetailFrequency * 2.0f, 2, 2.0f, 0.5f, Parameters.Seed + 4000);
            elevation += details * 0.05f * terrainMask;

            // Deeper valleys using inverted ridged noise
            float valleys = RidgedNoise(pos, Parameters.MountainFrequency * 0.8f, 3, 2.0f, 0.5f, Parameters.Seed + 5000);
            valleys = 1.0f - valleys;
            elevation -= valleys * 0.15f * terrainMask * continents;

            // Polar regions - don't flatten, just slight ice caps
            float polarFactor = MathF.Abs(directionFromPlanetCenter.Y);
            if (polarFactor > 0.85f)
            {
                float polarBlend = (polarFactor - 0.85f) / 0.15f;
                elevation += polarBlend * 0.1f; // Slight ice cap elevation
            }

            return elevation; // Allow negative values for underwater terrain
        }

        /// <summary>
        /// Multi-octave fractal noise (standard Perlin)
        /// </summary>
        private float FractalNoise(Vector3 pos, float frequency, int octaves, float lacunarity, float persistence, int seed)
        {
            float result = 0f;
            float amplitude = 1f;
            float maxAmplitude = 0f;

            for (int i = 0; i < octaves; i++)
            {
                result += PerlinNoise3D(pos * frequency + new Vector3(seed + i * 123)) * amplitude;
                maxAmplitude += amplitude;

                frequency *= lacunarity;
                amplitude *= persistence;
            }

            return result / maxAmplitude;
        }

        /// <summary>
        /// Ridged multi-fractal noise for mountain ranges
        /// </summary>
        private float RidgedNoise(Vector3 pos, float frequency, int octaves, float lacunarity, float persistence, int seed)
        {
            float result = 0f;
            float amplitude = 1f;
            float maxAmplitude = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float noise = PerlinNoise3D(pos * frequency + new Vector3(seed + i * 456));
                noise = 1.0f - MathF.Abs(noise); // Ridge operation
                noise = noise * noise; // Sharpen ridges

                result += noise * amplitude;
                maxAmplitude += amplitude;

                frequency *= lacunarity;
                amplitude *= persistence;
            }

            return result / maxAmplitude;
        }

        private void CreateHeightmapTexture()
        {
            heightmapTexture?.Dispose();
            heightmapTexture = new Texture2D(graphicsDevice, HeightmapResolution, HeightmapResolution);

            // Find min/max for normalization
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;

            for (int y = 0; y < HeightmapResolution; y++)
            {
                for (int x = 0; x < HeightmapResolution; x++)
                {
                    float height = heightmapData[x, y];
                    minHeight = MathF.Min(minHeight, height);
                    maxHeight = MathF.Max(maxHeight, height);
                }
            }

            // Ensure we have a good range
            float range = maxHeight - minHeight;
            if (range < 0.001f) range = 1.0f;

            Color[] heightColors = new Color[HeightmapResolution * HeightmapResolution];
            for (int y = 0; y < HeightmapResolution; y++)
            {
                for (int x = 0; x < HeightmapResolution; x++)
                {
                    float height = heightmapData[x, y];
                    // Normalize to [0,1] using actual min/max for full gradient range
                    float normalizedHeight = (height - minHeight) / range;
                    normalizedHeight = MathHelper.Clamp(normalizedHeight, 0f, 1f);

                    byte heightValue = (byte)(normalizedHeight * 255);
                    heightColors[y * HeightmapResolution + x] = new Color(heightValue, heightValue, heightValue, (byte)255);
                }
            }
            heightmapTexture.SetData(heightColors);
        }

        private void GenerateNormalMapData()
        {
            normalMapData = new Vector3[HeightmapResolution, HeightmapResolution];
            float strength = 1.0f; // Normal map strength

            for (int y = 0; y < HeightmapResolution; y++)
            {
                for (int x = 0; x < HeightmapResolution; x++)
                {
                    // Sample neighboring heights with wrapping
                    int left = (x - 1 + HeightmapResolution) % HeightmapResolution;
                    int right = (x + 1) % HeightmapResolution;
                    int top = (y - 1 + HeightmapResolution) % HeightmapResolution;
                    int bottom = (y + 1) % HeightmapResolution;

                    float heightL = heightmapData[left, y];
                    float heightR = heightmapData[right, y];
                    float heightU = heightmapData[x, top];
                    float heightD = heightmapData[x, bottom];

                    // Calculate gradient using finite differences
                    float dx = (heightR - heightL) * strength;
                    float dy = (heightD - heightU) * strength;

                    // Create normal vector (cross product approach)
                    Vector3 normal = Vector3.Normalize(new Vector3(-dx, 1.0f, -dy));

                    // Store normal data
                    normalMapData[x, y] = normal;
                }
            }
        }

        private void CreateNormalMapTexture()
        {
            normalMapTexture?.Dispose();
            normalMapTexture = new Texture2D(graphicsDevice, HeightmapResolution, HeightmapResolution);

            Color[] normalColors = new Color[HeightmapResolution * HeightmapResolution];
            for (int y = 0; y < HeightmapResolution; y++)
            {
                for (int x = 0; x < HeightmapResolution; x++)
                {
                    Vector3 normal = normalMapData[x, y];

                    // Convert from [-1,1] to [0,1] range for texture storage
                    byte r = (byte)((normal.X * 0.5f + 0.5f) * 255);
                    byte g = (byte)((normal.Y * 0.5f + 0.5f) * 255);
                    byte b = (byte)((normal.Z * 0.5f + 0.5f) * 255);

                    normalColors[y * HeightmapResolution + x] = new Color(r, g, b, (byte)255);
                }
            }
            normalMapTexture.SetData(normalColors);
        }

        private void GenerateMeshFromHeightmap()
        {
            List<VertexPositionNormalTexture> texturedVertices = new List<VertexPositionNormalTexture>();
            List<VertexPositionColor> coloredVertices = new List<VertexPositionColor>();
            List<int> indices = new List<int>();

            // Dynamic LOD based on camera distance (up to 5 subdivisions for high detail)
            int subdivisionLevel = 5; // Much higher detail
            var subdivVertices = new List<Vector3>(icosahedronVertices);
            var subdivIndices = new List<int>(icosahedronIndices);

            for (int subdivision = 0; subdivision < subdivisionLevel; subdivision++)
            {
                SubdivideIcosahedron(ref subdivVertices, ref subdivIndices);
            }

            // Create vertices with heightmap displacement
            var vertexData = new List<(Vector3 position, Vector3 normal, Vector2 uv, Color color)>();
            for (int i = 0; i < subdivVertices.Count; i++)
            {
                Vector3 normalizedPos = Vector3.Normalize(subdivVertices[i]);

                // Sample heightmap at this position
                float height = SampleHeightmapAtPosition(normalizedPos);

                // Create displaced position
                Vector3 position = normalizedPos * (radius + height * radius * 0.3f);

                // Calculate UV coordinates
                Vector2 texCoord = GetUVForSpherePosition(normalizedPos);

                // Store vertex data
                Color heightColor = GetHeightColor(height);
                vertexData.Add((position, normalizedPos, texCoord, heightColor));
            }

            // Fix UV seams by duplicating vertices where triangles cross seam boundaries
            // Use a per-triangle remap to handle multiple fixes per vertex
            for (int triIndex = 0; triIndex < subdivIndices.Count; triIndex += 3)
            {
                int i0 = subdivIndices[triIndex];
                int i1 = subdivIndices[triIndex + 1];
                int i2 = subdivIndices[triIndex + 2];

                Vector2 uv0 = vertexData[i0].uv;
                Vector2 uv1 = vertexData[i1].uv;
                Vector2 uv2 = vertexData[i2].uv;

                // Check if triangle crosses the UV seam (U wraps from ~1.0 to ~0.0)
                float maxU = MathF.Max(MathF.Max(uv0.X, uv1.X), uv2.X);
                float minU = MathF.Min(MathF.Min(uv0.X, uv1.X), uv2.X);
                bool crossesSeam = (maxU - minU) > 0.5f;

                if (crossesSeam)
                {
                    // Determine which side of the seam has more vertices (that's the "correct" side)
                    float avgU = (uv0.X + uv1.X + uv2.X) / 3.0f;

                    // Fix vertices that are far from average
                    if (MathF.Abs(uv0.X - avgU) > 0.4f)
                    {
                        var newData = vertexData[i0];
                        newData.uv.X = uv0.X < 0.5f ? uv0.X + 1.0f : uv0.X - 1.0f;
                        i0 = vertexData.Count;
                        vertexData.Add(newData);
                    }

                    if (MathF.Abs(uv1.X - avgU) > 0.4f)
                    {
                        var newData = vertexData[i1];
                        newData.uv.X = uv1.X < 0.5f ? uv1.X + 1.0f : uv1.X - 1.0f;
                        i1 = vertexData.Count;
                        vertexData.Add(newData);
                    }

                    if (MathF.Abs(uv2.X - avgU) > 0.4f)
                    {
                        var newData = vertexData[i2];
                        newData.uv.X = uv2.X < 0.5f ? uv2.X + 1.0f : uv2.X - 1.0f;
                        i2 = vertexData.Count;
                        vertexData.Add(newData);
                    }

                    subdivIndices[triIndex] = i0;
                    subdivIndices[triIndex + 1] = i1;
                    subdivIndices[triIndex + 2] = i2;
                }

                // Also check for pole issues (vertices near v=0 or v=1)
                float maxV = MathF.Max(MathF.Max(uv0.Y, uv1.Y), uv2.Y);
                float minV = MathF.Min(MathF.Min(uv0.Y, uv1.Y), uv2.Y);

                if (maxV > 0.95f || minV < 0.05f)
                {
                    // Near pole - ensure vertices are duplicated if they have significantly different U coords
                    if ((maxV > 0.95f || minV < 0.05f) && (maxU - minU) > 0.3f)
                    {
                        // Average the U coordinates for pole vertices
                        float targetU = (uv0.X + uv1.X + uv2.X) / 3.0f;

                        if (uv0.Y > 0.95f || uv0.Y < 0.05f)
                        {
                            if (MathF.Abs(uv0.X - targetU) > 0.1f)
                            {
                                var newData = vertexData[subdivIndices[triIndex]];
                                newData.uv.X = targetU;
                                subdivIndices[triIndex] = vertexData.Count;
                                vertexData.Add(newData);
                            }
                        }

                        if (uv1.Y > 0.95f || uv1.Y < 0.05f)
                        {
                            if (MathF.Abs(uv1.X - targetU) > 0.1f)
                            {
                                var newData = vertexData[subdivIndices[triIndex + 1]];
                                newData.uv.X = targetU;
                                subdivIndices[triIndex + 1] = vertexData.Count;
                                vertexData.Add(newData);
                            }
                        }

                        if (uv2.Y > 0.95f || uv2.Y < 0.05f)
                        {
                            if (MathF.Abs(uv2.X - targetU) > 0.1f)
                            {
                                var newData = vertexData[subdivIndices[triIndex + 2]];
                                newData.uv.X = targetU;
                                subdivIndices[triIndex + 2] = vertexData.Count;
                                vertexData.Add(newData);
                            }
                        }
                    }
                }
            }

            // Build final vertex buffers from fixed vertex data
            for (int i = 0; i < vertexData.Count; i++)
            {
                var data = vertexData[i];
                texturedVertices.Add(new VertexPositionNormalTexture(data.position, data.normal, data.uv));
                coloredVertices.Add(new VertexPositionColor(data.position, data.color));
            }

            // Use subdivision indices
            indices.AddRange(subdivIndices);

            // Create vertex buffers
            texturedVertexBuffer?.Dispose();
            texturedVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture),
                texturedVertices.Count, BufferUsage.WriteOnly);
            texturedVertexBuffer.SetData(texturedVertices.ToArray());

            coloredVertexBuffer?.Dispose();
            coloredVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor),
                coloredVertices.Count, BufferUsage.WriteOnly);
            coloredVertexBuffer.SetData(coloredVertices.ToArray());

            // Create index buffer
            indexBuffer?.Dispose();
            indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits,
                indices.Count, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices.ToArray());

            primitiveCount = indices.Count / 3;
        }

        private float SampleHeightmapAtPosition(Vector3 spherePos)
        {
            // Convert sphere position to heightmap UV coordinates with improved mapping
            float theta = MathF.Atan2(spherePos.X, spherePos.Z);
            float phi = MathF.Asin(MathHelper.Clamp(spherePos.Y, -1f, 1f));

            float u = 0.5f + theta / (2f * MathF.PI);
            float v = 0.5f + phi / MathF.PI;

            // Wrap u coordinate
            u = u - MathF.Floor(u);
            v = MathHelper.Clamp(v, 0f, 1f);

            // Sample heightmap with bilinear filtering
            int x = (int)(u * (HeightmapResolution - 1));
            int y = (int)(v * (HeightmapResolution - 1));

            x = MathHelper.Clamp(x, 0, HeightmapResolution - 1);
            y = MathHelper.Clamp(y, 0, HeightmapResolution - 1);

            return heightmapData[x, y];
        }

        private Vector2 GetUVForSpherePosition(Vector3 spherePos)
        {
            // Improved UV mapping for better alignment
            float theta = MathF.Atan2(spherePos.X, spherePos.Z); // Swapped for correct orientation
            float phi = MathF.Asin(MathHelper.Clamp(spherePos.Y, -1f, 1f)); // Use Asin for better distribution

            float u = 0.5f + theta / (2f * MathF.PI);
            float v = 0.5f + phi / MathF.PI;

            // Wrap UV coordinates
            u = u - MathF.Floor(u);
            v = MathHelper.Clamp(v, 0f, 1f);

            return new Vector2(u, v);
        }

        private Color GetHeightColor(float elevation)
        {
            if (elevation < -0.05f)
                return Color.DarkBlue;  // Deep water
            else if (elevation < 0.0f)
                return Color.Blue;      // Shallow water
            else if (elevation < 0.02f)
                return Color.SandyBrown;  // Beach
            else if (elevation < 0.1f)
                return Color.Green;     // Low land
            else if (elevation < 0.2f)
                return Color.DarkGreen; // Hills
            else if (elevation < 0.4f)
                return Color.Brown;     // Mountains
            else
                return Color.White;     // Snow peaks
        }

        private void SubdivideIcosahedron(ref List<Vector3> vertices, ref List<int> indices)
        {
            List<Vector3> newVertices = new List<Vector3>(vertices);
            List<int> newIndices = new List<int>();
            Dictionary<(int, int), int> midpointCache = new Dictionary<(int, int), int>();

            for (int i = 0; i < indices.Count; i += 3)
            {
                int v1 = indices[i];
                int v2 = indices[i + 1];
                int v3 = indices[i + 2];

                int m1 = GetMidpoint(v1, v2, newVertices, midpointCache);
                int m2 = GetMidpoint(v2, v3, newVertices, midpointCache);
                int m3 = GetMidpoint(v3, v1, newVertices, midpointCache);

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

        public void SaveHeightmapToDisk()
        {
            try
            {
                string filename = $"heightmap_seed_{Parameters.Seed}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string filepath = Path.Combine(".", filename);

                using (var stream = new FileStream(filepath, FileMode.Create))
                {
                    heightmapTexture.SaveAsPng(stream, HeightmapResolution, HeightmapResolution);
                }

                Console.WriteLine($"Heightmap saved to: {filepath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save heightmap: {ex.Message}");
            }
        }

        public void Draw(GraphicsDevice device, Matrix world, Matrix view, Matrix projection, Effect effect, bool useVertexColoring)
        {
            if (useVertexColoring)
            {
                device.SetVertexBuffer(coloredVertexBuffer);
            }
            else
            {
                device.SetVertexBuffer(texturedVertexBuffer);
            }

            device.Indices = indexBuffer;

            effect.Parameters["World"]?.SetValue(world);
            effect.Parameters["View"]?.SetValue(view);
            effect.Parameters["Projection"]?.SetValue(projection);

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, primitiveCount);
            }
        }

        // Noise functions
        private float PerlinNoise3D(Vector3 position)
        {
            // Simplified 3D Perlin noise implementation
            int xi = (int)MathF.Floor(position.X) & 255;
            int yi = (int)MathF.Floor(position.Y) & 255;
            int zi = (int)MathF.Floor(position.Z) & 255;

            float xf = position.X - MathF.Floor(position.X);
            float yf = position.Y - MathF.Floor(position.Y);
            float zf = position.Z - MathF.Floor(position.Z);

            float u = Fade(xf);
            float v = Fade(yf);
            float w = Fade(zf);

            int a = Perm(xi) + yi;
            int aa = Perm(a) + zi;
            int ab = Perm(a + 1) + zi;
            int b = Perm(xi + 1) + yi;
            int ba = Perm(b) + zi;
            int bb = Perm(b + 1) + zi;

            return Lerp(w,
                Lerp(v,
                    Lerp(u, Grad(Perm(aa), xf, yf, zf), Grad(Perm(ba), xf - 1, yf, zf)),
                    Lerp(u, Grad(Perm(ab), xf, yf - 1, zf), Grad(Perm(bb), xf - 1, yf - 1, zf))),
                Lerp(v,
                    Lerp(u, Grad(Perm(aa + 1), xf, yf, zf - 1), Grad(Perm(ba + 1), xf - 1, yf, zf - 1)),
                    Lerp(u, Grad(Perm(ab + 1), xf, yf - 1, zf - 1), Grad(Perm(bb + 1), xf - 1, yf - 1, zf - 1))));
        }

        private float WorleyNoise3D(Vector3 position)
        {
            // Simplified Worley noise (cellular noise) implementation
            Vector3 cell = new Vector3(MathF.Floor(position.X), MathF.Floor(position.Y), MathF.Floor(position.Z));
            Vector3 frac = position - cell;

            float minDist = float.MaxValue;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Vector3 neighbor = cell + new Vector3(x, y, z);
                        Vector3 point = neighbor + GetRandomPointInCell(neighbor);
                        float dist = Vector3.Distance(position, point);
                        minDist = Math.Min(minDist, dist);
                    }
                }
            }

            return 1.0f - minDist; // Invert for peaks instead of valleys
        }

        private Vector3 GetRandomPointInCell(Vector3 cell)
        {
            // Generate pseudo-random point within [0,1] cube for given cell
            int hash = ((int)cell.X * 73856093) ^ ((int)cell.Y * 19349663) ^ ((int)cell.Z * 83492791);
            hash = hash ^ (hash >> 16);

            float x = ((hash & 0xFF) / 255.0f);
            hash = hash >> 8;
            float y = ((hash & 0xFF) / 255.0f);
            hash = hash >> 8;
            float z = ((hash & 0xFF) / 255.0f);

            return new Vector3(x, y, z);
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

        private int Perm(int x)
        {
            return permutation[x & 255];
        }

        private static readonly int[] permutation = {
            151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,
            8,99,37,240,21,10,23,190,6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,
            35,11,32,57,177,33,88,237,149,56,87,174,20,125,136,171,168,68,175,74,165,71,
            134,139,48,27,166,77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,
            55,46,245,40,244,102,143,54,65,25,63,161,1,216,80,73,209,76,132,187,208,89,
            18,169,200,196,135,130,116,188,159,86,164,100,109,198,173,186,3,64,52,217,226,
            250,124,123,5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,
            189,28,42,223,183,170,213,119,248,152,2,44,154,163,70,221,153,101,155,167,43,
            172,9,129,22,39,253,19,98,108,110,79,113,224,232,178,185,112,104,218,246,97,
            228,251,34,242,193,238,210,144,12,191,179,162,241,81,51,145,235,249,14,239,
            107,49,192,214,31,181,199,106,157,184,84,204,176,115,121,50,45,127,4,150,254,
            138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
        };

        /// <summary>
        /// Sample height procedurally at 3D sphere position - bypasses texture quantization
        /// </summary>
        public float SampleHeightAtPosition(Vector3 spherePosition)
        {
            return GetPlanetHeight(spherePosition);
        }

        /// <summary>
        /// Sample heightmap at specific UV coordinates (public accessor for chunks)
        /// WARNING: This samples from texture and may have quantization artifacts
        /// Consider using SampleHeightAtPosition for higher precision
        /// </summary>
        public float SampleHeightAtUV(float u, float v)
        {
            if (heightmapData == null)
                return 0.5f;

            int x = (int)(u * (HeightmapResolution - 1));
            int y = (int)(v * (HeightmapResolution - 1));

            x = MathHelper.Clamp(x, 0, HeightmapResolution - 1);
            y = MathHelper.Clamp(y, 0, HeightmapResolution - 1);

            return heightmapData[x, y];
        }

        public void Dispose()
        {
            texturedVertexBuffer?.Dispose();
            coloredVertexBuffer?.Dispose();
            indexBuffer?.Dispose();
            heightmapTexture?.Dispose();
            normalMapTexture?.Dispose();
        }
    }
}