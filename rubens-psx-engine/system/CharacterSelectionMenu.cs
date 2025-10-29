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
        private int selectedIndex = -1; // Start with no selection
        private List<int> selectedIndices = new List<int>(); // Track up to 2 selections
        private bool isActive = false;
        private KeyboardState previousKeyboard;
        private MouseState previousMouse;
        private bool isInterrogationInProgress = false;
        private string warningMessage = "";
        private float warningTimer = 0f;
        private const float WarningDuration = 3f;

        // Track cell rectangles for mouse interaction
        private Dictionary<int, Rectangle> cellRectangles = new Dictionary<int, Rectangle>();
        private Dictionary<int, Rectangle> transcriptButtonRectangles = new Dictionary<int, Rectangle>();

        // Grid display settings
        private const int GridColumns = 4;
        private const float BoxPadding = 30f;
        private const float PortraitWidth = 64f;
        private const float PortraitHeight = 96f;
        private const float CellWidth = 180f;  // 40% wider than portrait (64 * 1.4 ≈ 90) for name space
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
        public event Action<SelectableCharacter> OnViewTranscript;

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
                new SelectableCharacter("Tehvora", "Tehvora", "Diplomatic Attache"),
                new SelectableCharacter("Lucky Chen", "LuckyChen", "Quartermaster"),
            };
        }

        /// <summary>
        /// Shows the character selection menu
        /// </summary>
        public void Show()
        {
            isActive = true;
            selectedIndex = -1; // No initial selection
            selectedIndices.Clear();
            warningMessage = "";
            warningTimer = 0f;
            Console.WriteLine("CharacterSelectionMenu: Opened - Select up to 2 characters");
        }

        /// <summary>
        /// Sets whether an interrogation is currently in progress
        /// </summary>
        public void SetInterrogationInProgress(bool inProgress)
        {
            isInterrogationInProgress = inProgress;
        }

        /// <summary>
        /// Shows a warning message to the player
        /// </summary>
        private void ShowWarning(string message)
        {
            warningMessage = message;
            warningTimer = WarningDuration;
            Console.WriteLine($"CharacterSelectionMenu: WARNING - {message}");
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

            // Update warning timer
            if (warningTimer > 0f)
            {
                warningTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (warningTimer <= 0f)
                {
                    warningMessage = "";
                }
            }

            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();

            // Mouse hover detection
            Point mousePosition = new Point(mouse.X, mouse.Y);
            int hoveredIndex = -1;
            foreach (var kvp in cellRectangles)
            {
                if (kvp.Value.Contains(mousePosition))
                {
                    hoveredIndex = kvp.Key;
                    break;
                }
            }

            // Update selected index based on mouse hover
            if (hoveredIndex != -1)
            {
                selectedIndex = hoveredIndex;
            }

            // Mouse click to toggle selection or view transcript
            if (mouse.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed)
            {
                // Check if clicking on transcript button first
                bool clickedTranscript = false;
                foreach (var kvp in transcriptButtonRectangles)
                {
                    if (kvp.Value.Contains(mousePosition) && characters[kvp.Key].IsInterrogated)
                    {
                        OnViewTranscript?.Invoke(characters[kvp.Key]);
                        clickedTranscript = true;
                        Console.WriteLine($"CharacterSelectionMenu: View transcript for {characters[kvp.Key]?.Name}");
                        break;
                    }
                }

                // Handle character selection if not clicking transcript button
                if (!clickedTranscript && hoveredIndex != -1)
                {
                    var character = characters[hoveredIndex];

                    // Check if already interrogated
                    if (character.IsInterrogated)
                    {
                        ShowWarning("Cannot select - Character already interrogated");
                    }
                    else if (selectedIndices.Contains(hoveredIndex))
                    {
                        // Deselect
                        selectedIndices.Remove(hoveredIndex);
                        Console.WriteLine($"CharacterSelectionMenu: Deselected {character.Name}");
                    }
                    else if (selectedIndices.Count < 2)
                    {
                        // Select (max 2)
                        selectedIndices.Add(hoveredIndex);
                        Console.WriteLine($"CharacterSelectionMenu: Selected {character.Name} ({selectedIndices.Count}/2)");
                    }
                    else
                    {
                        ShowWarning("Maximum 2 characters can be selected");
                    }
                }
            }

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
                if (selectedIndex >= 0 && selectedIndex < characters.Count)
                {
                    var character = characters[selectedIndex];

                    // Check if already interrogated
                    if (character.IsInterrogated)
                    {
                        ShowWarning("Cannot select - Character already interrogated");
                    }
                    else if (selectedIndices.Contains(selectedIndex))
                    {
                        // Deselect
                        selectedIndices.Remove(selectedIndex);
                        Console.WriteLine($"CharacterSelectionMenu: Deselected {character.Name}");
                    }
                    else if (selectedIndices.Count < 2)
                    {
                        // Select (max 2)
                        selectedIndices.Add(selectedIndex);
                        Console.WriteLine($"CharacterSelectionMenu: Selected {character.Name} ({selectedIndices.Count}/2)");
                    }
                    else
                    {
                        ShowWarning("Maximum 2 characters can be selected");
                    }
                }
            }

            // Confirm selection with Enter
            if (keyboard.IsKeyDown(Keys.Enter) && !previousKeyboard.IsKeyDown(Keys.Enter))
            {
                // Validate selection before confirming
                if (isInterrogationInProgress)
                {
                    ShowWarning("Cannot confirm - Interrogation in progress");
                }
                else if (selectedIndices.Count != 2)
                {
                    ShowWarning("Must select exactly 2 characters");
                }
                else
                {
                    // Check if any selected character is already interrogated
                    bool alreadyInterrogated = false;
                    foreach (var index in selectedIndices)
                    {
                        if (characters[index].IsInterrogated)
                        {
                            ShowWarning("Cannot confirm - Selected character already interrogated");
                            alreadyInterrogated = true;
                            break;
                        }
                    }

                    if (!alreadyInterrogated)
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
            }

            // Close menu with Tab (Escape reserved for pause menu)
            if (keyboard.IsKeyDown(Keys.Tab) && !previousKeyboard.IsKeyDown(Keys.Tab))
            {
                Hide();
            }

            previousKeyboard = keyboard;
            previousMouse = mouse;
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
            float cellWidth = CellWidth + ItemSpacing;
            float cellHeight = PortraitHeight + 60 + ItemSpacing; // Portrait + name/role space
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

            // Draw warning message if active
            if (!string.IsNullOrEmpty(warningMessage) && warningTimer > 0f)
            {
                string warning = $"WARNING: {warningMessage}";
                var warningSize = font.MeasureString(warning) * 0.7f;
                Vector2 warningPos = new Vector2(menuX + (menuWidth - warningSize.X) / 2, counterPos.Y + counterSize.Y + 10);

                // Flash warning
                float alpha = (float)Math.Sin(warningTimer * 10f) * 0.3f + 0.7f;
                Color warningColor = Color.Red * alpha;
                spriteBatch.DrawString(font, warning, warningPos, warningColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }

            // Draw character grid
            float startY = menuY + BoxPadding + titleSize.Y + counterSize.Y + 40;
            cellRectangles.Clear(); // Clear old rectangles
            transcriptButtonRectangles.Clear(); // Clear old transcript button rectangles

            for (int i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                int row = i / GridColumns;
                int col = i % GridColumns;

                float cellX = menuX + BoxPadding + (col * cellWidth);
                float cellY = startY + (row * cellHeight);

                // Store cell rectangle for mouse interaction (includes portrait + text area)
                Rectangle cellRect = new Rectangle(
                    (int)(cellX - 4),
                    (int)(cellY - 4),
                    (int)(CellWidth + 8),
                    (int)(PortraitHeight + 68)
                );
                cellRectangles[i] = cellRect;

                bool isHovered = i == selectedIndex;
                bool isConfirmed = selectedIndices.Contains(i);

                // Center portrait within the wider cell
                float portraitOffsetX = (CellWidth - PortraitWidth) / 2;

                // Draw selection highlight (wider to encompass entire cell)
                Color borderColor = isConfirmed ? ConfirmedColor : (isHovered ? SelectedColor : Color.Transparent);
                if (borderColor != Color.Transparent)
                {
                    DrawRectangleBorder(spriteBatch,
                        new Rectangle((int)cellX - 4, (int)cellY - 4, (int)CellWidth + 8, (int)PortraitHeight + 68),
                        borderColor, 3);
                }

                // Draw portrait (centered in cell)
                if (portraits != null && portraits.ContainsKey(character.PortraitKey))
                {
                    var portrait = portraits[character.PortraitKey];
                    Rectangle portraitRect = new Rectangle((int)(cellX + portraitOffsetX), (int)cellY, (int)PortraitWidth, (int)PortraitHeight);
                    spriteBatch.Draw(portrait, portraitRect, Color.White);
                }

                // Draw character name (scaled and centered within cell width)
                float textScale = 0.5f;
                Vector2 nameSize = font.MeasureString(character.Name) * textScale;
                Vector2 namePos = new Vector2(cellX + (CellWidth - nameSize.X) / 2, cellY + PortraitHeight + 5);
                spriteBatch.DrawString(font, character.Name, namePos, NormalColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

                // Draw character role (scaled and centered within cell width)
                Vector2 roleSize = font.MeasureString(character.Role) * textScale;
                Vector2 rolePos = new Vector2(cellX + (CellWidth - roleSize.X) / 2, namePos.Y + nameSize.Y + 2);
                spriteBatch.DrawString(font, character.Role, rolePos, RoleColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

                // Draw interrogated overlay and transcript button (centered in cell)
                if (character.IsInterrogated)
                {
                    DrawFilledRectangle(spriteBatch,
                        new Rectangle((int)(cellX + portraitOffsetX), (int)cellY, (int)PortraitWidth, (int)PortraitHeight),
                        Color.Black * 0.6f);

                    string status = "DONE";
                    Vector2 statusSize = font.MeasureString(status) * 0.6f;
                    Vector2 statusPos = new Vector2(cellX + portraitOffsetX + (PortraitWidth - statusSize.X) / 2, cellY + (PortraitHeight - statusSize.Y) / 2 - 10);
                    spriteBatch.DrawString(font, status, statusPos, InterrogatedColor, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

                    // Draw transcript button below status
                    string transcriptText = "[View]";
                    float transcriptScale = 0.45f;
                    Vector2 transcriptSize = font.MeasureString(transcriptText) * transcriptScale;
                    Vector2 transcriptPos = new Vector2(
                        cellX + portraitOffsetX + (PortraitWidth - transcriptSize.X) / 2,
                        statusPos.Y + statusSize.Y + 5
                    );

                    Rectangle transcriptButton = new Rectangle(
                        (int)(transcriptPos.X - 4),
                        (int)(transcriptPos.Y - 2),
                        (int)(transcriptSize.X + 8),
                        (int)(transcriptSize.Y + 4)
                    );
                    transcriptButtonRectangles[i] = transcriptButton;

                    // Highlight transcript button on hover
                    bool isTranscriptHovered = transcriptButton.Contains(new Point(Mouse.GetState().X, Mouse.GetState().Y));
                    Color transcriptColor = isTranscriptHovered ? SelectedColor : Color.LightGray;

                    DrawRectangleBorder(spriteBatch, transcriptButton, transcriptColor, 1);
                    spriteBatch.DrawString(font, transcriptText, transcriptPos, transcriptColor, 0f, Vector2.Zero, transcriptScale, SpriteEffects.None, 0f);
                }
            }

            // Draw controls hint at bottom
            string hint = "[Mouse/Arrows] Navigate  [Click/E/Space] Toggle  [Enter] Confirm  [Tab] Cancel";
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
