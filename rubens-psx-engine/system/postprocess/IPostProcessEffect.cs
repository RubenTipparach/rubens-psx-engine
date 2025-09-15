using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rubens_psx_engine.system.postprocess
{
    /// <summary>
    /// Interface for post-process effects that can be chained together
    /// </summary>
    public interface IPostProcessEffect
    {
        string Name { get; }
        bool Enabled { get; set; }
        int Priority { get; }
        
        /// <summary>
        /// Initialize the effect with graphics device and content manager
        /// </summary>
        void Initialize(GraphicsDevice graphicsDevice, Game game);
        
        /// <summary>
        /// Apply the post-process effect
        /// </summary>
        /// <param name="inputTexture">Input texture to process</param>
        /// <param name="outputTarget">Output render target (null for backbuffer)</param>
        /// <param name="spriteBatch">SpriteBatch for drawing</param>
        void Apply(Texture2D inputTexture, RenderTarget2D outputTarget, SpriteBatch spriteBatch);
        
        /// <summary>
        /// Cleanup resources
        /// </summary>
        void Dispose();
    }
}