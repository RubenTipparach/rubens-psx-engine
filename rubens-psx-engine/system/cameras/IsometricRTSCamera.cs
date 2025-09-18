using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace rubens_psx_engine
{
    public class IsometricRTSCamera : Camera
    {
        private Vector3 targetPosition;
        private float distance;
        private new float pitch;
        private new float yaw;
        private float zoomSpeed;
        private float panSpeed;
        private float rotationSpeed;
        private float minDistance;
        private float maxDistance;
        private Vector2 lastMousePosition;
        private bool isDragging;
        private GraphicsDeviceManager graphics;

        public new Vector3 Target 
        { 
            get => targetPosition; 
            set 
            { 
                targetPosition = value;
                UpdatePosition();
            } 
        }

        public float Distance 
        { 
            get => distance; 
            set 
            { 
                distance = MathHelper.Clamp(value, minDistance, maxDistance);
                UpdatePosition();
            } 
        }

        public float Pitch 
        { 
            get => pitch; 
            set 
            { 
                pitch = MathHelper.Clamp(value, -MathHelper.PiOver2 + 0.1f, MathHelper.PiOver2 - 0.1f);
                UpdatePosition();
            } 
        }

        public float Yaw 
        { 
            get => yaw; 
            set 
            { 
                yaw = value;
                UpdatePosition();
            } 
        }

        public IsometricRTSCamera(GraphicsDeviceManager graphics) : base(graphics.GraphicsDevice)
        {
            this.graphics = graphics;
            targetPosition = Vector3.Zero;
            distance = 50.0f;
            pitch = -MathHelper.PiOver4;
            yaw = MathHelper.PiOver4;
            zoomSpeed = 5.0f;
            panSpeed = 0.5f;
            rotationSpeed = 0.01f;
            minDistance = 10.0f;
            maxDistance = 200.0f;

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            float x = targetPosition.X + distance * (float)(Math.Cos(pitch) * Math.Cos(yaw));
            float y = targetPosition.Y + distance * (float)Math.Sin(pitch);
            float z = targetPosition.Z + distance * (float)(Math.Cos(pitch) * Math.Sin(yaw));

            Position = new Vector3(x, y, z);
        }

        public new void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();
            
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Vector2 currentMousePosition = new Vector2(mouseState.X, mouseState.Y);

            HandleKeyboardInput(keyboardState, deltaTime);
            HandleMouseInput(mouseState, currentMousePosition, deltaTime);

            lastMousePosition = currentMousePosition;
        }

        private void HandleKeyboardInput(KeyboardState keyboardState, float deltaTime)
        {
            Vector3 forward = Vector3.Normalize(new Vector3((float)Math.Cos(yaw), 0, (float)Math.Sin(yaw)));
            Vector3 right = Vector3.Cross(forward, Vector3.Up);

            Vector3 movement = Vector3.Zero;

            if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
                movement += forward;
            if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
                movement -= forward;
            if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
                movement -= right;
            if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
                movement += right;

            if (movement != Vector3.Zero)
            {
                movement.Normalize();
                Target += movement * panSpeed * distance * deltaTime;
            }

            if (keyboardState.IsKeyDown(Keys.Q))
                Yaw -= rotationSpeed * deltaTime * 10;
            if (keyboardState.IsKeyDown(Keys.E))
                Yaw += rotationSpeed * deltaTime * 10;

            if (keyboardState.IsKeyDown(Keys.R))
                Distance -= zoomSpeed * deltaTime * 10;
            if (keyboardState.IsKeyDown(Keys.F))
                Distance += zoomSpeed * deltaTime * 10;
        }

        private void HandleMouseInput(MouseState mouseState, Vector2 currentMousePosition, float deltaTime)
        {
            if (mouseState.MiddleButton == ButtonState.Pressed)
            {
                if (!isDragging)
                {
                    isDragging = true;
                    lastMousePosition = currentMousePosition;
                }
                else
                {
                    Vector2 mouseDelta = currentMousePosition - lastMousePosition;
                    
                    Vector3 forward = Vector3.Normalize(new Vector3((float)Math.Cos(yaw), 0, (float)Math.Sin(yaw)));
                    Vector3 right = Vector3.Cross(forward, Vector3.Up);

                    Vector3 panMovement = (-right * mouseDelta.X + forward * mouseDelta.Y) * panSpeed * 0.1f;
                    Target += panMovement;
                }
            }
            else
            {
                isDragging = false;
            }

            if (mouseState.RightButton == ButtonState.Pressed)
            {
                if (lastMousePosition != Vector2.Zero)
                {
                    Vector2 mouseDelta = currentMousePosition - lastMousePosition;
                    Yaw += mouseDelta.X * rotationSpeed;
                    Pitch += mouseDelta.Y * rotationSpeed;
                }
            }

            int scrollDelta = mouseState.ScrollWheelValue - (lastMousePosition != Vector2.Zero ? 
                (int)lastMousePosition.X : mouseState.ScrollWheelValue);
            if (scrollDelta != 0)
            {
                Distance -= scrollDelta * 0.01f;
            }
        }

        public Vector3 ScreenToWorld(Vector2 screenPosition, float? heightPlane = null)
        {
            Vector3 nearPoint = graphics.GraphicsDevice.Viewport.Unproject(
                new Vector3(screenPosition, 0), 
                Projection, View, Matrix.Identity);
            
            Vector3 farPoint = graphics.GraphicsDevice.Viewport.Unproject(
                new Vector3(screenPosition, 1), 
                Projection, View, Matrix.Identity);

            Vector3 direction = Vector3.Normalize(farPoint - nearPoint);
            
            float targetY = heightPlane ?? targetPosition.Y;
            float t = (targetY - nearPoint.Y) / direction.Y;
            
            return nearPoint + direction * t;
        }

        public void FocusOn(Vector3 worldPosition)
        {
            Target = worldPosition;
        }

        public void SetIsometricView()
        {
            pitch = -MathHelper.PiOver4;
            yaw = MathHelper.PiOver4;
            UpdatePosition();
        }
    }
}