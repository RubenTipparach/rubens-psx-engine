using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rubens_psx_engine.system.postprocess
{
    /// <summary>
    /// Manages a chain of post-process effects
    /// </summary>
    public class PostProcessStack : IDisposable
    {
        private readonly List<IPostProcessEffect> effects;
        private readonly GraphicsDevice graphicsDevice;
        private readonly Game game;
        private SpriteBatch spriteBatch;
        private RenderTarget2D sceneRenderTarget;
        private RenderTarget2D[] tempTargets;
        private bool isInitialized = false;

        public IReadOnlyList<IPostProcessEffect> Effects => effects.AsReadOnly();
        public bool Enabled { get; set; } = true;

        public PostProcessStack(GraphicsDevice graphicsDevice, Game game)
        {
            this.graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            this.game = game ?? throw new ArgumentNullException(nameof(game));
            effects = new List<IPostProcessEffect>();
        }

        public void Initialize()
        {
            if (isInitialized) return;

            spriteBatch = new SpriteBatch(graphicsDevice);
            
            // Create render targets
            var pp = graphicsDevice.PresentationParameters;
            int width = pp.BackBufferWidth;
            int height = pp.BackBufferHeight;
            var format = pp.BackBufferFormat;

            sceneRenderTarget = new RenderTarget2D(graphicsDevice, width, height, false,
                format, pp.DepthStencilFormat, pp.MultiSampleCount,
                RenderTargetUsage.DiscardContents);

            // Create temporary render targets for effect chaining
            tempTargets = new RenderTarget2D[2];
            tempTargets[0] = new RenderTarget2D(graphicsDevice, width, height, false, format, DepthFormat.None);
            tempTargets[1] = new RenderTarget2D(graphicsDevice, width, height, false, format, DepthFormat.None);

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
        /// Begin rendering to the scene buffer
        /// </summary>
        public void BeginScene()
        {
            if (!Enabled || !isInitialized) return;
            
            graphicsDevice.SetRenderTarget(sceneRenderTarget);
        }

        /// <summary>
        /// End scene rendering and apply all post-process effects
        /// </summary>
        public void EndScene()
        {
            if (!Enabled || !isInitialized)
            {
                // If post-processing is disabled, just render the scene directly
                graphicsDevice.SetRenderTarget(null);
                return;
            }

            var enabledEffects = effects.Where(e => e.Enabled).ToList();
            if (!enabledEffects.Any())
            {
                // No effects enabled, just copy to backbuffer
                CopyToBackBuffer(sceneRenderTarget);
                return;
            }

            Texture2D currentTexture = sceneRenderTarget;
            int targetIndex = 0;

            // Apply each effect in sequence
            for (int i = 0; i < enabledEffects.Count; i++)
            {
                var effect = enabledEffects[i];
                var isLastEffect = (i == enabledEffects.Count - 1);
                
                // Determine output target
                RenderTarget2D outputTarget = null;
                if (!isLastEffect)
                {
                    outputTarget = tempTargets[targetIndex];
                    targetIndex = 1 - targetIndex; // Ping-pong between targets
                }

                try
                {
                    effect.Apply(currentTexture, outputTarget, spriteBatch);
                    
                    // Update current texture for next effect
                    if (!isLastEffect)
                    {
                        currentTexture = outputTarget;
                    }
                }
                catch (Exception ex)
                {
                    // Log error and skip this effect
                    System.Diagnostics.Debug.WriteLine($"Error applying post-process effect '{effect.Name}': {ex.Message}");
                    
                    // If this was the last effect, copy current texture to backbuffer
                    if (isLastEffect)
                    {
                        CopyToBackBuffer(currentTexture);
                    }
                }
            }
        }

        private void CopyToBackBuffer(Texture2D texture)
        {
            graphicsDevice.SetRenderTarget(null);
            spriteBatch.Begin(0, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
            spriteBatch.Draw(texture, graphicsDevice.Viewport.Bounds, Color.White);
            spriteBatch.End();
        }

        public void Dispose()
        {
            foreach (var effect in effects)
            {
                effect?.Dispose();
            }
            
            sceneRenderTarget?.Dispose();
            tempTargets?[0]?.Dispose();
            tempTargets?[1]?.Dispose();
            spriteBatch?.Dispose();
            
            isInitialized = false;
        }
    }
}