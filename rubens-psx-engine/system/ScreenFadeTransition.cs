using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace anakinsoft.system
{
    /// <summary>
    /// Handles screen fade in/out transitions
    /// </summary>
    public class ScreenFadeTransition
    {
        private float fadeAlpha = 1f;
        private float fadeDuration = 1.0f; // seconds
        private float fadeTimer = 0f;
        private bool isFading = false;
        private FadeDirection fadeDirection;

        private Texture2D fadeTexture;
        private GraphicsDevice graphicsDevice;

        public bool IsFading => isFading;
        public bool IsBlack => fadeAlpha >= 1.0f;

        public event Action OnFadeOutComplete;
        public event Action OnFadeInComplete;

        private enum FadeDirection
        {
            In,  // From black to clear
            Out  // From clear to black
        }

        public ScreenFadeTransition(GraphicsDevice device)
        {
            graphicsDevice = device;
            fadeTexture = new Texture2D(device, 1, 1);
            fadeTexture.SetData(new[] { Color.Black });
        }

        /// <summary>
        /// Start a fade to black transition
        /// </summary>
        public void FadeOut(float duration = 1.0f)
        {
            fadeDuration = duration;
            fadeDirection = FadeDirection.Out;
            fadeTimer = 0f;
            isFading = true;
            Console.WriteLine($"[ScreenFade] Starting fade out ({duration}s)");
        }

        /// <summary>
        /// Start a fade from black to clear transition
        /// </summary>
        public void FadeIn(float duration = 1.0f)
        {
            fadeDuration = duration;
            fadeDirection = FadeDirection.In;
            fadeTimer = 0f;
            isFading = true;
            fadeAlpha = 1.0f; // Start black
            Console.WriteLine($"[ScreenFade] Starting fade in ({duration}s)");
        }

        /// <summary>
        /// Update the fade transition
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!isFading)
                return;

            fadeTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            float progress = Math.Min(fadeTimer / fadeDuration, 1.0f);

            if (fadeDirection == FadeDirection.Out)
            {
                fadeAlpha = progress;

                if (progress >= 1.0f)
                {
                    isFading = false;
                    Console.WriteLine("[ScreenFade] Fade out complete");
                    OnFadeOutComplete?.Invoke();
                }
            }
            else // FadeDirection.In
            {
                fadeAlpha = 1.0f - progress;

                if (progress >= 1.0f)
                {
                    isFading = false;
                    fadeAlpha = 0f;
                    Console.WriteLine("[ScreenFade] Fade in complete");
                    OnFadeInComplete?.Invoke();
                }
            }
        }

        /// <summary>
        /// Draw the fade overlay
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Rectangle screenBounds)
        {
            // Console.WriteLine($"[ScreenFade] Draw called - fadeAlpha: {fadeAlpha}, isFading: {isFading}, IsBlack: {IsBlack}");

            if (fadeAlpha > 0f)
            {
                spriteBatch.Draw(fadeTexture, screenBounds, Color.Black * fadeAlpha);
            }
        }

        /// <summary>
        /// Instantly set to black screen
        /// </summary>
        public void SetBlack()
        {
            fadeAlpha = 1.0f;
            isFading = false;
        }

        /// <summary>
        /// Instantly set to clear screen
        /// </summary>
        public void SetClear()
        {
            fadeAlpha = 0f;
            isFading = false;
        }
    }
}
