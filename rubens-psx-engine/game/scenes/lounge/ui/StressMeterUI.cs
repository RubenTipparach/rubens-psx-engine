using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine;
using System;
using System.Collections.Generic;

namespace anakinsoft.game.scenes.lounge.ui
{
    /// <summary>
    /// Displays the stress meter UI during interrogation with portrait, name, occupation
    /// </summary>
    public class StressMeterUI
    {
        private StressMeter stressMeter;
        private string characterName;
        private string characterOccupation;
        private string portraitKey; // Key to look up portrait in dictionary
        private Texture2D portraitTexture;
        private bool isVisible = false;

        // UI settings
        private const float PanelWidth = 280f;
        private const float PanelPadding = 15f;
        private const float TopMargin = 20f;
        private const float RightMargin = 20f;
        private const float TextScale = 0.7f; // Scale down text for better fit

        // Portrait dimensions - aspect ratio 64:96 (2:3)
        private const float PortraitWidth = 96f;
        private const float PortraitHeight = 144f;
        private const float BarWidth = PanelWidth - (PanelPadding * 2);
        private const float BarHeight = 24f;
        private const float BarInnerPadding = 3f;
        private const float ElementSpacing = 10f;

        // Colors
        private readonly Color PanelBackgroundColor = Color.Black * 0.85f;
        private readonly Color PanelBorderColor = Color.White * 0.8f;
        private readonly Color BarBackgroundColor = Color.Black * 0.9f;
        private readonly Color LowStressColor = new Color(50, 200, 50); // Green
        private readonly Color MediumStressColor = new Color(200, 200, 50); // Yellow
        private readonly Color HighStressColor = new Color(200, 50, 50); // Red
        private readonly Color TextColor = Color.White;
        private readonly Color TextShadowColor = Color.Black;

        public bool IsVisible => isVisible;

        // Cache for textures to avoid recreation
        private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

        public StressMeterUI()
        {
        }

        /// <summary>
        /// Show stress meter for a character
        /// </summary>
        public void Show(StressMeter meter, string charName, string occupation = null, string charPortraitKey = null, Texture2D portrait = null)
        {
            stressMeter = meter;
            characterName = charName;
            characterOccupation = occupation ?? "Suspect";
            portraitKey = charPortraitKey ?? charName; // Use portraitKey if provided, otherwise fallback to name
            portraitTexture = portrait;
            isVisible = true;
            Console.WriteLine($"[StressMeterUI] Showing stress meter for {characterName} (portrait key: {portraitKey})");
        }

        /// <summary>
        /// Hide stress meter
        /// </summary>
        public void Hide()
        {
            isVisible = false;
            stressMeter = null;
            characterName = null;
            characterOccupation = null;
            portraitKey = null;
            portraitTexture = null;
            Console.WriteLine($"[StressMeterUI] Hiding stress meter");
        }

        /// <summary>
        /// Draw the stress meter UI
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Dictionary<string, Texture2D> portraits = null)
        {
            if (!isVisible || stressMeter == null || font == null)
                return;

            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            // Calculate panel position (top right corner)
            float panelX = viewport.Width - PanelWidth - RightMargin;
            float panelY = TopMargin;

            // Calculate total panel height (account for text scaling)
            float panelHeight = PanelPadding + PortraitHeight + ElementSpacing +
                               (font.MeasureString("Name").Y * TextScale) + ElementSpacing +
                               (font.MeasureString("Occupation").Y * TextScale) + ElementSpacing +
                               BarHeight + ElementSpacing +
                               (font.MeasureString("Stress: 100%").Y * TextScale) + PanelPadding;

            // Draw panel background
            Rectangle panelRect = new Rectangle((int)panelX, (int)panelY, (int)PanelWidth, (int)panelHeight);
            DrawFilledRectangle(spriteBatch, panelRect, PanelBackgroundColor);
            DrawRectangleBorder(spriteBatch, panelRect, PanelBorderColor, 2);

            float currentY = panelY + PanelPadding;

            // Draw portrait with 64:96 aspect ratio
            float portraitX = panelX + (PanelWidth - PortraitWidth) / 2f;
            Rectangle portraitRect = new Rectangle((int)portraitX, (int)currentY, (int)PortraitWidth, (int)PortraitHeight);

            if (portraitTexture != null)
            {
                spriteBatch.Draw(portraitTexture, portraitRect, Color.White);
            }
            else if (portraits != null && !string.IsNullOrEmpty(portraitKey) && portraits.ContainsKey(portraitKey))
            {
                spriteBatch.Draw(portraits[portraitKey], portraitRect, Color.White);
            }
            else
            {
                // Draw placeholder - no portrait found
                DrawFilledRectangle(spriteBatch, portraitRect, Color.Gray * 0.5f);
                DrawRectangleBorder(spriteBatch, portraitRect, Color.White * 0.5f, 1);
            }

            currentY += PortraitHeight + ElementSpacing;

            // Draw character name (scaled)
            var nameSize = font.MeasureString(characterName) * TextScale;
            Vector2 namePos = new Vector2(panelX + (PanelWidth - nameSize.X) / 2f, currentY);
            DrawTextWithShadow(spriteBatch, font, characterName, namePos, TextColor, TextShadowColor, TextScale);
            currentY += nameSize.Y + ElementSpacing;

            // Draw occupation (scaled)
            var occupationSize = font.MeasureString(characterOccupation) * TextScale;
            Vector2 occupationPos = new Vector2(panelX + (PanelWidth - occupationSize.X) / 2f, currentY);
            DrawTextWithShadow(spriteBatch, font, characterOccupation, occupationPos, TextColor * 0.8f, TextShadowColor, TextScale);
            currentY += occupationSize.Y + ElementSpacing;

            // Draw stress bar
            float barX = panelX + PanelPadding;
            DrawStressBar(spriteBatch, barX, currentY, BarWidth, BarHeight);
            currentY += BarHeight + ElementSpacing;

            // Draw stress percentage text (scaled)
            float stressPercentage = stressMeter.StressPercentage;
            string stressText = $"Stress: {stressPercentage:F0}%";
            var stressTextSize = font.MeasureString(stressText) * TextScale;
            Vector2 stressTextPos = new Vector2(panelX + (PanelWidth - stressTextSize.X) / 2f, currentY);

            // Color text based on stress level
            Color stressTextColor = GetStressColor(stressPercentage);
            DrawTextWithShadow(spriteBatch, font, stressText, stressTextPos, stressTextColor, TextShadowColor, TextScale);
        }

        /// <summary>
        /// Draw the stress progress bar
        /// </summary>
        private void DrawStressBar(SpriteBatch spriteBatch, float x, float y, float width, float height)
        {
            // Draw outer bar background (black)
            Rectangle outerRect = new Rectangle((int)x, (int)y, (int)width, (int)height);
            DrawFilledRectangle(spriteBatch, outerRect, BarBackgroundColor);
            DrawRectangleBorder(spriteBatch, outerRect, Color.White * 0.6f, 2);

            // Calculate inner fill
            float stressPercentage = stressMeter.StressPercentage;
            float fillWidth = (width - BarInnerPadding * 2) * (stressPercentage / 100f);

            if (fillWidth > 0)
            {
                Rectangle innerRect = new Rectangle(
                    (int)(x + BarInnerPadding),
                    (int)(y + BarInnerPadding),
                    (int)fillWidth,
                    (int)(height - BarInnerPadding * 2)
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
        /// Draw text with shadow
        /// </summary>
        private void DrawTextWithShadow(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, Color color, Color shadowColor, float scale = 1.0f)
        {
            spriteBatch.DrawString(font, text, position + new Vector2(1, 1), shadowColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void DrawFilledRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            var texture = GetOrCreateTexture(spriteBatch.GraphicsDevice, "white_1x1");
            spriteBatch.Draw(texture, rect, color);
        }

        private void DrawRectangleBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            var texture = GetOrCreateTexture(spriteBatch.GraphicsDevice, "white_1x1");

            // Top
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            spriteBatch.Draw(texture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
        }

        private Texture2D GetOrCreateTexture(GraphicsDevice gd, string key)
        {
            if (!textureCache.ContainsKey(key))
            {
                var texture = new Texture2D(gd, 1, 1);
                texture.SetData(new[] { Color.White });
                textureCache[key] = texture;
            }
            return textureCache[key];
        }
    }
}
