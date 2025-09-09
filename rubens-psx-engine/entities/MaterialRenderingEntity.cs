using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rubens_psx_engine.entities
{
    /// <summary>
    /// Rendering entity that uses a material for custom shader effects
    /// </summary>
    public class MaterialRenderingEntity : RenderingEntity
    {
        protected Material material;
        
        public Material Material => material;

        public MaterialRenderingEntity(string modelPath, Material material) 
            : base(modelPath, null, null, true) // Don't load texture/effect in base class
        {
            this.material = material ?? throw new System.ArgumentNullException(nameof(material));
        }

        public override void Draw(GameTime gameTime, Camera camera)
        {
            if (!IsVisible || model == null || material?.Effect == null) return;

            model.CopyAbsoluteBoneTransformsTo(transforms);
            Matrix world = GetWorldMatrix();

            // Apply material settings
            material.Apply(camera, world);

            foreach (ModelMesh mesh in model.Meshes)
            {
                Matrix meshWorld = transforms[mesh.ParentBone.Index] * world;
                material.Effect.Parameters["World"]?.SetValue(meshWorld);

                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    part.Effect = material.Effect;
                    
                    // Set the current technique if available
                    if (material.Effect.Techniques.Count > 0)
                    {
                        material.Effect.CurrentTechnique = material.Effect.Techniques[0];
                    }
                }

                mesh.Draw();
            }
        }
    }
}