using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine.system.procedural
{
    public class WaterSphereRenderer : IDisposable
    {
        private GraphicsDevice graphicsDevice;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private int primitiveCount;
        private float baseRadius;
        private float currentRadius;
        private Effect waterShader;
        private BasicEffect fallbackEffect;

        public WaterSphereRenderer(GraphicsDevice gd, float sphereRadius, int subdivisionLevel = 3)
        {
            graphicsDevice = gd;
            baseRadius = sphereRadius;
            currentRadius = sphereRadius;

            // Generate sphere mesh
            GenerateWaterSphere(subdivisionLevel);

            // Create fallback effect for when water shader fails
            fallbackEffect = new BasicEffect(gd);
            fallbackEffect.VertexColorEnabled = false;
            fallbackEffect.TextureEnabled = false;
            fallbackEffect.LightingEnabled = true;
            fallbackEffect.EnableDefaultLighting();
            fallbackEffect.DiffuseColor = new Vector3(0.1f, 0.3f, 0.6f); // Water blue
            fallbackEffect.Alpha = 0.7f; // Transparency

            // Try to load water shader
            try
            {
                waterShader = rubens_psx_engine.Globals.screenManager.Content.Load<Effect>("shaders/WaterShader");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to load water shader: {ex.Message}");
                waterShader = null;
            }
        }

        private void GenerateWaterSphere(int subdivisionLevel)
        {
            // Create icosahedron base for the water sphere
            var vertices = new List<Vector3>(GetIcosahedronVertices());
            var indices = new List<int>(GetIcosahedronIndices());

            // Subdivide to create smoother sphere
            for (int i = 0; i < subdivisionLevel; i++)
            {
                SubdivideIcosahedron(ref vertices, ref indices);
            }

            // Create vertex buffer with positions, normals, and texture coordinates
            var sphereVertices = new VertexPositionNormalTexture[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 normalizedPos = Vector3.Normalize(vertices[i]);
                Vector3 position = normalizedPos * currentRadius;

                // Calculate UV coordinates for the sphere
                Vector2 texCoord = GetUVForSpherePosition(normalizedPos);

                sphereVertices[i] = new VertexPositionNormalTexture(
                    position,
                    normalizedPos, // Normal points outward
                    texCoord
                );
            }

            // Create buffers
            vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), sphereVertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(sphereVertices);

            indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices.ToArray());

            primitiveCount = indices.Count / 3;
        }

        private Vector3[] GetIcosahedronVertices()
        {
            float t = (1.0f + MathF.Sqrt(5.0f)) / 2.0f; // Golden ratio

            return new Vector3[]
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
        }

        private int[] GetIcosahedronIndices()
        {
            return new int[]
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
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

        private Vector2 GetUVForSpherePosition(Vector3 spherePos)
        {
            float u = 0.5f + MathF.Atan2(spherePos.Z, spherePos.X) / (2.0f * MathF.PI);
            float v = 0.5f - MathF.Asin(spherePos.Y) / MathF.PI;
            return new Vector2(u, v);
        }

        public void UpdateWaterLevel(float oceanLevel)
        {
            // Scale water sphere based on ocean level (0.0 to 1.0)
            // Ocean level represents how much of the planet is covered by water
            currentRadius = baseRadius * (0.95f + oceanLevel * 0.1f); // Scale from 95% to 105% of base radius
            GenerateWaterSphere(3); // Regenerate with current radius
        }

        public void Draw(GraphicsDevice device, Matrix world, Matrix view, Matrix projection, GameTime gameTime, PlanetParameters parameters)
        {
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;

            // Save previous render states
            var previousBlendState = device.BlendState;
            var previousRasterizerState = device.RasterizerState;
            var previousDepthStencilState = device.DepthStencilState;

            // Set up render states for transparent water sphere
            device.BlendState = BlendState.AlphaBlend;
            device.RasterizerState = RasterizerState.CullNone; // Disable culling so water is visible from inside
            device.DepthStencilState = DepthStencilState.DepthRead; // Read depth but don't write to avoid z-fighting

            if (waterShader != null)
            {
                // Use water shader
                waterShader.Parameters["World"]?.SetValue(world);
                waterShader.Parameters["View"]?.SetValue(view);
                waterShader.Parameters["Projection"]?.SetValue(projection);
                waterShader.Parameters["WorldInverseTranspose"]?.SetValue(Matrix.Transpose(Matrix.Invert(world)));

                // Extract camera position from inverse view matrix
                Matrix inverseView = Matrix.Invert(view);
                Vector3 cameraPosition = inverseView.Translation;
                waterShader.Parameters["CameraPosition"]?.SetValue(cameraPosition);
                waterShader.Parameters["Time"]?.SetValue((float)gameTime.TotalGameTime.TotalSeconds);

                // Set water-specific parameters
                waterShader.Parameters["WaterTransparency"]?.SetValue(0.7f);
                waterShader.Parameters["WaveHeight"]?.SetValue(0.02f);
                waterShader.Parameters["WaveFrequency"]?.SetValue(15.0f);
                waterShader.Parameters["WaveSpeed"]?.SetValue(2.0f);

                foreach (var pass in waterShader.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, primitiveCount);
                }
            }
            else
            {
                // Use fallback BasicEffect
                fallbackEffect.World = world;
                fallbackEffect.View = view;
                fallbackEffect.Projection = projection;

                foreach (var pass in fallbackEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, primitiveCount);
                }
            }

            // Restore previous render states
            device.BlendState = previousBlendState;
            device.RasterizerState = previousRasterizerState;
            device.DepthStencilState = previousDepthStencilState;
        }

        public void Dispose()
        {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();
            waterShader?.Dispose();
            fallbackEffect?.Dispose();
        }
    }
}