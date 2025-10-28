using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using anakinsoft.system;
using System;
using System.Collections.Generic;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Manages all UI rendering for The Lounge scene
    /// </summary>
    public class LoungeUIManager
    {
        // Character profiles (portraits)
        private Dictionary<string, Texture2D> characterPortraits;
        private Texture2D portraitFrame;

        // Intro text state
        private bool showIntroText = true;
        private float introTextTimer = 0f;
        private const float IntroTextDuration = 4.0f;
        private const string IntroText = "Welcome to the Lounge. You are a detective on board the UEFS Marron. The Telirian ambassador is dead. Question the suspects, determine motive, means, and opportunity. Determine who is guilty before the Telirians arrive. Failure to do so will mean all out war.";

        // Intro text teletype effect
        private float introTeletypeTimer = 0f;
        private int introVisibleCharacters = 0;
        private const float IntroCharactersPerSecond = 30f;
        private bool introTeletypeComplete = false;
        private KeyboardState previousKeyboardState;

        // Character load state
        private float characterLoadDelay = 1.0f;
        private float timeSinceLoad = 0f;

        // Current UI state
        private string hoveredCharacter = null;
        private string activeDialogueCharacter = null;

        public bool ShowIntroText => showIntroText;
        public Dictionary<string, Texture2D> CharacterPortraits => characterPortraits;

        public LoungeUIManager()
        {
            characterPortraits = new Dictionary<string, Texture2D>();
        }

        public void Initialize()
        {
            InitializeCharacterPortraits();
        }

        private void InitializeCharacterPortraits()
        {
            // Load character portraits from chars folder
            characterPortraits["NPC_Bartender"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/(NPC) bartender zix");
            characterPortraits["NPC_Ambassador"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/(NPC) Ambassador Tesh");
            characterPortraits["NPC_DrThorne"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/(NPC) Dr thorne - xenopathologist");
            characterPortraits["DrHarmon"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Dr Harmon - CMO");
            characterPortraits["CommanderSylar"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Commander Sylar Von - Body guard");
            characterPortraits["LtWebb"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Lt. Marcus Webb");
            characterPortraits["EnsignTork"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Ensign Tork - Junior Eng");
            characterPortraits["ChiefSolis"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Chief Kala Solis - Sec Cheif");
            characterPortraits["MavenKilroth"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Maven Kilroth - Smuggler");
            characterPortraits["Tehvora"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Tehvora - Diplomatic Atache (Kullan)");
            characterPortraits["LuckyChen"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Lucky Chen - Quartermaster");

            // Create portrait frame (simple colored rectangle)
            var whiteTexture = Globals.screenManager.Content.Load<Texture2D>("textures/white");
            portraitFrame = whiteTexture;

            Console.WriteLine($"Initialized {characterPortraits.Count} character portraits");
        }

        public void UpdateLoadTimer(GameTime gameTime)
        {
            timeSinceLoad += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public void UpdateIntroText(GameTime gameTime)
        {
            if (!showIntroText) return;

            var keyboard = Keyboard.GetState();

            // Update teletype effect
            if (!introTeletypeComplete)
            {
                introTeletypeTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                introVisibleCharacters = (int)(introTeletypeTimer * IntroCharactersPerSecond);

                if (introVisibleCharacters >= IntroText.Length)
                {
                    introVisibleCharacters = IntroText.Length;
                    introTeletypeComplete = true;
                }
            }

            // Handle E key press
            if (keyboard.IsKeyDown(Keys.E) && !previousKeyboardState.IsKeyDown(Keys.E))
            {
                if (!introTeletypeComplete)
                {
                    // Complete teletype immediately
                    introVisibleCharacters = IntroText.Length;
                    introTeletypeComplete = true;
                }
                else
                {
                    // Skip intro text entirely
                    showIntroText = false;
                }
            }

            previousKeyboardState = keyboard;

            // Auto-advance after duration if teletype is complete
            if (introTeletypeComplete)
            {
                introTextTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (introTextTimer >= IntroTextDuration)
                {
                    showIntroText = false;
                }
            }
        }

        public void SetHoveredCharacter(string characterKey)
        {
            hoveredCharacter = characterKey;
        }

        public void SetActiveDialogueCharacter(string characterKey)
        {
            activeDialogueCharacter = characterKey;
        }

        public void ClearActiveDialogueCharacter()
        {
            activeDialogueCharacter = null;
        }

        public void DrawUI(GameTime gameTime, SpriteBatch spriteBatch, SpriteFont font, InteractionSystem interactionSystem, bool isDialogueActive)
        {
            if (font == null) return;

            // Show loading indicator during character load delay
            if (timeSinceLoad <= characterLoadDelay)
            {
                DrawLoadingScreen(spriteBatch, font);
            }
            // Show intro text on black background
            else if (showIntroText)
            {
                DrawIntroText(spriteBatch, font);
            }

            // Draw interaction UI (only when not showing intro and not in dialogue)
            if (!showIntroText && !isDialogueActive)
            {
                interactionSystem?.DrawUI(spriteBatch, font);
            }

            // Draw character portrait if hovering or in dialogue
            string portraitCharacter = isDialogueActive && activeDialogueCharacter != null ? activeDialogueCharacter : hoveredCharacter;
            if (!showIntroText && portraitCharacter != null)
            {
                DrawCharacterPortrait(spriteBatch, portraitCharacter);
            }
        }

        private void DrawLoadingScreen(SpriteBatch spriteBatch, SpriteFont font)
        {
            string loadingText = "Loading The Lounge...";
            var textSize = font.MeasureString(loadingText);
            var screenCenter = new Vector2(
                Globals.screenManager.GraphicsDevice.Viewport.Width / 2f,
                Globals.screenManager.GraphicsDevice.Viewport.Height / 2f
            );

            // Draw loading text with fade effect
            float fadeAlpha = 1.0f - (timeSinceLoad / characterLoadDelay);
            spriteBatch.DrawString(font, loadingText,
                screenCenter - textSize / 2f,
                Color.White * fadeAlpha);
        }

        private void DrawIntroText(SpriteBatch spriteBatch, SpriteFont font)
        {
            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            // Draw black background
            var blackTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            blackTexture.SetData(new[] { Color.Black });
            spriteBatch.Draw(blackTexture, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black);

            // Get visible text based on teletype progress
            string visibleText = IntroText.Substring(0, Math.Min(introVisibleCharacters, IntroText.Length));

            // Wrap and measure the intro text
            string wrappedText = WrapText(visibleText, font, viewport.Width - 100); // 50px padding on each side
            var textSize = font.MeasureString(wrappedText);

            // Center the text on screen
            var textPosition = new Vector2(
                (viewport.Width - textSize.X) / 2f,
                (viewport.Height - textSize.Y) / 2f
            );

            // Draw the text with a slight shadow for readability
            spriteBatch.DrawString(font, wrappedText, textPosition + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(font, wrappedText, textPosition, Color.White);

            // Draw prompt
            string promptText = introTeletypeComplete ? "Press [E] to continue..." : "Press [E] to skip...";
            var promptSize = font.MeasureString(promptText);
            var promptPosition = new Vector2(
                (viewport.Width - promptSize.X) / 2f,
                viewport.Height - 100
            );
            spriteBatch.DrawString(font, promptText, promptPosition, Color.Gray);
        }

        private void DrawCharacterPortrait(SpriteBatch spriteBatch, string characterKey)
        {
            if (!characterPortraits.ContainsKey(characterKey))
                return;

            var viewport = Globals.screenManager.GraphicsDevice.Viewport;
            var portrait = characterPortraits[characterKey];

            // Portrait dimensions (matching 64x96 ratio = 2:3 aspect ratio)
            int portraitWidth = 128;  // 2x scale of 64
            int portraitHeight = 192; // 2x scale of 96
            int frameThickness = 4;
            int margin = 20;

            // Position in top-right corner
            Rectangle portraitRect = new Rectangle(
                viewport.Width - portraitWidth - margin - frameThickness,
                margin,
                portraitWidth,
                portraitHeight
            );

            // Frame rectangle (slightly larger)
            Rectangle frameRect = new Rectangle(
                portraitRect.X - frameThickness,
                portraitRect.Y - frameThickness,
                portraitRect.Width + frameThickness * 2,
                portraitRect.Height + frameThickness * 2
            );

            // Draw frame
            spriteBatch.Draw(portraitFrame, frameRect, Color.Gold);

            // Draw portrait
            spriteBatch.Draw(portrait, portraitRect, Color.White);
        }

        private string WrapText(string text, SpriteFont font, float maxWidth)
        {
            string[] words = text.Split(' ');
            string wrappedText = "";
            string line = "";

            foreach (string word in words)
            {
                string testLine = line + word + " ";
                Vector2 testSize = font.MeasureString(testLine);

                if (testSize.X > maxWidth && line.Length > 0)
                {
                    wrappedText += line.TrimEnd() + "\n";
                    line = word + " ";
                }
                else
                {
                    line = testLine;
                }
            }

            wrappedText += line.TrimEnd();
            return wrappedText;
        }
    }
}
