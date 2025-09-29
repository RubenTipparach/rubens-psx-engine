using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine.system.procedural
{
    public struct VertexPositionNormalColorTexture : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color Color;
        public Vector2 TextureCoordinate;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
        );

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public VertexPositionNormalColorTexture(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate)
        {
            Position = position;
            Normal = normal;
            Color = color;
            TextureCoordinate = textureCoordinate;
        }
    }

    public class ImprovedProceduralPlanet
    {
        private GraphicsDevice graphicsDevice;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private int primitiveCount;

        public float Radius { get; private set; }
        public int SubdivisionLevel { get; private set; }

        // Terrain generation parameters
        private float continentFrequency = 1.0f;
        private float mountainFrequency = 4.0f;
        private float detailFrequency = 8.0f;
        private float oceanDepth = -0.1f;
        private float continentHeight = 0.1f;
        private float mountainHeight = 0.5f;

        private Random random;
        private int seed;

        private struct CubeFace
        {
            public Vector3 Normal;
            public Vector3 Up;
            public Vector3 Right;

            public CubeFace(Vector3 normal, Vector3 up)
            {
                Normal = normal;
                Up = up;
                Right = Vector3.Cross(normal, up);
            }
        }

        private static readonly CubeFace[] cubeFaces = new CubeFace[]
        {
            new CubeFace(Vector3.Forward, Vector3.Up),    // Front
            new CubeFace(Vector3.Backward, Vector3.Up),   // Back
            new CubeFace(Vector3.Left, Vector3.Up),       // Left
            new CubeFace(Vector3.Right, Vector3.Up),      // Right
            new CubeFace(Vector3.Up, Vector3.Backward),   // Top
            new CubeFace(Vector3.Down, Vector3.Forward)   // Bottom
        };

        public ImprovedProceduralPlanet(GraphicsDevice device, float radius = 10f, int subdivisionLevel = 64, int? seed = null)
        {
            graphicsDevice = device;
            Radius = radius;
            SubdivisionLevel = subdivisionLevel;
            this.seed = seed ?? DateTime.Now.Millisecond;
            random = new Random(this.seed);

            GenerateMesh();
        }

        private void GenerateMesh()
        {
            List<VertexPositionNormalColorTexture> vertices = new List<VertexPositionNormalColorTexture>();
            List<int> indices = new List<int>();

            // Generate each face of the cube
            foreach (var face in cubeFaces)
            {
                GenerateFace(face, vertices, indices);
            }

            // Create vertex buffer
            vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalColorTexture),
                vertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices.ToArray());

            // Create index buffer
            indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits,
                indices.Count, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices.ToArray());

            primitiveCount = indices.Count / 3;
        }

        private void GenerateFace(CubeFace face, List<VertexPositionNormalColorTexture> vertices, List<int> indices)
        {
            int startIndex = vertices.Count;

            // Create grid of vertices for this face
            for (int y = 0; y <= SubdivisionLevel; y++)
            {
                for (int x = 0; x <= SubdivisionLevel; x++)
                {
                    // Calculate position on the cube face (-1 to 1)
                    float u = (x / (float)SubdivisionLevel) * 2f - 1f;
                    float v = (y / (float)SubdivisionLevel) * 2f - 1f;

                    // Calculate 3D position on cube face
                    Vector3 cubePos = face.Normal + face.Right * u + face.Up * v;

                    // Transform cube position to sphere
                    Vector3 spherePos = CubeToSphere(cubePos);
                    Vector3 normalizedPos = Vector3.Normalize(spherePos);

                    // Generate height from improved noise
                    float elevation = GenerateElevation(normalizedPos);

                    // Apply height to radius
                    Vector3 finalPos = normalizedPos * (Radius + elevation * Radius * 0.2f);

                    // Calculate normal (pointing outward from planet center)
                    Vector3 normal = Vector3.Normalize(finalPos);

                    // Calculate color and properties based on elevation
                    Color vertexColor;
                    float specular;
                    GetBiomeProperties(elevation, normalizedPos, out vertexColor, out specular);

                    // UV coordinates encode elevation and specular in texture coords
                    Vector2 texCoord = new Vector2(elevation * 0.5f + 0.5f, specular);

                    vertices.Add(new VertexPositionNormalColorTexture(finalPos, normal, vertexColor, texCoord));
                }
            }

            // Generate indices for this face with correct winding order
            for (int y = 0; y < SubdivisionLevel; y++)
            {
                for (int x = 0; x < SubdivisionLevel; x++)
                {
                    int topLeft = startIndex + y * (SubdivisionLevel + 1) + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + SubdivisionLevel + 1;
                    int bottomRight = bottomLeft + 1;

                    // Correct winding order for outward-facing normals
                    // First triangle
                    indices.Add(topLeft);
                    indices.Add(topRight);
                    indices.Add(bottomLeft);

                    // Second triangle
                    indices.Add(topRight);
                    indices.Add(bottomRight);
                    indices.Add(bottomLeft);
                }
            }
        }

        private Vector3 CubeToSphere(Vector3 cubePoint)
        {
            // Improved cube to sphere mapping
            float x2 = cubePoint.X * cubePoint.X;
            float y2 = cubePoint.Y * cubePoint.Y;
            float z2 = cubePoint.Z * cubePoint.Z;

            float x = cubePoint.X * MathF.Sqrt(1f - y2 * 0.5f - z2 * 0.5f + y2 * z2 / 3f);
            float y = cubePoint.Y * MathF.Sqrt(1f - z2 * 0.5f - x2 * 0.5f + z2 * x2 / 3f);
            float z = cubePoint.Z * MathF.Sqrt(1f - x2 * 0.5f - y2 * 0.5f + x2 * y2 / 3f);

            return new Vector3(x, y, z);
        }

        private float GenerateElevation(Vector3 sphereNormal)
        {
            float elevation = 0f;

            // Continental shelf - large scale features
            float continents = 0f;
            continents += SimplexNoise3D(sphereNormal * continentFrequency + new Vector3(seed)) * 1.0f;
            continents += SimplexNoise3D(sphereNormal * continentFrequency * 2.1f + new Vector3(seed * 1.3f)) * 0.5f;
            continents = (continents + 1.0f) * 0.5f; // Normalize to 0-1

            // Apply a curve to create more distinct land/water boundaries
            continents = MathF.Pow(continents, 2.2f);

            // Mountain ranges - medium scale features
            float mountains = 0f;
            if (continents > 0.3f) // Only on land
            {
                mountains += SimplexNoise3D(sphereNormal * mountainFrequency + new Vector3(seed * 2)) * 0.5f;
                mountains += SimplexNoise3D(sphereNormal * mountainFrequency * 2.3f + new Vector3(seed * 2.7f)) * 0.25f;
                mountains = Math.Max(0, mountains);
                mountains *= (continents - 0.3f) / 0.7f; // Scale by distance from coast
            }

            // Detail noise - small scale features
            float details = 0f;
            details += SimplexNoise3D(sphereNormal * detailFrequency + new Vector3(seed * 3)) * 0.1f;
            details += SimplexNoise3D(sphereNormal * detailFrequency * 2.7f + new Vector3(seed * 3.3f)) * 0.05f;

            // Combine layers
            elevation = continents * continentHeight + mountains * mountainHeight + details;

            // Ocean depth
            if (continents < 0.4f)
            {
                elevation = oceanDepth * (1.0f - continents / 0.4f);
            }

            // Polar ice caps
            float polarFactor = Math.Abs(sphereNormal.Y);
            if (polarFactor > 0.7f)
            {
                float iceFactor = (polarFactor - 0.7f) / 0.3f;
                elevation = Math.Max(elevation, 0.05f * iceFactor);
            }

            return elevation;
        }

        private float SimplexNoise3D(Vector3 pos)
        {
            // Improved 3D noise function
            float x = pos.X;
            float y = pos.Y;
            float z = pos.Z;

            // Find unit cube that contains point
            int X = (int)MathF.Floor(x) & 255;
            int Y = (int)MathF.Floor(y) & 255;
            int Z = (int)MathF.Floor(z) & 255;

            // Find relative x, y, z of point in cube
            x -= MathF.Floor(x);
            y -= MathF.Floor(y);
            z -= MathF.Floor(z);

            // Compute fade curves
            float u = Fade(x);
            float v = Fade(y);
            float w = Fade(z);

            // Hash coordinates of the 8 cube corners
            int A = Hash(X) + Y;
            int AA = Hash(A) + Z;
            int AB = Hash(A + 1) + Z;
            int B = Hash(X + 1) + Y;
            int BA = Hash(B) + Z;
            int BB = Hash(B + 1) + Z;

            // Blend results from 8 corners of cube
            float result = Lerp(w,
                Lerp(v,
                    Lerp(u, Grad(Hash(AA), x, y, z), Grad(Hash(BA), x - 1, y, z)),
                    Lerp(u, Grad(Hash(AB), x, y - 1, z), Grad(Hash(BB), x - 1, y - 1, z))),
                Lerp(v,
                    Lerp(u, Grad(Hash(AA + 1), x, y, z - 1), Grad(Hash(BA + 1), x - 1, y, z - 1)),
                    Lerp(u, Grad(Hash(AB + 1), x, y - 1, z - 1), Grad(Hash(BB + 1), x - 1, y - 1, z - 1))));

            return result;
        }

        private float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private float Lerp(float t, float a, float b)
        {
            return a + t * (b - a);
        }

        private int Hash(int n)
        {
            n = (n ^ seed) * 1103515245 + 12345;
            return (n >> 16) & 0x7fff;
        }

        private float Grad(int hash, float x, float y, float z)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : h == 12 || h == 14 ? x : z;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        private void GetBiomeProperties(float elevation, Vector3 sphereNormal, out Color color, out float specular)
        {
            specular = 0.0f;
            float polarFactor = Math.Abs(sphereNormal.Y);

            // Deep ocean
            if (elevation < -0.05f)
            {
                color = new Color(10, 30, 80);
                specular = 0.8f; // High specular for water
            }
            // Shallow ocean
            else if (elevation < 0.0f)
            {
                color = new Color(20, 60, 120);
                specular = 0.7f;
            }
            // Beach/Coast
            else if (elevation < 0.02f)
            {
                color = new Color(220, 200, 130);
                specular = 0.1f;
            }
            // Plains/Grassland
            else if (elevation < 0.1f)
            {
                // Check for polar regions
                if (polarFactor > 0.7f)
                {
                    color = new Color(240, 240, 250); // Ice
                    specular = 0.4f;
                }
                else if (polarFactor > 0.5f)
                {
                    color = new Color(100, 140, 100); // Tundra
                    specular = 0.05f;
                }
                else
                {
                    // Check for deserts (based on position)
                    float desertFactor = SimplexNoise3D(sphereNormal * 2f + new Vector3(seed * 5));
                    if (desertFactor > 0.3f && Math.Abs(sphereNormal.Y) < 0.4f)
                    {
                        color = new Color(210, 180, 100); // Desert
                        specular = 0.05f;
                    }
                    else
                    {
                        color = new Color(80, 160, 60); // Grassland
                        specular = 0.05f;
                    }
                }
            }
            // Hills/Forest
            else if (elevation < 0.25f)
            {
                if (polarFactor > 0.6f)
                {
                    color = new Color(230, 230, 240); // Snow
                    specular = 0.3f;
                }
                else
                {
                    color = new Color(50, 100, 40); // Forest
                    specular = 0.05f;
                }
            }
            // Mountains
            else if (elevation < 0.4f)
            {
                color = new Color(120, 100, 80); // Rock
                specular = 0.1f;
            }
            // High mountains/Snow caps
            else
            {
                color = new Color(245, 245, 250); // Snow
                specular = 0.3f;
            }
        }

        public void Draw(GraphicsDevice device, Matrix world, Matrix view, Matrix projection, Effect effect)
        {
            // Set vertex and index buffers
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;

            // Set effect parameters
            if (effect is BasicEffect basicEffect)
            {
                basicEffect.World = world;
                basicEffect.View = view;
                basicEffect.Projection = projection;
                basicEffect.VertexColorEnabled = true;
                basicEffect.TextureEnabled = false;
                basicEffect.LightingEnabled = true;

                // Set up lighting
                basicEffect.DirectionalLight0.Enabled = true;
                basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1, -1, -1));
                basicEffect.DirectionalLight0.DiffuseColor = new Vector3(0.8f, 0.8f, 0.7f);
                basicEffect.DirectionalLight0.SpecularColor = new Vector3(0.6f, 0.6f, 0.6f);

                basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.25f);
                basicEffect.EmissiveColor = new Vector3(0, 0, 0);
                basicEffect.SpecularPower = 32f;
            }

            // Draw the mesh
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, primitiveCount);
            }
        }

        public void Dispose()
        {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();
        }
    }
}