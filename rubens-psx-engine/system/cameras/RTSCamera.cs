using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace rubens_psx_engine
{
    public class RTSCamera : Camera
    {
        private Vector3 cameraPosition;
        private float height;
        private float panSpeed;
        private float zoomSpeed;
        private float minHeight;
        private float maxHeight;
        private float viewAngle; // Fixed angle below horizon
        private Vector2 terrainBounds; // For clamping camera movement
        private GraphicsDeviceManager graphics;
        
        private KeyboardState previousKeyboardState;
        private MouseState previousMouseState;

        public Vector3 CameraPosition 
        { 
            get => cameraPosition; 
            set 
            { 
                cameraPosition = ClampToTerrain(value);
                UpdateCameraMatrices();
            } 
        }

        public float Height 
        { 
            get => height; 
            set 
            { 
                height = MathHelper.Clamp(value, minHeight, maxHeight);
                UpdateCameraMatrices();
            } 
        }

        public float ViewAngle => viewAngle;

        public RTSCamera(GraphicsDeviceManager graphics) : base(graphics.GraphicsDevice)
        {
            this.graphics = graphics;
            cameraPosition = Vector3.Zero;
            height = 50.0f;
            panSpeed = 100.0f;
            zoomSpeed = 50.0f;
            minHeight = 20.0f;
            maxHeight = 200.0f;
            viewAngle = MathHelper.ToRadians(45.0f); // 45 degrees below horizon
            terrainBounds = new Vector2(1000, 1000); // Default large bounds
            
            UpdateCameraMatrices();
        }

        public void SetTerrainBounds(float width, float height)
        {
            terrainBounds = new Vector2(width, height);
        }

        private Vector3 ClampToTerrain(Vector3 position)
        {
            return new Vector3(
                MathHelper.Clamp(position.X, 0, terrainBounds.X),
                position.Y,
                MathHelper.Clamp(position.Z, 0, terrainBounds.Y)
            );
        }

        private void UpdateCameraMatrices()
        {
            // Calculate camera position above the target point
            float distanceFromTarget = height / (float)Math.Tan(viewAngle);
            
            Position = new Vector3(
                cameraPosition.X,
                cameraPosition.Y + height,
                cameraPosition.Z + distanceFromTarget
            );

            // Camera always looks down at the target position
            Vector3 target = new Vector3(cameraPosition.X, cameraPosition.Y, cameraPosition.Z);
            
            // Update base class properties
            base.Target = target;
        }

        public new void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();
            
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            HandleKeyboardInput(keyboardState, deltaTime);
            HandleMouseInput(mouseState, deltaTime);

            previousKeyboardState = keyboardState;
            previousMouseState = mouseState;
        }

        private void HandleKeyboardInput(KeyboardState keyboardState, float deltaTime)
        {
            Vector3 movement = Vector3.Zero;

            // WASD for horizontal panning only
            if (keyboardState.IsKeyDown(Keys.W))
                movement.Z -= 1.0f;
            if (keyboardState.IsKeyDown(Keys.S))
                movement.Z += 1.0f;
            if (keyboardState.IsKeyDown(Keys.A))
                movement.X -= 1.0f;
            if (keyboardState.IsKeyDown(Keys.D))
                movement.X += 1.0f;

            // Arrow keys as alternative
            if (keyboardState.IsKeyDown(Keys.Up))
                movement.Z -= 1.0f;
            if (keyboardState.IsKeyDown(Keys.Down))
                movement.Z += 1.0f;
            if (keyboardState.IsKeyDown(Keys.Left))
                movement.X -= 1.0f;
            if (keyboardState.IsKeyDown(Keys.Right))
                movement.X += 1.0f;

            // Apply movement
            if (movement != Vector3.Zero)
            {
                movement.Normalize();
                CameraPosition += movement * panSpeed * deltaTime;
            }

            // Height adjustment (zoom)
            if (keyboardState.IsKeyDown(Keys.Q))
                Height -= zoomSpeed * deltaTime;
            if (keyboardState.IsKeyDown(Keys.E))
                Height += zoomSpeed * deltaTime;
        }

        private void HandleMouseInput(MouseState mouseState, float deltaTime)
        {
            // Mouse wheel for zoom
            if (previousMouseState.ScrollWheelValue != 0)
            {
                int scrollDelta = mouseState.ScrollWheelValue - previousMouseState.ScrollWheelValue;
                Height -= scrollDelta * 0.01f;
            }

            // Middle mouse button for panning
            if (mouseState.MiddleButton == ButtonState.Pressed && previousMouseState.MiddleButton == ButtonState.Pressed)
            {
                var mouseDelta = mouseState.Position - previousMouseState.Position;
                
                Vector3 panMovement = new Vector3(
                    -mouseDelta.X * 0.1f,
                    0,
                    mouseDelta.Y * 0.1f
                );
                
                CameraPosition += panMovement;
            }
        }

        public Vector3 ScreenToWorld(Vector2 screenPosition, float? heightPlane = null)
        {
            var viewport = graphics.GraphicsDevice.Viewport;
            
            Vector3 nearPoint = viewport.Unproject(
                new Vector3(screenPosition, 0), 
                Projection, View, Matrix.Identity);
            
            Vector3 farPoint = viewport.Unproject(
                new Vector3(screenPosition, 1), 
                Projection, View, Matrix.Identity);

            Vector3 direction = Vector3.Normalize(farPoint - nearPoint);
            
            float targetY = heightPlane ?? 0.0f;
            float t = (targetY - nearPoint.Y) / direction.Y;
            
            return nearPoint + direction * t;
        }

        public void FocusOn(Vector3 worldPosition)
        {
            CameraPosition = new Vector3(worldPosition.X, worldPosition.Y, worldPosition.Z);
        }

        // Get camera frustum corners for minimap display
        public Vector3[] GetFrustumCorners(float groundLevel = 0.0f)
        {
            var viewport = graphics.GraphicsDevice.Viewport;
            
            // Get the four corners of the screen
            Vector2[] screenCorners = new Vector2[]
            {
                new Vector2(0, 0), // Top-left
                new Vector2(viewport.Width, 0), // Top-right
                new Vector2(viewport.Width, viewport.Height), // Bottom-right
                new Vector2(0, viewport.Height) // Bottom-left
            };

            Vector3[] worldCorners = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                worldCorners[i] = ScreenToWorld(screenCorners[i], groundLevel);
            }

            return worldCorners;
        }

        // Get camera center position on the ground plane
        public Vector3 GetGroundCenter(float groundLevel = 0.0f)
        {
            var viewport = graphics.GraphicsDevice.Viewport;
            Vector2 screenCenter = new Vector2(viewport.Width * 0.5f, viewport.Height * 0.5f);
            return ScreenToWorld(screenCenter, groundLevel);
        }
    }
}