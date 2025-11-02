using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
        private Effect tintEffect;

        private RenderTarget2D tintTarget;
        private RenderTarget2D renderTarget1;
        private RenderTarget2D renderTarget2;
        private RenderTarget2D bloomCombineTarget;

        private GraphicsDevice cachedGraphicsDevice;
        private int lastRenderWidth = -1;
        private int lastRenderHeight = -1;

        private BloomSettings settings = BloomSettings.PresetSettings[0];
        private IntermediateBuffer showBuffer = IntermediateBuffer.FinalResult;

        public string Name => "Bloom";
        public bool Enabled { get; set; } = true;
        public int Priority => 100; // Higher priority = later in chain

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

        // Tint properties
        public bool EnableTint { get; set; } = false;
        public Color TintColor { get; set; } = Color.White;
        public float TintIntensity { get; set; } = 1.0f;

        // Dither properties
        public bool EnableDither { get; set; } = true;
        public float DitherStrength { get; set; } = 1.0f;
        public float ColorLevels { get; set; } = 6.0f;
        public Vector2 ScreenResolution { get; set; } = Vector2.One;

        public enum IntermediateBuffer
        {
            PreTint = 0,
            PreBloom = 1,
            BlurredHorizontally = 2,
            BlurredBothWays = 3,
            FinalResult = 4,
        }

        public void Initialize(GraphicsDevice graphicsDevice, Game game)
        {
            if (graphicsDevice == null) throw new ArgumentNullException(nameof(graphicsDevice));
            if (game == null) throw new ArgumentNullException(nameof(game));

            cachedGraphicsDevice = graphicsDevice;

            // Load effects
            tintEffect = game.Content.Load<Effect>("shaders/postprocess/Tint");
            bloomExtractEffect = game.Content.Load<Effect>("shaders/postprocess/BloomExtract");
            bloomCombineEffect = game.Content.Load<Effect>("shaders/postprocess/BloomCombine");
            gaussianBlurEffect = game.Content.Load<Effect>("shaders/postprocess/GaussianBlur");
            ditherEffect = game.Content.Load<Effect>("shaders/postprocess/Dither");

            // Don't create render targets here - they'll be created lazily in Apply()
            // based on the actual input texture size
        }

        private void EnsureRenderTargets(int width, int height)
        {
            if (renderTarget1 != null && lastRenderWidth == width && lastRenderHeight == height)
                return; // Already correct size

            // Dispose old targets if they exist
            tintTarget?.Dispose();
            renderTarget1?.Dispose();
            renderTarget2?.Dispose();
            bloomCombineTarget?.Dispose();

            lastRenderWidth = width;
            lastRenderHeight = height;

            var format = cachedGraphicsDevice.PresentationParameters.BackBufferFormat;

            // Tint target at full resolution
            tintTarget = new RenderTarget2D(cachedGraphicsDevice, width, height, false, format, DepthFormat.None);

            // Blur targets at half resolution for performance
            int halfWidth = width / 2;
            int halfHeight = height / 2;

            renderTarget1 = new RenderTarget2D(cachedGraphicsDevice, halfWidth, halfHeight, false, format, DepthFormat.None);
            renderTarget2 = new RenderTarget2D(cachedGraphicsDevice, halfWidth, halfHeight, false, format, DepthFormat.None);

            // Bloom combine target at full render resolution
            bloomCombineTarget = new RenderTarget2D(cachedGraphicsDevice, width, height, false, format, DepthFormat.None);
        }

        public void Apply(Texture2D inputTexture, RenderTarget2D outputTarget, SpriteBatch spriteBatch)
        {
            if (inputTexture == null) throw new ArgumentNullException(nameof(inputTexture));
            if (spriteBatch == null) throw new ArgumentNullException(nameof(spriteBatch));

            var graphicsDevice = spriteBatch.GraphicsDevice;

            // Ensure render targets match input texture size
            EnsureRenderTargets(inputTexture.Width, inputTexture.Height);

            graphicsDevice.SamplerStates[1] = SamplerState.LinearClamp;

            // Determine the source texture for bloom extraction
            Texture2D bloomSource = inputTexture;

            // Pass 0: Apply tint if enabled
            if (EnableTint)
            {
                graphicsDevice.SetRenderTarget(tintTarget);

                tintEffect.Parameters["TintColor"]?.SetValue(TintColor.ToVector4());
                tintEffect.Parameters["TintIntensity"]?.SetValue(TintIntensity);

                spriteBatch.Begin(0, BlendState.Opaque, null, null, null, tintEffect);
                spriteBatch.Draw(inputTexture, new Rectangle(0, 0, tintTarget.Width, tintTarget.Height), Color.White);
                spriteBatch.End();

                bloomSource = tintTarget;
            }

            // Pass 1: Extract bright parts of the scene
            bloomExtractEffect.Parameters["BloomThreshold"].SetValue(Settings.BloomThreshold);
            DrawFullscreenQuad(bloomSource, renderTarget1, bloomExtractEffect,
                              IntermediateBuffer.PreBloom, spriteBatch);

            // Pass 2: Horizontal blur
            SetBlurEffectParameters(1.0f / renderTarget1.Width, 0);
            DrawFullscreenQuad(renderTarget1, renderTarget2, gaussianBlurEffect,
                              IntermediateBuffer.BlurredHorizontally, spriteBatch);

            // Pass 3: Vertical blur
            SetBlurEffectParameters(0, 1.0f / renderTarget1.Height);
            DrawFullscreenQuad(renderTarget2, renderTarget1, gaussianBlurEffect,
                              IntermediateBuffer.BlurredBothWays, spriteBatch);

            // Pass 4: Combine blurred bloom with original scene into temp target
            graphicsDevice.SetRenderTarget(bloomCombineTarget);

            var parameters = bloomCombineEffect.Parameters;
            parameters["BloomIntensity"].SetValue(Settings.BloomIntensity);
            parameters["BaseIntensity"].SetValue(Settings.BaseIntensity);
            parameters["BloomSaturation"].SetValue(Settings.BloomSaturation);
            parameters["BaseSaturation"].SetValue(Settings.BaseSaturation);
            parameters["LuminanceWeights"].SetValue(Settings.LuminanceWeights);
            parameters["BaseTexture"].SetValue(bloomSource); // Use tinted or original scene for BaseSampler

            DrawFullscreenQuad(renderTarget1, bloomCombineTarget.Width, bloomCombineTarget.Height,
                              bloomCombineEffect, IntermediateBuffer.FinalResult, spriteBatch);

            // Clear the BaseTexture parameter to avoid holding a reference
            bloomCombineEffect.Parameters["BaseTexture"].SetValue((Texture2D)null);

            // Pass 5: Apply dither to the combined bloom result
            if (EnableDither)
            {
                graphicsDevice.SetRenderTarget(outputTarget);

                ditherEffect.Parameters["DitherStrength"]?.SetValue(DitherStrength);
                ditherEffect.Parameters["ScreenSize"]?.SetValue(ScreenResolution);
                ditherEffect.Parameters["ColorLevels"]?.SetValue(ColorLevels);

                var bounds = outputTarget?.Bounds ?? graphicsDevice.Viewport.Bounds;

                spriteBatch.Begin(0, BlendState.Opaque, SamplerState.PointClamp,
                                DepthStencilState.None, RasterizerState.CullCounterClockwise, ditherEffect);
                spriteBatch.Draw(bloomCombineTarget, bounds, Color.White);
                spriteBatch.End();
            }
            else
            {
                // No dither, just copy bloom result to output
                graphicsDevice.SetRenderTarget(outputTarget);
                var bounds = outputTarget?.Bounds ?? graphicsDevice.Viewport.Bounds;

                spriteBatch.Begin(0, BlendState.Opaque, SamplerState.LinearClamp,
                                DepthStencilState.None, RasterizerState.CullCounterClockwise);
                spriteBatch.Draw(bloomCombineTarget, bounds, Color.White);
                spriteBatch.End();
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
            tintTarget?.Dispose();
            renderTarget1?.Dispose();
            renderTarget2?.Dispose();
            bloomCombineTarget?.Dispose();
            // Effects are managed by content manager, don't dispose them here
        }
    }
}