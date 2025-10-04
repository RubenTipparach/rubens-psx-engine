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
                System.Console.WriteLine($"Failed to load water shader: {ex.Message} " + ex.StackTrace);
                waterShader = null;
            }
        }

        private void GenerateWaterSphere(int subdivisionLevel)
        {
            // Use cube-to-sphere mapping like PlanetChunk for consistent geometry
            // Generate 6 cube faces and map to sphere
            var vertices = new List<VertexPositionNormalTexture>();
            var indices = new List<int>();

            int resolution = 32; // Resolution per face (can be adjusted)

            // Define the 6 cube face directions
            Vector3[] faceDirections = new Vector3[]
            {
                new Vector3(0, 1, 0),  // Top
                new Vector3(0, -1, 0), // Bottom
                new Vector3(1, 0, 0),  // Right
                new Vector3(-1, 0, 0), // Left
                new Vector3(0, 0, 1),  // Front
                new Vector3(0, 0, -1)  // Back
            };

            foreach (var localUp in faceDirections)
            {
                // Calculate perpendicular axes for this face
                Vector3 axisA = new Vector3(localUp.Y, localUp.Z, localUp.X);
                Vector3 axisB = Vector3.Cross(localUp, axisA);

                int vertexOffset = vertices.Count;

                // Generate grid of vertices for this face
                for (int y = 0; y <= resolution; y++)
                {
                    for (int x = 0; x <= resolution; x++)
                    {
                        float xPercent = x / (float)resolution;
                        float yPercent = y / (float)resolution;

                        // Map to cube face coordinates [-1, 1]
                        Vector3 pointOnCube = localUp + (xPercent * 2f - 1f) * axisA + (yPercent * 2f - 1f) * axisB;

                        // Normalize to get point on sphere
                        Vector3 pointOnSphere = Vector3.Normalize(pointOnCube);

                        // Scale by radius
                        Vector3 position = pointOnSphere * currentRadius;

                        // Calculate UV for texturing
                        Vector2 uv = GetSphericalUV(pointOnSphere);

                        vertices.Add(new VertexPositionNormalTexture(position, pointOnSphere, uv));
                    }
                }

                // Generate indices for this face
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        int i0 = vertexOffset + y * (resolution + 1) + x;
                        int i1 = i0 + 1;
                        int i2 = i0 + (resolution + 1);
                        int i3 = i2 + 1;

                        indices.Add(i0);
                        indices.Add(i2);
                        indices.Add(i1);

                        indices.Add(i1);
                        indices.Add(i2);
                        indices.Add(i3);
                    }
                }
            }

            // Fix UV seams
            FixUVSeams(vertices, indices);

            // Create buffers
            vertexBuffer?.Dispose();
            vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), vertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices.ToArray());

            indexBuffer?.Dispose();
            indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices.ToArray());

            primitiveCount = indices.Count / 3;
        }

        private void FixUVSeams(List<VertexPositionNormalTexture> vertices, List<int> indices)
        {
            // Fix UV seams by duplicating vertices where triangles cross the UV wrap boundary
            for (int i = 0; i < indices.Count; i += 3)
            {
                int i0 = indices[i];
                int i1 = indices[i + 1];
                int i2 = indices[i + 2];

                Vector2 uv0 = vertices[i0].TextureCoordinate;
                Vector2 uv1 = vertices[i1].TextureCoordinate;
                Vector2 uv2 = vertices[i2].TextureCoordinate;

                // Check if triangle crosses the UV seam (U wraps from 1 to 0)
                float maxU = MathF.Max(MathF.Max(uv0.X, uv1.X), uv2.X);
                float minU = MathF.Min(MathF.Min(uv0.X, uv1.X), uv2.X);
                bool crossesSeam = (maxU - minU) > 0.5f;

                if (crossesSeam)
                {
                    // Duplicate vertices that are on the wrong side of the seam
                    // Average U coordinate to determine which side of the seam we're on
                    float avgU = (uv0.X + uv1.X + uv2.X) / 3.0f;

                    // Fix vertex 0 if needed
                    if (MathF.Abs(uv0.X - avgU) > 0.4f)
                    {
                        var newVertex = vertices[i0];
                        var newUV = newVertex.TextureCoordinate;
                        newUV.X = uv0.X < 0.5f ? uv0.X + 1.0f : uv0.X - 1.0f;
                        newVertex.TextureCoordinate = newUV;
                        i0 = vertices.Count;
                        vertices.Add(newVertex);
                    }

                    // Fix vertex 1 if needed
                    if (MathF.Abs(uv1.X - avgU) > 0.4f)
                    {
                        var newVertex = vertices[i1];
                        var newUV = newVertex.TextureCoordinate;
                        newUV.X = uv1.X < 0.5f ? uv1.X + 1.0f : uv1.X - 1.0f;
                        newVertex.TextureCoordinate = newUV;
                        i1 = vertices.Count;
                        vertices.Add(newVertex);
                    }

                    // Fix vertex 2 if needed
                    if (MathF.Abs(uv2.X - avgU) > 0.4f)
                    {
                        var newVertex = vertices[i2];
                        var newUV = newVertex.TextureCoordinate;
                        newUV.X = uv2.X < 0.5f ? uv2.X + 1.0f : uv2.X - 1.0f;
                        newVertex.TextureCoordinate = newUV;
                        i2 = vertices.Count;
                        vertices.Add(newVertex);
                    }

                    // Update indices to point to new vertices
                    indices[i] = i0;
                    indices[i + 1] = i1;
                    indices[i + 2] = i2;
                }
            }
        }

        private Vector2 GetSphericalUV(Vector3 spherePos)
        {
            float u = MathF.Atan2(spherePos.X, spherePos.Z) / (2.0f * MathF.PI) + 0.5f;
            float v = MathF.Asin(MathHelper.Clamp(spherePos.Y, -1.0f, 1.0f)) / MathF.PI + 0.5f;
            return new Vector2(u, v);
        }

        public void UpdateWaterLevel(float oceanLevel)
        {
            // Scale water sphere based on ocean level (0.0 to 1.0)
            // Ocean level represents how much of the planet is covered by water
            currentRadius = baseRadius * (0.95f + oceanLevel * 0.1f); // Scale from 95% to 105% of base radius
            GenerateWaterSphere(3); // Regenerate with current radius
        }

        public void Draw(GraphicsDevice device, Matrix world, Matrix view, Matrix projection, GameTime gameTime, PlanetParameters parameters,
            float waveHeight = 0.05f, float waveSpeed = 1.0f, float normalStrength = 0.6f, float distortion = 0.5f, float scrollSpeed = 1.0f, float foamDepth = 3.5f, float detailScale = 1.0f)
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

                // Planet parameters for depth calculation
                waterShader.Parameters["PlanetCenter"]?.SetValue(Vector3.Zero);
                waterShader.Parameters["PlanetRadius"]?.SetValue(baseRadius);

                // Sun direction (should match atmosphere shader)
                waterShader.Parameters["SunDirection"]?.SetValue(new Vector3(0.0f, 0.5f, 0.866f));

                // Water appearance parameters
                waterShader.Parameters["WaveHeight"]?.SetValue(0.05f); // Small-scale waves only
                waterShader.Parameters["WaveSpeed"]?.SetValue(1.0f);
                waterShader.Parameters["WaterClarity"]?.SetValue(15.0f);
                waterShader.Parameters["ReflectionStrength"]?.SetValue(0.8f);
                waterShader.Parameters["SpecularPower"]?.SetValue(128.0f);
                waterShader.Parameters["SpecularIntensity"]?.SetValue(2.0f);
                waterShader.Parameters["SubsurfaceStrength"]?.SetValue(0.8f);
                waterShader.Parameters["FoamAmount"]?.SetValue(0.6f);
                waterShader.Parameters["FoamCutoff"]?.SetValue(0.4f);

                // Set user-controllable parameters
                waterShader.Parameters["WaveHeight"]?.SetValue(waveHeight);
                waterShader.Parameters["WaveSpeed"]?.SetValue(waveSpeed);
                waterShader.Parameters["WaveNormalStrength"]?.SetValue(normalStrength);
                waterShader.Parameters["WaveDistortion"]?.SetValue(distortion);
                waterShader.Parameters["WaveScrollSpeed"]?.SetValue(scrollSpeed);
                waterShader.Parameters["FoamEdgeDistance"]?.SetValue(foamDepth);
                waterShader.Parameters["WaveUVScale"]?.SetValue(detailScale);

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