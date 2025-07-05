using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rubens_psx_engine
{
    //Very basic 3D camera.
    public abstract class Camera
    {
        protected float yaw, pitch; // need to define rotation.

        public Vector3 Position { get; set; }
        public Vector3 Target { get; protected set; }
        public Vector3 Up { get; protected set; } = Vector3.Up;
        public Vector3 Forward { get; protected set; }
        public Vector3 Right { get; protected set; }
        public Matrix View => Matrix.CreateLookAt(Position, Target, Up);
        public Matrix Projection { get; protected set; }

        public Camera(GraphicsDevice graphicsDevice)
        {
            float aspectRatio = graphicsDevice.Viewport.AspectRatio;
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.1f, 1000f);
        }

        public virtual void Update(GameTime gameTime)
        {

            Forward = Vector3.Normalize(Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(yaw, pitch, 0)));
            Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.Up));

        }
    }
}