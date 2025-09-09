using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace rubens_psx_engine.entities
{
    /// <summary>
    /// Rendering entity that supports multiple material channels for complex models
    /// Each material channel can have different textures and shader parameters
    /// </summary>
    public class MultiMaterialRenderingEntity : RenderingEntity
    {
        protected Dictionary<int, Material> materials;
        
        public Dictionary<int, Material> Materials => materials;

        public MultiMaterialRenderingEntity(string modelPath, Dictionary<int, Material> materialChannels) 
            : base(modelPath, null, null, true) // Don't load texture/effect in base class
        {
            this.materials = materialChannels ?? throw new System.ArgumentNullException(nameof(materialChannels));
        }

        public MultiMaterialRenderingEntity(string modelPath, params Material[] materialList) 
            : base(modelPath, null, null, true)
        {
            materials = new Dictionary<int, Material>();
            for (int i = 0; i < materialList.Length; i++)
            {
                materials[i] = materialList[i];
            }
        }

        public override void Draw(GameTime gameTime, Camera camera)
        {
            if (!IsVisible || model == null || !materials.Any()) return;

            model.CopyAbsoluteBoneTransformsTo(transforms);
            Matrix world = GetWorldMatrix();

            // Draw each mesh with its corresponding material
            int meshIndex = 0;
            foreach (ModelMesh mesh in model.Meshes)
            {
                // Get the material for this mesh
                if (materials.TryGetValue(meshIndex, out Material material) && material?.Effect != null)
                {
                    Matrix meshWorld = transforms[mesh.ParentBone.Index] * world;
                    
                    // Apply material settings for this mesh
                    material.Apply(camera, meshWorld);
                    
                    // Set technique if available
                    if (material.Effect.Techniques.Count > 0 && material.Effect.CurrentTechnique == null)
                    {
                        material.Effect.CurrentTechnique = material.Effect.Techniques[0];
                    }

                    // Apply the material's effect to all mesh parts
                    foreach (ModelMeshPart part in mesh.MeshParts)
                    {
                        part.Effect = material.Effect;
                    }

                    // Draw the mesh using its assigned material
                    mesh.Draw();
                }
                else
                {
                    // Fallback: draw with original effects if no material assigned
                    Matrix meshWorld = transforms[mesh.ParentBone.Index] * world;
                    
                    foreach (ModelMeshPart part in mesh.MeshParts)
                    {
                        if (part.Effect is BasicEffect basicEffect)
                        {
                            basicEffect.World = meshWorld;
                            basicEffect.View = camera.View;
                            basicEffect.Projection = camera.Projection;
                        }
                    }
                    mesh.Draw();
                }
                
                meshIndex++;
            }
        }


        public void SetMaterial(int channel, Material material)
        {
            materials[channel] = material;
        }

        public Material GetMaterial(int channel)
        {
            return materials.TryGetValue(channel, out Material material) ? material : null;
        }
    }
}