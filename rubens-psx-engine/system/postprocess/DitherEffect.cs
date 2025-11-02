using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.system.config;

namespace rubens_psx_engine.system.postprocess
{
    /// <summary>
    /// Dithering post-process effect for retro PSX-style rendering
    /// </summary>
    public class DitherEffect : IPostProcessEffect
    {
        private Effect ditherEffect;
        
        public string Name => "Dither";
        public bool Enabled { get; set; } = true;
        public int Priority => 50; // Lower priority = earlier in chain (before bloom)

        public float DitherStrength { get; set; } = 1.0f;
        public Vector2 ScreenResolution { get; set; } = Vector2.One;
        public float ColorLevels { get; set; } = 6.0f;

        public void Initialize(GraphicsDevice graphicsDevice, Game game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (graphicsDevice == null) throw new ArgumentNullException(nameof(graphicsDevice));
            
            ditherEffect = game.Content.Load<Effect>("shaders/postprocess/Dither");
            
            // Load settings from config
            LoadFromConfig();
        }

        /// <summary>
        /// Load dither settings from configuration
        /// </summary>
        public void LoadFromConfig()
        {
            var config = RenderingConfigManager.Config.Dither;
            
            DitherStrength = config.Strength;
            ColorLevels = config.ColorLevels;
            ScreenResolution = new Vector2(config.RenderWidth, config.RenderHeight);
        }

        public void Apply(Texture2D inputTexture, RenderTarget2D outputTarget, SpriteBatch spriteBatch)
        {
            if (inputTexture == null) throw new ArgumentNullException(nameof(inputTexture));
            if (spriteBatch == null) throw new ArgumentNullException(nameof(spriteBatch));

            var graphicsDevice = spriteBatch.GraphicsDevice;
            graphicsDevice.SetRenderTarget(outputTarget);

            // Set effect parameters
            ditherEffect.Parameters["DitherStrength"]?.SetValue(DitherStrength);
            ditherEffect.Parameters["ScreenSize"]?.SetValue(ScreenResolution);
            ditherEffect.Parameters["ColorLevels"]?.SetValue(ColorLevels);

            var bounds = outputTarget?.Bounds ?? graphicsDevice.Viewport.Bounds;

            spriteBatch.Begin(0, BlendState.Opaque, SamplerState.PointClamp, 
                            DepthStencilState.None, RasterizerState.CullCounterClockwise, ditherEffect);
            spriteBatch.Draw(inputTexture, bounds, Color.White);
            spriteBatch.End();
        }

        public void Dispose()
        {
            // Effect is managed by content manager
        }
    }
}