using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine.system.procedural
{
    public class ProceduralPlanet
    {
        private GraphicsDevice graphicsDevice;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private int primitiveCount;

        public float Radius { get; private set; }
        public int SubdivisionLevel { get; private set; }

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

        public ProceduralPlanet(GraphicsDevice device, float radius = 10f, int subdivisionLevel = 32)
        {
            graphicsDevice = device;
            Radius = radius;
            SubdivisionLevel = subdivisionLevel;

            GenerateMesh();
        }

        private void GenerateMesh()
        {
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();
            List<int> indices = new List<int>();

            // Generate each face of the cube
            foreach (var face in cubeFaces)
            {
                GenerateFace(face, vertices, indices);
            }

            // Create vertex buffer
            vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor),
                vertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices.ToArray());

            // Create index buffer
            indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits,
                indices.Count, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices.ToArray());

            primitiveCount = indices.Count / 3;
        }

        private void GenerateFace(CubeFace face, List<VertexPositionColor> vertices, List<int> indices)
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

                    // Generate height from noise
                    float height = GenerateHeight(spherePos);

                    // Apply height to radius
                    Vector3 finalPos = spherePos * (Radius + height);

                    // Calculate color based on elevation
                    Color vertexColor = GetColorByElevation(height);

                    vertices.Add(new VertexPositionColor(finalPos, vertexColor));
                }
            }

            // Generate indices for this face
            for (int y = 0; y < SubdivisionLevel; y++)
            {
                for (int x = 0; x < SubdivisionLevel; x++)
                {
                    int topLeft = startIndex + y * (SubdivisionLevel + 1) + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + SubdivisionLevel + 1;
                    int bottomRight = bottomLeft + 1;

                    // First triangle
                    indices.Add(topLeft);
                    indices.Add(bottomLeft);
                    indices.Add(topRight);

                    // Second triangle
                    indices.Add(topRight);
                    indices.Add(bottomLeft);
                    indices.Add(bottomRight);
                }
            }
        }

        private Vector3 CubeToSphere(Vector3 cubePoint)
        {
            // Normalize the cube point to get sphere direction
            Vector3 normalized = Vector3.Normalize(cubePoint);

            // Apply spherical distortion for smoother mapping
            float x2 = cubePoint.X * cubePoint.X;
            float y2 = cubePoint.Y * cubePoint.Y;
            float z2 = cubePoint.Z * cubePoint.Z;

            float x = cubePoint.X * MathF.Sqrt(1f - y2 * 0.5f - z2 * 0.5f + y2 * z2 / 3f);
            float y = cubePoint.Y * MathF.Sqrt(1f - z2 * 0.5f - x2 * 0.5f + z2 * x2 / 3f);
            float z = cubePoint.Z * MathF.Sqrt(1f - x2 * 0.5f - y2 * 0.5f + x2 * y2 / 3f);

            return new Vector3(x, y, z);
        }

        private float GenerateHeight(Vector3 sphereNormal)
        {
            // Multi-octave noise for terrain generation
            float elevation = 0f;
            float frequency = 1f;
            float amplitude = 1f;
            float maxValue = 0f;

            for (int i = 0; i < 5; i++)
            {
                elevation += SimplexNoise(sphereNormal * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            elevation = elevation / maxValue;

            // Apply power curve for more realistic terrain
            if (elevation > 0)
            {
                elevation = MathF.Pow(elevation, 1.5f);
            }

            // Scale to desired height range
            return elevation * 2f; // -2 to +2 units height variation
        }

        private float SimplexNoise(Vector3 pos)
        {
            // Simple noise implementation
            // For production, you'd want to use a proper simplex/perlin noise library
            float n = MathF.Sin(pos.X * 12.9898f + pos.Y * 78.233f + pos.Z * 37.719f) * 43758.5453f;
            return (n - MathF.Floor(n)) * 2f - 1f;
        }

        private Color GetColorByElevation(float height)
        {
            // Define elevation thresholds and colors
            if (height < -0.5f)
            {
                // Deep water
                return new Color(0, 50, 150);
            }
            else if (height < -0.1f)
            {
                // Shallow water
                return new Color(0, 100, 200);
            }
            else if (height < 0.1f)
            {
                // Sand/Beach
                return new Color(240, 220, 130);
            }
            else if (height < 0.5f)
            {
                // Grass
                return new Color(80, 180, 70);
            }
            else if (height < 1.0f)
            {
                // Forest/Hills
                return new Color(60, 120, 50);
            }
            else if (height < 1.5f)
            {
                // Mountains
                return new Color(139, 90, 43);
            }
            else
            {
                // Snow caps
                return new Color(245, 245, 250);
            }
        }

        public void Draw(GraphicsDevice device, Matrix world, Matrix view, Matrix projection, Effect effect)
        {
            // Set vertex and index buffers
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;

            // Set effect parameters if it's a BasicEffect
            if (effect is BasicEffect basicEffect)
            {
                basicEffect.World = world;
                basicEffect.View = view;
                basicEffect.Projection = projection;
                basicEffect.VertexColorEnabled = true;
                basicEffect.TextureEnabled = false;
                basicEffect.LightingEnabled = false; // No lighting without normals
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