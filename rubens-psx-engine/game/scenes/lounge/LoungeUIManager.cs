using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using anakinsoft.system;
using anakinsoft.game.scenes.lounge;
using anakinsoft.game.scenes.lounge.characters;
using anakinsoft.game.scenes.lounge.ui;
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
        private CharacterProfileManager profileManager;

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
        private CharacterStateMachine activeCharacterStateMachine = null; // Optional state machine for interrogations (includes stress)
        private string lastDrawnPortrait = null; // For debug logging

        // Time passage message state
        private bool showTimePassageMessage = false;
        private string timePassageText = "";
        private float timePassageTimer = 0f;
        private const float TimePassageDuration = 3.0f; // Show for 3 seconds

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

        /// <summary>
        /// Set the character profile manager for dynamic portrait loading
        /// </summary>
        public void SetProfileManager(CharacterProfileManager manager)
        {
            profileManager = manager;
            if (profileManager != null)
            {
                // Merge profile manager portraits with existing portraits
                var profilePortraits = profileManager.GetAllPortraits();
                foreach (var kvp in profilePortraits)
                {
                    characterPortraits[kvp.Key] = kvp.Value;
                }
                Console.WriteLine($"[LoungeUIManager] Integrated {profilePortraits.Count} portraits from ProfileManager");
            }
        }

        private void InitializeCharacterPortraits()
        {
            // Load character portraits from chars folder
            characterPortraits["NPC_Bartender"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/(NPC) bartender zix");
            characterPortraits["NPC_Ambassador"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/(NPC) Ambassador Tesh");
            characterPortraits["NPC_DrThorne"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/(NPC) Dr thorne - xenobiologist");
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
            // CRITICAL: Never override active dialogue character if dialogue is already running
            if (activeDialogueCharacter != null && activeDialogueCharacter != characterKey)
            {
                Console.WriteLine($"[LoungeUIManager] WARNING: Attempted to set active dialogue character to '{characterKey}' but '{activeDialogueCharacter}' is already active. Ignoring request.");
                return;
            }

            activeDialogueCharacter = characterKey;
            Console.WriteLine($"[LoungeUIManager] Active dialogue character set to: {characterKey}");
        }

        public void ClearActiveDialogueCharacter()
        {
            Console.WriteLine($"[LoungeUIManager] Clearing active dialogue character (was: {activeDialogueCharacter})");
            activeDialogueCharacter = null;
            activeCharacterStateMachine = null; // Also clear state machine when portrait is cleared
        }

        public void SetActiveStressMeter(CharacterStateMachine stateMachine)
        {
            activeCharacterStateMachine = stateMachine;
        }

        public void ClearActiveStressMeter()
        {
            activeCharacterStateMachine = null;
        }

        /// <summary>
        /// Show time passage message at end of round
        /// </summary>
        public void ShowTimePassageMessage(int hoursPassed, int hoursRemaining)
        {
            string hoursPassedText = hoursPassed == 1 ? "1 hour passed" : $"{hoursPassed} hours passed";
            string hoursRemainingText = hoursRemaining == 1 ? "1 hour left" : $"{hoursRemaining} hours left";

            timePassageText = $"{hoursPassedText}, {hoursRemainingText}";
            showTimePassageMessage = true;
            timePassageTimer = 0f;

            Console.WriteLine($"[LoungeUIManager] Showing time passage: {timePassageText}");
        }

        /// <summary>
        /// Update time passage message timer
        /// </summary>
        public void UpdateTimePassageMessage(GameTime gameTime)
        {
            if (!showTimePassageMessage) return;

            timePassageTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (timePassageTimer >= TimePassageDuration)
            {
                showTimePassageMessage = false;
                Console.WriteLine("[LoungeUIManager] Time passage message dismissed");
            }
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
                // Debug logging to track portrait changes
                if (portraitCharacter != lastDrawnPortrait)
                {
                    Console.WriteLine($"[LoungeUIManager] Drawing portrait: {portraitCharacter} (dialogue active: {isDialogueActive}, activeChar: {activeDialogueCharacter}, hovered: {hoveredCharacter})");
                    lastDrawnPortrait = portraitCharacter;
                }
                DrawCharacterPortrait(spriteBatch, portraitCharacter);
            }
            else if (lastDrawnPortrait != null)
            {
                Console.WriteLine($"[LoungeUIManager] Stopped drawing portrait");
                lastDrawnPortrait = null;
            }

            // Draw time passage message
            if (showTimePassageMessage)
            {
                DrawTimePassageMessage(spriteBatch, font);
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

        private void DrawTimePassageMessage(SpriteBatch spriteBatch, SpriteFont font)
        {
            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            // Create semi-transparent dark background
            var blackTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            blackTexture.SetData(new[] { Color.Black });

            // Full screen semi-transparent overlay
            spriteBatch.Draw(blackTexture, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.7f);

            // Measure and center the time passage text
            var textSize = font.MeasureString(timePassageText);
            var textPosition = new Vector2(
                (viewport.Width - textSize.X) / 2f,
                (viewport.Height - textSize.Y) / 2f
            );

            // Draw text with shadow
            spriteBatch.DrawString(font, timePassageText, textPosition + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(font, timePassageText, textPosition, Color.White);

            blackTexture.Dispose();
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
            var font = Globals.fontNTR;

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

            // Check if portrait failed to load
            bool portraitFailed = (portrait == null);

            // Draw frame (red if portrait failed)
            spriteBatch.Draw(portraitFrame, frameRect, portraitFailed ? Color.Red : Color.Gold);

            // Draw portrait or error indicator
            if (portraitFailed)
            {
                // Draw red background for missing portrait
                spriteBatch.Draw(portraitFrame, portraitRect, Color.DarkRed);

                // Draw "MISSING" text
                string errorText = "MISSING\nPORTRAIT";
                Vector2 errorSize = font.MeasureString(errorText);
                float errorScale = 0.5f;
                Vector2 errorPos = new Vector2(
                    portraitRect.X + (portraitWidth - errorSize.X * errorScale) / 2,
                    portraitRect.Y + (portraitHeight - errorSize.Y * errorScale) / 2
                );
                spriteBatch.DrawString(font, errorText, errorPos, Color.White,
                    0f, Vector2.Zero, errorScale, SpriteEffects.None, 0f);
            }
            else
            {
                // Draw portrait normally
                spriteBatch.Draw(portrait, portraitRect, Color.White);
            }

            // Get character info
            var (name, role) = GetCharacterInfo(characterKey);

            // Draw name and role below portrait
            if (!string.IsNullOrEmpty(name))
            {
                int textYOffset = 8;
                float textScale = 0.7f; // Scale down text to 70%
                float maxTextWidth = portraitWidth; // Don't exceed portrait width

                // Wrap name if needed
                string wrappedName = WrapText(name, font, maxTextWidth / textScale);
                Vector2 nameSize = font.MeasureString(wrappedName) * textScale;
                Vector2 namePosition = new Vector2(
                    portraitRect.X + (portraitWidth - nameSize.X) / 2,
                    portraitRect.Bottom + textYOffset
                );

                // Draw name with scale
                spriteBatch.DrawString(font, wrappedName, namePosition, Color.Gold,
                    0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

                // Draw role if available
                float currentY = namePosition.Y + nameSize.Y + 2;
                if (!string.IsNullOrEmpty(role))
                {
                    string wrappedRole = WrapText(role, font, maxTextWidth / textScale);
                    Vector2 roleSize = font.MeasureString(wrappedRole) * textScale;
                    Vector2 rolePosition = new Vector2(
                        portraitRect.X + (portraitWidth - roleSize.X) / 2,
                        currentY
                    );
                    spriteBatch.DrawString(font, wrappedRole, rolePosition, Color.Gray,
                        0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                    currentY = rolePosition.Y + roleSize.Y + 4;
                }

                // Draw stress bar if in interrogation
                if (activeCharacterStateMachine != null)
                {
                    DrawStressBar(spriteBatch, portraitRect.X, currentY, portraitWidth);
                }
            }
        }

        /// <summary>
        /// Draws stress bar below the portrait info during interrogation
        /// </summary>
        private void DrawStressBar(SpriteBatch spriteBatch, float x, float y, float width)
        {
            const float barHeight = 20f;
            const float barPadding = 3f;

            // Draw background
            Rectangle bgRect = new Rectangle((int)x, (int)y, (int)width, (int)barHeight);
            spriteBatch.Draw(portraitFrame, bgRect, Color.Black * 0.9f);

            // Draw border
            spriteBatch.Draw(portraitFrame, new Rectangle((int)x, (int)y, (int)width, 2), Color.White * 0.6f);
            spriteBatch.Draw(portraitFrame, new Rectangle((int)x, (int)(y + barHeight - 2), (int)width, 2), Color.White * 0.6f);
            spriteBatch.Draw(portraitFrame, new Rectangle((int)x, (int)y, 2, (int)barHeight), Color.White * 0.6f);
            spriteBatch.Draw(portraitFrame, new Rectangle((int)(x + width - 2), (int)y, 2, (int)barHeight), Color.White * 0.6f);

            // Draw fill
            float stressPercentage = activeCharacterStateMachine.StressPercentage;
            float fillWidth = (width - barPadding * 2) * (stressPercentage / 100f);

            if (fillWidth > 0)
            {
                Rectangle fillRect = new Rectangle(
                    (int)(x + barPadding),
                    (int)(y + barPadding),
                    (int)fillWidth,
                    (int)(barHeight - barPadding * 2)
                );

                // Determine color based on stress level
                Color fillColor;
                if (stressPercentage < 33f)
                    fillColor = new Color(50, 200, 50); // Green
                else if (stressPercentage < 66f)
                    fillColor = new Color(200, 200, 50); // Yellow
                else
                    fillColor = new Color(200, 50, 50); // Red

                spriteBatch.Draw(portraitFrame, fillRect, fillColor);
            }

            // Draw stress percentage text
            var font = Globals.fontNTR;
            string stressText = $"{stressPercentage:F0}%";
            Vector2 textSize = font.MeasureString(stressText);
            float textScale = 0.5f;
            Vector2 scaledTextSize = textSize * textScale;
            Vector2 textPos = new Vector2(
                x + (width - scaledTextSize.X) / 2,
                y + (barHeight - scaledTextSize.Y) / 2
            );
            spriteBatch.DrawString(font, stressText, textPos + Vector2.One, Color.Black, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, stressText, textPos, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
        }

        private (string name, string role) GetCharacterInfo(string characterKey)
        {
            return characterKey switch
            {
                "NPC_Bartender" => ("Zix", "Bartender"),
                "DrHarmon" => ("Dr. Harmon Kerrigan", "Chief Medical Officer"),
                "NPC_Ambassador" => ("Ambassador Telir", "Telirian Diplomat"),
                "NPC_DrThorne" => ("Dr. Lyssa Thorne", "Xenobiologist"),
                "CommanderSylar" => ("Commander Sylara Von", "Security Chief"),
                "LtWebb" => ("Lt. Marcus Webb", "Navigation Officer"),
                "EnsignTork" => ("Ensign Tork", "Junior Engineer"),
                "ChiefSolis" => ("Chief Kala Solis", "Security Chief"),
                "MavenKilroth" => ("Maven Kilroth", "Smuggler"),
                "Tehvora" => ("Tehvora", "Diplomatic Attache"),
                "LuckyChen" => ("Lucky Chen", "Quartermaster"),
                _ => (characterKey, "Unknown")
            };
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
