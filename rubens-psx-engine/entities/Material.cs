using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine;
using System.Linq;

namespace rubens_psx_engine.entities
{
    /// <summary>
    /// Abstract base class for material wrappers around shaders
    /// </summary>
    public abstract class Material
    {
        protected Effect effect;
        protected Texture2D texture;
        
        public Effect Effect => effect;
        public Texture2D Texture => texture;
        
        protected Material(string effectPath, string texturePath = null)
        {
            LoadEffect(effectPath);
            LoadTexture(texturePath);
        }

        protected virtual void LoadEffect(string effectPath)
        {
            if (!string.IsNullOrEmpty(effectPath))
            {
                try
                {
                    var loadedEffect = Globals.screenManager.Content.Load<Effect>(effectPath);
                    // Clone the effect to avoid sharing between materials
                    effect = loadedEffect.Clone();
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"Failed to load effect {effectPath}: {ex.Message}");
                }
            }
        }

        protected virtual void LoadTexture(string texturePath)
        {
            if (!string.IsNullOrEmpty(texturePath))
            {
                try
                {
                    texture = Globals.screenManager.Content.Load<Texture2D>(texturePath);
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"Failed to load texture {texturePath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Apply material parameters to the shader
        /// </summary>
        /// <param name="camera">Current camera for view/projection matrices</param>
        /// <param name="worldMatrix">World transformation matrix</param>
        public abstract void Apply(Camera camera, Matrix worldMatrix);
        
        /// <summary>
        /// Set material-specific properties
        /// Override in concrete materials to expose shader parameters
        /// </summary>
        public virtual void SetProperties() { }
        
        /// <summary>
        /// Check if this material is compatible with the given vertex declaration
        /// Override in concrete materials to specify vertex requirements
        /// </summary>
        /// <param name="vertexDeclaration">Vertex declaration to check</param>
        /// <returns>True if compatible, false otherwise</returns>
        public virtual bool IsCompatibleWithVertexDeclaration(VertexDeclaration vertexDeclaration)
        {
            // Default implementation: compatible with any vertex declaration
            return true;
        }
        
        /// <summary>
        /// Check if this material can be applied to the given model mesh part
        /// </summary>
        /// <param name="meshPart">The mesh part to check</param>
        /// <returns>True if compatible, false otherwise</returns>
        public virtual bool CanApplyToMeshPart(ModelMeshPart meshPart)
        {
            if (meshPart?.VertexBuffer?.VertexDeclaration == null)
                return false;
                
            return IsCompatibleWithVertexDeclaration(meshPart.VertexBuffer.VertexDeclaration);
        }
    }
}