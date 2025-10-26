using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rubens_psx_engine
{
    //Very basic 3D camera.
    public abstract class Camera
    {
        protected float yaw, pitch; // need to define rotation.
        private bool rotationSetExternally = false; // Track if rotation was set via SetRotation()
        protected bool IsRotationLocked => rotationSetExternally; // Allow subclasses to check if rotation is externally controlled

        public Vector3 Position { get; set; }
        public Vector3 Target { get; protected set; }
        public Vector3 Up { get; protected set; } = Vector3.Up;
        public Vector3 Forward { get; protected set; }
        public Vector3 Right { get; protected set; }
        public Matrix View => Matrix.CreateLookAt(Position, Target, Up);
        public Matrix Projection { get; protected set; }

        public Vector2 NearFarPlane = new Vector2(1, 10000f);

        public Camera(GraphicsDevice graphicsDevice)
        {
            float aspectRatio = graphicsDevice.Viewport.AspectRatio;
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, NearFarPlane.X, NearFarPlane.Y);
        }

        public virtual void Update(GameTime gameTime)
        {
            // Only recalculate Forward/Right from yaw/pitch if rotation wasn't set externally
            if (!rotationSetExternally)
            {
                Forward = Vector3.Normalize(Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(yaw, pitch, 0)));
                Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.Up));
            }

            // Reset the flag - rotation is only "external" for one frame
            rotationSetExternally = false;
        }

        /// <summary>
        /// Gets the camera's world rotation as a quaternion.
        /// Converts from view matrix (camera space) to world rotation.
        /// </summary>
        public virtual Quaternion GetRotation()
        {
            return Quaternion.CreateFromRotationMatrix(Matrix.Invert(View));
        }

        /// <summary>
        /// Sets the camera's world rotation from a quaternion.
        /// Updates Forward, Right, Target, yaw, and pitch from the rotation.
        /// </summary>
        public virtual void SetRotation(Quaternion rotation)
        {
            // Apply rotation directly to get Forward and Right vectors
            Forward = Vector3.Normalize(Vector3.Transform(Vector3.Forward, rotation));
            Right = Vector3.Normalize(Vector3.Transform(Vector3.Right, rotation));

            // Update Target to point in the new Forward direction
            Target = Position + Forward;

            // Also update yaw/pitch for subclasses that use them (like FPSCamera)
            yaw = (float)Math.Atan2(-Forward.X, -Forward.Z);
            pitch = (float)Math.Asin(Forward.Y);

            // Mark that rotation was set externally so Update() doesn't overwrite it
            rotationSetExternally = true;
        }
    }
}