using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;
using System.Collections.Generic;

namespace anakinsoft.game.scenes.lounge.ui
{
    /// <summary>
    /// UI for selecting evidence to present during accusation
    /// </summary>
    public class EvidenceSelectionUI
    {
        private bool isVisible = false;
        private List<EvidenceItem> availableEvidence;
        private int selectedIndex = 0;
        private KeyboardState previousKeyboard;
        private MouseState previousMouse;

        // UI settings
        private const float BoxPadding = 20f;
        private const float ItemHeight = 40f;
        private const float ItemSpacing = 5f;
        private readonly Color BackgroundColor = Color.Black * 0.90f;
        private readonly Color SelectedColor = Color.Yellow;
        private readonly Color NormalColor = Color.White;
        private readonly Color DescriptionColor = Color.LightGray;

        // Events
        public event Action<string> OnEvidenceSelected;  // Fires with evidence ID
        public event Action OnCancelled;

        public bool IsVisible => isVisible;

        public EvidenceSelectionUI()
        {
            availableEvidence = new List<EvidenceItem>();
        }

        /// <summary>
        /// Show the evidence selection UI
        /// </summary>
        public void Show(List<EvidenceItem> evidence)
        {
            if (evidence == null || evidence.Count == 0)
            {
                Console.WriteLine("[EvidenceSelectionUI] No evidence available");
                return;
            }

            availableEvidence = new List<EvidenceItem>(evidence);
            selectedIndex = 0;
            isVisible = true;
            Console.WriteLine($"[EvidenceSelectionUI] Showing {evidence.Count} evidence items");
        }

        /// <summary>
        /// Hide the evidence selection UI
        /// </summary>
        public void Hide()
        {
            isVisible = false;
            availableEvidence.Clear();
            selectedIndex = 0;
            Console.WriteLine("[EvidenceSelectionUI] Hidden");
        }

        /// <summary>
        /// Update the UI (handles input)
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!isVisible || availableEvidence.Count == 0)
                return;

            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();

            // Navigate up
            if (keyboard.IsKeyDown(Keys.Up) && !previousKeyboard.IsKeyDown(Keys.Up))
            {
                selectedIndex--;
                if (selectedIndex < 0)
                    selectedIndex = availableEvidence.Count - 1;
            }

            // Navigate down
            if (keyboard.IsKeyDown(Keys.Down) && !previousKeyboard.IsKeyDown(Keys.Down))
            {
                selectedIndex++;
                if (selectedIndex >= availableEvidence.Count)
                    selectedIndex = 0;
            }

            // Select with Enter or E
            if ((keyboard.IsKeyDown(Keys.Enter) && !previousKeyboard.IsKeyDown(Keys.Enter)) ||
                (keyboard.IsKeyDown(Keys.E) && !previousKeyboard.IsKeyDown(Keys.E)))
            {
                var selectedEvidence = availableEvidence[selectedIndex];
                Console.WriteLine($"[EvidenceSelectionUI] Selected evidence: {selectedEvidence.Name}");
                OnEvidenceSelected?.Invoke(selectedEvidence.Id);
                Hide();
            }

            // Cancel with Escape or Tab
            if ((keyboard.IsKeyDown(Keys.Escape) && !previousKeyboard.IsKeyDown(Keys.Escape)) ||
                (keyboard.IsKeyDown(Keys.Tab) && !previousKeyboard.IsKeyDown(Keys.Tab)))
            {
                Console.WriteLine("[EvidenceSelectionUI] Cancelled");
                OnCancelled?.Invoke();
                Hide();
            }

            previousKeyboard = keyboard;
            previousMouse = mouse;
        }

        /// <summary>
        /// Draw the evidence selection UI
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (!isVisible || availableEvidence.Count == 0 || font == null)
                return;

            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            // Calculate menu dimensions
            float menuWidth = 600f;
            float menuHeight = BoxPadding * 2 +
                              font.MeasureString("SELECT EVIDENCE").Y +
                              (ItemHeight + ItemSpacing) * availableEvidence.Count +
                              font.MeasureString("[Enter] Select  [Tab] Cancel").Y + 20;

            // Center the menu
            float menuX = (viewport.Width - menuWidth) / 2;
            float menuY = (viewport.Height - menuHeight) / 2;

            // Draw background
            Rectangle backgroundRect = new Rectangle((int)menuX, (int)menuY, (int)menuWidth, (int)menuHeight);
            DrawFilledRectangle(spriteBatch, backgroundRect, BackgroundColor);
            DrawRectangleBorder(spriteBatch, backgroundRect, Color.White, 3);

            float currentY = menuY + BoxPadding;

            // Draw title
            string title = "SELECT EVIDENCE TO PRESENT";
            var titleSize = font.MeasureString(title) * 0.7f;
            Vector2 titlePos = new Vector2(menuX + (menuWidth - titleSize.X) / 2, currentY);
            spriteBatch.DrawString(font, title, titlePos, SelectedColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            currentY += titleSize.Y + 20;

            // Draw evidence items
            for (int i = 0; i < availableEvidence.Count; i++)
            {
                var evidence = availableEvidence[i];
                bool isSelected = i == selectedIndex;

                // Draw selection highlight
                if (isSelected)
                {
                    Rectangle highlightRect = new Rectangle(
                        (int)(menuX + 10),
                        (int)(currentY - 5),
                        (int)(menuWidth - 20),
                        (int)(ItemHeight + 10)
                    );
                    DrawRectangleBorder(spriteBatch, highlightRect, SelectedColor, 2);
                }

                // Draw evidence name
                float nameScale = 0.6f;
                Color nameColor = isSelected ? SelectedColor : NormalColor;
                Vector2 namePos = new Vector2(menuX + BoxPadding, currentY);
                spriteBatch.DrawString(font, evidence.Name, namePos, nameColor, 0f, Vector2.Zero, nameScale, SpriteEffects.None, 0f);

                // Draw evidence description (smaller, below name)
                float descScale = 0.4f;
                Vector2 descPos = new Vector2(menuX + BoxPadding, currentY + font.MeasureString(evidence.Name).Y * nameScale);
                string wrappedDesc = WrapText(evidence.Description, font, menuWidth - BoxPadding * 2, descScale);
                spriteBatch.DrawString(font, wrappedDesc, descPos, DescriptionColor, 0f, Vector2.Zero, descScale, SpriteEffects.None, 0f);

                currentY += ItemHeight + ItemSpacing;
            }

            // Draw controls hint
            currentY = menuY + menuHeight - BoxPadding - font.MeasureString("Hint").Y * 0.5f;
            string hint = "[Up/Down] Navigate  [Enter/E] Select  [Tab] Cancel";
            var hintSize = font.MeasureString(hint) * 0.5f;
            Vector2 hintPos = new Vector2(menuX + (menuWidth - hintSize.X) / 2, currentY);
            spriteBatch.DrawString(font, hint, hintPos, Color.Gray, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// Wrap text to fit within a specified width
        /// </summary>
        private string WrapText(string text, SpriteFont font, float maxWidth, float scale)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string[] words = text.Split(' ');
            string wrappedText = "";
            string line = "";

            foreach (string word in words)
            {
                string testLine = line + word + " ";
                Vector2 testSize = font.MeasureString(testLine) * scale;

                if (testSize.X > maxWidth && line.Length > 0)
                {
                    wrappedText += line.Trim() + "\n";
                    line = word + " ";
                }
                else
                {
                    line = testLine;
                }
            }

            wrappedText += line.Trim();
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

    /// <summary>
    /// Represents a piece of evidence that can be presented
    /// </summary>
    public class EvidenceItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public EvidenceItem(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }
}
