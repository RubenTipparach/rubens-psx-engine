using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using anakinsoft.game.scenes.lounge.characters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace anakinsoft.game.scenes.lounge.ui
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
        public bool IsDismissed { get; set; }

        public SelectableCharacter(string name, string portraitKey, string role)
        {
            Name = name;
            PortraitKey = portraitKey;
            Role = role;
            IsInterrogated = false;
            IsDismissed = false;
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

        // Finale button state
        private bool showFinaleButton = false;
        private bool finaleButtonEnabled = false;
        private Rectangle finaleButtonRect;

        // Track cell rectangles for mouse interaction
        private Dictionary<int, Rectangle> cellRectangles = new Dictionary<int, Rectangle>();

        // Grid display settings
        private const int GridColumns = 4;
        private const float BoxPadding = 30f;
        private const float PortraitWidth = 64f;
        private const float PortraitHeight = 96f;
        private const float CellWidth = 180f;  // 40% wider than portrait (64 * 1.4 ~= 90) for name space
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
        public event Action OnFinaleButtonClicked;

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
                new SelectableCharacter("Dr. Lyssa Thorne", "NPC_DrThorne", "ologist"),
                new SelectableCharacter("Lieutenant Marcus Webb", "LtWebb", "Navigation Officer"),
                new SelectableCharacter("Ensign Tork", "EnsignTork", "Junior Engineer"),
                new SelectableCharacter("Maven Kilroth", "MavenKilroth", "Smuggler"),
                new SelectableCharacter("Chief Kala Solis", "ChiefSolis", "Security Chief"),
                new SelectableCharacter("Tehvora", "Tehvora", "Diplomatic Attache"),
                new SelectableCharacter("Lucky Chen", "LuckyChen", "Quartermaster"),
            };
        }

        /// <summary>
        /// Load characters from CharacterProfileManager
        /// Replaces hardcoded character list with data from profiles
        /// </summary>
        public void LoadFromProfiles(CharacterProfileManager profileManager)
        {
            if (profileManager == null)
                return;

            var interrogatableProfiles = profileManager.GetInterrogatableProfiles().ToList();
            if (interrogatableProfiles.Count == 0)
                return;

            characters.Clear();
            foreach (var profile in interrogatableProfiles)
            {
                // Skip bartender and pathologist (they're not suspects)
                if (profile.Id == "bartender" || profile.Id == "pathologist")
                    continue;

                // Sanitize text to remove unsupported characters
                string sanitizedName = SanitizeText(profile.Name);
                string sanitizedRole = SanitizeText(profile.Role);

                var character = new SelectableCharacter(
                    sanitizedName,
                    profile.PortraitKey,
                    sanitizedRole
                );
                characters.Add(character);
            }

            // Scramble the order to make each playthrough unique
            ScrambleCharacterOrder();

            Console.WriteLine($"[CharacterSelectionMenu] Loaded {characters.Count} characters from ProfileManager (order scrambled)");
        }

        /// <summary>
        /// Scramble the character list order using Fisher-Yates shuffle
        /// </summary>
        private void ScrambleCharacterOrder()
        {
            Random rng = new Random();
            int n = characters.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var temp = characters[k];
                characters[k] = characters[n];
                characters[n] = temp;
            }
        }

        /// <summary>
        /// Remove characters that might not be supported by the font
        /// </summary>
        private string SanitizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Remove or replace common problematic characters
            return text
                .Replace("\u2018", "'")  // Replace left single quote with straight apostrophe
                .Replace("\u2019", "'")  // Replace right single quote with straight apostrophe
                .Replace("\u201C", "\"") // Replace left double quote with straight quote
                .Replace("\u201D", "\"") // Replace right double quote with straight quote
                .Replace("\u2014", "-")  // Replace em dash with hyphen
                .Replace("\u2013", "-")  // Replace en dash with hyphen
                .Replace("\u2026", "..."); // Replace ellipsis with three dots
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

        public bool IsInterrogationInProgress => isInterrogationInProgress;

        /// <summary>
        /// Show the finale button (disabled by default)
        /// </summary>
        public void ShowFinaleButton()
        {
            showFinaleButton = true;
            finaleButtonEnabled = false;
            Console.WriteLine("[CharacterSelectionMenu] Showing finale button (disabled)");
        }

        /// <summary>
        /// Enable the finale button (allows clicking)
        /// </summary>
        public void EnableFinaleButton()
        {
            finaleButtonEnabled = true;
            Console.WriteLine("[CharacterSelectionMenu] Finale button enabled");
        }

        /// <summary>
        /// Hide the finale button
        /// </summary>
        public void HideFinaleButton()
        {
            showFinaleButton = false;
            finaleButtonEnabled = false;
            Console.WriteLine("[CharacterSelectionMenu] Hiding finale button");
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

            // If showing finale button, handle that instead of character selection
            if (showFinaleButton)
            {
                Point finaleMousePos = new Point(mouse.X, mouse.Y);
                bool isHoveringButton = finaleButtonRect.Contains(finaleMousePos);

                // Mouse click on finale button
                if (mouse.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed)
                {
                    if (isHoveringButton && finaleButtonEnabled)
                    {
                        Console.WriteLine("[CharacterSelectionMenu] Finale button clicked!");
                        OnFinaleButtonClicked?.Invoke();
                        Hide();
                    }
                    else if (isHoveringButton && !finaleButtonEnabled)
                    {
                        ShowWarning("Dismiss both suspects first");
                    }
                }

                // ESC to close
                if (keyboard.IsKeyDown(Keys.Escape) && !previousKeyboard.IsKeyDown(Keys.Escape))
                {
                    Console.WriteLine("CharacterSelectionMenu: Closed");
                    OnMenuClosed?.Invoke();
                    Hide();
                }

                previousKeyboard = keyboard;
                previousMouse = mouse;
                return;
            }

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

            // Mouse click to toggle selection
            if (mouse.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed)
            {
                // Handle character selection
                if (hoveredIndex != -1)
                {
                    var character = characters[hoveredIndex];

                    // Check if already interrogated or dismissed
                    if (character.IsInterrogated)
                    {
                        ShowWarning("Cannot select - Character already interrogated");
                    }
                    else if (character.IsDismissed)
                    {
                        ShowWarning("Cannot select - Character dismissed");
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

            // Toggle selection with Space
            if (keyboard.IsKeyDown(Keys.Space) && !previousKeyboard.IsKeyDown(Keys.Space))
            {
                if (selectedIndex >= 0 && selectedIndex < characters.Count)
                {
                    var character = characters[selectedIndex];

                    // Check if already interrogated or dismissed
                    if (character.IsInterrogated)
                    {
                        ShowWarning("Cannot select - Character already interrogated");
                    }
                    else if (character.IsDismissed)
                    {
                        ShowWarning("Cannot select - Character dismissed");
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
                        if (characters[index].IsDismissed)
                        {
                            ShowWarning("Cannot confirm - Selected character dismissed");
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
                Rectangle portraitRect = new Rectangle((int)(cellX + portraitOffsetX), (int)cellY, (int)PortraitWidth, (int)PortraitHeight);

                if (portraits != null && portraits.ContainsKey(character.PortraitKey))
                {
                    var portrait = portraits[character.PortraitKey];

                    if (portrait != null)
                    {
                        // Draw portrait normally
                        spriteBatch.Draw(portrait, portraitRect, Color.White);
                    }
                    else
                    {
                        // Portrait failed to load - draw red error indicator
                        DrawFilledRectangle(spriteBatch, portraitRect, Color.DarkRed);

                        string errorText = "MISSING";
                        float errorScale = 0.4f;
                        Vector2 errorSize = font.MeasureString(errorText) * errorScale;
                        Vector2 errorPos = new Vector2(
                            portraitRect.X + (PortraitWidth - errorSize.X) / 2,
                            portraitRect.Y + (PortraitHeight - errorSize.Y) / 2
                        );
                        spriteBatch.DrawString(font, errorText, errorPos, Color.White,
                            0f, Vector2.Zero, errorScale, SpriteEffects.None, 0f);
                    }
                }
                else
                {
                    // Portrait key not found - draw red placeholder
                    DrawFilledRectangle(spriteBatch, portraitRect, Color.DarkRed);

                    string errorText = "NO KEY";
                    float errorScale = 0.4f;
                    Vector2 errorSize = font.MeasureString(errorText) * errorScale;
                    Vector2 errorPos = new Vector2(
                        portraitRect.X + (PortraitWidth - errorSize.X) / 2,
                        portraitRect.Y + (PortraitHeight - errorSize.Y) / 2
                    );
                    spriteBatch.DrawString(font, errorText, errorPos, Color.White,
                        0f, Vector2.Zero, errorScale, SpriteEffects.None, 0f);
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

                // Draw interrogated/dismissed overlay (centered in cell)
                if (character.IsInterrogated || character.IsDismissed)
                {
                    DrawFilledRectangle(spriteBatch,
                        new Rectangle((int)(cellX + portraitOffsetX), (int)cellY, (int)PortraitWidth, (int)PortraitHeight),
                        Color.Black * 0.6f);

                    string status = character.IsDismissed ? "DISMISSED" : "DONE";
                    Vector2 statusSize = font.MeasureString(status) * 0.5f;
                    Vector2 statusPos = new Vector2(cellX + portraitOffsetX + (PortraitWidth - statusSize.X) / 2, cellY + (PortraitHeight - statusSize.Y) / 2);
                    spriteBatch.DrawString(font, status, statusPos, InterrogatedColor, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                }
            }

            // Draw controls hint at bottom (unless finale button is showing)
            if (!showFinaleButton)
            {
                string hint = "[Mouse/Arrows] Navigate  [Click/Space] Toggle  [Enter] Confirm  [Tab] Cancel";
                var hintSize = font.MeasureString(hint) * 0.6f;
                Vector2 hintPos = new Vector2(menuX + (menuWidth - hintSize.X) / 2, menuY + menuHeight - BoxPadding);
                spriteBatch.DrawString(font, hint, hintPos, Color.Gray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            }

            // If showing finale button, draw it overlaid on the disabled character grid
            if (showFinaleButton)
            {
                DrawFinaleButtonOverlay(spriteBatch, font, viewport, menuX, menuY, menuWidth, menuHeight);
            }
        }

        private void DrawFinaleButtonOverlay(SpriteBatch spriteBatch, SpriteFont font, Viewport viewport, float menuX, float menuY, float menuWidth, float menuHeight)
        {
            // Draw semi-transparent overlay to dim the character grid
            DrawFilledRectangle(spriteBatch, new Rectangle((int)menuX, (int)menuY, (int)menuWidth, (int)menuHeight), Color.Black * 0.7f);

            // Draw title centered at top
            string title = "ROUND 3 COMPLETE";
            var titleSize = font.MeasureString(title) * 1.2f;
            Vector2 titlePos = new Vector2(menuX + (menuWidth - titleSize.X) / 2, menuY + 60);
            spriteBatch.DrawString(font, title, titlePos, SelectedColor, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);

            // Draw button
            float buttonWidth = 400;
            float buttonHeight = 80;
            float buttonX = menuX + (menuWidth - buttonWidth) / 2;
            float buttonY = menuY + (menuHeight - buttonHeight) / 2 + 20;

            finaleButtonRect = new Rectangle((int)buttonX, (int)buttonY, (int)buttonWidth, (int)buttonHeight);

            // Button color based on state
            Color buttonColor = finaleButtonEnabled ? ConfirmedColor : InterrogatedColor;
            Color textColor = finaleButtonEnabled ? Color.Black : Color.DarkGray;

            DrawFilledRectangle(spriteBatch, finaleButtonRect, buttonColor);
            DrawRectangleBorder(spriteBatch, finaleButtonRect, Color.White, 3);

            // Button text
            string buttonText = "READY FOR FINALE";
            var buttonTextSize = font.MeasureString(buttonText) * 1.2f;
            Vector2 buttonTextPos = new Vector2(buttonX + (buttonWidth - buttonTextSize.X) / 2, buttonY + (buttonHeight - buttonTextSize.Y) / 2);
            spriteBatch.DrawString(font, buttonText, buttonTextPos, textColor, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);

            // Draw status text
            string statusText = finaleButtonEnabled ? "Talk to bartender Zix to begin" : "Dismiss both suspects first";
            var statusTextSize = font.MeasureString(statusText) * 0.7f;
            Vector2 statusPos = new Vector2(menuX + (menuWidth - statusTextSize.X) / 2, buttonY + buttonHeight + 20);
            spriteBatch.DrawString(font, statusText, statusPos, Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            // Draw additional hint if enabled
            if (finaleButtonEnabled)
            {
                string hintText = "Zix will ask you the 7 critical questions";
                var hintTextSize = font.MeasureString(hintText) * 0.6f;
                Vector2 hintPos = new Vector2(menuX + (menuWidth - hintTextSize.X) / 2, statusPos.Y + statusTextSize.Y + 10);
                spriteBatch.DrawString(font, hintText, hintPos, Color.Gray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            }

            // Draw warning message if active
            if (!string.IsNullOrEmpty(warningMessage) && warningTimer > 0f)
            {
                var warningSize = font.MeasureString(warningMessage) * 0.8f;
                Vector2 warningPos = new Vector2(menuX + (menuWidth - warningSize.X) / 2, menuY + menuHeight - 50);
                spriteBatch.DrawString(font, warningMessage, warningPos, Color.Red, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
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
