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
        private const float CharactersPerSecond = 30f; // Speed of teletype effect
        private bool teletypeComplete = false;
        private bool acceptInput = false; // Prevent input on first frame after dialogue starts

        // State machine integration (optional - only shown during interrogation, includes stress)
        private CharacterStateMachine activeCharacterStateMachine = null;

        // Display settings
        private const float BoxPadding = 20f;
        private const float LineHeight = 30f;
        private const float SpeakerOffset = 40f;
        private const float FixedBoxWidth = 800f; // Fixed width for consistency
        private const float StressBarWidth = 200f;
        private const float StressBarHeight = 20f;
        private readonly Color BoxColor = Color.Black * 0.85f;
        private readonly Color SpeakerColor = Color.Yellow;
        private readonly Color TextColor = Color.White;
        private readonly Color PromptColor = Color.Gray;
        private readonly Color LowStressColor = new Color(50, 200, 50); // Green
        private readonly Color MediumStressColor = new Color(200, 200, 50); // Yellow
        private readonly Color HighStressColor = new Color(200, 50, 50); // Red

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
        /// Sets the state machine to display during dialogue (for interrogations with stress)
        /// </summary>
        public void SetStressMeter(CharacterStateMachine stateMachine)
        {
            activeCharacterStateMachine = stateMachine;
        }

        /// <summary>
        /// Clears the state machine display
        /// </summary>
        public void ClearStressMeter()
        {
            activeCharacterStateMachine = null;
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

            // Reset teletype effect for first line
            ResetTeletype();

            OnDialogueStart?.Invoke();
            OnLineChanged?.Invoke(CurrentLine);

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

                if (visibleCharacters >= CurrentLine.Text.Length)
                {
                    visibleCharacters = CurrentLine.Text.Length;
                    teletypeComplete = true;
                }
            }

            // Handle E key press
            if (keyboard.IsKeyDown(Keys.E) && !previousKeyboard.IsKeyDown(Keys.E))
            {
                if (!teletypeComplete)
                {
                    // Complete teletype immediately
                    visibleCharacters = CurrentLine?.Text.Length ?? 0;
                    teletypeComplete = true;
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

            // Draw stress bar next to speaker name (if in interrogation mode)
            if (activeCharacterStateMachine != null)
            {
                float stressBarX = boxX + boxWidth - StressBarWidth - BoxPadding;
                float stressBarY = boxY + BoxPadding;
                DrawStressBar(spriteBatch, stressBarX, stressBarY, StressBarWidth, StressBarHeight);
            }

            // Draw dialogue text
            Vector2 dialoguePos = new Vector2(boxX + BoxPadding, boxY + BoxPadding + speakerSize.Y + SpeakerOffset);
            spriteBatch.DrawString(font, dialogueText, dialoguePos + Vector2.One, Color.Black); // Shadow
            spriteBatch.DrawString(font, dialogueText, dialoguePos, TextColor);

            // Draw prompt
            Vector2 promptPos = new Vector2(boxX + BoxPadding, boxY + boxHeight - promptSize.Y - BoxPadding);
            spriteBatch.DrawString(font, promptText, promptPos, PromptColor);
        }

        /// <summary>
        /// Draw the stress progress bar (for interrogations)
        /// </summary>
        private void DrawStressBar(SpriteBatch spriteBatch, float x, float y, float width, float height)
        {
            if (activeCharacterStateMachine == null)
                return;

            // Draw outer bar background (black)
            Rectangle outerRect = new Rectangle((int)x, (int)y, (int)width, (int)height);
            DrawFilledRectangle(spriteBatch, outerRect, Color.Black * 0.9f);
            DrawRectangleBorder(spriteBatch, outerRect, Color.White * 0.6f, 2);

            // Calculate inner fill
            float stressPercentage = activeCharacterStateMachine.StressPercentage;
            float fillWidth = (width - 6) * (stressPercentage / 100f); // 3px padding on each side

            if (fillWidth > 0)
            {
                Rectangle innerRect = new Rectangle(
                    (int)(x + 3),
                    (int)(y + 3),
                    (int)fillWidth,
                    (int)(height - 6)
                );

                Color fillColor = GetStressColor(stressPercentage);
                DrawFilledRectangle(spriteBatch, innerRect, fillColor);
            }
        }

        /// <summary>
        /// Get color based on stress level
        /// </summary>
        private Color GetStressColor(float stressPercentage)
        {
            if (stressPercentage < 33f)
                return LowStressColor;
            else if (stressPercentage < 66f)
                return MediumStressColor;
            else
                return HighStressColor;
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
