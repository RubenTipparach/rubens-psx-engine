using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rubens_psx_engine.system.postprocess
{
    /// <summary>
    /// Simple tint post-process effect
    /// </summary>
    public class TintEffect : IPostProcessEffect
    {
        private Effect tintEffect;
        
        public string Name => "Tint";
        public bool Enabled { get; set; } = true;
        public int Priority => 50; // Lower priority = earlier in chain

        public Color TintColor { get; set; } = Color.White;
        public float Intensity { get; set; } = 1.0f;

        public void Initialize(GraphicsDevice graphicsDevice, Game game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            
            tintEffect = game.Content.Load<Effect>("shaders/postprocess/Tint");
        }

        public void Apply(Texture2D inputTexture, RenderTarget2D outputTarget, SpriteBatch spriteBatch)
        {
            if (inputTexture == null) throw new ArgumentNullException(nameof(inputTexture));
            if (spriteBatch == null) throw new ArgumentNullException(nameof(spriteBatch));

            var graphicsDevice = spriteBatch.GraphicsDevice;
            graphicsDevice.SetRenderTarget(outputTarget);

            // Set effect parameters
            tintEffect.Parameters["TintColor"]?.SetValue(TintColor.ToVector4());
            tintEffect.Parameters["Intensity"]?.SetValue(Intensity);

            var bounds = outputTarget?.Bounds ?? graphicsDevice.Viewport.Bounds;

            spriteBatch.Begin(0, BlendState.Opaque, SamplerState.LinearClamp, 
                            DepthStencilState.None, RasterizerState.CullCounterClockwise, tintEffect);
            spriteBatch.Draw(inputTexture, bounds, Color.White);
            spriteBatch.End();
        }

        public void Dispose()
        {
            // Effect is managed by content manager
        }
    }
}