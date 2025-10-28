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
        private List<int> selectedIndices = new List<int>(); // Track up to 2 selections
        private bool isActive = false;
        private KeyboardState previousKeyboard;

        // Grid display settings
        private const int GridColumns = 4;
        private const float BoxPadding = 30f;
        private const float PortraitSize = 128f;
        private const float ItemSpacing = 20f;
        private readonly Color BackgroundColor = Color.Black * 0.90f;
        private readonly Color SelectedColor = Color.Yellow;
        private readonly Color ConfirmedColor = Color.Green;
        private readonly Color NormalColor = Color.White;
        private readonly Color InterrogatedColor = Color.Gray;
        private readonly Color RoleColor = Color.LightGray;

        // Events
        public event Action<List<SelectableCharacter>> OnCharactersSelected;
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
                new SelectableCharacter("Commander Sylara Von", "CommanderSylar", "Security Chief"),
                new SelectableCharacter("Dr. Lyssa Thorne", "NPC_DrThorne", "Xenopathologist"),
                new SelectableCharacter("Lieutenant Marcus Webb", "LtWebb", "Navigation Officer"),
                new SelectableCharacter("Ensign Tork", "EnsignTork", "Junior Engineer"),
                new SelectableCharacter("Maven Kilroth", "MavenKilroth", "Smuggler"),
                new SelectableCharacter("Chief Kala Solis", "ChiefSolis", "Security Chief"),
                new SelectableCharacter("Tehvora", "Tehvora", "Diplomatic Attaché"),
                new SelectableCharacter("Lucky Chen", "LuckyChen", "Quartermaster"),
            };
        }

        /// <summary>
        /// Shows the character selection menu
        /// </summary>
        public void Show()
        {
            isActive = true;
            selectedIndex = 0;
            selectedIndices.Clear();
            Console.WriteLine("CharacterSelectionMenu: Opened - Select up to 2 characters");
        }

        /// <summary>
        /// Hides the character selection menu
        /// </summary>
        public void Hide()
        {
            isActive = false;
            selectedIndices.Clear();
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

            // Grid navigation - Up
            if (keyboard.IsKeyDown(Keys.Up) && !previousKeyboard.IsKeyDown(Keys.Up))
            {
                selectedIndex -= GridColumns;
                if (selectedIndex < 0)
                    selectedIndex = characters.Count + (selectedIndex % GridColumns);
            }

            // Grid navigation - Down
            if (keyboard.IsKeyDown(Keys.Down) && !previousKeyboard.IsKeyDown(Keys.Down))
            {
                selectedIndex += GridColumns;
                if (selectedIndex >= characters.Count)
                    selectedIndex = selectedIndex % GridColumns;
            }

            // Grid navigation - Left
            if (keyboard.IsKeyDown(Keys.Left) && !previousKeyboard.IsKeyDown(Keys.Left))
            {
                selectedIndex--;
                if (selectedIndex < 0)
                    selectedIndex = characters.Count - 1;
            }

            // Grid navigation - Right
            if (keyboard.IsKeyDown(Keys.Right) && !previousKeyboard.IsKeyDown(Keys.Right))
            {
                selectedIndex++;
                if (selectedIndex >= characters.Count)
                    selectedIndex = 0;
            }

            // Toggle selection with E or Space
            if ((keyboard.IsKeyDown(Keys.E) && !previousKeyboard.IsKeyDown(Keys.E)) ||
                (keyboard.IsKeyDown(Keys.Space) && !previousKeyboard.IsKeyDown(Keys.Space)))
            {
                if (selectedIndices.Contains(selectedIndex))
                {
                    // Deselect
                    selectedIndices.Remove(selectedIndex);
                    Console.WriteLine($"CharacterSelectionMenu: Deselected {SelectedCharacter?.Name}");
                }
                else if (selectedIndices.Count < 2)
                {
                    // Select (max 2)
                    selectedIndices.Add(selectedIndex);
                    Console.WriteLine($"CharacterSelectionMenu: Selected {SelectedCharacter?.Name} ({selectedIndices.Count}/2)");
                }
            }

            // Confirm selection with Enter
            if (keyboard.IsKeyDown(Keys.Enter) && !previousKeyboard.IsKeyDown(Keys.Enter))
            {
                if (selectedIndices.Count > 0)
                {
                    var selected = new List<SelectableCharacter>();
                    foreach (var index in selectedIndices)
                    {
                        characters[index].IsInterrogated = true;
                        selected.Add(characters[index]);
                    }
                    Console.WriteLine($"CharacterSelectionMenu: Confirmed {selected.Count} character(s) for interrogation");
                    OnCharactersSelected?.Invoke(selected);
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
        /// Draws the character selection menu in a grid layout
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Dictionary<string, Texture2D> portraits)
        {
            if (!isActive || font == null)
                return;

            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            // Calculate grid dimensions
            int rows = (int)Math.Ceiling((float)characters.Count / GridColumns);
            float cellWidth = PortraitSize + ItemSpacing;
            float cellHeight = PortraitSize + 60 + ItemSpacing; // Portrait + name/role space
            float menuWidth = (cellWidth * GridColumns) + BoxPadding * 2;
            float menuHeight = (cellHeight * rows) + BoxPadding * 3 + 80; // Extra space for title and hints

            // Center the menu
            float menuX = (viewport.Width - menuWidth) / 2;
            float menuY = (viewport.Height - menuHeight) / 2;

            // Draw background
            DrawFilledRectangle(spriteBatch, new Rectangle((int)menuX, (int)menuY, (int)menuWidth, (int)menuHeight), BackgroundColor);

            // Draw border
            DrawRectangleBorder(spriteBatch, new Rectangle((int)menuX, (int)menuY, (int)menuWidth, (int)menuHeight), Color.White, 3);

            // Draw title
            string title = "SELECT SUSPECTS (Max 2)";
            var titleSize = font.MeasureString(title) * 0.8f;
            Vector2 titlePos = new Vector2(menuX + (menuWidth - titleSize.X) / 2, menuY + BoxPadding / 2);
            spriteBatch.DrawString(font, title, titlePos, SelectedColor, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

            // Draw selection counter
            string counter = $"Selected: {selectedIndices.Count}/2";
            var counterSize = font.MeasureString(counter) * 0.6f;
            Vector2 counterPos = new Vector2(menuX + (menuWidth - counterSize.X) / 2, titlePos.Y + titleSize.Y + 5);
            spriteBatch.DrawString(font, counter, counterPos, Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

            // Draw character grid
            float startY = menuY + BoxPadding + titleSize.Y + counterSize.Y + 20;
            for (int i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                int row = i / GridColumns;
                int col = i % GridColumns;

                float cellX = menuX + BoxPadding + (col * cellWidth);
                float cellY = startY + (row * cellHeight);

                bool isHovered = i == selectedIndex;
                bool isConfirmed = selectedIndices.Contains(i);

                // Draw selection highlight
                Color borderColor = isConfirmed ? ConfirmedColor : (isHovered ? SelectedColor : Color.Transparent);
                if (borderColor != Color.Transparent)
                {
                    DrawRectangleBorder(spriteBatch,
                        new Rectangle((int)cellX - 4, (int)cellY - 4, (int)PortraitSize + 8, (int)PortraitSize + 68),
                        borderColor, 3);
                }

                // Draw portrait
                if (portraits != null && portraits.ContainsKey(character.PortraitKey))
                {
                    var portrait = portraits[character.PortraitKey];
                    Rectangle portraitRect = new Rectangle((int)cellX, (int)cellY, (int)PortraitSize, (int)PortraitSize);
                    spriteBatch.Draw(portrait, portraitRect, Color.White);
                }

                // Draw character name (scaled and centered)
                float textScale = 0.5f;
                Vector2 nameSize = font.MeasureString(character.Name) * textScale;
                Vector2 namePos = new Vector2(cellX + (PortraitSize - nameSize.X) / 2, cellY + PortraitSize + 5);
                spriteBatch.DrawString(font, character.Name, namePos, NormalColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

                // Draw character role (scaled and centered)
                Vector2 roleSize = font.MeasureString(character.Role) * textScale;
                Vector2 rolePos = new Vector2(cellX + (PortraitSize - roleSize.X) / 2, namePos.Y + nameSize.Y + 2);
                spriteBatch.DrawString(font, character.Role, rolePos, RoleColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

                // Draw interrogated overlay
                if (character.IsInterrogated)
                {
                    DrawFilledRectangle(spriteBatch,
                        new Rectangle((int)cellX, (int)cellY, (int)PortraitSize, (int)PortraitSize),
                        Color.Black * 0.6f);
                    string status = "DONE";
                    Vector2 statusSize = font.MeasureString(status) * 0.6f;
                    Vector2 statusPos = new Vector2(cellX + (PortraitSize - statusSize.X) / 2, cellY + (PortraitSize - statusSize.Y) / 2);
                    spriteBatch.DrawString(font, status, statusPos, InterrogatedColor, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                }
            }

            // Draw controls hint at bottom
            string hint = "[Arrows] Navigate  [E/Space] Toggle  [Enter] Confirm  [ESC] Cancel";
            var hintSize = font.MeasureString(hint) * 0.6f;
            Vector2 hintPos = new Vector2(menuX + (menuWidth - hintSize.X) / 2, menuY + menuHeight - BoxPadding);
            spriteBatch.DrawString(font, hint, hintPos, Color.Gray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
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
