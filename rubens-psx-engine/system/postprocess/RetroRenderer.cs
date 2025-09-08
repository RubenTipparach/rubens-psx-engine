using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.system.config;

namespace rubens_psx_engine.system.postprocess
{
    /// <summary>
    /// High-level renderer that manages the retro post-processing pipeline and native UI rendering
    /// </summary>
    public class RetroRenderer : IDisposable
    {
        private ScaledPostProcessStack postProcessStack;
        private readonly GraphicsDevice graphicsDevice;
        private readonly Game game;
        private SpriteBatch uiSpriteBatch;
        private RenderTarget2D nativeUITarget;
        private bool isInitialized = false;

        public bool Enabled { get; set; } = true;
        public ScaledPostProcessStack PostProcessStack => postProcessStack;
        
        /// <summary>
        /// SpriteBatch for native resolution UI rendering
        /// </summary>
        public SpriteBatch UISpriteBatch => uiSpriteBatch;

        public RetroRenderer(GraphicsDevice graphicsDevice, Game game)
        {
            this.graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            this.game = game ?? throw new ArgumentNullException(nameof(game));
        }

        public void Initialize()
        {
            if (isInitialized) return;

            // Create the post-process stack
            postProcessStack = new ScaledPostProcessStack(graphicsDevice, game);
            
            // Load configuration and set up effects based on config
            var config = RenderingConfigManager.Config;
            
            // Set post-process stack enabled state based on config
            postProcessStack.Enabled = config.Rendering.EnablePostProcessing;
            
            if (config.Rendering.EnablePostProcessing)
            {
                // Add tint effect if enabled
                if (config.Tint.Enabled)
                {
                    var tintEffect = new TintEffect
                    {
                        TintColor = config.Tint.GetColor(),
                        Intensity = config.Tint.Intensity
                    };
                    postProcessStack.AddEffect(tintEffect);
                }

                // Add bloom effect
                var bloomEffect = new BloomEffect();
                if (config.Bloom.Preset >= 0 && config.Bloom.Preset < BloomSettings.PresetSettings.Length)
                {
                    bloomEffect.Settings = BloomSettings.PresetSettings[config.Bloom.Preset];
                }
                postProcessStack.AddEffect(bloomEffect);

                // Add dither effect (this handles the pixelation)
                var ditherEffect = new DitherEffect();
                ditherEffect.LoadFromConfig(); // Load config values
                postProcessStack.AddEffect(ditherEffect);
            }

            postProcessStack.Initialize();
            
            // Initialize UI rendering components
            InitializeUIRendering();
            
            isInitialized = true;
        }
        
        /// <summary>
        /// Initialize native resolution UI rendering components
        /// </summary>
        private void InitializeUIRendering()
        {
            var config = RenderingConfigManager.Config.Rendering.UI;
            
            // Create SpriteBatch for UI rendering
            uiSpriteBatch = new SpriteBatch(graphicsDevice);
            
            // Create render target for UI at native resolution
            if (config.UseNativeResolution)
            {
                int nativeWidth = graphicsDevice.PresentationParameters.BackBufferWidth;
                int nativeHeight = graphicsDevice.PresentationParameters.BackBufferHeight;
                
                // Apply scaling factor if specified
                float scaleFactor = config.GetValidatedScaleFactor();
                if (scaleFactor != 1.0f)
                {
                    nativeWidth = (int)(nativeWidth / scaleFactor);
                    nativeHeight = (int)(nativeHeight / scaleFactor);
                }
                
                nativeUITarget = new RenderTarget2D(graphicsDevice, nativeWidth, nativeHeight);
            }
        }

        /// <summary>
        /// Begin rendering the scene at the configured low resolution
        /// </summary>
        public void BeginScene()
        {
            if (!isInitialized) return;
            
            postProcessStack.BeginScene();
        }

        /// <summary>
        /// End scene rendering and apply post-processing effects
        /// </summary>
        public void EndScene()
        {
            if (!isInitialized) return;
            
            postProcessStack.EndScene();
        }
        
        /// <summary>
        /// Begin native resolution UI rendering
        /// Call this after EndScene() to render UI on top of the processed 3D world
        /// </summary>
        public void BeginUIRendering()
        {
            if (!isInitialized) return;
            
            var config = RenderingConfigManager.Config.Rendering.UI;
            
            if (config.UseNativeResolution && nativeUITarget != null)
            {
                // Store current render target state before switching
                var previousRenderTarget = graphicsDevice.GetRenderTargets();
                var previousViewport = graphicsDevice.Viewport;
                
                // Render UI to native resolution render target
                graphicsDevice.SetRenderTarget(nativeUITarget);
                graphicsDevice.Clear(Color.Transparent); // Clear with transparency
                
                // Set viewport to native resolution
                var nativeViewport = new Viewport(0, 0, nativeUITarget.Width, nativeUITarget.Height);
                graphicsDevice.Viewport = nativeViewport;
                
                // Begin UI SpriteBatch for crisp rendering
                uiSpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);
            }
            else
            {
                // Render UI directly to back buffer at current resolution
                graphicsDevice.SetRenderTarget(null);
                
                // Begin UI SpriteBatch with standard settings
                uiSpriteBatch.Begin();
            }
        }
        
        /// <summary>
        /// End UI rendering and composite it over the final image
        /// </summary>
        public void EndUIRendering()
        {
            if (!isInitialized) return;
            
            var config = RenderingConfigManager.Config.Rendering.UI;
            
            if (config.UseNativeResolution && nativeUITarget != null)
            {
                // End UI SpriteBatch
                uiSpriteBatch.End();
                
                // Restore back buffer as render target
                graphicsDevice.SetRenderTarget(null);
                
                // Restore full screen viewport
                var fullViewport = new Viewport(0, 0, 
                    graphicsDevice.PresentationParameters.BackBufferWidth,
                    graphicsDevice.PresentationParameters.BackBufferHeight);
                graphicsDevice.Viewport = fullViewport;
                
                // Composite UI over the final processed image using a temporary SpriteBatch
                // Use AlphaBlend to overlay UI on top of existing back buffer content
                using (var compositeBatch = new SpriteBatch(graphicsDevice))
                {
                    compositeBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);
                    compositeBatch.Draw(nativeUITarget, Vector2.Zero, Color.White);
                    compositeBatch.End();
                }
            }
            else
            {
                // End UI SpriteBatch for legacy rendering
                uiSpriteBatch.End();
            }
        }

        /// <summary>
        /// Reload configuration and update effects
        /// </summary>
        public void ReloadConfig()
        {
            RenderingConfigManager.ReloadConfig();
            
            // Update post-process stack enabled state
            var config = RenderingConfigManager.Config;
            postProcessStack.Enabled = config.Rendering.EnablePostProcessing;
            
            // Update dither effect with new settings
            var ditherEffect = postProcessStack.GetEffect<DitherEffect>();
            ditherEffect?.LoadFromConfig();
            
            // Update tint effect
            var tintEffect = postProcessStack.GetEffect<TintEffect>();
            if (tintEffect != null)
            {
                var tintConfig = config.Tint;
                tintEffect.TintColor = tintConfig.GetColor();
                tintEffect.Intensity = tintConfig.Intensity;
                tintEffect.Enabled = tintConfig.Enabled;
            }
            
            // Update bloom effect
            var bloomEffect = postProcessStack.GetEffect<BloomEffect>();
            if (bloomEffect != null)
            {
                var bloomConfig = config.Bloom;
                if (bloomConfig.Preset >= 0 && bloomConfig.Preset < BloomSettings.PresetSettings.Length)
                {
                    bloomEffect.Settings = BloomSettings.PresetSettings[bloomConfig.Preset];
                }
            }
        }

        /// <summary>
        /// Call when screen resolution changes
        /// </summary>
        public void OnResolutionChanged()
        {
            postProcessStack?.OnResolutionChanged();
        }

        /// <summary>
        /// Get current render resolution from config
        /// </summary>
        public Point GetRenderResolution()
        {
            return RenderingConfigManager.GetRenderResolution();
        }

        /// <summary>
        /// Get current display resolution
        /// </summary>
        public Point GetDisplayResolution()
        {
            return postProcessStack?.DisplayResolution ?? Point.Zero;
        }

        public void Dispose()
        {
            postProcessStack?.Dispose();
            uiSpriteBatch?.Dispose();
            nativeUITarget?.Dispose();
        }
    }
}