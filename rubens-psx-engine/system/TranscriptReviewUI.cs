using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using anakinsoft.game.scenes.lounge.characters;
using rubens_psx_engine;

namespace anakinsoft.system
{
    /// <summary>
    /// UI system for reviewing character interview transcripts
    /// Two modes: Character selection and Transcript viewing
    /// </summary>
    public class TranscriptReviewUI
    {
        private bool isActive;
        private Dictionary<string, CharacterStateMachine> characterStateMachines;
        private List<string> characterNames; // Ordered list of character names
        private int selectedIndex;
        private int scrollOffset;
        private const int MaxVisibleLines = 15;
        private KeyboardState previousKeyboard;

        // Two-mode system
        private bool isViewingTranscript; // false = selection mode, true = viewing mode
        private string viewingCharacterName;
        private int subjectScrollOffset;

        public bool IsActive => isActive;

        public TranscriptReviewUI()
        {
            isActive = false;
            selectedIndex = 0;
            scrollOffset = 0;
            isViewingTranscript = false;
            subjectScrollOffset = 0;
        }

        /// <summary>
        /// Open the transcript review UI with character state machines
        /// </summary>
        public void Open(Dictionary<string, CharacterStateMachine> stateMachines)
        {
            characterStateMachines = stateMachines;
            characterNames = stateMachines.Keys.ToList();
            isActive = true;
            selectedIndex = 0;
            scrollOffset = 0;
            isViewingTranscript = false;
            viewingCharacterName = null;
            subjectScrollOffset = 0;

            // IMPORTANT: Initialize previousKeyboard to current state to prevent immediate E press
            previousKeyboard = Keyboard.GetState();

            Console.WriteLine($"[TranscriptReviewUI] Opened with {characterNames.Count} characters");
        }

        /// <summary>
        /// Close the transcript review UI
        /// </summary>
        public void Close()
        {
            isActive = false;
            characterStateMachines = null;
            characterNames = null;
            Console.WriteLine("[TranscriptReviewUI] Closed");
        }

        /// <summary>
        /// Update the transcript review UI
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!isActive || characterStateMachines == null)
                return;

            var keyboard = Keyboard.GetState();

            if (isViewingTranscript)
            {
                UpdateViewingMode(keyboard);
            }
            else
            {
                UpdateSelectionMode(keyboard);
            }

            previousKeyboard = keyboard;
        }

        private void UpdateSelectionMode(KeyboardState keyboard)
        {
            // Navigation
            if (keyboard.IsKeyDown(Keys.Up) && !previousKeyboard.IsKeyDown(Keys.Up))
            {
                selectedIndex = Math.Max(0, selectedIndex - 1);
                if (selectedIndex < scrollOffset)
                    scrollOffset = selectedIndex;
            }

            if (keyboard.IsKeyDown(Keys.Down) && !previousKeyboard.IsKeyDown(Keys.Down))
            {
                selectedIndex = Math.Min(characterNames.Count - 1, selectedIndex + 1);
                if (selectedIndex >= scrollOffset + MaxVisibleLines)
                    scrollOffset = selectedIndex - MaxVisibleLines + 1;
            }

            // Press E to view transcript (only if character has been interviewed)
            if (keyboard.IsKeyDown(Keys.E) && !previousKeyboard.IsKeyDown(Keys.E))
            {
                if (selectedIndex >= 0 && selectedIndex < characterNames.Count)
                {
                    string characterName = characterNames[selectedIndex];
                    var stateMachine = characterStateMachines[characterName];

                    if (stateMachine.HasBeenInterviewed())
                    {
                        // Enter viewing mode
                        isViewingTranscript = true;
                        viewingCharacterName = characterName;
                        subjectScrollOffset = 0;
                        Console.WriteLine($"[TranscriptReviewUI] Viewing transcript for {characterName}");
                    }
                }
            }

            // Press Tab to close
            if (keyboard.IsKeyDown(Keys.Tab) && !previousKeyboard.IsKeyDown(Keys.Tab))
            {
                Close();
            }
        }

        private void UpdateViewingMode(KeyboardState keyboard)
        {
            var stateMachine = characterStateMachines[viewingCharacterName];
            var subjects = stateMachine.GetTranscriptSubjects();

            // Scroll through transcript content
            if (keyboard.IsKeyDown(Keys.Up) && !previousKeyboard.IsKeyDown(Keys.Up))
            {
                subjectScrollOffset = Math.Max(0, subjectScrollOffset - 1);
            }

            if (keyboard.IsKeyDown(Keys.Down) && !previousKeyboard.IsKeyDown(Keys.Down))
            {
                // Calculate max scroll based on content
                int totalLines = 0;
                foreach (var subject in subjects)
                {
                    totalLines += 2; // Subject header + spacing
                    totalLines += stateMachine.GetSubjectLines(subject).Count;
                }
                subjectScrollOffset = Math.Min(Math.Max(0, totalLines - MaxVisibleLines), subjectScrollOffset + 1);
            }

            // Press Tab to go back to character selection
            if (keyboard.IsKeyDown(Keys.Tab) && !previousKeyboard.IsKeyDown(Keys.Tab))
            {
                isViewingTranscript = false;
                viewingCharacterName = null;
                subjectScrollOffset = 0;
                Console.WriteLine("[TranscriptReviewUI] Returned to character selection");
            }
        }

        /// <summary>
        /// Draw the transcript review UI
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (!isActive || characterStateMachines == null || font == null)
                return;

            var viewport = Globals.screenManager.GraphicsDevice.Viewport;
            int screenWidth = viewport.Width;
            int screenHeight = viewport.Height;

            // Black semi-transparent background overlay for better text readability
            var backgroundTexture = new Texture2D(Globals.screenManager.GraphicsDevice, 1, 1);
            backgroundTexture.SetData(new[] { new Color(0, 0, 0, 220) }); // Darker for better readability
            spriteBatch.Draw(backgroundTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);

            if (isViewingTranscript)
            {
                DrawViewingMode(spriteBatch, font, screenWidth, screenHeight);
            }
            else
            {
                DrawSelectionMode(spriteBatch, font, screenWidth, screenHeight);
            }

            backgroundTexture.Dispose();
        }

        private void DrawSelectionMode(SpriteBatch spriteBatch, SpriteFont font, int screenWidth, int screenHeight)
        {
            // Main panel
            int panelWidth = (int)(screenWidth * 0.7f);
            int panelHeight = (int)(screenHeight * 0.7f);
            int panelX = (screenWidth - panelWidth) / 2;
            int panelY = (screenHeight - panelHeight) / 2;

            var panelTexture = new Texture2D(Globals.screenManager.GraphicsDevice, 1, 1);
            panelTexture.SetData(new[] { new Color(20, 20, 30, 255) });
            spriteBatch.Draw(panelTexture, new Rectangle(panelX, panelY, panelWidth, panelHeight), Color.White);

            // Border
            DrawBorder(spriteBatch, new Rectangle(panelX, panelY, panelWidth, panelHeight), Color.Yellow, 2);

            // Title
            string title = "=== INTERVIEW TRANSCRIPTS ===";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2((screenWidth - titleSize.X) / 2, panelY + 20);
            spriteBatch.DrawString(font, title, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(font, title, titlePos, Color.Yellow);

            // Instructions
            string instructions = "[Up/Down] Navigate | [E] View Transcript | [Tab] Close";
            Vector2 instructionsSize = font.MeasureString(instructions);
            Vector2 instructionsPos = new Vector2((screenWidth - instructionsSize.X) / 2, panelY + 50);
            spriteBatch.DrawString(font, instructions, instructionsPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, instructions, instructionsPos, Color.Gray);

            // Character list
            int yOffset = panelY + 90;
            int lineHeight = 25;

            for (int i = scrollOffset; i < Math.Min(characterNames.Count, scrollOffset + MaxVisibleLines); i++)
            {
                string characterName = characterNames[i];
                var stateMachine = characterStateMachines[characterName];
                bool isSelected = (i == selectedIndex);
                bool hasBeenInterviewed = stateMachine.HasBeenInterviewed();

                // Highlight selected item
                if (isSelected)
                {
                    var highlightTexture = new Texture2D(Globals.screenManager.GraphicsDevice, 1, 1);
                    highlightTexture.SetData(new[] { new Color(100, 100, 50, 100) });
                    spriteBatch.Draw(highlightTexture, new Rectangle(panelX + 20, yOffset - 5, panelWidth - 40, lineHeight), Color.White);
                    highlightTexture.Dispose();
                }

                // Character name
                Color nameColor = hasBeenInterviewed ? Color.White : Color.Gray;
                if (isSelected) nameColor = Color.Yellow;

                Vector2 namePos = new Vector2(panelX + 30, yOffset);
                spriteBatch.DrawString(font, characterName, namePos + new Vector2(1, 1), Color.Black);
                spriteBatch.DrawString(font, characterName, namePos, nameColor);

                // Status indicator
                string status = hasBeenInterviewed ? "[INTERVIEWED]" : "[NOT INTERVIEWED]";
                Color statusColor = hasBeenInterviewed ? Color.Green : Color.Red;
                Vector2 statusPos = new Vector2(panelX + panelWidth - 200, yOffset);
                spriteBatch.DrawString(font, status, statusPos + new Vector2(1, 1), Color.Black);
                spriteBatch.DrawString(font, status, statusPos, statusColor);

                yOffset += lineHeight;
            }

            panelTexture.Dispose();
        }

        private void DrawViewingMode(SpriteBatch spriteBatch, SpriteFont font, int screenWidth, int screenHeight)
        {
            var stateMachine = characterStateMachines[viewingCharacterName];
            var subjects = stateMachine.GetTranscriptSubjects();

            // Main panel
            int panelWidth = (int)(screenWidth * 0.8f);
            int panelHeight = (int)(screenHeight * 0.8f);
            int panelX = (screenWidth - panelWidth) / 2;
            int panelY = (screenHeight - panelHeight) / 2;

            var panelTexture = new Texture2D(Globals.screenManager.GraphicsDevice, 1, 1);
            panelTexture.SetData(new[] { new Color(20, 20, 30, 255) });
            spriteBatch.Draw(panelTexture, new Rectangle(panelX, panelY, panelWidth, panelHeight), Color.White);

            // Border
            DrawBorder(spriteBatch, new Rectangle(panelX, panelY, panelWidth, panelHeight), Color.Yellow, 2);

            // Title
            string title = $"=== {viewingCharacterName} - TRANSCRIPT ===";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2((screenWidth - titleSize.X) / 2, panelY + 20);
            spriteBatch.DrawString(font, title, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(font, title, titlePos, Color.Cyan);

            // Instructions
            string instructions = "[Up/Down] Scroll | [Tab] Back to List";
            Vector2 instructionsSize = font.MeasureString(instructions);
            Vector2 instructionsPos = new Vector2((screenWidth - instructionsSize.X) / 2, panelY + 50);
            spriteBatch.DrawString(font, instructions, instructionsPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, instructions, instructionsPos, Color.Gray);

            // Transcript content
            int yOffset = panelY + 90;
            float textScale = 0.5f; // 50% font size
            int baseLineHeight = (int)(font.LineSpacing * textScale);
            int lineSpacing = baseLineHeight + 8; // Add spacing between lines
            int currentLine = 0;

            foreach (var subject in subjects)
            {
                // Subject header
                if (currentLine >= subjectScrollOffset)
                {
                    string subjectHeader = $"--- {subject} ---";
                    Vector2 subjectPos = new Vector2(panelX + 30, yOffset);
                    spriteBatch.DrawString(font, subjectHeader, subjectPos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(font, subjectHeader, subjectPos, Color.Yellow, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                    yOffset += lineSpacing + 5; // Extra spacing after subject header

                    if (yOffset > panelY + panelHeight - 30)
                        break;
                }
                currentLine++;

                // Character's lines for this subject
                var lines = stateMachine.GetSubjectLines(subject);
                foreach (var line in lines)
                {
                    if (currentLine >= subjectScrollOffset)
                    {
                        // Proper word wrapping for long lines
                        List<string> wrappedLines = WrapText(font, line, panelWidth - 100, textScale);

                        foreach (var wrappedLine in wrappedLines)
                        {
                            Vector2 linePos = new Vector2(panelX + 50, yOffset);
                            spriteBatch.DrawString(font, wrappedLine, linePos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                            spriteBatch.DrawString(font, wrappedLine, linePos, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                            yOffset += lineSpacing;

                            if (yOffset > panelY + panelHeight - 30)
                                break;
                        }

                        if (yOffset > panelY + panelHeight - 30)
                            break;
                    }
                    currentLine++;
                }

                // Spacing between subjects
                yOffset += lineSpacing;
                currentLine++;

                if (yOffset > panelY + panelHeight - 30)
                    break;
            }

            panelTexture.Dispose();
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            var borderTexture = new Texture2D(Globals.screenManager.GraphicsDevice, 1, 1);
            borderTexture.SetData(new[] { Color.White });

            // Top
            spriteBatch.Draw(borderTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(borderTexture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(borderTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            spriteBatch.Draw(borderTexture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);

            borderTexture.Dispose();
        }

        /// <summary>
        /// Wrap text to fit within a specified width
        /// </summary>
        private List<string> WrapText(SpriteFont font, string text, int maxWidth, float scale)
        {
            List<string> lines = new List<string>();
            string[] words = text.Split(' ');
            string currentLine = "";

            foreach (string word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                Vector2 testSize = font.MeasureString(testLine) * scale;

                if (testSize.X > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    // Current line is full, add it and start new line
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            // Add remaining text
            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }

            // If no lines were created (empty text), return empty list
            if (lines.Count == 0)
            {
                lines.Add("");
            }

            return lines;
        }
    }
}
