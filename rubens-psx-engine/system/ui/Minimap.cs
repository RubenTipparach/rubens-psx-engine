using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.system.terrain;
using System;
using System.Collections.Generic;
using rubens_psx_engine.game.units;

namespace rubens_psx_engine.system.ui
{
    public class Minimap
    {
        private GraphicsDevice graphicsDevice;
        private SpriteBatch spriteBatch;
        private Texture2D pixelTexture;
        private Rectangle bounds;
        private TerrainData terrainData;
        private RTSCamera camera;
        private UnitManager unitManager;

        private Color backgroundColor = Color.Black * 0.7f;
        private Color terrainColor = Color.DarkGreen;
        private Color cameraPositionColor = Color.Yellow;
        private Color frustumColor = Color.White * 0.8f;
        private Color borderColor = Color.White;
        private Color unitColor = Color.Red;

        public Rectangle Bounds => bounds;

        public Minimap(GraphicsDevice device, SpriteBatch spriteBatch, int width, int height, int x, int y)
        {
            this.graphicsDevice = device;
            this.spriteBatch = spriteBatch;
            this.bounds = new Rectangle(x, y, width, height);
            
            // Create a 1x1 white pixel texture for drawing
            pixelTexture = new Texture2D(device, 1, 1);
            pixelTexture.SetData(new[] { new Color(0.3f,.3f,.1f, .2f)});
        }

        public void SetTerrain(TerrainData terrain)
        {
            terrainData = terrain;
        }

        public void SetCamera(RTSCamera rtsCamera)
        {
            camera = rtsCamera;
        }

        public void SetUnitManager(UnitManager manager)
        {
            unitManager = manager;
        }

        public void Draw()
        {
            if (terrainData == null || camera == null)
                return;

            // Draw background
            spriteBatch.Draw(pixelTexture, bounds, backgroundColor);

            // Draw terrain representation (simplified)
            DrawTerrain();

            // Draw camera frustum
            DrawCameraFrustum();

            // Draw camera position
            DrawCameraPosition();

            // Draw units
            DrawUnits();

            // Draw border
            DrawBorder();
        }

        private void DrawTerrain()
        {
            float terrainWidth = terrainData.Width * terrainData.Scale;
            float terrainHeight = terrainData.Height * terrainData.Scale;

            // Sample terrain heights and draw as colored pixels
            int sampleRate = Math.Max(1, terrainData.Width / bounds.Width);
            
            for (int x = 0; x < terrainData.Width; x += sampleRate)
            {
                for (int z = 0; z < terrainData.Height; z += sampleRate)
                {
                    float height = terrainData.HeightMap[x, z];
                    
                    // Convert world position to minimap position
                    Vector2 minimapPos = WorldToMinimap(new Vector3(x * terrainData.Scale, 0, z * terrainData.Scale));
                    
                    // Color based on height
                    float normalizedHeight = (height + 1.0f) * 0.5f; // Assuming height range -1 to 1
                    Color heightColor = Color.Lerp(Color.DarkBlue, Color.Brown, normalizedHeight);
                    
                    Rectangle pixelRect = new Rectangle((int)minimapPos.X, (int)minimapPos.Y, 
                        Math.Max(1, bounds.Width / (terrainData.Width / sampleRate)), 
                        Math.Max(1, bounds.Height / (terrainData.Height / sampleRate)));
                    
                    spriteBatch.Draw(pixelTexture, pixelRect, heightColor);
                }
            }
        }

        private void DrawCameraFrustum()
        {
            Vector3[] frustumCorners = camera.GetFrustumCorners();
            
            // Convert world coordinates to minimap coordinates
            Vector2[] minimapCorners = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                minimapCorners[i] = WorldToMinimap(frustumCorners[i]);
            }

            // Draw frustum outline
            for (int i = 0; i < 4; i++)
            {
                Vector2 start = minimapCorners[i];
                Vector2 end = minimapCorners[(i + 1) % 4];
                DrawLine(start, end, frustumColor, 1);
            }

            // Draw field of view fill (semi-transparent)
            DrawQuad(minimapCorners, frustumColor * 0.3f);
        }

        private void DrawCameraPosition()
        {
            Vector3 cameraGroundPos = camera.GetGroundCenter();
            Vector2 minimapPos = WorldToMinimap(cameraGroundPos);
            
            // Draw camera position as a circle
            int radius = 3;
            Rectangle cameraRect = new Rectangle(
                (int)minimapPos.X - radius, 
                (int)minimapPos.Y - radius, 
                radius * 2, 
                radius * 2
            );
            
            spriteBatch.Draw(pixelTexture, cameraRect, cameraPositionColor);
        }

        private void DrawUnits()
        {
            if (unitManager == null) return;

            foreach (var unit in unitManager.Units)
            {
                if (unit.State == UnitState.Dead) continue;

                Vector2 minimapPos = WorldToMinimap(unit.Position);

                // Clamp to minimap bounds
                minimapPos.X = MathHelper.Clamp(minimapPos.X, bounds.X, bounds.X + bounds.Width);
                minimapPos.Y = MathHelper.Clamp(minimapPos.Y, bounds.Y, bounds.Y + bounds.Height);

                // Draw unit as a small square
                int unitSize = 3;
                Rectangle unitRect = new Rectangle(
                    (int)minimapPos.X - unitSize / 2,
                    (int)minimapPos.Y - unitSize / 2,
                    unitSize,
                    unitSize
                );

                // Use unit's team color
                Color color = unit.TeamColor;
                if (unit.IsSelected)
                {
                    // Make selected units brighter/white outline
                    spriteBatch.Draw(pixelTexture, new Rectangle(unitRect.X - 1, unitRect.Y - 1, unitRect.Width + 2, unitRect.Height + 2), Color.White);
                }

                spriteBatch.Draw(pixelTexture, unitRect, color);
            }
        }

        private void DrawBorder()
        {
            // Top border
            spriteBatch.Draw(pixelTexture, new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), borderColor);
            // Bottom border
            spriteBatch.Draw(pixelTexture, new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), borderColor);
            // Left border
            spriteBatch.Draw(pixelTexture, new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), borderColor);
            // Right border
            spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), borderColor);
        }

        private Vector2 WorldToMinimap(Vector3 worldPosition)
        {
            if (terrainData == null)
                return Vector2.Zero;

            float terrainWidth = terrainData.Width * terrainData.Scale;
            float terrainHeight = terrainData.Height * terrainData.Scale;

            // Normalize world position to 0-1 range
            float normalizedX = worldPosition.X / terrainWidth;
            float normalizedZ = worldPosition.Z / terrainHeight;

            // Convert to minimap coordinates
            return new Vector2(
                bounds.X + normalizedX * bounds.Width,
                bounds.Y + normalizedZ * bounds.Height
            );
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 direction = end - start;
            float length = direction.Length();
            
            if (length < 1)
                return;

            direction.Normalize();
            
            float angle = (float)Math.Atan2(direction.Y, direction.X);
            
            Rectangle lineRect = new Rectangle(
                (int)start.X,
                (int)start.Y - thickness / 2,
                (int)length,
                thickness
            );

            spriteBatch.Draw(pixelTexture, lineRect, null, color, angle, Vector2.Zero, SpriteEffects.None, 0);
        }

        private void DrawQuad(Vector2[] corners, Color color)
        {
            // Simple quad fill using triangles (approximate)
            Vector2 center = (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;
            
            // Draw as triangles from center to edges
            for (int i = 0; i < 4; i++)
            {
                Vector2 p1 = corners[i];
                Vector2 p2 = corners[(i + 1) % 4];
                
                // Approximate triangle fill with lines
                Vector2 edge = p2 - p1;
                int steps = (int)edge.Length();
                
                for (int step = 0; step <= steps; step++)
                {
                    float t = steps > 0 ? (float)step / steps : 0;
                    Vector2 edgePoint = Vector2.Lerp(p1, p2, t);
                    DrawLine(center, edgePoint, color, 1);
                }
            }
        }

        public void Dispose()
        {
            pixelTexture?.Dispose();
        }
    }
}