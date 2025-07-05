using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace anakinsoft.system.cameras
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;
    using rubens_psx_engine;

    public class OrbitCamera : Camera
    {
        private float distance = 10f;
        private float yaw = 0f;
        private float pitch = 0f;
        private float sensitivity = 0.01f;
        private float orbitSpeed = 2f;

        public OrbitCamera(GraphicsDevice graphicsDevice, Vector3 target) : base(graphicsDevice)
        {
            Target = target;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            //float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            //MouseState mouse = Mouse.GetState();
            //yaw -= mouse.X * sensitivity;
            //pitch -= mouse.Y * sensitivity;
            //pitch = MathHelper.Clamp(pitch, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f);
            //Mouse.SetPosition(400, 300);

            //Vector3 offset = Vector3.Transform(new Vector3(0, 0, distance),
            //    Matrix.CreateFromYawPitchRoll(yaw, pitch, 0));
            //Position = Target + offset;

            //// Optional: WASD movement of the orbit center on XZ plane
            //KeyboardState keys = Keyboard.GetState();
            //Vector3 move = Vector3.Zero;
            //if (keys.IsKeyDown(Keys.W)) move.Z -= orbitSpeed * delta;
            //if (keys.IsKeyDown(Keys.S)) move.Z += orbitSpeed * delta;
            //if (keys.IsKeyDown(Keys.A)) move.X -= orbitSpeed * delta;
            //if (keys.IsKeyDown(Keys.D)) move.X += orbitSpeed * delta;
            //Target += move;

            //View = Matrix.CreateLookAt(Position, Target, Vector3.Up);
        }
    }

}
