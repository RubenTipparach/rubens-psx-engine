using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;
using System.Collections.Generic;
using anakinsoft.game.scenes.lounge.characters;

namespace anakinsoft.game.scenes.lounge.ui
{
    /// <summary>
    /// Represents a single dialogue line with speaker info
    /// </summary>
    public class DialogueLine
    {
        public string Speaker { get; set; }
        public string Text { get; set; }
        public Action OnComplete { get; set; }

        public DialogueLine(string speaker, string text, Action onComplete = null)
        {
            Speaker = speaker;
            Text = text;
            OnComplete = onComplete;
        }
    }

    /// <summary>
    /// Represents a complete dialogue sequence
    /// </summary>
    public class DialogueSequence
    {
        public string SequenceName { get; set; }
        public List<DialogueLine> Lines { get; set; }
        public Action OnSequenceComplete { get; set; }

        public DialogueSequence(string name)
        {
            SequenceName = name;
            Lines = new List<DialogueLine>();
        }

        public void AddLine(string speaker, string text, Action onComplete = null)
        {
            Lines.Add(new DialogueLine(speaker, text, onComplete));
        }
    }

    /// <summary>
    /// Manages dialogue display and progression
    /// </summary>
    public class DialogueSystem
    {
        private DialogueSequence currentSequence;
        private int currentLineIndex = -1;
        private bool isActive = false;
        private KeyboardState previousKeyboard;

        // Teletype effect
        private float teletypeTimer = 0f;
        private int visibleCharacters = 0;
        private int previousVisibleCharacters = 0; // Track previous frame's character count for blip triggering
        private const float CharactersPerSecond = 30f; // Speed of teletype effect
        private bool teletypeComplete = false;
        private bool acceptInput = false; // Prevent input on first frame after dialogue starts

        // State machine integration (optional - only shown during interrogation, includes stress)
        private CharacterStateMachine activeCharacterStateMachine = null;

        // Audio manager for text blip sound
        private rubens_psx_engine.system.GameAudioManager audioManager = null;

        // Display settings
        private const float BoxPadding = 20f;
        private const float SpeakerOffset = 10f; // Reduced from 40f to minimize space between name and dialogue
        private const float FixedBoxWidth = 800f; // Fixed width for consistency
        private readonly Color BoxColor = Color.Black * 0.85f;
        private readonly Color SpeakerColor = Color.Yellow;
        private readonly Color TextColor = Color.White;
        private readonly Color PromptColor = Color.Gray;

        // Events
        public event Action OnDialogueStart;
        public event Action OnDialogueEnd;
        public event Action<DialogueLine> OnLineChanged;

        public bool IsActive => isActive;
        public DialogueLine CurrentLine =>
            currentSequence != null && currentLineIndex >= 0 && currentLineIndex < currentSequence.Lines.Count
                ? currentSequence.Lines[currentLineIndex]
                : null;

        public DialogueSystem()
        {
        }

        /// <summary>
        /// Sets the active character state machine for this dialogue
        /// Used for stress display during interrogations AND transcript recording for all dialogues
        /// </summary>
        public void SetActiveCharacter(CharacterStateMachine stateMachine)
        {
            activeCharacterStateMachine = stateMachine;
        }

        /// <summary>
        /// Clears the active character state machine
        /// </summary>
        public void ClearActiveCharacter()
        {
            activeCharacterStateMachine = null;
        }

        /// <summary>
        /// Sets the audio manager for playing text blip sounds
        /// </summary>
        public void SetAudioManager(rubens_psx_engine.system.GameAudioManager manager)
        {
            audioManager = manager;
        }

        /// <summary>
        /// Starts a dialogue sequence
        /// </summary>
        public void StartDialogue(DialogueSequence sequence)
        {
            if (sequence == null || sequence.Lines.Count == 0)
            {
                Console.WriteLine("DialogueSystem: Cannot start empty dialogue sequence");
                return;
            }

            currentSequence = sequence;
            currentLineIndex = 0;
            isActive = true;
            acceptInput = false; // Don't accept input on first frame

            // Begin transcript recording for this sequence
            if (activeCharacterStateMachine != null)
            {
                activeCharacterStateMachine.BeginSequence(sequence.SequenceName);
            }

            // Reset teletype effect for first line
            ResetTeletype();

            OnDialogueStart?.Invoke();
            OnLineChanged?.Invoke(CurrentLine);

            // Record the first line in transcript
            if (activeCharacterStateMachine != null && CurrentLine != null)
            {
                activeCharacterStateMachine.RecordDialogueLine(CurrentLine.Speaker, CurrentLine.Text);
            }

            Console.WriteLine($"DialogueSystem: Started dialogue '{sequence.SequenceName}' with {sequence.Lines.Count} lines");
        }

        /// <summary>
        /// Stops the current dialogue
        /// </summary>
        public void StopDialogue()
        {
            if (!isActive)
                return;

            isActive = false;

            // Stop text blip sound when dialogue ends
            audioManager?.StopTextBlip();

            var sequence = currentSequence;
            currentSequence = null;
            currentLineIndex = -1;

            OnDialogueEnd?.Invoke();
            sequence?.OnSequenceComplete?.Invoke();

            Console.WriteLine("DialogueSystem: Dialogue ended");
        }

        /// <summary>
        /// Advances to the next dialogue line
        /// </summary>
        public void NextLine()
        {
            if (!isActive || currentSequence == null)
                return;

            // Call completion callback for current line
            CurrentLine?.OnComplete?.Invoke();

            currentLineIndex++;

            if (currentLineIndex >= currentSequence.Lines.Count)
            {
                // Dialogue complete
                StopDialogue();
            }
            else
            {
                // Reset teletype effect for new line
                ResetTeletype();
                acceptInput = false; // Don't accept input on first frame of new line
                OnLineChanged?.Invoke(CurrentLine);

                // Record the new line in transcript
                if (activeCharacterStateMachine != null && CurrentLine != null)
                {
                    activeCharacterStateMachine.RecordDialogueLine(CurrentLine.Speaker, CurrentLine.Text);
                }

                Console.WriteLine($"DialogueSystem: Line {currentLineIndex + 1}/{currentSequence.Lines.Count}");
            }
        }

        /// <summary>
        /// Updates the dialogue system (handles input and teletype effect)
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!isActive)
                return;

            var keyboard = Keyboard.GetState();

            // Enable input after first frame
            if (!acceptInput)
            {
                acceptInput = true;
                previousKeyboard = keyboard; // Consume any keys pressed on start frame
                return;
            }

            // Update teletype effect
            if (!teletypeComplete && CurrentLine != null)
            {
                teletypeTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                visibleCharacters = (int)(teletypeTimer * CharactersPerSecond);

                // Play text blip when a new character appears
                if (visibleCharacters > previousVisibleCharacters)
                {
                    audioManager?.PlayTextBlip();
                    previousVisibleCharacters = visibleCharacters;
                }

                if (visibleCharacters >= CurrentLine.Text.Length)
                {
                    visibleCharacters = CurrentLine.Text.Length;
                    teletypeComplete = true;
                    // Stop text blip when teletype completes
                    audioManager?.StopTextBlip();
                }
            }

            // Handle E key press
            if (keyboard.IsKeyDown(Keys.E) && !previousKeyboard.IsKeyDown(Keys.E))
            {
                if (!teletypeComplete)
                {
                    // Complete teletype immediately
                    visibleCharacters = CurrentLine?.Text.Length ?? 0;
                    previousVisibleCharacters = visibleCharacters;
                    teletypeComplete = true;
                    // Stop text blip when user skips
                    audioManager?.StopTextBlip();
                }
                else
                {
                    // Advance to next line only when teletype is complete
                    NextLine();
                }
            }

            // Also allow Space to advance (for compatibility) - only when teletype is complete
            if (keyboard.IsKeyDown(Keys.Space) && !previousKeyboard.IsKeyDown(Keys.Space) && teletypeComplete)
            {
                NextLine();
            }

            // NOTE: ESC removed - it causes serious bugs by bypassing normal dialogue completion
            // Player must advance through all dialogue lines normally

            previousKeyboard = keyboard;
        }

        /// <summary>
        /// Resets the teletype effect for a new line
        /// </summary>
        private void ResetTeletype()
        {
            teletypeTimer = 0f;
            visibleCharacters = 0;
            previousVisibleCharacters = 0;
            teletypeComplete = false;
        }

        /// <summary>
        /// Draws the dialogue UI
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (!isActive || CurrentLine == null || font == null)
                return;

            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            // Use fixed box width for consistency
            float boxWidth = Math.Min(FixedBoxWidth, viewport.Width - 100); // Ensure it fits on screen

            // Measure text
            var speakerText = CurrentLine.Speaker;
            var fullText = CurrentLine.Text;
            var visibleText = fullText.Substring(0, Math.Min(visibleCharacters, fullText.Length));
            var dialogueText = WrapText(visibleText, font, boxWidth - BoxPadding * 2);
            var promptText = teletypeComplete ? "Press [E] to continue..." : "Press [E] to skip...";

            var speakerSize = font.MeasureString(speakerText);
            var dialogueSize = font.MeasureString(dialogueText);
            var promptSize = font.MeasureString(promptText);

            // Calculate box height (width is fixed)
            float boxHeight = speakerSize.Y + dialogueSize.Y + promptSize.Y + BoxPadding * 2 + SpeakerOffset + 10;

            // Position at bottom of screen
            float boxX = (viewport.Width - boxWidth) / 2;
            float boxY = viewport.Height - boxHeight - 50;

            // Draw background box
            DrawFilledRectangle(spriteBatch, new Rectangle((int)boxX, (int)boxY, (int)boxWidth, (int)boxHeight), BoxColor);

            // Draw border
            DrawRectangleBorder(spriteBatch, new Rectangle((int)boxX, (int)boxY, (int)boxWidth, (int)boxHeight), Color.White, 2);

            // Draw speaker name
            Vector2 speakerPos = new Vector2(boxX + BoxPadding, boxY + BoxPadding);
            spriteBatch.DrawString(font, speakerText, speakerPos + Vector2.One, Color.Black); // Shadow
            spriteBatch.DrawString(font, speakerText, speakerPos, SpeakerColor);

            // Note: Stress meter is drawn by LoungeUIManager, not in the dialogue box

            // Draw dialogue text
            Vector2 dialoguePos = new Vector2(boxX + BoxPadding, boxY + BoxPadding + speakerSize.Y + SpeakerOffset);
            spriteBatch.DrawString(font, dialogueText, dialoguePos + Vector2.One, Color.Black); // Shadow
            spriteBatch.DrawString(font, dialogueText, dialoguePos, TextColor);

            // Draw prompt
            Vector2 promptPos = new Vector2(boxX + BoxPadding, boxY + boxHeight - promptSize.Y - BoxPadding);
            spriteBatch.DrawString(font, promptText, promptPos, PromptColor);
        }

        /// <summary>
        /// Wraps text to fit within a specified width
        /// </summary>
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
                    wrappedText += line + "\n";
                    line = word + " ";
                }
                else
                {
                    line = testLine;
                }
            }

            wrappedText += line;
            return wrappedText;
        }

        private void DrawFilledRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            var texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            texture.SetData(new[] { Color.White });
            spriteBatch.Draw(texture, rect, color);
        }

        private void DrawRectangleBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            var texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            texture.SetData(new[] { Color.White });

            // Top
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            spriteBatch.Draw(texture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}
