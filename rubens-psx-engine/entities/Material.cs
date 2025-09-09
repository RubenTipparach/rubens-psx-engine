using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine;

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
                    effect = Globals.screenManager.Content.Load<Effect>(effectPath);
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
    }
}