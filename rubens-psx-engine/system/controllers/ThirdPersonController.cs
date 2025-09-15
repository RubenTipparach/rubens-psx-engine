using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;

namespace rubens_psx_engine.system.controllers
{
    /// <summary>
    /// Third-person camera controller (no physics character, just camera movement)
    /// </summary>
    public class ThirdPersonController : IPlayerController
    {
        private Vector3 position;
        private float moveSpeed = 50f;
        private Vector3 cameraOffset = new Vector3(0, 5, -10); // Camera behind and above player

        public ThirdPersonController()
        {
            position = new Vector3(0, 2, 0); // Starting position
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Simple movement (no physics)
            var movement = Vector3.Zero;
            if (keyboard.IsKeyDown(Keys.W)) movement.Z += 1;
            if (keyboard.IsKeyDown(Keys.S)) movement.Z -= 1;
            if (keyboard.IsKeyDown(Keys.A)) movement.X -= 1;
            if (keyboard.IsKeyDown(Keys.D)) movement.X += 1;
            
            if (movement.LengthSquared() > 0)
            {
                movement.Normalize();
                position += movement * moveSpeed * dt;
            }
        }

        public void UpdateCamera(Camera camera)
        {
            // Position camera behind and above the player position
            camera.Position = position + cameraOffset;
            
            // Look at the player position (or slightly ahead of it)
            var lookAtPosition = position + Vector3.Forward * 5; // Look ahead of player
            
            // Calculate direction for camera's forward vector
            var direction = Vector3.Normalize(lookAtPosition - camera.Position);
            
            // Convert to yaw/pitch for the camera system
            var yaw = MathF.Atan2(direction.X, direction.Z);
            var pitch = MathF.Asin(-direction.Y);
            
            // Update the camera's internal rotation values using reflection
            var cameraType = typeof(Camera);
            var yawField = cameraType.GetField("yaw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pitchField = cameraType.GetField("pitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            yawField?.SetValue(camera, yaw);
            pitchField?.SetValue(camera, pitch);
        }

        public Vector3 GetPosition()
        {
            return position;
        }

        public void SetMouseLocked(bool locked)
        {
            // Third person doesn't use mouse lock
        }

        public bool IsMouseLocked()
        {
            return false;
        }

        public void Dispose()
        {
            // No resources to dispose
        }
    }
}