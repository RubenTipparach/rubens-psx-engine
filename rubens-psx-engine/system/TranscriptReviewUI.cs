using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using rubens_psx_engine;

namespace anakinsoft.system
{
    /// <summary>
    /// UI system for reviewing suspect interview transcripts
    /// </summary>
    public class TranscriptReviewUI
    {
        private bool isActive;
        private CrimeSceneFile crimeSceneFile;
        private int selectedIndex;
        private int scrollOffset;
        private const int MaxVisibleLines = 15;
        private KeyboardState previousKeyboard;

        public bool IsActive => isActive;

        public TranscriptReviewUI()
        {
            isActive = false;
            selectedIndex = 0;
            scrollOffset = 0;
        }

        /// <summary>
        /// Open the transcript review UI
        /// </summary>
        public void Open(CrimeSceneFile file)
        {
            crimeSceneFile = file;
            isActive = true;
            selectedIndex = 0;
            scrollOffset = 0;
            Console.WriteLine("Opened transcript review UI");
        }

        /// <summary>
        /// Close the transcript review UI
        /// </summary>
        public void Close()
        {
            isActive = false;
            crimeSceneFile = null;
            Console.WriteLine("Closed transcript review UI");
        }

        /// <summary>
        /// Update the transcript review UI
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!isActive || crimeSceneFile == null)
                return;

            var keyboard = Keyboard.GetState();

            // Navigation
            if (keyboard.IsKeyDown(Keys.Up) && !previousKeyboard.IsKeyDown(Keys.Up))
            {
                selectedIndex = Math.Max(0, selectedIndex - 1);
                if (selectedIndex < scrollOffset)
                    scrollOffset = selectedIndex;
            }

            if (keyboard.IsKeyDown(Keys.Down) && !previousKeyboard.IsKeyDown(Keys.Down))
            {
                selectedIndex = Math.Min(crimeSceneFile.Transcripts.Count - 1, selectedIndex + 1);
                if (selectedIndex >= scrollOffset + MaxVisibleLines)
                    scrollOffset = selectedIndex - MaxVisibleLines + 1;
            }

            // Close with Escape or E
            if ((keyboard.IsKeyDown(Keys.Escape) || keyboard.IsKeyDown(Keys.E)) &&
                (!previousKeyboard.IsKeyDown(Keys.Escape) && !previousKeyboard.IsKeyDown(Keys.E)))
            {
                Close();
            }

            previousKeyboard = keyboard;
        }

        /// <summary>
        /// Draw the transcript review UI
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (!isActive || crimeSceneFile == null || font == null)
                return;

            var viewport = Globals.screenManager.GraphicsDevice.Viewport;
            int screenWidth = viewport.Width;
            int screenHeight = viewport.Height;

            // Background overlay
            var backgroundTexture = new Texture2D(Globals.screenManager.GraphicsDevice, 1, 1);
            backgroundTexture.SetData(new[] { new Color(0, 0, 0, 200) });
            spriteBatch.Draw(backgroundTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);

            // Main panel
            int panelWidth = (int)(screenWidth * 0.8f);
            int panelHeight = (int)(screenHeight * 0.8f);
            int panelX = (screenWidth - panelWidth) / 2;
            int panelY = (screenHeight - panelHeight) / 2;

            var panelTexture = new Texture2D(Globals.screenManager.GraphicsDevice, 1, 1);
            panelTexture.SetData(new[] { new Color(20, 20, 30, 255) });
            spriteBatch.Draw(panelTexture, new Rectangle(panelX, panelY, panelWidth, panelHeight), Color.White);

            // Border
            var borderTexture = new Texture2D(Globals.screenManager.GraphicsDevice, 1, 1);
            borderTexture.SetData(new[] { Color.Yellow });
            int borderThickness = 2;
            spriteBatch.Draw(borderTexture, new Rectangle(panelX, panelY, panelWidth, borderThickness), Color.White); // Top
            spriteBatch.Draw(borderTexture, new Rectangle(panelX, panelY + panelHeight - borderThickness, panelWidth, borderThickness), Color.White); // Bottom
            spriteBatch.Draw(borderTexture, new Rectangle(panelX, panelY, borderThickness, panelHeight), Color.White); // Left
            spriteBatch.Draw(borderTexture, new Rectangle(panelX + panelWidth - borderThickness, panelY, borderThickness, panelHeight), Color.White); // Right

            // Title
            string title = "=== CRIME SCENE FILE - SUSPECT TRANSCRIPTS ===";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2((screenWidth - titleSize.X) / 2, panelY + 20);
            spriteBatch.DrawString(font, title, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(font, title, titlePos, Color.Yellow);

            // Instructions
            string instructions = "[Up/Down] Navigate | [E/Escape] Close";
            Vector2 instructionsSize = font.MeasureString(instructions);
            Vector2 instructionsPos = new Vector2((screenWidth - instructionsSize.X) / 2, panelY + 50);
            spriteBatch.DrawString(font, instructions, instructionsPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, instructions, instructionsPos, Color.Gray);

            // Transcripts list
            int yOffset = panelY + 90;
            int lineHeight = 25;
            int contentStartY = yOffset;

            for (int i = scrollOffset; i < Math.Min(crimeSceneFile.Transcripts.Count, scrollOffset + MaxVisibleLines); i++)
            {
                var transcript = crimeSceneFile.Transcripts[i];
                bool isSelected = (i == selectedIndex);

                // Highlight selected item
                if (isSelected)
                {
                    var highlightTexture = new Texture2D(Globals.screenManager.GraphicsDevice, 1, 1);
                    highlightTexture.SetData(new[] { new Color(100, 100, 50, 100) });
                    spriteBatch.Draw(highlightTexture, new Rectangle(panelX + 20, yOffset - 5, panelWidth - 40, lineHeight), Color.White);
                }

                // Suspect name
                string suspectName = $"{transcript.SuspectName}";
                Color nameColor = transcript.WasQuestioned ? Color.White : Color.Gray;
                if (isSelected) nameColor = Color.Yellow;

                Vector2 namePos = new Vector2(panelX + 30, yOffset);
                spriteBatch.DrawString(font, suspectName, namePos + new Vector2(1, 1), Color.Black);
                spriteBatch.DrawString(font, suspectName, namePos, nameColor);

                // Status indicator
                string status = transcript.WasQuestioned ? "[INTERVIEWED]" : "[NOT INTERVIEWED]";
                Color statusColor = transcript.WasQuestioned ? Color.Green : Color.Red;
                Vector2 statusPos = new Vector2(panelX + panelWidth - 200, yOffset);
                spriteBatch.DrawString(font, status, statusPos + new Vector2(1, 1), Color.Black);
                spriteBatch.DrawString(font, status, statusPos, statusColor);

                yOffset += lineHeight;
            }

            // Selected transcript content
            if (selectedIndex >= 0 && selectedIndex < crimeSceneFile.Transcripts.Count)
            {
                var selectedTranscript = crimeSceneFile.Transcripts[selectedIndex];

                // Divider line
                int dividerY = contentStartY + (MaxVisibleLines * lineHeight) + 10;
                spriteBatch.Draw(borderTexture, new Rectangle(panelX + 20, dividerY, panelWidth - 40, 2), Color.Yellow);

                // Transcript content
                string contentTitle = $"--- {selectedTranscript.SuspectName} ---";
                Vector2 contentTitlePos = new Vector2(panelX + 30, dividerY + 15);
                spriteBatch.DrawString(font, contentTitle, contentTitlePos + new Vector2(1, 1), Color.Black);
                spriteBatch.DrawString(font, contentTitle, contentTitlePos, Color.Cyan);

                // Wrap and display transcript content
                string content = selectedTranscript.Content;
                Vector2 contentPos = new Vector2(panelX + 30, dividerY + 45);
                DrawWrappedText(spriteBatch, font, content, contentPos, panelWidth - 60, Color.White);
            }

            backgroundTexture.Dispose();
            panelTexture.Dispose();
            borderTexture.Dispose();
        }

        private void DrawWrappedText(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, int maxWidth, Color color)
        {
            string[] words = text.Split(' ');
            string currentLine = "";
            Vector2 currentPos = position;

            foreach (string word in words)
            {
                string testLine = currentLine + word + " ";
                Vector2 testSize = font.MeasureString(testLine);

                if (testSize.X > maxWidth && currentLine != "")
                {
                    // Draw current line
                    spriteBatch.DrawString(font, currentLine, currentPos + new Vector2(1, 1), Color.Black);
                    spriteBatch.DrawString(font, currentLine, currentPos, color);
                    currentPos.Y += font.LineSpacing;
                    currentLine = word + " ";
                }
                else
                {
                    currentLine = testLine;
                }
            }

            // Draw remaining text
            if (currentLine != "")
            {
                spriteBatch.DrawString(font, currentLine, currentPos + new Vector2(1, 1), Color.Black);
                spriteBatch.DrawString(font, currentLine, currentPos, color);
            }
        }
    }
}
