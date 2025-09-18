using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.system.terrain;
using rubens_psx_engine.system.lighting;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine.entities
{
    /// <summary>
    /// Specialized rendering entity for terrain with material-based rendering
    /// </summary>
    public class TerrainRenderingEntity : IDisposable
    {
        private GraphicsDevice graphicsDevice;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private TerrainMaterial material;
        private TerrainData terrainData;

        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Scale { get; set; } = Vector3.One;
        public bool IsVisible { get; set; } = true;
        public TerrainMaterial Material => material;

        public TerrainRenderingEntity(GraphicsDevice device, string texturePath)
        {
            graphicsDevice = device;
            material = new TerrainMaterial(texturePath);
        }

        public void LoadTerrain(TerrainData terrain)
        {
            terrainData = terrain;

            // Dispose existing buffers
            if (vertexBuffer != null)
                vertexBuffer.Dispose();
            if (indexBuffer != null)
                indexBuffer.Dispose();

            // Create new buffers
            vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture),
                terrain.Vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(terrain.Vertices);

            indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits,
                terrain.Indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(terrain.Indices);
        }

        public void Update(GameTime gameTime)
        {
            // Override in derived classes for custom update logic
        }

        public void Draw(GameTime gameTime, Camera camera)
        {
            if (!IsVisible || terrainData == null || vertexBuffer == null || indexBuffer == null)
                return;

            // Set vertex and index buffers
            graphicsDevice.SetVertexBuffer(vertexBuffer);
            graphicsDevice.Indices = indexBuffer;

            // Calculate world matrix
            Matrix world = Matrix.CreateScale(Scale) * Matrix.CreateTranslation(Position);

            // Apply material
            material.Apply(camera, world);

            // Draw terrain
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0,
                terrainData.Indices.Length / 3);
        }

        public void ApplyEnvironmentLight(EnvironmentLight environmentLight)
        {
            material?.ApplyEnvironmentLight(environmentLight);
        }

        public void ApplyPointLights(IEnumerable<PointLight> pointLights)
        {
            material?.ApplyPointLights(pointLights, Position);
        }

        public void SetTextureTiling(Vector2 tiling)
        {
            if (material != null)
                material.TextureTiling = tiling;
        }

        public float GetHeightAt(float x, float z)
        {
            return terrainData?.GetHeightAt(x, z) ?? 0f;
        }

        public void Dispose()
        {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();
            // Note: Material effect is managed by the material itself and is cloned per instance
        }
    }
}