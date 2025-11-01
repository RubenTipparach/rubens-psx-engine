using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;

namespace anakinsoft.game.scenes.lounge.ui
{
    /// <summary>
    /// Represents an interrogation action the player can take
    /// </summary>
    public enum InterrogationAction
    {
        Alibi,
        Relationship,
        Doubt,
        Accuse,
        StepAway,
        Dismiss
    }

    /// <summary>
    /// UI for selecting interrogation actions (Alibi, Relationship, Doubt, Accuse, Dismiss)
    /// Displays as numbered boxes at the bottom of the screen
    /// </summary>
    public class InterrogationActionUI
    {
        private bool isActive = false;
        private int hoveredIndex = -1; // -1 means no hover
        private MouseState previousMouse;
        private Rectangle[] buttonRects = new Rectangle[6]; // Store button rectangles for hit testing (6 buttons now)

        // Layout settings - 2 buttons per row
        private const float ButtonWidth = 350f;
        private const float ButtonHeight = 80f;
        private const float ButtonSpacingX = 30f;
        private const float ButtonSpacingY = 20f;
        private const float BottomRowButtonWidth = 350f; // Step Away and Dismiss on bottom row
        private const float BottomMargin = 40f;
        private const float BottomRowMarginTop = 30f;

        // Colors
        private readonly Color BackgroundColor = Color.Black * 0.8f;
        private readonly Color HoverColor = Color.Yellow;
        private readonly Color UnselectedColor = Color.White;
        private readonly Color BorderColor = Color.White;
        private readonly Color DismissColor = Color.Red;

        // Action data (removed numbers)
        private readonly string[] actionLabels = new string[]
        {
            "Alibi",
            "Relationship",
            "Doubt",
            "Accuse",
            "Step Away",
            "Dismiss"
        };

        private readonly string[] actionDescriptions = new string[]
        {
            "Where were you?",
            "Your relationship?",
            "I don't believe you",
            "Present evidence",
            "Check evidence",
            "That's all for now"
        };

        // Events
        public event Action<InterrogationAction> OnActionSelected;
        public event Action OnCancelled;

        public bool IsActive => isActive;

        public InterrogationActionUI()
        {
            previousMouse = Mouse.GetState();
        }

        /// <summary>
        /// Show the interrogation action menu
        /// </summary>
        public void Show()
        {
            isActive = true;
            hoveredIndex = -1;
            previousMouse = Mouse.GetState();
            Console.WriteLine("[InterrogationActionUI] Shown");
        }

        /// <summary>
        /// Hide the interrogation action menu
        /// </summary>
        public void Hide()
        {
            isActive = false;
            Console.WriteLine("[InterrogationActionUI] Hidden");
        }

        /// <summary>
        /// Update mouse input handling
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!isActive) return;

            var mouse = Mouse.GetState();

            // Check hover state
            hoveredIndex = -1;
            for (int i = 0; i < buttonRects.Length; i++)
            {
                if (buttonRects[i].Contains(mouse.X, mouse.Y))
                {
                    hoveredIndex = i;
                    break;
                }
            }

            // Check for mouse click
            if (mouse.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed)
            {
                // Check if click was on a button
                for (int i = 0; i < buttonRects.Length; i++)
                {
                    if (buttonRects[i].Contains(mouse.X, mouse.Y))
                    {
                        SelectAction(i);
                        break;
                    }
                }
            }

            previousMouse = mouse;
        }

        /// <summary>
        /// Select an action and trigger event
        /// </summary>
        private void SelectAction(int index)
        {
            if (index < 0 || index >= actionLabels.Length) return;

            InterrogationAction action = (InterrogationAction)index;
            Console.WriteLine($"[InterrogationActionUI] Action selected: {action}");

            OnActionSelected?.Invoke(action);
            Hide();
        }

        /// <summary>
        /// Draw the interrogation action UI
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (!isActive || font == null) return;

            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            // Layout: 2 buttons per row
            // Row 1: Alibi, Relationship
            // Row 2: Doubt, Accuse
            // Row 3: Step Away, Dismiss

            float totalWidth = (ButtonWidth * 2) + ButtonSpacingX;
            float startX = (viewport.Width - totalWidth) / 2f;

            // Calculate starting Y (3 rows of buttons + spacing + margin)
            float totalHeight = (ButtonHeight * 3) + (ButtonSpacingY * 2) + BottomRowMarginTop;
            float startY = viewport.Height - BottomMargin - totalHeight;

            // Row 1: Alibi (0) and Relationship (1)
            float row1Y = startY;
            buttonRects[0] = new Rectangle((int)startX, (int)row1Y, (int)ButtonWidth, (int)ButtonHeight);
            buttonRects[1] = new Rectangle((int)(startX + ButtonWidth + ButtonSpacingX), (int)row1Y, (int)ButtonWidth, (int)ButtonHeight);
            DrawActionButton(spriteBatch, font, 0, startX, row1Y, ButtonWidth, ButtonHeight);
            DrawActionButton(spriteBatch, font, 1, startX + ButtonWidth + ButtonSpacingX, row1Y, ButtonWidth, ButtonHeight);

            // Row 2: Doubt (2) and Accuse (3)
            float row2Y = startY + ButtonHeight + ButtonSpacingY;
            buttonRects[2] = new Rectangle((int)startX, (int)row2Y, (int)ButtonWidth, (int)ButtonHeight);
            buttonRects[3] = new Rectangle((int)(startX + ButtonWidth + ButtonSpacingX), (int)row2Y, (int)ButtonWidth, (int)ButtonHeight);
            DrawActionButton(spriteBatch, font, 2, startX, row2Y, ButtonWidth, ButtonHeight);
            DrawActionButton(spriteBatch, font, 3, startX + ButtonWidth + ButtonSpacingX, row2Y, ButtonWidth, ButtonHeight);

            // Row 3: Step Away (4) and Dismiss (5)
            float row3Y = row2Y + ButtonHeight + BottomRowMarginTop;
            buttonRects[4] = new Rectangle((int)startX, (int)row3Y, (int)BottomRowButtonWidth, (int)ButtonHeight);
            buttonRects[5] = new Rectangle((int)(startX + BottomRowButtonWidth + ButtonSpacingX), (int)row3Y, (int)BottomRowButtonWidth, (int)ButtonHeight);
            DrawActionButton(spriteBatch, font, 4, startX, row3Y, BottomRowButtonWidth, ButtonHeight);
            DrawActionButton(spriteBatch, font, 5, startX + BottomRowButtonWidth + ButtonSpacingX, row3Y, BottomRowButtonWidth, ButtonHeight);

            // Draw instruction text above buttons
            string instruction = "Click to select action";
            var instructionSize = font.MeasureString(instruction);
            Vector2 instructionPos = new Vector2(
                (viewport.Width - instructionSize.X) / 2f,
                startY - instructionSize.Y - 15f
            );
            spriteBatch.DrawString(font, instruction, instructionPos + Vector2.One, Color.Black);
            spriteBatch.DrawString(font, instruction, instructionPos, Color.White * 0.7f);
        }

        /// <summary>
        /// Draw a single action button
        /// </summary>
        private void DrawActionButton(SpriteBatch spriteBatch, SpriteFont font, int index, float x, float y, float width, float height)
        {
            bool isHovered = (index == hoveredIndex);
            bool isDismiss = (index == 5); // Dismiss is now at index 5

            // Draw black background
            Rectangle bgRect = new Rectangle((int)x, (int)y, (int)width, (int)height);
            DrawFilledRectangle(spriteBatch, bgRect, Color.Black * 0.9f);

            // Draw border (yellow on hover, red for dismiss default, yellow on dismiss hover)
            Color borderColor;
            if (isHovered)
            {
                borderColor = HoverColor; // Yellow when hovering (any button including dismiss)
            }
            else if (isDismiss)
            {
                borderColor = DismissColor; // Red for dismiss when not hovering
            }
            else
            {
                borderColor = BorderColor; // White for normal buttons when not hovering
            }
            DrawRectangleBorder(spriteBatch, bgRect, borderColor, isHovered ? 3 : 2);

            // Draw label
            string label = actionLabels[index];
            string description = actionDescriptions[index];

            var labelSize = font.MeasureString(label);
            var descSize = font.MeasureString(description);

            // Center label
            Vector2 labelPos = new Vector2(
                x + (width - labelSize.X) / 2f,
                y + 10f
            );

            // Center description
            Vector2 descPos = new Vector2(
                x + (width - descSize.X) / 2f,
                y + height - descSize.Y - 10f
            );

            // Draw label (yellow when hovered, red for dismiss when not hovered)
            Color textColor;
            if (isHovered)
            {
                textColor = HoverColor; // Yellow when hovering (any button including dismiss)
            }
            else if (isDismiss)
            {
                textColor = DismissColor; // Red for dismiss when not hovering
            }
            else
            {
                textColor = UnselectedColor; // White for normal buttons when not hovering
            }

            spriteBatch.DrawString(font, label, labelPos + Vector2.One, Color.Black);
            spriteBatch.DrawString(font, label, labelPos, textColor);

            // Draw description
            spriteBatch.DrawString(font, description, descPos + Vector2.One, Color.Black);
            spriteBatch.DrawString(font, description, descPos, textColor * 0.7f);
        }

        private void DrawFilledRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            var texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            texture.SetData(new[] { Color.White });
            spriteBatch.Draw(texture, rect, color);
            // Note: Don't dispose - SpriteBatch uses deferred rendering
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

            // Note: Don't dispose - SpriteBatch uses deferred rendering
        }
    }
}
