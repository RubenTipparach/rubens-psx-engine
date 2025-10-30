using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;

namespace anakinsoft.game.scenes.lounge.ui
{
    /// <summary>
    /// Simple confirmation dialog for yes/no choices
    /// </summary>
    public class ConfirmationDialogUI
    {
        private bool isActive = false;
        private string message = "";
        private int hoveredButton = -1; // 0 = Yes, 1 = No, -1 = none
        private MouseState previousMouse;
        private Rectangle yesButtonRect;
        private Rectangle noButtonRect;

        // Layout settings
        private const float DialogWidth = 600f;
        private const float DialogHeight = 200f;
        private const float ButtonWidth = 150f;
        private const float ButtonHeight = 60f;
        private const float ButtonSpacing = 30f;

        // Colors
        private readonly Color BackgroundColor = Color.Black * 0.95f;
        private readonly Color BorderColor = Color.White;
        private readonly Color HoverColor = Color.Yellow;
        private readonly Color YesColor = Color.Green;
        private readonly Color NoColor = Color.Red;

        // Events
        public event Action OnYes;
        public event Action OnNo;

        public bool IsActive => isActive;

        public ConfirmationDialogUI()
        {
            previousMouse = Mouse.GetState();
        }

        /// <summary>
        /// Show the confirmation dialog with a message
        /// </summary>
        public void Show(string message)
        {
            this.message = message;
            isActive = true;
            hoveredButton = -1;
            previousMouse = Mouse.GetState();
            Console.WriteLine($"[ConfirmationDialogUI] Shown: {message}");
        }

        /// <summary>
        /// Hide the dialog
        /// </summary>
        public void Hide()
        {
            isActive = false;
            Console.WriteLine("[ConfirmationDialogUI] Hidden");
        }

        /// <summary>
        /// Update input handling
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!isActive) return;

            var mouse = Mouse.GetState();

            // Check hover state
            hoveredButton = -1;
            if (yesButtonRect.Contains(mouse.X, mouse.Y))
            {
                hoveredButton = 0;
            }
            else if (noButtonRect.Contains(mouse.X, mouse.Y))
            {
                hoveredButton = 1;
            }

            // Check for mouse click
            if (mouse.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed)
            {
                if (yesButtonRect.Contains(mouse.X, mouse.Y))
                {
                    Console.WriteLine("[ConfirmationDialogUI] Yes selected");
                    OnYes?.Invoke();
                    Hide();
                }
                else if (noButtonRect.Contains(mouse.X, mouse.Y))
                {
                    Console.WriteLine("[ConfirmationDialogUI] No selected");
                    OnNo?.Invoke();
                    Hide();
                }
            }

            previousMouse = mouse;
        }

        /// <summary>
        /// Draw the confirmation dialog
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (!isActive || font == null) return;

            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            // Center dialog on screen
            float dialogX = (viewport.Width - DialogWidth) / 2f;
            float dialogY = (viewport.Height - DialogHeight) / 2f;

            // Draw background
            Rectangle bgRect = new Rectangle((int)dialogX, (int)dialogY, (int)DialogWidth, (int)DialogHeight);
            DrawFilledRectangle(spriteBatch, bgRect, BackgroundColor);
            DrawRectangleBorder(spriteBatch, bgRect, BorderColor, 3);

            // Draw message text
            Vector2 messageSize = font.MeasureString(message);
            Vector2 messagePos = new Vector2(
                dialogX + (DialogWidth - messageSize.X) / 2f,
                dialogY + 40f
            );
            spriteBatch.DrawString(font, message, messagePos + Vector2.One, Color.Black);
            spriteBatch.DrawString(font, message, messagePos, Color.White);

            // Calculate button positions
            float buttonsY = dialogY + DialogHeight - ButtonHeight - 30f;
            float totalButtonsWidth = (ButtonWidth * 2) + ButtonSpacing;
            float buttonsStartX = dialogX + (DialogWidth - totalButtonsWidth) / 2f;

            float yesButtonX = buttonsStartX;
            float noButtonX = buttonsStartX + ButtonWidth + ButtonSpacing;

            yesButtonRect = new Rectangle((int)yesButtonX, (int)buttonsY, (int)ButtonWidth, (int)ButtonHeight);
            noButtonRect = new Rectangle((int)noButtonX, (int)buttonsY, (int)ButtonWidth, (int)ButtonHeight);

            // Draw Yes button
            DrawButton(spriteBatch, font, yesButtonRect, "Yes", hoveredButton == 0, YesColor);

            // Draw No button
            DrawButton(spriteBatch, font, noButtonRect, "No", hoveredButton == 1, NoColor);
        }

        private void DrawButton(SpriteBatch spriteBatch, SpriteFont font, Rectangle rect, string text, bool isHovered, Color normalColor)
        {
            // Draw background
            DrawFilledRectangle(spriteBatch, rect, Color.Black * 0.9f);

            // Draw border (yellow when hovered, normal color otherwise)
            Color borderColor = isHovered ? HoverColor : normalColor;
            DrawRectangleBorder(spriteBatch, rect, borderColor, isHovered ? 3 : 2);

            // Draw text (yellow when hovered, normal color otherwise)
            Color textColor = isHovered ? HoverColor : normalColor;
            Vector2 textSize = font.MeasureString(text);
            Vector2 textPos = new Vector2(
                rect.X + (rect.Width - textSize.X) / 2f,
                rect.Y + (rect.Height - textSize.Y) / 2f
            );
            spriteBatch.DrawString(font, text, textPos + Vector2.One, Color.Black);
            spriteBatch.DrawString(font, text, textPos, textColor);
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
