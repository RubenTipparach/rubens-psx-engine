using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine;
using System;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Starfield rendered on an inverted sphere using Voronoi noise shader
    /// </summary>
    public class LoungeStarfieldSphere
    {
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private Effect starFieldEffect;
        private int indexCount;

        // Sphere parameters
        private const float SphereRadius = 5000f; // Large radius to encompass scene
        private const int SphereSegments = 32; // Tessellation quality

        // Exposed shader parameters
        public float StarDensity { get; set; } = 12.0f;         // Higher = more stars (default 8.0)
        public float StarBrightness { get; set; } = 3.0f;       // Star brightness multiplier (default 2.0)
        public float StarSize { get; set; } = 0.015f;           // Star size threshold (default 0.02)
        public float StarTwinkle { get; set; } = 0.3f;          // Star twinkling amount
        public Vector3 StarColor { get; set; } = new Vector3(1.0f, 0.95f, 0.9f);

        public float NebulaBrightness { get; set; } = 0.15f;    // Background nebula glow (default 0.1)
        public Vector3 NebulaColor1 { get; set; } = new Vector3(0.1f, 0.05f, 0.2f);  // Deep purple
        public Vector3 NebulaColor2 { get; set; } = new Vector3(0.05f, 0.1f, 0.15f); // Deep blue

        public LoungeStarfieldSphere()
        {
            InitializeSphere();
            LoadEffect();
        }

        private void LoadEffect()
        {
            var graphicsDevice = Globals.screenManager.GraphicsDevice;

            // Load the star field shader
            starFieldEffect = Globals.screenManager.Content.Load<Effect>("shaders/surface/StarField");
        }

        private void InitializeSphere()
        {
            var graphicsDevice = Globals.screenManager.GraphicsDevice;

            // Generate inverted sphere mesh (normals point inward)
            var vertices = new VertexPositionNormal[(SphereSegments + 1) * (SphereSegments + 1)];
            var indices = new int[SphereSegments * SphereSegments * 6];

            int vertexIndex = 0;
            for (int lat = 0; lat <= SphereSegments; lat++)
            {
                float theta = lat * MathHelper.Pi / SphereSegments;
                float sinTheta = (float)Math.Sin(theta);
                float cosTheta = (float)Math.Cos(theta);

                for (int lon = 0; lon <= SphereSegments; lon++)
                {
                    float phi = lon * 2 * MathHelper.Pi / SphereSegments;
                    float sinPhi = (float)Math.Sin(phi);
                    float cosPhi = (float)Math.Cos(phi);

                    Vector3 position = new Vector3(
                        SphereRadius * sinTheta * cosPhi,
                        SphereRadius * cosTheta,
                        SphereRadius * sinTheta * sinPhi
                    );

                    // Inverted normals (point inward)
                    Vector3 normal = -Vector3.Normalize(position);

                    vertices[vertexIndex++] = new VertexPositionNormal(position, normal);
                }
            }

            int indexOffset = 0;
            for (int lat = 0; lat < SphereSegments; lat++)
            {
                for (int lon = 0; lon < SphereSegments; lon++)
                {
                    int current = lat * (SphereSegments + 1) + lon;
                    int next = current + SphereSegments + 1;

                    // Inverted winding order for inverted sphere (clockwise instead of counter-clockwise)
                    indices[indexOffset++] = current;
                    indices[indexOffset++] = current + 1;
                    indices[indexOffset++] = next;

                    indices[indexOffset++] = current + 1;
                    indices[indexOffset++] = next + 1;
                    indices[indexOffset++] = next;
                }
            }

            indexCount = indices.Length;

            // Create vertex buffer
            vertexBuffer = new VertexBuffer(
                graphicsDevice,
                typeof(VertexPositionNormal),
                vertices.Length,
                BufferUsage.WriteOnly
            );
            vertexBuffer.SetData(vertices);

            // Create index buffer
            indexBuffer = new IndexBuffer(
                graphicsDevice,
                IndexElementSize.ThirtyTwoBits,
                indices.Length,
                BufferUsage.WriteOnly
            );
            indexBuffer.SetData(indices);
        }

        public void Update(GameTime gameTime)
        {
            // Update time parameter for animation
            if (starFieldEffect != null)
            {
                float time = (float)gameTime.TotalGameTime.TotalSeconds;
                starFieldEffect.Parameters["Time"]?.SetValue(time);
            }
        }

        public void Draw(Camera camera)
        {
            if (starFieldEffect == null) return;

            var graphicsDevice = Globals.screenManager.GraphicsDevice;

            // Save original state
            var originalRasterizerState = graphicsDevice.RasterizerState;
            var originalDepthStencilState = graphicsDevice.DepthStencilState;

            // Set render state for inverted sphere
            // Use CullCounterClockwiseMode to render the inside of the sphere
            graphicsDevice.RasterizerState = new RasterizerState
            {
                CullMode = CullMode.CullCounterClockwiseFace, // Cull front faces, render back faces
                FillMode = FillMode.Solid
            };

            // Disable depth writing so stars don't occlude scene objects
            graphicsDevice.DepthStencilState = new DepthStencilState
            {
                DepthBufferEnable = true,
                DepthBufferWriteEnable = false
            };

            // Set buffers
            graphicsDevice.SetVertexBuffer(vertexBuffer);
            graphicsDevice.Indices = indexBuffer;

            // Set shader parameters - matrices
            // Translate the sphere to follow camera position (removes parallax effect)
            Matrix world = Matrix.CreateTranslation(camera.Position);
            starFieldEffect.Parameters["World"]?.SetValue(world);
            starFieldEffect.Parameters["View"]?.SetValue(camera.View);
            starFieldEffect.Parameters["Projection"]?.SetValue(camera.Projection);

            // Set shader parameters - star properties
            starFieldEffect.Parameters["StarDensity"]?.SetValue(StarDensity);
            starFieldEffect.Parameters["StarBrightness"]?.SetValue(StarBrightness);
            starFieldEffect.Parameters["StarSize"]?.SetValue(StarSize);
            starFieldEffect.Parameters["StarTwinkle"]?.SetValue(StarTwinkle);
            starFieldEffect.Parameters["StarColor"]?.SetValue(StarColor);

            // Set shader parameters - nebula properties
            starFieldEffect.Parameters["NebulaBrightness"]?.SetValue(NebulaBrightness);
            starFieldEffect.Parameters["NebulaColor1"]?.SetValue(NebulaColor1);
            starFieldEffect.Parameters["NebulaColor2"]?.SetValue(NebulaColor2);

            // Draw the sphere
            foreach (var pass in starFieldEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    0,
                    indexCount / 3
                );
            }

            // Restore original state
            graphicsDevice.RasterizerState = originalRasterizerState;
            graphicsDevice.DepthStencilState = originalDepthStencilState;
        }

        public void Dispose()
        {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();
        }
    }
}
