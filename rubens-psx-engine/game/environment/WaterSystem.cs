using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.entities;
using rubens_psx_engine.entities.materials;
using rubens_psx_engine.system.terrain;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine.game.environment
{
    public class WaterSystem
    {
        private GraphicsDevice graphicsDevice;
        private VertexBuffer waterVertexBuffer;
        private IndexBuffer waterIndexBuffer;
        private VertexLitUnitsMaterial waterMaterial;
        private Texture2D waterTexture;

        // Water animation
        private float waterTime;
        private Vector2 waterScrollSpeed = new Vector2(0.1f, 0.05f);
        private Vector2 waterTiling = new Vector2(4.0f, 4.0f);

        // Water properties
        public float WaterLevel { get; set; } = -1.0f;
        public Color WaterColor { get; set; } = new Color(0.3f, 0.6f, 0.9f, 0.7f);
        public float WaterAlpha { get; set; } = 0.7f;

        // River geometry
        private List<Vector3> riverPath;
        private float riverWidth = 12.0f;

        public WaterSystem(GraphicsDevice device)
        {
            graphicsDevice = device;
            waterTime = 0f;
            riverPath = new List<Vector3>();

            LoadWaterAssets();
        }

        private void LoadWaterAssets()
        {
            // Create a simple water texture if not available, or load existing
            try
            {
                waterTexture = Globals.screenManager.Content.Load<Texture2D>("textures/prototype/water");
            }
            catch
            {
                // Create a procedural water texture
                waterTexture = CreateWaterTexture();
            }

            // Create water material with transparency
            waterMaterial = new VertexLitUnitsMaterial()
            {
                DiffuseColor = WaterColor.ToVector3(),
                Alpha = WaterAlpha,
                Texture = waterTexture
            };
        }

        private Texture2D CreateWaterTexture()
        {
            int size = 64;
            Texture2D texture = new Texture2D(graphicsDevice, size, size);
            Color[] data = new Color[size * size];

            Random rand = new Random(12345);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // Create a watery pattern
                    float noise1 = (float)Math.Sin(x * 0.2f) * 0.5f + 0.5f;
                    float noise2 = (float)Math.Sin(y * 0.15f) * 0.5f + 0.5f;
                    float noise3 = (float)rand.NextDouble() * 0.1f;

                    float intensity = (noise1 + noise2 + noise3) / 3.0f;
                    intensity = MathHelper.Clamp(intensity, 0.3f, 0.9f);

                    Color waterColor = Color.Lerp(
                        new Color(0.2f, 0.4f, 0.8f, 0.7f),
                        new Color(0.4f, 0.7f, 1.0f, 0.7f),
                        intensity
                    );

                    data[x + y * size] = waterColor;
                }
            }

            texture.SetData(data);
            return texture;
        }

        public void GenerateRiverGeometry(TerrainData terrainData)
        {
            riverPath.Clear();

            // Generate river path based on terrain
            int centerZ = terrainData.Height / 2;
            float terrainScale = terrainData.Scale;

            for (int x = 0; x < terrainData.Width; x++)
            {
                // Create meandering pattern (same as terrain generation)
                float meander = (float)Math.Sin((float)x / terrainData.Width * Math.PI * 3) * 8f;
                int riverCenterZ = (int)(centerZ + meander);

                Vector3 riverPoint = new Vector3(
                    x * terrainScale,
                    WaterLevel,
                    riverCenterZ * terrainScale
                );

                riverPath.Add(riverPoint);
            }

            CreateWaterMesh();
        }

        private void CreateWaterMesh()
        {
            if (riverPath.Count < 2) return;

            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
            List<int> indices = new List<int>();

            // Create water plane segments along the river
            for (int i = 0; i < riverPath.Count - 1; i++)
            {
                Vector3 current = riverPath[i];
                Vector3 next = riverPath[i + 1];

                // Calculate perpendicular direction for width
                Vector3 direction = Vector3.Normalize(next - current);
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.Up) * riverWidth * 0.5f;

                // Create quad for this river segment
                Vector3 p1 = current - perpendicular;
                Vector3 p2 = current + perpendicular;
                Vector3 p3 = next - perpendicular;
                Vector3 p4 = next + perpendicular;

                // Add vertices
                int baseIndex = vertices.Count;

                vertices.Add(new VertexPositionNormalTexture(p1, Vector3.Up, new Vector2(0, 0)));
                vertices.Add(new VertexPositionNormalTexture(p2, Vector3.Up, new Vector2(1, 0)));
                vertices.Add(new VertexPositionNormalTexture(p3, Vector3.Up, new Vector2(0, 1)));
                vertices.Add(new VertexPositionNormalTexture(p4, Vector3.Up, new Vector2(1, 1)));

                // Add indices for two triangles
                indices.Add(baseIndex + 0);
                indices.Add(baseIndex + 1);
                indices.Add(baseIndex + 2);

                indices.Add(baseIndex + 1);
                indices.Add(baseIndex + 3);
                indices.Add(baseIndex + 2);
            }

            // Create vertex buffer
            if (waterVertexBuffer != null)
                waterVertexBuffer.Dispose();

            waterVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture),
                vertices.Count, BufferUsage.WriteOnly);
            waterVertexBuffer.SetData(vertices.ToArray());

            // Create index buffer
            if (waterIndexBuffer != null)
                waterIndexBuffer.Dispose();

            waterIndexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits,
                indices.Count, BufferUsage.WriteOnly);
            waterIndexBuffer.SetData(indices.ToArray());
        }

        public void Update(GameTime gameTime)
        {
            waterTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public void Draw(Matrix world, Matrix view, Matrix projection)
        {
            if (waterVertexBuffer == null || waterIndexBuffer == null) return;

            // Enable transparency
            var previousBlendState = graphicsDevice.BlendState;
            var previousDepthStencilState = graphicsDevice.DepthStencilState;

            graphicsDevice.BlendState = BlendState.AlphaBlend;
            graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

            // Set water material properties
            waterMaterial.DiffuseColor = WaterColor.ToVector3();
            waterMaterial.Alpha = WaterAlpha;

            // Animate texture coordinates
            Vector2 animatedOffset = waterScrollSpeed * waterTime;
            // Note: Would need to extend material to support texture offset
            // For now, we'll apply the animation in the shader if available

            // Apply material
            var camera = new BasicCamera { View = view, Projection = projection };
            waterMaterial.Apply(camera, world);

            // Set geometry
            graphicsDevice.SetVertexBuffer(waterVertexBuffer);
            graphicsDevice.Indices = waterIndexBuffer;

            // Draw water
            int indexCount = waterIndexBuffer.IndexCount;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, indexCount / 3);

            // Restore render states
            graphicsDevice.BlendState = previousBlendState;
            graphicsDevice.DepthStencilState = previousDepthStencilState;
        }

        public bool IsOverWater(Vector3 position)
        {
            // Simple check if position is over the river
            foreach (var riverPoint in riverPath)
            {
                Vector2 riverPos2D = new Vector2(riverPoint.X, riverPoint.Z);
                Vector2 pos2D = new Vector2(position.X, position.Z);

                if (Vector2.Distance(riverPos2D, pos2D) <= riverWidth * 0.5f)
                {
                    return true;
                }
            }
            return false;
        }

        public Vector3? GetNearestRiverPoint(Vector3 position)
        {
            Vector3? nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var riverPoint in riverPath)
            {
                float distance = Vector3.Distance(position, riverPoint);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = riverPoint;
                }
            }

            return nearest;
        }

        public void Dispose()
        {
            waterVertexBuffer?.Dispose();
            waterIndexBuffer?.Dispose();
            waterTexture?.Dispose();
        }

        // Helper camera class for material application
        private class BasicCamera : Camera
        {
            public new Matrix View { get; set; }
            public new Matrix Projection { get; set; }

            public BasicCamera() : base(Globals.screenManager.getGraphicsDevice.GraphicsDevice)
            {
            }
        }
    }
}