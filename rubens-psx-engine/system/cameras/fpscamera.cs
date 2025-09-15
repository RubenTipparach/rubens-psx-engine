using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;

namespace anakinsoft.system.cameras
{


    public class FPSCamera : Camera
    {
        private float moveSpeed = 50f;
        private float mouseSensitivity = 0.0015f;
        private GraphicsDevice device;
        private Point screenCenter;
        bool disableControls = false;
        public FPSCamera(GraphicsDevice graphicsDevice, Vector3 startPosition) : base(graphicsDevice)
        {
            device = graphicsDevice;
            Position = startPosition;
            screenCenter = new Point(device.Viewport.Width / 2, device.Viewport.Height / 2);
            Mouse.SetPosition(screenCenter.X, screenCenter.Y);
        }

        /// <summary>
        /// Sets the camera's orientation from a quaternion
        /// </summary>
        /// <param name="rotation">Quaternion representing the desired orientation</param>
        public void SetRotation(Quaternion rotation)
        {
            // Extract yaw and pitch from the quaternion
            Vector3 forward = Vector3.Transform(Vector3.Forward, rotation);
            yaw = (float)Math.Atan2(-forward.X, -forward.Z);
            pitch = (float)Math.Asin(forward.Y);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            // Disable controls when any menu is open (game is paused)
            disableControls = HasActiveMenu();
            
            if (!disableControls)
            {
                float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

                // Handle input
                var k = Keyboard.GetState();
                var mouse = Mouse.GetState();

                float dx = (mouse.X - screenCenter.X) * mouseSensitivity;
                float dy = (mouse.Y - screenCenter.Y) * mouseSensitivity;

                yaw -= dx;
                pitch -= dy;
                pitch = MathHelper.Clamp(pitch, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f);


                if (k.IsKeyDown(Keys.W)) Position += Forward * moveSpeed * delta;
                if (k.IsKeyDown(Keys.S)) Position -= Forward * moveSpeed * delta;
                if (k.IsKeyDown(Keys.A)) Position -= Right * moveSpeed * delta;
                if (k.IsKeyDown(Keys.D)) Position += Right * moveSpeed * delta;


                // Reset cursor
                Mouse.SetPosition(screenCenter.X, screenCenter.Y);
            }

            Target = Position + Forward;

        }

        /// <summary>
        /// Check if any menu screen is currently active (game is paused)
        /// </summary>
        /// <returns>True if any menu is active</returns>
        private bool HasActiveMenu()
        {
            var screens = Globals.screenManager.GetScreens();
            
            foreach (var screen in screens)
            {
                if (screen is rubens_psx_engine.system.MenuScreen && 
                    screen.getState != ScreenState.Deactivated)
                {
                    return true;
                }
            }
            
            return false;
        }
    }

}
