using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace rubens_psx_engine.entities
{
    /// <summary>
    /// Factory for creating rendering entities with various configurations
    /// </summary>
    public static class RenderingEntityFactory
    {
        /// <summary>
        /// Create a rendering entity with a material
        /// </summary>
        public static RenderingEntity CreateWithMaterial(Vector3 position, string modelPath, Material material)
        {
            var entity = new MaterialRenderingEntity(modelPath, material);
            entity.Position = position;
            entity.Scale = Vector3.One;
            return entity;
        }
        
        /// <summary>
        /// Create a box rendering entity with a material
        /// </summary>
        public static RenderingEntity CreateBox(Vector3 position, Material material, Vector3 scale = default)
        {
            if (scale == default) scale = Vector3.One;
            
            var entity = new MaterialRenderingEntity("models/cube", material);
            entity.Position = position;
            entity.Scale = scale;
            return entity;
        }
        
        /// <summary>
        /// Create a sphere rendering entity with a material
        /// </summary>
        public static RenderingEntity CreateSphere(Vector3 position, Material material, Vector3 scale = default)
        {
            if (scale == default) scale = Vector3.One;
            
            var entity = new MaterialRenderingEntity("models/sphere", material);
            entity.Position = position;
            entity.Scale = scale;
            return entity;
        }
        
        /// <summary>
        /// Create a basic rendering entity (without custom material)
        /// </summary>
        public static RenderingEntity CreateBasic(Vector3 position, string modelPath, 
            string texturePath = null, string effectPath = "shaders/surface/Unlit")
        {
            var entity = new RenderingEntity(modelPath, texturePath, effectPath);
            entity.Position = position;
            entity.Scale = Vector3.One;
            return entity;
        }
        
        /// <summary>
        /// Create a multi-material rendering entity
        /// </summary>
        public static MultiMaterialRenderingEntity CreateMultiMaterial(Vector3 position, string modelPath, 
            params Material[] materials)
        {
            var entity = new MultiMaterialRenderingEntity(modelPath, materials);
            entity.Position = position;
            entity.Scale = Vector3.One;
            return entity;
        }
        
        /// <summary>
        /// Create a multi-material rendering entity with dictionary
        /// </summary>
        public static MultiMaterialRenderingEntity CreateMultiMaterial(Vector3 position, string modelPath, 
            Dictionary<int, Material> materialChannels)
        {
            var entity = new MultiMaterialRenderingEntity(modelPath, materialChannels);
            entity.Position = position;
            entity.Scale = Vector3.One;
            return entity;
        }
    }
}