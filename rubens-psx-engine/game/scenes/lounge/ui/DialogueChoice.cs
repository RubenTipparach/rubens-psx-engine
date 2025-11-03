using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;
using System.Collections.Generic;

namespace anakinsoft.game.scenes.lounge.ui
{
    /// <summary>
    /// Represents a choice in dialogue
    /// </summary>
    public class DialogueOption
    {
        public string Text { get; set; }
        public Action OnSelected { get; set; }

        public DialogueOption(string text, Action onSelected = null)
        {
            Text = text;
            OnSelected = onSelected;
        }
    }

    /// <summary>
    /// Dialogue choice selection UI
    /// </summary>
    public class DialogueChoiceSystem
    {
        private List<DialogueOption> options = new List<DialogueOption>();
        private int selectedIndex = 0;
        private bool isActive = false;
        private KeyboardState previousKeyboard;
        private MouseState previousMouse;
        private string promptText = "";
        private List<Rectangle> optionBounds = new List<Rectangle>(); // Mouse hit boxes for options
        private bool mouseOnlyMode = false; // When true, keyboard input is disabled

        // Display settings
        private const float BoxPadding = 20f;
        private const float OptionHeight = 40f;
        private const float OptionSpacing = 10f;
        private readonly Color BackgroundColor = Color.Black * 0.90f;
        private readonly Color SelectedColor = Color.Yellow;
        private readonly Color NormalColor = Color.White;
        private readonly Color PromptColor = Color.LightGray;

        // Events
        public event Action<DialogueOption> OnOptionSelected;
        public event Action OnChoiceCancelled;

        public bool IsActive => isActive;

        /// <summary>
        /// Show dialogue choices
        /// </summary>
        public void ShowChoices(string prompt, List<DialogueOption> choices, bool mouseOnly = false)
        {
            if (choices == null || choices.Count == 0)
            {
                Console.WriteLine("DialogueChoiceSystem: No choices provided");
                return;
            }

            promptText = prompt;
            options = new List<DialogueOption>(choices);
            selectedIndex = 0;
            isActive = true;
            mouseOnlyMode = mouseOnly;

            Console.WriteLine($"DialogueChoiceSystem: Showing {options.Count} choices (mouseOnly={mouseOnly})");
        }

        /// <summary>
        /// Hide choice menu
        /// </summary>
        public void Hide()
        {
            isActive = false;
            options.Clear();
            Console.WriteLine("DialogueChoiceSystem: Hidden");
        }

        /// <summary>
        /// Update choice selection
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!isActive)
                return;

            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();
            var mousePosition = new Point(mouse.X, mouse.Y);

            // Mouse hover detection - update selected index based on hover
            for (int i = 0; i < optionBounds.Count; i++)
            {
                if (optionBounds[i].Contains(mousePosition))
                {
                    selectedIndex = i;
                    break;
                }
            }

            // Mouse click selection
            if (mouse.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed)
            {
                for (int i = 0; i < optionBounds.Count; i++)
                {
                    if (optionBounds[i].Contains(mousePosition))
                    {
                        var selectedOption = options[i];
                        Console.WriteLine($"DialogueChoiceSystem: Mouse selected '{selectedOption.Text}'");

                        selectedOption.OnSelected?.Invoke();
                        OnOptionSelected?.Invoke(selectedOption);
                        Hide();
                        previousMouse = mouse;
                        return;
                    }
                }
            }

            // Keyboard input only if not in mouse-only mode
            if (!mouseOnlyMode)
            {
                // Keyboard: Navigate up
                if (keyboard.IsKeyDown(Keys.Up) && !previousKeyboard.IsKeyDown(Keys.Up))
                {
                    selectedIndex--;
                    if (selectedIndex < 0)
                        selectedIndex = options.Count - 1;
                }

                // Keyboard: Navigate down
                if (keyboard.IsKeyDown(Keys.Down) && !previousKeyboard.IsKeyDown(Keys.Down))
                {
                    selectedIndex++;
                    if (selectedIndex >= options.Count)
                        selectedIndex = 0;
                }

                // Keyboard: Select option with E or Enter
                if ((keyboard.IsKeyDown(Keys.E) && !previousKeyboard.IsKeyDown(Keys.E)) ||
                    (keyboard.IsKeyDown(Keys.Enter) && !previousKeyboard.IsKeyDown(Keys.Enter)))
                {
                    if (selectedIndex >= 0 && selectedIndex < options.Count)
                    {
                        var selectedOption = options[selectedIndex];
                        Console.WriteLine($"DialogueChoiceSystem: Keyboard selected '{selectedOption.Text}'");

                        selectedOption.OnSelected?.Invoke();
                        OnOptionSelected?.Invoke(selectedOption);
                        Hide();
                    }
                }
            }

            // ESC removed - causes serious bugs by bypassing dialogue flow
            // Player must make a choice to continue

            previousKeyboard = keyboard;
            previousMouse = mouse;
        }

        /// <summary>
        /// Draw choice menu
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (!isActive || font == null)
                return;

            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            // Calculate menu dimensions
            float menuWidth = 600f;
            float promptHeight = string.IsNullOrEmpty(promptText) ? 0 : font.MeasureString(promptText).Y + 20f;
            float menuHeight = promptHeight + (OptionHeight + OptionSpacing) * options.Count + BoxPadding * 2;

            // Center the menu
            float menuX = (viewport.Width - menuWidth) / 2;
            float menuY = (viewport.Height - menuHeight) / 2;

            // Draw background
            DrawFilledRectangle(spriteBatch, new Rectangle((int)menuX, (int)menuY, (int)menuWidth, (int)menuHeight), BackgroundColor);

            // Draw border
            DrawRectangleBorder(spriteBatch, new Rectangle((int)menuX, (int)menuY, (int)menuWidth, (int)menuHeight), Color.White, 3);

            float currentY = menuY + BoxPadding;

            // Draw prompt text if provided
            if (!string.IsNullOrEmpty(promptText))
            {
                var wrappedPrompt = WrapText(promptText, font, menuWidth - BoxPadding * 2);
                Vector2 promptPos = new Vector2(menuX + BoxPadding, currentY);
                spriteBatch.DrawString(font, wrappedPrompt, promptPos + Vector2.One, Color.Black); // Shadow
                spriteBatch.DrawString(font, wrappedPrompt, promptPos, PromptColor);
                currentY += font.MeasureString(wrappedPrompt).Y + 20f;
            }

            // Draw options and populate mouse hit boxes
            optionBounds.Clear(); // Clear previous bounds
            for (int i = 0; i < options.Count; i++)
            {
                bool isSelected = i == selectedIndex;
                Color optionColor = isSelected ? SelectedColor : NormalColor;

                // Calculate bounds for mouse hit detection
                Rectangle bounds = new Rectangle(
                    (int)menuX + 10,
                    (int)currentY - 5,
                    (int)menuWidth - 20,
                    (int)OptionHeight
                );
                optionBounds.Add(bounds);

                // Draw selection highlight
                if (isSelected)
                {
                    DrawFilledRectangle(spriteBatch, bounds, Color.Yellow * 0.2f);
                }

                // Draw option text
                string optionText = $"{(isSelected ? "> " : "  ")}{options[i].Text}";
                Vector2 optionPos = new Vector2(menuX + BoxPadding, currentY);
                spriteBatch.DrawString(font, optionText, optionPos + Vector2.One, Color.Black); // Shadow
                spriteBatch.DrawString(font, optionText, optionPos, optionColor);

                currentY += OptionHeight + OptionSpacing;
            }

            // Draw controls hint
            string hint = "[Up/Down] Navigate  [E] Select  [ESC] Cancel";
            var hintSize = font.MeasureString(hint);
            Vector2 hintPos = new Vector2(menuX + (menuWidth - hintSize.X) / 2, menuY + menuHeight - BoxPadding - hintSize.Y + 10);
            spriteBatch.DrawString(font, hint, hintPos, Color.Gray);
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
