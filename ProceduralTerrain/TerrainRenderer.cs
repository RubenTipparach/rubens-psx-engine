using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProceduralTerrain
{
    /// <summary>
    /// Handles rendering of terrain data using GPU resources
    /// </summary>
    public class TerrainRenderer
    {
        private GraphicsDevice graphicsDevice;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private Effect effect;
        private Texture2D texture;
        private TerrainData terrainData;

        public TerrainRenderer(GraphicsDevice device, Effect terrainEffect)
        {
            graphicsDevice = device;
            effect = terrainEffect;
        }

        public void LoadTerrain(TerrainData terrain, Texture2D terrainTexture)
        {
            terrainData = terrain;
            texture = terrainTexture;

            if (vertexBuffer != null)
                vertexBuffer.Dispose();
            if (indexBuffer != null)
                indexBuffer.Dispose();

            vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), 
                terrain.Vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(terrain.Vertices);

            indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, 
                terrain.Indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(terrain.Indices);
        }

        public void Render(Matrix world, Matrix view, Matrix projection, Vector3 lightDirection)
        {
            if (terrainData == null || vertexBuffer == null || indexBuffer == null)
                return;

            graphicsDevice.SetVertexBuffer(vertexBuffer);
            graphicsDevice.Indices = indexBuffer;

            effect.Parameters["World"]?.SetValue(world);
            effect.Parameters["View"]?.SetValue(view);
            effect.Parameters["Projection"]?.SetValue(projection);
            effect.Parameters["LightDirection"]?.SetValue(lightDirection);
            effect.Parameters["Texture"]?.SetValue(texture);

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 
                    terrainData.Indices.Length / 3);
            }
        }

        public void Dispose()
        {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();
        }
    }
}