using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace anakinsoft.system.cameras
{
    public class EditorCamera : rubens_psx_engine.Camera
    {
        private float moveSpeed = 20f;
        private float fastMoveSpeed = 60f;
        private float rotationSpeed = 0.003f;
        private float rollSpeed = 1.5f;

        private Vector3 velocity;
        private float friction = 0.9f;
        private float roll = 0f;

        private bool isRightMouseDown = false;
        private Point lastMousePosition;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Math.Max(0.1f, value);
        }

        public float RotationSpeed
        {
            get => rotationSpeed;
            set => rotationSpeed = Math.Max(0.0001f, value);
        }

        public EditorCamera(GraphicsDevice graphicsDevice, Vector3 position) : base(graphicsDevice)
        {
            Position = position;

            // Calculate initial yaw and pitch to look at planet center (0,0,0)
            Vector3 directionToPlanet = Vector3.Normalize(position- Vector3.Zero);
            yaw = MathF.Atan2(directionToPlanet.X, directionToPlanet.Z);
            pitch = MathF.Asin(-directionToPlanet.Y);

            NearFarPlane = new Vector2(.001f, 1000f);
            float aspectRatio = graphicsDevice.Viewport.AspectRatio;
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, NearFarPlane.X, NearFarPlane.Y);

            UpdateVectors();
        }

        public override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Handle mouse rotation (only when right mouse is held)
            HandleMouseRotation(mouseState);

            // Handle keyboard movement
            HandleKeyboardMovement(keyboardState, deltaTime);

            // Apply velocity with friction
            Position += velocity * deltaTime;
            velocity *= friction;

            // Update camera vectors
            UpdateVectors();

            base.Update(gameTime);
        }

        private void HandleMouseRotation(MouseState mouseState)
        {
            // Check right mouse button state
            bool rightMouseCurrentlyDown = mouseState.RightButton == ButtonState.Pressed;

            if (rightMouseCurrentlyDown)
            {
                if (!isRightMouseDown)
                {
                    // Just pressed - store initial position
                    lastMousePosition = new Point(mouseState.X, mouseState.Y);
                    isRightMouseDown = true;
                }
                else
                {
                    // Calculate mouse delta
                    int deltaX = mouseState.X - lastMousePosition.X;
                    int deltaY = mouseState.Y - lastMousePosition.Y;

                    // Apply rotation
                    yaw -= deltaX * rotationSpeed;
                    pitch -= deltaY * rotationSpeed;

                    // Clamp pitch to prevent camera flip
                    pitch = MathHelper.Clamp(pitch, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f);

                    // Update last position
                    lastMousePosition = new Point(mouseState.X, mouseState.Y);
                }
            }
            else
            {
                isRightMouseDown = false;
            }

            // Mouse wheel for zoom speed adjustment
            int scrollDelta = mouseState.ScrollWheelValue - lastScrollValue;
            if (scrollDelta != 0)
            {
                moveSpeed *= 1f + (scrollDelta / 1200f);
                moveSpeed = MathHelper.Clamp(moveSpeed, 1f, 200f);
            }
            lastScrollValue = mouseState.ScrollWheelValue;
        }

        private int lastScrollValue = 0;

        private void HandleKeyboardMovement(KeyboardState keyboardState, float deltaTime)
        {
            Vector3 moveVector = Vector3.Zero;
            float currentSpeed = keyboardState.IsKeyDown(Keys.LeftShift) ? fastMoveSpeed : moveSpeed;

            // Forward/Backward (W/S)
            if (keyboardState.IsKeyDown(Keys.W))
                moveVector += Forward;
            if (keyboardState.IsKeyDown(Keys.S))
                moveVector -= Forward;

            // Strafe (A/D)
            if (keyboardState.IsKeyDown(Keys.A))
                moveVector -= Right;
            if (keyboardState.IsKeyDown(Keys.D))
                moveVector += Right;

            // Up/Down (Space/Ctrl)
            if (keyboardState.IsKeyDown(Keys.Space))
                moveVector += Vector3.Up;
            if (keyboardState.IsKeyDown(Keys.LeftControl))
                moveVector -= Vector3.Up;

            // Camera Roll (Q/E)
            if (keyboardState.IsKeyDown(Keys.Q))
                roll += rollSpeed * deltaTime;
            if (keyboardState.IsKeyDown(Keys.E))
                roll -= rollSpeed * deltaTime;

            // Normalize and apply speed
            if (moveVector != Vector3.Zero)
            {
                moveVector.Normalize();
                velocity += moveVector * currentSpeed * deltaTime * 10f;
            }
        }

        private void UpdateVectors()
        {
            // Use the same coordinate system as base Camera class with roll support
            Forward = Vector3.Normalize(Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(yaw, pitch, roll)));
            Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.Up));
            Up = Vector3.Normalize(Vector3.Cross(Right, Forward));

            // Update target
            Target = Position + Forward;
        }
    }
}