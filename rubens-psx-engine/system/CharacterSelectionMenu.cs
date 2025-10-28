using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;
using System.Collections.Generic;

namespace anakinsoft.system
{
    /// <summary>
    /// Represents a selectable character for interrogation
    /// </summary>
    public class SelectableCharacter
    {
        public string Name { get; set; }
        public string PortraitKey { get; set; }
        public string Role { get; set; }
        public bool IsInterrogated { get; set; }

        public SelectableCharacter(string name, string portraitKey, string role)
        {
            Name = name;
            PortraitKey = portraitKey;
            Role = role;
            IsInterrogated = false;
        }
    }

    /// <summary>
    /// Character selection menu for interrogation
    /// </summary>
    public class CharacterSelectionMenu
    {
        private List<SelectableCharacter> characters;
        private int selectedIndex = 0;
        private bool isActive = false;
        private KeyboardState previousKeyboard;

        // Display settings
        private const float BoxPadding = 30f;
        private const float ItemHeight = 100f;
        private const float ItemSpacing = 20f;
        private readonly Color BackgroundColor = Color.Black * 0.90f;
        private readonly Color SelectedColor = Color.Yellow;
        private readonly Color NormalColor = Color.White;
        private readonly Color InterrogatedColor = Color.Gray;
        private readonly Color RoleColor = Color.LightGray;

        // Events
        public event Action<SelectableCharacter> OnCharacterSelected;
        public event Action OnMenuClosed;

        public bool IsActive => isActive;
        public SelectableCharacter SelectedCharacter =>
            characters != null && selectedIndex >= 0 && selectedIndex < characters.Count
                ? characters[selectedIndex]
                : null;

        public CharacterSelectionMenu()
        {
            InitializeCharacters();
        }

        private void InitializeCharacters()
        {
            characters = new List<SelectableCharacter>
            {
                // Start with 2 suspects as requested
                new SelectableCharacter("Commander Sylara Von", "CommanderSylar", "Ambassador's Head of Security"),
                new SelectableCharacter("Dr. Lyssa Thorne", "NPC_DrThorne", "Xenoanthropologist & Cultural Liaison"),

                // Additional suspects (can be unlocked later)
                // new SelectableCharacter("Lieutenant Marcus Webb", "LtWebb", "Tactical Officer"),
                // new SelectableCharacter("Ensign Tork", "EnsignTork", "Junior Engineer"),
                // new SelectableCharacter("Maven Kilroth", "MavenKilroth", "Trade Negotiator"),
                // new SelectableCharacter("Chief Raina Solis", "ChiefSolis", "Head of Ship Security"),
                // new SelectableCharacter("T'Vora", "Tehvora", "Federation Diplomatic Attaché"),
                // new SelectableCharacter("Lucky Chen", "LuckyChen", "Ship's Quartermaster"),
            };
        }

        /// <summary>
        /// Shows the character selection menu
        /// </summary>
        public void Show()
        {
            isActive = true;
            selectedIndex = 0;
            Console.WriteLine("CharacterSelectionMenu: Opened");
        }

        /// <summary>
        /// Hides the character selection menu
        /// </summary>
        public void Hide()
        {
            isActive = false;
            Console.WriteLine("CharacterSelectionMenu: Closed");
            OnMenuClosed?.Invoke();
        }

        /// <summary>
        /// Updates the menu (handles input)
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!isActive)
                return;

            var keyboard = Keyboard.GetState();

            // Navigate up
            if (keyboard.IsKeyDown(Keys.Up) && !previousKeyboard.IsKeyDown(Keys.Up))
            {
                selectedIndex--;
                if (selectedIndex < 0)
                    selectedIndex = characters.Count - 1;
            }

            // Navigate down
            if (keyboard.IsKeyDown(Keys.Down) && !previousKeyboard.IsKeyDown(Keys.Down))
            {
                selectedIndex++;
                if (selectedIndex >= characters.Count)
                    selectedIndex = 0;
            }

            // Select character with E or Enter
            if ((keyboard.IsKeyDown(Keys.E) && !previousKeyboard.IsKeyDown(Keys.E)) ||
                (keyboard.IsKeyDown(Keys.Enter) && !previousKeyboard.IsKeyDown(Keys.Enter)))
            {
                if (SelectedCharacter != null)
                {
                    Console.WriteLine($"CharacterSelectionMenu: Selected {SelectedCharacter.Name}");
                    SelectedCharacter.IsInterrogated = true;
                    OnCharacterSelected?.Invoke(SelectedCharacter);
                    Hide();
                }
            }

            // Close menu with Escape or Tab
            if ((keyboard.IsKeyDown(Keys.Escape) && !previousKeyboard.IsKeyDown(Keys.Escape)) ||
                (keyboard.IsKeyDown(Keys.Tab) && !previousKeyboard.IsKeyDown(Keys.Tab)))
            {
                Hide();
            }

            previousKeyboard = keyboard;
        }

        /// <summary>
        /// Draws the character selection menu
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Dictionary<string, Texture2D> portraits)
        {
            if (!isActive || font == null)
                return;

            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            // Calculate menu dimensions
            float menuWidth = 600f;
            float menuHeight = (ItemHeight + ItemSpacing) * characters.Count + BoxPadding * 2;

            // Center the menu
            float menuX = (viewport.Width - menuWidth) / 2;
            float menuY = (viewport.Height - menuHeight) / 2;

            // Draw background
            DrawFilledRectangle(spriteBatch, new Rectangle((int)menuX, (int)menuY, (int)menuWidth, (int)menuHeight), BackgroundColor);

            // Draw border
            DrawRectangleBorder(spriteBatch, new Rectangle((int)menuX, (int)menuY, (int)menuWidth, (int)menuHeight), Color.White, 3);

            // Draw title
            string title = "SELECT CHARACTER TO INTERROGATE";
            var titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(menuX + (menuWidth - titleSize.X) / 2, menuY + BoxPadding);
            spriteBatch.DrawString(font, title, titlePos + Vector2.One, Color.Black); // Shadow
            spriteBatch.DrawString(font, title, titlePos, SelectedColor);

            // Draw character items
            float itemY = menuY + BoxPadding + titleSize.Y + 30;
            for (int i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                bool isSelected = i == selectedIndex;
                Color itemColor = character.IsInterrogated ? InterrogatedColor :
                                  isSelected ? SelectedColor : NormalColor;

                // Draw selection background
                if (isSelected)
                {
                    DrawFilledRectangle(spriteBatch,
                        new Rectangle((int)menuX + 10, (int)itemY - 5, (int)menuWidth - 20, (int)ItemHeight - 10),
                        Color.Yellow * 0.2f);
                }

                // Draw portrait if available
                if (portraits != null && portraits.ContainsKey(character.PortraitKey))
                {
                    var portrait = portraits[character.PortraitKey];
                    int portraitSize = 80;
                    Rectangle portraitRect = new Rectangle((int)menuX + 20, (int)itemY, portraitSize, portraitSize);
                    spriteBatch.Draw(portrait, portraitRect, Color.White);
                }

                // Draw character name
                Vector2 namePos = new Vector2(menuX + 120, itemY + 10);
                spriteBatch.DrawString(font, character.Name, namePos + Vector2.One, Color.Black); // Shadow
                spriteBatch.DrawString(font, character.Name, namePos, itemColor);

                // Draw character role
                Vector2 rolePos = new Vector2(menuX + 120, itemY + 40);
                spriteBatch.DrawString(font, character.Role, rolePos, RoleColor);

                // Draw interrogated status
                if (character.IsInterrogated)
                {
                    string status = "[INTERROGATED]";
                    Vector2 statusPos = new Vector2(menuX + 120, itemY + 65);
                    spriteBatch.DrawString(font, status, statusPos, InterrogatedColor);
                }

                itemY += ItemHeight + ItemSpacing;
            }

            // Draw controls hint at bottom
            string hint = "[↑↓] Navigate  [E] Select  [ESC] Cancel";
            var hintSize = font.MeasureString(hint);
            Vector2 hintPos = new Vector2(menuX + (menuWidth - hintSize.X) / 2, menuY + menuHeight - BoxPadding - hintSize.Y);
            spriteBatch.DrawString(font, hint, hintPos, Color.Gray);
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
