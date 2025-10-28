using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine;
using rubens_psx_engine.Extensions;
using System;
using System.Collections.Generic;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Manages the animated starfield background for The Lounge scene
    /// </summary>
    public class LoungeStarfield
    {
        private struct Star
        {
            public Vector3 Position;
            public Color Color;
            public float Depth; // 0 = farthest, 1 = closest
            public float Speed; // Movement speed based on depth
        }

        private List<Star> stars;

        // Starfield constants
        private const int StarCount = 1000;
        private const float StarfieldZStart = 8000f; // Stars spawn here (front)
        private const float StarfieldZEnd = -200f; // Stars despawn here (far back)
        private const float StarfieldRadius = 2000f; // Large radius to extend beyond lounge geometry
        private const float StarfieldMinRadius = 100f; // Exclude center area to avoid lounge geometry
        private const float StarLineLength = 500.0f; // Streak length
        private const float StarBaseSpeed = 2000f; // Base movement speed in units/second

        // Star color table
        private static readonly Color[] StarColors = new Color[]
        {
            ColorExtensions.FromHex("#492d38"), // Farthest - dark purple
            ColorExtensions.FromHex("#ab5236"), // Medium-far - tan
            ColorExtensions.FromHex("#ffccaa"), // Medium-close - peach
            ColorExtensions.FromHex("#fff1e8")  // Closest - light peach
        };

        // Calculate max distance based on farthest possible spawn point
        private static readonly float MaxStarDistance = (float)Math.Sqrt(StarfieldRadius * StarfieldRadius + StarfieldZEnd * StarfieldZEnd);

        public LoungeStarfield()
        {
            stars = new List<Star>();
            InitializeStarfield();
        }

        private void InitializeStarfield()
        {
            Random random = new Random();

            // Create evenly distributed stars along the Z axis
            for (int i = 0; i < StarCount; i++)
            {
                // Random position in XY plane within radius, excluding center area (donut shape)
                float angle = (float)(random.NextDouble() * Math.PI * 2);

                // Map random value to range between MinRadius and MaxRadius
                float normalizedDistance = (float)Math.Sqrt(random.NextDouble());
                float distance = StarfieldMinRadius + normalizedDistance * (StarfieldRadius - StarfieldMinRadius);

                float x = distance * (float)Math.Cos(angle);
                float y = distance * (float)Math.Sin(angle);

                // Evenly distribute along Z axis from start to end (prewarm the starfield)
                // This ensures stars are visible immediately when the game loads
                float z = StarfieldZStart + (float)random.NextDouble() * (StarfieldZEnd - StarfieldZStart);

                Vector3 position = new Vector3(x, y, z);

                // Get color based on position
                Color starColor = GetStarColorFromPosition(position);

                // Calculate depth for speed variation
                float distanceFromOrigin = position.Length();
                float depth = 1f - Math.Min(distanceFromOrigin / MaxStarDistance, 1f);
                float speed = StarBaseSpeed * (0.5f + depth * 0.5f);

                stars.Add(new Star
                {
                    Position = position,
                    Color = starColor,
                    Depth = depth,
                    Speed = speed
                });
            }
        }

        private Color GetStarColorFromPosition(Vector3 position)
        {
            // Calculate depth based on distance from origin (0,0,0)
            float distanceFromOrigin = position.Length();
            float depth = 1f - Math.Min(distanceFromOrigin / StarfieldZStart, 1f);

            // Hard transition between 4 color bands based on depth
            if (depth < 0.33f)
            {
                return StarColors[0]; // Farthest
            }
            else if (depth < 0.46f)
            {
                return StarColors[1]; // Medium-far
            }
            else if (depth < 0.6f)
            {
                return StarColors[2]; // Medium-close
            }
            else
            {
                return StarColors[3]; // Closest
            }
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = 0; i < stars.Count; i++)
            {
                var star = stars[i];

                // Move star backward along -Z axis (away from camera)
                star.Position.Z -= star.Speed * deltaTime;

                // If star passed the end point, respawn at start
                if (star.Position.Z < StarfieldZEnd)
                {
                    // Reset Z to start position (close to camera)
                    star.Position.Z = StarfieldZStart;
                }

                // Update color based on current position (distance from origin)
                star.Color = GetStarColorFromPosition(star.Position);

                // Update depth for speed variation
                float distanceFromOrigin = star.Position.Length();
                star.Depth = 1f - Math.Min(distanceFromOrigin / MaxStarDistance, 1f);

                stars[i] = star;
            }
        }

        public void Draw(Camera camera)
        {
            var graphicsDevice = Globals.screenManager.GraphicsDevice;

            // Create a basic effect for rendering star streaks
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = camera.View;
            basicEffect.Projection = camera.Projection;
            basicEffect.World = Matrix.Identity;

            // Create star streak lines pointing along the Z axis (direction of movement)
            var starVertices = new List<VertexPositionColor>();

            foreach (var star in stars)
            {
                // Create a line streak pointing forward along +Z axis (trail effect showing motion away from camera)
                Vector3 startPoint = star.Position;
                Vector3 endPoint = star.Position + new Vector3(0, 0, StarLineLength);

                // Add the line (2 vertices per star)
                starVertices.Add(new VertexPositionColor(startPoint, star.Color));
                starVertices.Add(new VertexPositionColor(endPoint, star.Color));
            }

            // Draw the star streaks as line list
            if (starVertices.Count > 0)
            {
                foreach (var pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.LineList,
                        starVertices.ToArray(),
                        0,
                        starVertices.Count / 2
                    );
                }
            }

            basicEffect.Dispose();
        }
    }
}
