using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine.system.terrain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace rubens_psx_engine.game.units
{
    public class UnitManager
    {
        private List<Unit> units;
        private List<Unit> selectedUnits;
        private MouseState previousMouseState;
        private KeyboardState previousKeyboardState;
        private TerrainData terrainData;
        private Texture2D pixelTexture;

        // Selection box
        private bool isSelecting;
        private Vector2 selectionStart;
        private Vector2 selectionEnd;

        public IReadOnlyList<Unit> Units => units.AsReadOnly();
        public IReadOnlyList<Unit> SelectedUnits => selectedUnits.AsReadOnly();

        public UnitManager()
        {
            units = new List<Unit>();
            selectedUnits = new List<Unit>();
            previousMouseState = Mouse.GetState();
            previousKeyboardState = Keyboard.GetState();
            isSelecting = false;
            selectionStart = Vector2.Zero;
            selectionEnd = Vector2.Zero;

            // Create pixel texture for UI drawing
            var graphicsDevice = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
        }

        public void AddUnit(Unit unit)
        {
            if (unit != null && !units.Contains(unit))
            {
                units.Add(unit);
            }
        }

        public void RemoveUnit(Unit unit)
        {
            if (unit != null)
            {
                units.Remove(unit);
                selectedUnits.Remove(unit);
            }
        }

        public void SetTerrainData(TerrainData terrain)
        {
            terrainData = terrain;
        }

        public void CreateUnit(UnitType type, Vector3 position, Color teamColor, string name = null)
        {
            if (name == null)
            {
                name = $"{type} {units.Count + 1}";
            }

            Unit newUnit = new Unit(type, position, teamColor, name);
            AddUnit(newUnit);
        }

        public void CreateUnitAtMousePosition(UnitType type, Vector3 worldPosition, Color teamColor, string name = null)
        {
            // Adjust height to terrain if terrain data is available
            if (terrainData != null)
            {
                try
                {
                    float terrainHeight = terrainData.GetHeightAt(worldPosition.X, worldPosition.Z);
                    worldPosition.Y = terrainHeight + 1.0f; // Place unit 1 unit above terrain
                }
                catch
                {
                    worldPosition.Y = 1.0f; // Fallback height
                }
            }
            else
            {
                worldPosition.Y = 1.0f; // Default height if no terrain
            }

            CreateUnit(type, worldPosition, teamColor, name);
        }

        public void Update(GameTime gameTime, rubens_psx_engine.RTSCamera camera)
        {
            // Update all units
            foreach (var unit in units.ToList()) // ToList to avoid modification during iteration
            {
                unit.Update(gameTime);

                // Remove dead units
                if (unit.State == UnitState.Dead)
                {
                    RemoveUnit(unit);
                }
            }

            // Handle input
            HandleInput(camera);

            previousMouseState = Mouse.GetState();
            previousKeyboardState = Keyboard.GetState();
        }

        private void HandleInput(rubens_psx_engine.RTSCamera camera)
        {
            var mouseState = Mouse.GetState();
            var keyboardState = Keyboard.GetState();

            // Handle unit selection
            HandleSelection(mouseState, camera);

            // Handle unit commands
            HandleCommands(mouseState, keyboardState, camera);

            // Handle unit creation (for testing)
            HandleUnitCreation(mouseState, keyboardState, camera);
        }

        private void HandleSelection(MouseState mouseState, rubens_psx_engine.RTSCamera camera)
        {
            Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);

            // Start selection
            if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                isSelecting = true;
                selectionStart = mousePosition;
                selectionEnd = mousePosition;
            }

            // Update selection
            if (isSelecting && mouseState.LeftButton == ButtonState.Pressed)
            {
                selectionEnd = mousePosition;
            }

            // End selection
            if (isSelecting && mouseState.LeftButton == ButtonState.Released)
            {
                PerformSelection(camera);
                isSelecting = false;
                selectionStart = Vector2.Zero;
                selectionEnd = Vector2.Zero;
            }
        }

        private void PerformSelection(rubens_psx_engine.RTSCamera camera)
        {
            // Clear previous selection if not holding Ctrl
            var keyboardState = Keyboard.GetState();
            if (!keyboardState.IsKeyDown(Keys.LeftControl) && !keyboardState.IsKeyDown(Keys.RightControl))
            {
                ClearSelection();
            }

            // Determine selection area
            Vector2 topLeft = new Vector2(
                Math.Min(selectionStart.X, selectionEnd.X),
                Math.Min(selectionStart.Y, selectionEnd.Y)
            );
            Vector2 bottomRight = new Vector2(
                Math.Max(selectionStart.X, selectionEnd.X),
                Math.Max(selectionStart.Y, selectionEnd.Y)
            );

            // Single click selection
            if (Vector2.Distance(selectionStart, selectionEnd) < 5f)
            {
                // Raycast to find unit under mouse
                Vector3 worldPosition = camera.ScreenToWorld(selectionStart, 0);
                Unit clickedUnit = GetUnitAt(worldPosition, 2.0f); // 2 unit radius for clicking

                if (clickedUnit != null)
                {
                    SelectUnit(clickedUnit);
                }
            }
            else
            {
                // Box selection
                foreach (var unit in units)
                {
                    Vector2 screenPos = camera.WorldToScreen(unit.Position);

                    if (screenPos.X >= topLeft.X && screenPos.X <= bottomRight.X &&
                        screenPos.Y >= topLeft.Y && screenPos.Y <= bottomRight.Y)
                    {
                        SelectUnit(unit);
                    }
                }
            }
        }

        private void HandleCommands(MouseState mouseState, KeyboardState keyboardState, rubens_psx_engine.RTSCamera camera)
        {
            // Right click to move selected units
            if (mouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released)
            {
                if (selectedUnits.Count > 0)
                {
                    Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
                    Vector3 targetPosition = camera.ScreenToWorld(mousePosition, 0);

                    // Move selected units to target position
                    MoveSelectedUnits(targetPosition);
                }
            }

            // Delete selected units (for testing)
            if (keyboardState.IsKeyDown(Keys.Delete) && !previousKeyboardState.IsKeyDown(Keys.Delete))
            {
                foreach (var unit in selectedUnits.ToList())
                {
                    RemoveUnit(unit);
                }
                ClearSelection();
            }
        }

        private void HandleUnitCreation(MouseState mouseState, KeyboardState keyboardState, rubens_psx_engine.RTSCamera camera)
        {
            Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
            Vector3 worldPosition = camera.ScreenToWorld(mousePosition, 0);

            // Create worker with 1 key (smaller scale)
            if (keyboardState.IsKeyDown(Keys.D1) && !previousKeyboardState.IsKeyDown(Keys.D1))
            {
                CreateUnitAtMousePosition(UnitType.Worker, worldPosition, Color.Blue, 0.8f);
                System.Console.WriteLine($"Created Worker at {worldPosition}");
            }

            // Create soldier with 2 key (normal scale)
            if (keyboardState.IsKeyDown(Keys.D2) && !previousKeyboardState.IsKeyDown(Keys.D2))
            {
                CreateUnitAtMousePosition(UnitType.Soldier, worldPosition, Color.Red, 1.0f);
                System.Console.WriteLine($"Created Soldier at {worldPosition}");
            }

            // Create tank with 3 key (larger scale)
            if (keyboardState.IsKeyDown(Keys.D3) && !previousKeyboardState.IsKeyDown(Keys.D3))
            {
                CreateUnitAtMousePosition(UnitType.Tank, worldPosition, Color.Green, 1.5f);
                System.Console.WriteLine($"Created Tank at {worldPosition}");
            }
        }

        private void MoveSelectedUnits(Vector3 targetPosition)
        {
            if (selectedUnits.Count == 1)
            {
                // Single unit - move directly to target
                selectedUnits[0].MoveTo(targetPosition);
            }
            else if (selectedUnits.Count > 1)
            {
                // Multiple units - spread them out in formation
                int unitsPerRow = (int)Math.Ceiling(Math.Sqrt(selectedUnits.Count));
                float spacing = 2.0f;

                for (int i = 0; i < selectedUnits.Count; i++)
                {
                    int row = i / unitsPerRow;
                    int col = i % unitsPerRow;

                    Vector3 offset = new Vector3(
                        (col - unitsPerRow / 2f) * spacing,
                        0,
                        (row - unitsPerRow / 2f) * spacing
                    );

                    selectedUnits[i].MoveTo(targetPosition + offset);
                }
            }

            System.Console.WriteLine($"Moving {selectedUnits.Count} units to {targetPosition}");
        }

        private Unit GetUnitAt(Vector3 worldPosition, float radius)
        {
            return units.FirstOrDefault(unit =>
            {
                float distance = Vector3.Distance(unit.Position, worldPosition);
                return distance <= radius && unit.State != UnitState.Dead;
            });
        }

        public void SelectUnit(Unit unit)
        {
            if (unit != null && !selectedUnits.Contains(unit))
            {
                selectedUnits.Add(unit);
                unit.SetSelected(true);
            }
        }

        public void DeselectUnit(Unit unit)
        {
            if (unit != null && selectedUnits.Contains(unit))
            {
                selectedUnits.Remove(unit);
                unit.SetSelected(false);
            }
        }

        public void ClearSelection()
        {
            foreach (var unit in selectedUnits)
            {
                unit.SetSelected(false);
            }
            selectedUnits.Clear();
        }

        private void CreateUnitAtMousePosition(UnitType unitType, Vector3 worldPosition, Color teamColor, float scale = 1.0f)
        {
            // Adjust position to terrain height if terrain data is available
            if (terrainData != null)
            {
                float terrainHeight = terrainData.GetHeightAt(worldPosition.X, worldPosition.Z);
                worldPosition.Y = terrainHeight;
            }

            // Create the unit with specified scale
            Unit newUnit = new Unit(unitType, worldPosition, teamColor, unitType.ToString(), scale);
            AddUnit(newUnit);
        }

        public void Draw(rubens_psx_engine.RTSCamera camera)
        {
            // Draw all units
            foreach (var unit in units)
            {
                //Console.WriteLine("drawing units " + unit.Name);
                unit.Draw(camera);
            }
        }

        private void DrawLabels(rubens_psx_engine.RTSCamera camera)
        {
            var spriteBatch = Globals.screenManager.getSpriteBatch;
            var font = Globals.screenManager.Content.Load<Microsoft.Xna.Framework.Graphics.SpriteFont>("fonts/Arial");

            foreach (var unit in units)
            {
                if (unit.State == UnitState.Dead) continue;

                // Convert unit world position to screen position
                Vector2 screenPos = camera.WorldToScreen(unit.Position + Vector3.Up * 2.5f); // Above unit

                // Draw unit label with selection status
                string label = $"{unit.Type} ({unit.Position.X:F1}, {unit.Position.Z:F1})";
                if (unit.IsSelected)
                {
                    label += " [SELECTED]";
                }

                Vector2 textSize = font.MeasureString(label);
                Vector2 textPos = screenPos - textSize * 0.5f;

                // Draw background
                spriteBatch.Draw(pixelTexture,
                    new Rectangle((int)textPos.X - 2, (int)textPos.Y - 2, (int)textSize.X + 4, (int)textSize.Y + 4),
                    Color.Black * 0.7f);

                // Draw text - green if selected, team color if not
                Color labelColor = unit.IsSelected ? Color.LimeGreen : unit.TeamColor;
                spriteBatch.DrawString(font, label, textPos, labelColor);
            }
        }

        public void DrawUI(rubens_psx_engine.RTSCamera camera)
        {
            // Draw selection box
            if (isSelecting)
            {
                DrawSelectionBox();
            }

            // Draw unit count and selection info
            DrawUnitInfo();

            // Draw unit labels
            DrawLabels(camera);
        }

        private void DrawSelectionBox()
        {
            // Only draw if we're actively selecting and the mouse has moved significantly
            if (!isSelecting) return;

            Vector2 size = selectionEnd - selectionStart;
            if (size.Length() < 10f) return; // Minimum drag distance before showing box

            var spriteBatch = Globals.screenManager.getSpriteBatch;
            var graphicsDevice = Globals.screenManager.getGraphicsDevice.GraphicsDevice;

            // Calculate selection rectangle
            Rectangle selectionRect = new Rectangle(
                (int)Math.Min(selectionStart.X, selectionEnd.X),
                (int)Math.Min(selectionStart.Y, selectionEnd.Y),
                (int)Math.Abs(selectionEnd.X - selectionStart.X),
                (int)Math.Abs(selectionEnd.Y - selectionStart.Y)
            );

            // Clamp to screen bounds to prevent huge rectangles
            selectionRect.X = Math.Max(0, Math.Min(selectionRect.X, graphicsDevice.Viewport.Width));
            selectionRect.Y = Math.Max(0, Math.Min(selectionRect.Y, graphicsDevice.Viewport.Height));
            selectionRect.Width = Math.Max(10, Math.Min(selectionRect.Width, graphicsDevice.Viewport.Width - selectionRect.X));
            selectionRect.Height = Math.Max(10, Math.Min(selectionRect.Height, graphicsDevice.Viewport.Height - selectionRect.Y));

            // Debug output
            System.Console.WriteLine($"Drawing selection box: {selectionRect}, isSelecting: {isSelecting}");

            // Draw selection rectangle background (very subtle)
            spriteBatch.Draw(pixelTexture, selectionRect, Color.Green * 0.05f);

            // Draw selection rectangle border
            int borderThickness = 1;
            // Top border
            spriteBatch.Draw(pixelTexture, new Rectangle(selectionRect.X, selectionRect.Y, selectionRect.Width, borderThickness), Color.Green);
            // Bottom border
            spriteBatch.Draw(pixelTexture, new Rectangle(selectionRect.X, selectionRect.Bottom - borderThickness, selectionRect.Width, borderThickness), Color.Green);
            // Left border
            spriteBatch.Draw(pixelTexture, new Rectangle(selectionRect.X, selectionRect.Y, borderThickness, selectionRect.Height), Color.Green);
            // Right border
            spriteBatch.Draw(pixelTexture, new Rectangle(selectionRect.Right - borderThickness, selectionRect.Y, borderThickness, selectionRect.Height), Color.Green);
        }

        private void DrawUnitInfo()
        {
            // This would display unit information in the UI
            // For now, we'll use console output for important events
        }

        // Utility methods
        public int GetUnitCount(UnitType type)
        {
            return units.Count(u => u.Type == type && u.State != UnitState.Dead);
        }

        public List<Unit> GetUnitsInArea(Vector3 center, float radius)
        {
            return units.Where(u =>
            {
                float distance = Vector3.Distance(u.Position, center);
                return distance <= radius && u.State != UnitState.Dead;
            }).ToList();
        }

        public void Dispose()
        {
            pixelTexture?.Dispose();
        }
    }
}