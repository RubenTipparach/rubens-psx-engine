using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.system.config;

namespace rubens_psx_engine.system.postprocess
{
    /// <summary>
    /// Enhanced post-process stack that renders at a different internal resolution
    /// then scales to the display resolution for authentic retro pixelated look
    /// </summary>
    public class ScaledPostProcessStack : IDisposable
    {
        private readonly List<IPostProcessEffect> effects;
        private readonly GraphicsDevice graphicsDevice;
        private readonly Game game;
        private SpriteBatch spriteBatch;
        
        // Render targets for dual-resolution rendering
        private RenderTarget2D lowResSceneTarget;     // Scene rendered at low resolution
        private RenderTarget2D lowResProcessTarget1;  // For ping-pong post-processing
        private RenderTarget2D lowResProcessTarget2;  // For ping-pong post-processing
        
        private bool isInitialized = false;
        private Point renderResolution;
        private Point displayResolution;

        public IReadOnlyList<IPostProcessEffect> Effects => effects.AsReadOnly();
        public bool Enabled { get; set; } = true;
        
        public Point RenderResolution => renderResolution;
        public Point DisplayResolution => displayResolution;

        public ScaledPostProcessStack(GraphicsDevice graphicsDevice, Game game)
        {
            this.graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            this.game = game ?? throw new ArgumentNullException(nameof(game));
            effects = new List<IPostProcessEffect>();
        }

        public void Initialize()
        {
            if (isInitialized) return;

            spriteBatch = new SpriteBatch(graphicsDevice);
            
            // Get resolutions from config
            var config = RenderingConfigManager.Config;
            renderResolution = RenderingConfigManager.GetRenderResolution();
            
            var pp = graphicsDevice.PresentationParameters;
            displayResolution = new Point(pp.BackBufferWidth, pp.BackBufferHeight);
            
            var format = pp.BackBufferFormat;

            // Create low-resolution render targets for internal rendering
            lowResSceneTarget = new RenderTarget2D(graphicsDevice, 
                renderResolution.X, renderResolution.Y, false,
                format, DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents);

            // Create processing targets at render resolution
            lowResProcessTarget1 = new RenderTarget2D(graphicsDevice, 
                renderResolution.X, renderResolution.Y, false, format, DepthFormat.None);
            lowResProcessTarget2 = new RenderTarget2D(graphicsDevice, 
                renderResolution.X, renderResolution.Y, false, format, DepthFormat.None);

            // Initialize all effects
            foreach (var effect in effects)
            {
                effect.Initialize(graphicsDevice, game);
            }

            isInitialized = true;
        }

        public void AddEffect(IPostProcessEffect effect)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            
            effects.Add(effect);
            effects.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            
            if (isInitialized)
            {
                effect.Initialize(graphicsDevice, game);
            }
        }

        public void RemoveEffect(IPostProcessEffect effect)
        {
            effects.Remove(effect);
        }

        public void RemoveEffect(string name)
        {
            var effect = effects.FirstOrDefault(e => e.Name == name);
            if (effect != null)
            {
                RemoveEffect(effect);
            }
        }

        public T GetEffect<T>() where T : class, IPostProcessEffect
        {
            return effects.OfType<T>().FirstOrDefault();
        }

        public IPostProcessEffect GetEffect(string name)
        {
            return effects.FirstOrDefault(e => e.Name == name);
        }

        /// <summary>
        /// Begin rendering to the low-resolution scene buffer
        /// </summary>
        public void BeginScene()
        {
            if (!Enabled || !isInitialized) 
            {
                // If disabled, still need to render at low resolution for consistency
                graphicsDevice.SetRenderTarget(lowResSceneTarget);
                return;
            }
            
            graphicsDevice.SetRenderTarget(lowResSceneTarget);
            
            // Set viewport to render resolution
            var viewport = new Viewport(0, 0, renderResolution.X, renderResolution.Y);
            graphicsDevice.Viewport = viewport;
        }

        /// <summary>
        /// End scene rendering, apply post-processing, and scale to display resolution
        /// </summary>
        public void EndScene()
        {
            if (!isInitialized)
            {
                graphicsDevice.SetRenderTarget(null);
                return;
            }

            // Restore display viewport
            var displayViewport = new Viewport(0, 0, displayResolution.X, displayResolution.Y);
            graphicsDevice.Viewport = displayViewport;

            if (!Enabled)
            {
                // Just scale the low-res scene to display resolution
                ScaleToDisplay(lowResSceneTarget);
                return;
            }

            var enabledEffects = effects.Where(e => e.Enabled).ToList();
            if (!enabledEffects.Any())
            {
                // No effects enabled, just scale to display
                ScaleToDisplay(lowResSceneTarget);
                return;
            }

            // Apply post-processing effects at render resolution
            Texture2D currentTexture = lowResSceneTarget;
            int targetIndex = 0;

            for (int i = 0; i < enabledEffects.Count; i++)
            {
                var effect = enabledEffects[i];
                var isLastEffect = (i == enabledEffects.Count - 1);
                
                RenderTarget2D outputTarget = null;
                if (!isLastEffect)
                {
                    outputTarget = targetIndex == 0 ? lowResProcessTarget1 : lowResProcessTarget2;
                    targetIndex = 1 - targetIndex; // Ping-pong between targets
                }

                try
                {
                    // Set viewport for effect processing at render resolution
                    graphicsDevice.Viewport = new Viewport(0, 0, renderResolution.X, renderResolution.Y);
                    
                    effect.Apply(currentTexture, outputTarget, spriteBatch);
                    
                    if (!isLastEffect)
                    {
                        currentTexture = outputTarget;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error applying post-process effect '{effect.Name}': {ex.Message}");
                    
                    if (isLastEffect)
                    {
                        ScaleToDisplay(currentTexture);
                        return;
                    }
                }
            }

            // Restore display viewport and scale final result
            graphicsDevice.Viewport = displayViewport;
            ScaleToDisplay(currentTexture);
        }

        /// <summary>
        /// Scale the low-resolution processed image to display resolution
        /// </summary>
        private void ScaleToDisplay(Texture2D lowResTexture)
        {
            graphicsDevice.SetRenderTarget(null);
            
            var config = RenderingConfigManager.Config;
            var samplerState = RenderingConfigManager.GetSamplerState();
            
            Rectangle destinationRect;
            
            if (config.Rendering.MaintainAspectRatio)
            {
                // Calculate scaling to maintain aspect ratio
                float renderAspect = (float)renderResolution.X / renderResolution.Y;
                float displayAspect = (float)displayResolution.X / displayResolution.Y;
                
                if (renderAspect > displayAspect)
                {
                    // Fit to width
                    int scaledHeight = (int)(displayResolution.X / renderAspect);
                    int offsetY = (displayResolution.Y - scaledHeight) / 2;
                    destinationRect = new Rectangle(0, offsetY, displayResolution.X, scaledHeight);
                }
                else
                {
                    // Fit to height
                    int scaledWidth = (int)(displayResolution.Y * renderAspect);
                    int offsetX = (displayResolution.X - scaledWidth) / 2;
                    destinationRect = new Rectangle(offsetX, 0, scaledWidth, displayResolution.Y);
                }
                
                // Fill letterbox areas with black
                graphicsDevice.Clear(Color.Black);
            }
            else
            {
                // Stretch to fill entire display
                destinationRect = new Rectangle(0, 0, displayResolution.X, displayResolution.Y);
            }

            spriteBatch.Begin(0, BlendState.Opaque, samplerState, 
                            DepthStencilState.None, RasterizerState.CullCounterClockwise);
            spriteBatch.Draw(lowResTexture, destinationRect, Color.White);
            spriteBatch.End();
        }

        /// <summary>
        /// Update render targets when resolution changes
        /// </summary>
        public void OnResolutionChanged()
        {
            if (!isInitialized) return;
            
            // Dispose old targets
            lowResSceneTarget?.Dispose();
            lowResProcessTarget1?.Dispose();
            lowResProcessTarget2?.Dispose();
            
            // Reinitialize with new settings
            isInitialized = false;
            Initialize();
        }

        public void Dispose()
        {
            foreach (var effect in effects)
            {
                effect?.Dispose();
            }
            
            lowResSceneTarget?.Dispose();
            lowResProcessTarget1?.Dispose();
            lowResProcessTarget2?.Dispose();
            spriteBatch?.Dispose();
            
            isInitialized = false;
        }
    }
}