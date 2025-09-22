using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine.system.vehicles;
using rubens_psx_engine;
using System;

namespace anakinsoft.system.cameras
{
    public class VehicleCamera : Camera
    {
        private PodracerVehicle targetVehicle;
        private float cameraDistance = 20f;
        private float cameraHeight = 8f;
        private float cameraLookAhead = 5f;
        private float smoothSpeed = 5f;
        private float rotationSmoothSpeed = 8f;

        private Vector3 currentPosition;
        private Vector3 currentLookAt;
        private Vector3 velocityOffset;

        private bool allowFreeLook = false;
        private float freeLookYaw = 0f;
        private float freeLookPitch = 0f;
        private float mouseSensitivity = 0.002f;
        private Point lastMousePos;
        private GraphicsDevice device;

        public PodracerVehicle TargetVehicle
        {
            get => targetVehicle;
            set => targetVehicle = value;
        }

        public float CameraDistance
        {
            get => cameraDistance;
            set => cameraDistance = MathHelper.Clamp(value, 5f, 50f);
        }

        public float CameraHeight
        {
            get => cameraHeight;
            set => cameraHeight = MathHelper.Clamp(value, 2f, 30f);
        }

        public VehicleCamera(GraphicsDevice graphicsDevice, PodracerVehicle vehicle) : base(graphicsDevice)
        {
            device = graphicsDevice;
            targetVehicle = vehicle;

            if (vehicle != null)
            {
                currentPosition = vehicle.Position - vehicle.Forward * cameraDistance + Vector3.Up * cameraHeight;
                currentLookAt = vehicle.Position;
            }
            else
            {
                currentPosition = Vector3.Zero;
                currentLookAt = Vector3.Forward;
            }

            Position = currentPosition;
            Target = currentLookAt;
        }

        public override void Update(GameTime gameTime)
        {
            if (targetVehicle == null)
            {
                base.Update(gameTime);
                return;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();

            HandleCameraControls(keyboardState, mouseState);

            Vector3 vehiclePosition = targetVehicle.Position;
            Vector3 vehicleForward = targetVehicle.Forward;
            Vector3 vehicleVelocity = targetVehicle.Velocity;

            float speed = vehicleVelocity.Length();
            velocityOffset = Vector3.Lerp(velocityOffset, vehicleVelocity * 0.02f, deltaTime * 2f);

            Vector3 desiredPosition;
            Vector3 desiredLookAt;

            if (allowFreeLook)
            {
                Matrix freeLookRotation = Matrix.CreateFromYawPitchRoll(freeLookYaw, freeLookPitch, 0);
                Vector3 freeLookOffset = Vector3.Transform(new Vector3(0, 0, -cameraDistance), freeLookRotation);
                desiredPosition = vehiclePosition + freeLookOffset + Vector3.Up * cameraHeight;
                desiredLookAt = vehiclePosition + vehicleForward * cameraLookAhead;
            }
            else
            {
                float dynamicDistance = cameraDistance + speed * 0.05f;
                float dynamicHeight = cameraHeight + speed * 0.02f;

                desiredPosition = vehiclePosition - vehicleForward * dynamicDistance +
                    Vector3.Up * dynamicHeight - velocityOffset;

                desiredLookAt = vehiclePosition + vehicleForward * (cameraLookAhead + speed * 0.1f);
            }

            currentPosition = Vector3.Lerp(currentPosition, desiredPosition, smoothSpeed * deltaTime);
            currentLookAt = Vector3.Lerp(currentLookAt, desiredLookAt, rotationSmoothSpeed * deltaTime);

            Position = currentPosition;
            Target = currentLookAt;
            Up = Vector3.Up;

            base.Update(gameTime);
        }

        private void HandleCameraControls(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.PageUp))
            {
                cameraDistance = Math.Max(5f, cameraDistance - 0.5f);
            }
            if (keyboardState.IsKeyDown(Keys.PageDown))
            {
                cameraDistance = Math.Min(50f, cameraDistance + 0.5f);
            }

            if (keyboardState.IsKeyDown(Keys.Home))
            {
                cameraHeight = Math.Min(30f, cameraHeight + 0.3f);
            }
            if (keyboardState.IsKeyDown(Keys.End))
            {
                cameraHeight = Math.Max(2f, cameraHeight - 0.3f);
            }

            if (mouseState.RightButton == ButtonState.Pressed)
            {
                if (!allowFreeLook)
                {
                    allowFreeLook = true;
                    lastMousePos = new Point(mouseState.X, mouseState.Y);
                    freeLookYaw = 0f;
                    freeLookPitch = 0f;
                }

                int deltaX = mouseState.X - lastMousePos.X;
                int deltaY = mouseState.Y - lastMousePos.Y;

                freeLookYaw -= deltaX * mouseSensitivity;
                freeLookPitch -= deltaY * mouseSensitivity;

                freeLookPitch = MathHelper.Clamp(freeLookPitch, -MathHelper.PiOver2 * 0.8f, MathHelper.PiOver2 * 0.8f);

                lastMousePos = new Point(mouseState.X, mouseState.Y);
            }
            else
            {
                allowFreeLook = false;
            }
        }

        public void SetTarget(PodracerVehicle vehicle)
        {
            targetVehicle = vehicle;

            if (vehicle != null)
            {
                currentPosition = vehicle.Position - vehicle.Forward * cameraDistance + Vector3.Up * cameraHeight;
                currentLookAt = vehicle.Position;
            }
        }

        public void Reset()
        {
            if (targetVehicle != null)
            {
                currentPosition = targetVehicle.Position - targetVehicle.Forward * cameraDistance +
                    Vector3.Up * cameraHeight;
                currentLookAt = targetVehicle.Position;
                velocityOffset = Vector3.Zero;
                freeLookYaw = 0f;
                freeLookPitch = 0f;
                allowFreeLook = false;
            }
        }
    }
}