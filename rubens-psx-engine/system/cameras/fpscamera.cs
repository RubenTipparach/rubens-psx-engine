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
        private float yaw, pitch;
        private float moveSpeed = 50f;
        private float mouseSensitivity = 0.0015f;
        private GraphicsDevice device;
        private Point screenCenter;

        public FPSCamera(GraphicsDevice graphicsDevice, Vector3 startPosition) : base(graphicsDevice)
        {
            device = graphicsDevice;
            Position = startPosition;
            screenCenter = new Point(device.Viewport.Width / 2, device.Viewport.Height / 2);
            Mouse.SetPosition(screenCenter.X, screenCenter.Y);
        }

        public override void Update(GameTime gameTime)
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

            Vector3 forward = Vector3.Normalize(Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(yaw, pitch, 0)));
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.Up));

            if (k.IsKeyDown(Keys.W)) Position += forward * moveSpeed * delta;
            if (k.IsKeyDown(Keys.S)) Position -= forward * moveSpeed * delta;
            if (k.IsKeyDown(Keys.A)) Position -= right * moveSpeed * delta;
            if (k.IsKeyDown(Keys.D)) Position += right * moveSpeed * delta;

            Target = Position + forward;

            // Reset cursor
            Mouse.SetPosition(screenCenter.X, screenCenter.Y);
        }
    }

}
