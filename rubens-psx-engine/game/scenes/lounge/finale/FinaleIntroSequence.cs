using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine;
using rubens_psx_engine.system.config;
using System;

namespace anakinsoft.game.scenes.lounge.finale
{
    /// <summary>
    /// Manages the dramatic intro sequence when the finale begins:
    /// - Fade from black
    /// - Text: "0 hours remain, the moment of judgement has arrived."
    /// - Odysseus ship zooms in and parks outside window
    /// - Starfield slows to a stop and disappears
    /// </summary>
    public class FinaleIntroSequence
    {
        private enum IntroState
        {
            FadeIn,
            ShowText,
            ShipApproach,
            Complete
        }

        private IntroState currentState;
        private float stateTimer;
        private bool isActive;
        private bool isComplete;
        private SpriteFont font;

        // Audio manager for warp speed sound
        private rubens_psx_engine.system.GameAudioManager audioManager;

        // Ship animation settings (from config)
        private Vector3 shipStartPosition;
        private Vector3 shipEndPosition;
        private Vector3 currentShipPosition;
        private float shipApproachDuration;

        // Timing constants
        private const float FadeInDuration = 1.0f;
        private const float TextDisplayDuration = 4.0f;

        // Fade
        private float fadeAlpha = 1.0f; // Start fully black

        // Text
        private const string IntroText = "0 HOURS REMAIN.\nTHE MOMENT OF JUDGEMENT HAS ARRIVED.";
        private const string InstructionText = "Talk to Zix and provide a solution to the murder.";

        public bool IsActive => isActive;
        public bool IsComplete => isComplete;
        public Vector3 ShipPosition => currentShipPosition;
        public float StarfieldSpeedMultiplier { get; private set; } = 1.0f;
        public float StarfieldLengthMultiplier { get; private set; } = 1.0f;
        public bool IsShipVisible => currentState == IntroState.ShipApproach || currentState == IntroState.Complete;

        public event Action OnSequenceComplete;

        public FinaleIntroSequence()
        {
            // Load ship config
            var shipConfig = OdysseusShipConfigManager.Config;
            shipStartPosition = shipConfig.GetStartPosition();
            shipEndPosition = shipConfig.GetEndPosition();
            shipApproachDuration = shipConfig.ApproachDuration;
            currentShipPosition = shipStartPosition;
            Console.WriteLine($"[FinaleIntroSequence] Ship settings - Duration: {shipApproachDuration}s, Start: {shipStartPosition}, End: {shipEndPosition}");
        }

        public void Initialize()
        {
            font = Globals.screenManager.Content.Load<SpriteFont>("fonts/Arial");
        }

        public void SetAudioManager(rubens_psx_engine.system.GameAudioManager manager)
        {
            audioManager = manager;
        }

        public void Start()
        {
            isActive = true;
            isComplete = false;
            currentState = IntroState.FadeIn;
            stateTimer = 0f;
            fadeAlpha = 1.0f;
            currentShipPosition = shipStartPosition;

            StarfieldSpeedMultiplier = 1.0f;
            StarfieldLengthMultiplier = 1.0f;

            // Play finale intro music when sequence starts ("judgement is here" moment)
            audioManager?.PlayFinaleIntroMusic();

            // Play warp speed sound when Odysseus ship spawns
            audioManager?.PlayWarpSpeed();

            Console.WriteLine("[FinaleIntroSequence] Sequence started with finale music and warp speed sound");
        }

        public void Update(GameTime gameTime)
        {
            if (!isActive || isComplete) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            stateTimer += deltaTime;

            switch (currentState)
            {
                case IntroState.FadeIn:
                    UpdateFadeIn(deltaTime);
                    break;

                case IntroState.ShowText:
                    UpdateShowText(deltaTime);
                    UpdateShipApproach(deltaTime);
                    break;

                case IntroState.ShipApproach:
                    UpdateShipApproach(deltaTime);
                    break;

                case IntroState.Complete:
                    CompleteSequence();
                    break;
            }
        }

        private void UpdateFadeIn(float deltaTime)
        {
            // Fade from black over 5 seconds
            fadeAlpha = 1.0f - (stateTimer / FadeInDuration);

            if (stateTimer >= FadeInDuration)
            {
                fadeAlpha = 0f;
                currentState = IntroState.ShowText;
                stateTimer = 0f;
                Console.WriteLine("[FinaleIntroSequence] Fade complete, showing text");
            }
        }

        private void UpdateShowText(float deltaTime)
        {
            // Display text for a few seconds
            if (stateTimer >= TextDisplayDuration)
            {
                currentState = IntroState.ShipApproach;
                stateTimer = 0f;
                Console.WriteLine("[FinaleIntroSequence] Starting ship approach");
            }
        }

        private void UpdateShipApproach(float deltaTime)
        {
            float progress = stateTimer / shipApproachDuration;
            progress = MathHelper.Clamp(progress, 0f, 1f);

            // Ease-out cubic for smooth deceleration
            float easedProgress = 1f - (float)Math.Pow(1f - progress, 3);

            // Animate ship position
            currentShipPosition = Vector3.Lerp(shipStartPosition, shipEndPosition, easedProgress);

            // Slow down starfield and shorten streaks as ship approaches
            // Speed: 2000 -> 0, Length: 500 -> 0 over 10 seconds
            StarfieldSpeedMultiplier = 1.0f - progress; // Goes from 1.0 to 0.0
            StarfieldLengthMultiplier = 1.0f - progress; // Goes from 1.0 to 0.0

            if (stateTimer >= shipApproachDuration)
            {
                currentShipPosition = shipEndPosition;
                StarfieldSpeedMultiplier = 0f;
                StarfieldLengthMultiplier = 0f;
                currentState = IntroState.Complete;
                Console.WriteLine("[FinaleIntroSequence] Ship arrived, starfield stopped");
            }
        }

        private void CompleteSequence()
        {
            isComplete = true;
            isActive = false;
            Console.WriteLine("[FinaleIntroSequence] Sequence complete");
            OnSequenceComplete?.Invoke();
        }

        public void DrawUI(SpriteBatch spriteBatch)
        {
            if (!isActive) return;

            int screenWidth = Globals.screenManager.GraphicsDevice.Viewport.Width;
            int screenHeight = Globals.screenManager.GraphicsDevice.Viewport.Height;

            // Draw fade overlay
            if (fadeAlpha > 0f)
            {
                var texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                texture.SetData(new[] { Color.White });

                var fullScreenRect = new Rectangle(0, 0, screenWidth, screenHeight);
                spriteBatch.Draw(texture, fullScreenRect, Color.Black * fadeAlpha);
                texture.Dispose();
            }

            // Draw intro text during ShowText state
            if (currentState == IntroState.ShowText ||
                (currentState == IntroState.ShipApproach && stateTimer < 2.0f))
            {
                // Calculate text opacity (fade out during ship approach)
                float textAlpha = 1.0f;
                if (currentState == IntroState.ShipApproach)
                {
                    textAlpha = 1.0f - (stateTimer / 2.0f); // Fade out over 2 seconds
                }

                // Draw main intro text centered
                Vector2 textSize = font.MeasureString(IntroText);
                Vector2 textPosition = new Vector2(
                    (screenWidth - textSize.X) / 2,
                    (screenHeight - textSize.Y) / 2 - 40
                );

                // Draw text with shadow
                spriteBatch.DrawString(font, IntroText, textPosition + Vector2.One * 2, Color.Black * textAlpha);
                spriteBatch.DrawString(font, IntroText, textPosition, Color.White * textAlpha);

                // Draw instruction text below
                Vector2 instructionSize = font.MeasureString(InstructionText);
                Vector2 instructionPosition = new Vector2(
                    (screenWidth - instructionSize.X) / 2,
                    textPosition.Y + textSize.Y + 40
                );

                spriteBatch.DrawString(font, InstructionText, instructionPosition + Vector2.One * 2, Color.Black * textAlpha);
                spriteBatch.DrawString(font, InstructionText, instructionPosition, Color.Yellow * textAlpha);
            }
        }

        public void Reset()
        {
            isActive = false;
            isComplete = false;
            currentState = IntroState.FadeIn;
            stateTimer = 0f;
            fadeAlpha = 1.0f;
            currentShipPosition = shipStartPosition;
            StarfieldSpeedMultiplier = 1.0f;
        }
    }
}
