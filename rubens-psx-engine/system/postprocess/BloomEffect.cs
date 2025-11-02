using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.system.config;

namespace rubens_psx_engine.system.postprocess
{
    /// <summary>
    /// Bloom post-process effect that implements the IPostProcessEffect interface
    /// </summary>
    public class BloomEffect : IPostProcessEffect
    {
        private Effect bloomExtractEffect;
        private Effect bloomCombineEffect;
        private Effect gaussianBlurEffect;
        private Effect ditherEffect;

        private RenderTarget2D renderTarget1;
        private RenderTarget2D renderTarget2;

        private BloomSettings settings = BloomSettings.PresetSettings[0];
        private IntermediateBuffer showBuffer = IntermediateBuffer.FinalResult;

        public string Name => "Bloom";
        public bool Enabled { get; set; } = true;
        public int Priority => 100; // Higher priority = later in chain

        /// <summary>
        /// If true, bloom is added on top of the input image (for use after dithering).
        /// If false, bloom combines with original scene (standalone mode).
        /// </summary>
        public bool AdditiveMode { get; set; } = false;

        public BloomSettings Settings
        {
            get => settings;
            set => settings = value ?? BloomSettings.PresetSettings[0];
        }

        public IntermediateBuffer ShowBuffer
        {
            get => showBuffer;
            set => showBuffer = value;
        }

        public enum IntermediateBuffer
        {
            PreBloom = 1,
            BlurredHorizontally = 2,
            BlurredBothWays = 3,
            FinalResult = 4,
        }

        public void Initialize(GraphicsDevice graphicsDevice, Game game)
        {
            if (graphicsDevice == null) throw new ArgumentNullException(nameof(graphicsDevice));
            if (game == null) throw new ArgumentNullException(nameof(game));

            // Load effects
            bloomExtractEffect = game.Content.Load<Effect>("shaders/postprocess/BloomExtract");
            bloomCombineEffect = game.Content.Load<Effect>("shaders/postprocess/BloomCombine");
            gaussianBlurEffect = game.Content.Load<Effect>("shaders/postprocess/GaussianBlur");
            ditherEffect = game.Content.Load<Effect>("shaders/postprocess/Dither");

            // Create render targets at half resolution for performance
            var pp = graphicsDevice.PresentationParameters;
            int width = pp.BackBufferWidth / 2;
            int height = pp.BackBufferHeight / 2;
            var format = pp.BackBufferFormat;

            renderTarget1 = new RenderTarget2D(graphicsDevice, width, height, false, format, DepthFormat.None);
            renderTarget2 = new RenderTarget2D(graphicsDevice, width, height, false, format, DepthFormat.None);
        }

        public void Apply(Texture2D inputTexture, RenderTarget2D outputTarget, SpriteBatch spriteBatch)
        {
            if (inputTexture == null) throw new ArgumentNullException(nameof(inputTexture));
            if (spriteBatch == null) throw new ArgumentNullException(nameof(spriteBatch));

            var graphicsDevice = spriteBatch.GraphicsDevice;
            graphicsDevice.SamplerStates[1] = SamplerState.LinearClamp;

            Console.WriteLine($"[BloomEffect] Apply called - AdditiveMode: {AdditiveMode}, DitherEffect loaded: {ditherEffect != null}");

            // Pass 1: Extract bright parts of the scene
            bloomExtractEffect.Parameters["BloomThreshold"].SetValue(Settings.BloomThreshold);
            DrawFullscreenQuad(inputTexture, renderTarget1, bloomExtractEffect, 
                              IntermediateBuffer.PreBloom, spriteBatch);

            // Pass 2: Horizontal blur
            SetBlurEffectParameters(1.0f / renderTarget1.Width, 0);
            DrawFullscreenQuad(renderTarget1, renderTarget2, gaussianBlurEffect,
                              IntermediateBuffer.BlurredHorizontally, spriteBatch);

            // Pass 3: Vertical blur
            SetBlurEffectParameters(0, 1.0f / renderTarget1.Height);
            DrawFullscreenQuad(renderTarget2, renderTarget1, gaussianBlurEffect,
                              IntermediateBuffer.BlurredBothWays, spriteBatch);

            // Pass 4: Combine bloom with input
            graphicsDevice.SetRenderTarget(outputTarget);

            if (AdditiveMode)
            {
                // Additive mode: Combine bloom with input first, THEN apply dither
                // renderTarget1 contains blurred bloom extracted from original input
                var bounds = outputTarget?.Bounds ?? graphicsDevice.Viewport.Bounds;

                if (ditherEffect != null)
                {
                    // First, combine bloom and base image into renderTarget2
                    graphicsDevice.SetRenderTarget(renderTarget2);

                    // Draw base image
                    spriteBatch.Begin(0, BlendState.Opaque, SamplerState.PointClamp, null, null, null);
                    spriteBatch.Draw(inputTexture, new Rectangle(0, 0, renderTarget2.Width, renderTarget2.Height), Color.White);
                    spriteBatch.End();

                    // Add bloom on top
                    spriteBatch.Begin(0, BlendState.Additive, SamplerState.LinearClamp, null, null, null);
                    spriteBatch.Draw(renderTarget1, new Rectangle(0, 0, renderTarget2.Width, renderTarget2.Height), Color.White * Settings.BloomIntensity);
                    spriteBatch.End();

                    // Now apply dither shader to the combined bloom+base result
                    graphicsDevice.SetRenderTarget(outputTarget);

                    // Set dither shader parameters
                    var ditherConfig = RenderingConfigManager.Config.Dither;
                    ditherEffect.Parameters["DitherStrength"]?.SetValue(ditherConfig.Strength);
                    ditherEffect.Parameters["ScreenSize"]?.SetValue(new Vector2(ditherConfig.RenderWidth, ditherConfig.RenderHeight));
                    ditherEffect.Parameters["ColorLevels"]?.SetValue(ditherConfig.ColorLevels);

                    spriteBatch.Begin(0, BlendState.Opaque, SamplerState.PointClamp, null, null, ditherEffect);
                    spriteBatch.Draw(renderTarget2, bounds, Color.White);
                    spriteBatch.End();
                }
                else
                {
                    // No dither shader - fallback to simple additive bloom
                    spriteBatch.Begin(0, BlendState.Opaque, SamplerState.PointClamp, null, null, null);
                    spriteBatch.Draw(inputTexture, bounds, Color.White);
                    spriteBatch.End();

                    spriteBatch.Begin(0, BlendState.Additive, SamplerState.LinearClamp, null, null, null);
                    spriteBatch.Draw(renderTarget1, bounds, Color.White * Settings.BloomIntensity);
                    spriteBatch.End();
                }
            }
            else
            {
                // Standard mode: Use bloom combine shader
                var parameters = bloomCombineEffect.Parameters;
                parameters["BloomIntensity"].SetValue(Settings.BloomIntensity);
                parameters["BaseIntensity"].SetValue(Settings.BaseIntensity);
                parameters["BloomSaturation"].SetValue(Settings.BloomSaturation);
                parameters["BaseSaturation"].SetValue(Settings.BaseSaturation);

                graphicsDevice.Textures[1] = inputTexture; // Original scene

                var bounds = outputTarget?.Bounds ?? graphicsDevice.Viewport.Bounds;
                DrawFullscreenQuad(renderTarget1, bounds.Width, bounds.Height,
                                  bloomCombineEffect, IntermediateBuffer.FinalResult, spriteBatch);
            }
        }

        private void DrawFullscreenQuad(Texture2D texture, RenderTarget2D renderTarget,
                                      Effect effect, IntermediateBuffer currentBuffer, SpriteBatch spriteBatch)
        {
            spriteBatch.GraphicsDevice.SetRenderTarget(renderTarget);
            DrawFullscreenQuad(texture, renderTarget.Width, renderTarget.Height,
                              effect, currentBuffer, spriteBatch);
        }

        private void DrawFullscreenQuad(Texture2D texture, int width, int height,
                                      Effect effect, IntermediateBuffer currentBuffer, SpriteBatch spriteBatch)
        {
            // Skip effect if showing intermediate buffer
            if (showBuffer < currentBuffer)
            {
                effect = null;
            }

            spriteBatch.Begin(0, BlendState.Opaque, null, null, null, effect);
            spriteBatch.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
            spriteBatch.End();

            spriteBatch.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        private void SetBlurEffectParameters(float dx, float dy)
        {
            var weightsParameter = gaussianBlurEffect.Parameters["SampleWeights"];
            var offsetsParameter = gaussianBlurEffect.Parameters["SampleOffsets"];

            int sampleCount = weightsParameter.Elements.Count;
            float[] sampleWeights = new float[sampleCount];
            Vector2[] sampleOffsets = new Vector2[sampleCount];

            sampleWeights[0] = ComputeGaussian(0);
            sampleOffsets[0] = Vector2.Zero;

            float totalWeights = sampleWeights[0];

            for (int i = 0; i < sampleCount / 2; i++)
            {
                float weight = ComputeGaussian(i + 1);
                sampleWeights[i * 2 + 1] = weight;
                sampleWeights[i * 2 + 2] = weight;
                totalWeights += weight * 2;

                float sampleOffset = i * 2 + 1.5f;
                Vector2 delta = new Vector2(dx, dy) * sampleOffset;

                sampleOffsets[i * 2 + 1] = delta;
                sampleOffsets[i * 2 + 2] = -delta;
            }

            for (int i = 0; i < sampleWeights.Length; i++)
            {
                sampleWeights[i] /= totalWeights;
            }

            weightsParameter.SetValue(sampleWeights);
            offsetsParameter.SetValue(sampleOffsets);
        }

        private float ComputeGaussian(float n)
        {
            float theta = Settings.BlurAmount;
            return (float)((1.0 / Math.Sqrt(2 * Math.PI * theta)) *
                          Math.Exp(-(n * n) / (2 * theta * theta)));
        }

        public void Dispose()
        {
            renderTarget1?.Dispose();
            renderTarget2?.Dispose();
            // Effects are managed by content manager, don't dispose them here
        }
    }
}