using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rubens_psx_engine
{
    //Very basic 3D camera.
    public abstract class Camera
    {
        public Vector3 Position { get; set; }
        public Vector3 Target { get; protected set; }
        public Vector3 Up { get; protected set; } = Vector3.Up;
        public Vector3 Forward => Target;// you sure? lol
        public Matrix View => Matrix.CreateLookAt(Position, Target, Up);
        public Matrix Projection { get; protected set; }

        public Camera(GraphicsDevice graphicsDevice)
        {
            float aspectRatio = graphicsDevice.Viewport.AspectRatio;
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.1f, 1000f);
        }

        public abstract void Update(GameTime gameTime);
    }
}