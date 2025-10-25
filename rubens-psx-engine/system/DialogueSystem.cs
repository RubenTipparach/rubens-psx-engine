using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;
using System.Collections.Generic;

namespace anakinsoft.system
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

        // Display settings
        private const float BoxPadding = 20f;
        private const float LineHeight = 30f;
        private const float SpeakerOffset = 40f;
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
                OnLineChanged?.Invoke(CurrentLine);
                Console.WriteLine($"DialogueSystem: Line {currentLineIndex + 1}/{currentSequence.Lines.Count}");
            }
        }

        /// <summary>
        /// Updates the dialogue system (handles input)
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!isActive)
                return;

            var keyboard = Keyboard.GetState();

            // Advance dialogue with Space or E key
            if ((keyboard.IsKeyDown(Keys.Space) && !previousKeyboard.IsKeyDown(Keys.Space)) ||
                (keyboard.IsKeyDown(Keys.E) && !previousKeyboard.IsKeyDown(Keys.E)))
            {
                NextLine();
            }

            // Skip dialogue with Escape
            if (keyboard.IsKeyDown(Keys.Escape) && !previousKeyboard.IsKeyDown(Keys.Escape))
            {
                StopDialogue();
            }

            previousKeyboard = keyboard;
        }

        /// <summary>
        /// Draws the dialogue UI
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (!isActive || CurrentLine == null || font == null)
                return;

            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            // Measure text
            var speakerText = CurrentLine.Speaker;
            var dialogueText = WrapText(CurrentLine.Text, font, viewport.Width - BoxPadding * 4);
            var promptText = "Press [SPACE] or [E] to continue...";

            var speakerSize = font.MeasureString(speakerText);
            var dialogueSize = font.MeasureString(dialogueText);
            var promptSize = font.MeasureString(promptText);

            // Calculate box dimensions
            float boxWidth = Math.Max(Math.Max(speakerSize.X, dialogueSize.X), promptSize.X) + BoxPadding * 2;
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
