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

        // Quaternion-based orientation
        private Quaternion orientation;

        private bool isRightMouseDown = false;
        private Point lastMousePosition;

        // Height-based speed scaling
        private rubens_psx_engine.system.procedural.ProceduralPlanetGenerator planetGenerator;
        private float planetRadius = 20f;

        // Frustum update state
        private int currentFrustumLevel = -1; // -1 = uninitialized, 0 = ground, 1 = transition, 2 = space
        private float aspectRatio;

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

        public void SetPlanetContext(rubens_psx_engine.system.procedural.ProceduralPlanetGenerator generator, float radius)
        {
            planetGenerator = generator;
            planetRadius = radius;
        }

        public EditorCamera(GraphicsDevice graphicsDevice, Vector3 position) : base(graphicsDevice)
        {
            Position = position;

            // Calculate initial orientation to look at planet center (0,0,0)
            Vector3 directionToPlanet = Vector3.Normalize(Vector3.Zero - position);
            Vector3 up = Vector3.Up;

            // Create orientation quaternion from look direction
            Matrix lookMatrix = Matrix.CreateWorld(Vector3.Zero, directionToPlanet, up);
            orientation = Quaternion.CreateFromRotationMatrix(lookMatrix);

            NearFarPlane = new Vector2(.001f, 1000f);
            aspectRatio = graphicsDevice.Viewport.AspectRatio;
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

            // Update camera vectors after rotation but before movement

            // Handle keyboard movement (uses updated Forward/Right/Up vectors)
            HandleKeyboardMovement(keyboardState, deltaTime);

            // Apply velocity with friction
            Position += velocity * deltaTime;
            velocity *= friction;
            UpdateVectors();

            // Update frustum based on height above ground
            UpdateFrustumBasedOnHeight();

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
                    // Just pressed - store initial mouse position
                    lastMousePosition = new Point(mouseState.X, mouseState.Y);
                    isRightMouseDown = true;
                }
                else
                {
                    // Calculate mouse delta from last frame
                    int deltaX = mouseState.X - lastMousePosition.X;
                    int deltaY = mouseState.Y - lastMousePosition.Y;

                    if (deltaX != 0 || deltaY != 0)
                    {
                        // Get current camera axes using helper functions
                        Vector3 currentRight = GetRightVector();
                        Vector3 currentUp = GetUpVector();

                        // Create rotation quaternions around current axes
                        Quaternion pitchRotation = Quaternion.CreateFromAxisAngle(currentRight, -deltaY * rotationSpeed);
                        Quaternion yawRotation = Quaternion.CreateFromAxisAngle(currentUp, -deltaX * rotationSpeed);

                        // Apply rotations: pitch first, then yaw
                        orientation = yawRotation * pitchRotation * orientation;
                        orientation.Normalize();

                        // Update last mouse position
                        lastMousePosition = new Point(mouseState.X, mouseState.Y);
                    }
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
            float baseSpeed = keyboardState.IsKeyDown(Keys.LeftShift) ? fastMoveSpeed : moveSpeed;

            // Scale speed based on height above ground
            float currentSpeed = baseSpeed;
            if (planetGenerator != null)
            {
                float heightAboveGround = GetHeightAboveGround(planetGenerator, planetRadius);
                // Minimum speed at 0.1 distance per second, scales up with height
                float speedMultiplier = MathHelper.Clamp(heightAboveGround / 10f, 0.1f, 1f);
                currentSpeed = baseSpeed * speedMultiplier;
            }

            // Get current direction vectors from quaternion
            Vector3 forward = GetForwardVector();
            Vector3 right = GetRightVector();
            Vector3 up = GetUpVector();

            // Forward/Backward (W/S)
            if (keyboardState.IsKeyDown(Keys.W))
                moveVector += forward;
            if (keyboardState.IsKeyDown(Keys.S))
                moveVector -= forward;

            // Strafe (A/D) - calculate strafe direction based on roll
            //Matrix rotMatrix = Matrix.CreateFromQuaternion(orientation);
            //float roll = MathF.Atan2(rotMatrix.M23, rotMatrix.M33);
            //Vector3 strafeDir = Vector3.Normalize(Vector3.Transform(right, Matrix.CreateFromAxisAngle(forward, roll)));
            if (keyboardState.IsKeyDown(Keys.A))
                moveVector -= right;
            if (keyboardState.IsKeyDown(Keys.D))
                moveVector += right;

            // Up/Down (Space/Ctrl)
            if (keyboardState.IsKeyDown(Keys.Space))
                moveVector += up;
            if (keyboardState.IsKeyDown(Keys.LeftControl))
                moveVector -= up;

            // Camera Roll (Q/E) - rotate orientation around forward axis
            if (keyboardState.IsKeyDown(Keys.Q))
            {
                Quaternion rollRotation = Quaternion.CreateFromAxisAngle(forward, rollSpeed * deltaTime);
                orientation = rollRotation * orientation;
                orientation.Normalize();
            }
            if (keyboardState.IsKeyDown(Keys.E))
            {
                Quaternion rollRotation = Quaternion.CreateFromAxisAngle(forward, -rollSpeed * deltaTime);
                orientation = rollRotation * orientation;
                orientation.Normalize();
            }

            // Normalize and apply speed
            if (moveVector != Vector3.Zero)
            {
                moveVector.Normalize();
                velocity += moveVector * currentSpeed * deltaTime * 10f;
            }
        }

        private Vector3 GetForwardVector()
        {
            return Vector3.Normalize(Vector3.Transform(Vector3.Forward, orientation));
        }

        private Vector3 GetRightVector()
        {
            return Vector3.Normalize(Vector3.Transform(Vector3.Right, orientation));
        }

        private Vector3 GetUpVector()
        {
            return Vector3.Normalize(Vector3.Transform(Vector3.Up, orientation));
        }

        public float GetHeightAboveGround(rubens_psx_engine.system.procedural.ProceduralPlanetGenerator planetGenerator, float planetRadius)
        {
            // Get normalized direction from planet center to camera
            Vector3 direction = Vector3.Normalize(Position);

            // Convert to spherical UV coordinates
            float u = MathF.Atan2(direction.X, direction.Z) / (2.0f * MathF.PI) + 0.5f;
            float v = MathF.Asin(MathHelper.Clamp(direction.Y, -1.0f, 1.0f)) / MathF.PI + 0.5f;

            // Sample height from heightmap (0-1 range)
            float heightSample = planetGenerator.SampleHeightAtUV(u, v);

            // Calculate terrain height at this position (matching shader calculation)
            float heightScale = planetRadius * 0.1f; // 10% of radius
            float terrainHeight = planetRadius + (heightSample - 0.5f) * heightScale * 2f;

            // Calculate distance from camera to planet center
            float cameraDistance = Position.Length();

            // Return height above ground
            return cameraDistance - terrainHeight;
        }

        private void UpdateFrustumBasedOnHeight()
        {
            if (planetGenerator == null)
                return;

            float heightAboveGround = GetHeightAboveGround(planetGenerator, planetRadius);

            // Determine which level we should be at with hysteresis
            int targetLevel;

            if (currentFrustumLevel == -1)
            {
                // First time - initialize based on current height
                if (heightAboveGround < 10f)
                    targetLevel = 0; // Ground
                else if (heightAboveGround < 100f)
                    targetLevel = 1; // Transition
                else
                    targetLevel = 2; // Space
            }
            else
            {
                // Use hysteresis to prevent flickering at boundaries
                if (currentFrustumLevel == 0) // Currently at ground level
                {
                    targetLevel = heightAboveGround > 15f ? 1 : 0; // Need to go 5 units above threshold
                }
                else if (currentFrustumLevel == 1) // Currently at transition level
                {
                    if (heightAboveGround < 8f)
                        targetLevel = 0; // Drop to ground (2 units hysteresis)
                    else if (heightAboveGround > 110f)
                        targetLevel = 2; // Move to space (10 units hysteresis)
                    else
                        targetLevel = 1; // Stay in transition
                }
                else // currentFrustumLevel == 2, currently at space level
                {
                    targetLevel = heightAboveGround < 90f ? 1 : 2; // Need to drop 10 units below threshold
                }
            }

            // Only update if level changed
            if (targetLevel != currentFrustumLevel)
            {
                currentFrustumLevel = targetLevel;

                float nearPlane, farPlane;

                if (currentFrustumLevel == 0)
                {
                    // Ground level
                    nearPlane = 0.001f;
                    farPlane = 1000f;
                }
                else if (currentFrustumLevel == 1)
                {
                    // Transition level
                    nearPlane = 0.1f;
                    farPlane = 3000f;
                }
                else
                {
                    // Space level
                    nearPlane = 10f;
                    farPlane = 10000f;
                }

                NearFarPlane = new Vector2(nearPlane, farPlane);

                // Update projection matrix with new planes (using stored aspect ratio)
                Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, nearPlane, farPlane);
            }
        }

        private void UpdateVectors()
        {
            // Extract camera axes from orientation quaternion using helper functions
            Forward = GetForwardVector();
            Right = GetRightVector();
            Up = GetUpVector();

            // Update target
            Target = Position + Forward;
        }
    }
}