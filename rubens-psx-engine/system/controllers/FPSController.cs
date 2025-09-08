using anakinsoft.system.character;
using anakinsoft.system.physics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;
using Vector3N = System.Numerics.Vector3;

namespace rubens_psx_engine.system.controllers
{
    /// <summary>
    /// First-person physics-based player controller
    /// </summary>
    public class FPSController : IPlayerController
    {
        private PhysicsSystem physicsSystem;
        private CharacterControllers characterControllers;
        private BodyHandle characterBodyHandle;
        private int characterIndex;
        
        // Input tracking
        private MouseState lastMouseState;
        private bool mouseLocked = false;
        private Vector2 mouseSensitivity = new Vector2(0.003f);

        public FPSController(PhysicsSystem physics, CharacterControllers characters)
        {
            physicsSystem = physics;
            characterControllers = characters;
            
            CreateCharacterController();
            lastMouseState = Mouse.GetState();
        }

        private void CreateCharacterController()
        {
            // Create character body (capsule)
            var capsuleShape = new Capsule(0.5f, 1.0f); // radius, length
            
            var bodyDescription = BodyDescription.CreateConvexDynamic(
                pose: new RigidPose(new Vector3N(0, 5, 0)), // Start position
                velocity: new BodyVelocity(),
                mass: 1f,
                shapes: physicsSystem.Simulation.Shapes,
                shape: capsuleShape);

            characterBodyHandle = physicsSystem.Simulation.Bodies.Add(bodyDescription);

            // Create character controller
            ref var character = ref characterControllers.AllocateCharacter(characterBodyHandle);
            character.LocalUp = Vector3N.UnitY;
            character.CosMaximumSlope = MathF.Cos(MathF.PI * 0.25f); // 45 degree slope
            character.JumpVelocity = 8;
            character.MaximumHorizontalForce = 20;
            character.MaximumVerticalForce = 100;
            character.MinimumSupportDepth = -0.01f;
            character.MinimumSupportContinuationDepth = -0.1f;
            character.ViewDirection = Vector3N.UnitZ; // Forward
            
            characterIndex = characterControllers.GetCharacterIndexForBodyHandle(characterBodyHandle.Value);
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            UpdateCharacterInput(dt, keyboard, mouse);
        }

        private void UpdateCharacterInput(float dt, KeyboardState keyboard, MouseState mouse)
        {
            ref var character = ref characterControllers.GetCharacterByIndex(characterIndex);
            
            // Mouse look
            if (mouseLocked)
            {
                var mouseDelta = new Vector2(
                    mouse.X - lastMouseState.X, 
                    mouse.Y - lastMouseState.Y
                );

                // Update view direction based on mouse movement
                var yaw = -mouseDelta.X * mouseSensitivity.X;
                var pitch = -mouseDelta.Y * mouseSensitivity.Y;

                // Apply rotation to view direction
                var currentDirection = character.ViewDirection;
                var rotationY = System.Numerics.Matrix4x4.CreateRotationY(yaw);
                character.ViewDirection = Vector3N.Transform(currentDirection, rotationY);
                character.ViewDirection = Vector3N.Normalize(character.ViewDirection);
            }

            lastMouseState = mouse;

            // Movement input
            var targetVelocity = Vector3N.Zero;
            if (keyboard.IsKeyDown(Keys.W)) targetVelocity += character.ViewDirection;
            if (keyboard.IsKeyDown(Keys.S)) targetVelocity -= character.ViewDirection;
            if (keyboard.IsKeyDown(Keys.A)) targetVelocity += Vector3N.Cross(character.LocalUp, character.ViewDirection);
            if (keyboard.IsKeyDown(Keys.D)) targetVelocity -= Vector3N.Cross(character.LocalUp, character.ViewDirection);

            var speed = 6.0f;
            if (targetVelocity.LengthSquared() > 0)
            {
                targetVelocity = Vector3N.Normalize(targetVelocity) * speed;
            }

            // Set character target velocity (convert to 2D)
            var forward2D = Vector3N.Normalize(new Vector3N(character.ViewDirection.X, 0, character.ViewDirection.Z));
            var right2D = Vector3N.Cross(Vector3N.UnitY, forward2D);
            
            character.TargetVelocity = new System.Numerics.Vector2(
                Vector3N.Dot(targetVelocity, right2D),
                Vector3N.Dot(targetVelocity, forward2D)
            );

            // Jump
            if (keyboard.IsKeyDown(Keys.Space))
            {
                character.TryJump = true;
            }
        }

        public void UpdateCamera(Camera camera)
        {
            var characterBody = physicsSystem.Simulation.Bodies[characterBodyHandle];
            var characterPosition = characterBody.Pose.Position;
            
            // Convert to XNA Vector3 and offset for head height
            var cameraPosition = new Vector3(characterPosition.X, characterPosition.Y + 1.5f, characterPosition.Z);
            camera.Position = cameraPosition;

            // Get character view direction
            ref var character = ref characterControllers.GetCharacterByIndex(characterIndex);
            
            // Convert to yaw/pitch for the camera system
            var viewDir = character.ViewDirection;
            var yaw = MathF.Atan2(viewDir.X, viewDir.Z);
            var pitch = MathF.Asin(-viewDir.Y);
            
            // Update the camera's internal rotation values
            // Since yaw and pitch are protected, we'll use reflection
            var cameraType = typeof(Camera);
            var yawField = cameraType.GetField("yaw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pitchField = cameraType.GetField("pitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            yawField?.SetValue(camera, yaw);
            pitchField?.SetValue(camera, pitch);
        }

        public Vector3 GetPosition()
        {
            var characterBody = physicsSystem.Simulation.Bodies[characterBodyHandle];
            return new Vector3(characterBody.Pose.Position.X, characterBody.Pose.Position.Y, characterBody.Pose.Position.Z);
        }

        public void SetMouseLocked(bool locked)
        {
            mouseLocked = locked;
        }

        public bool IsMouseLocked()
        {
            return mouseLocked;
        }

        public void Dispose()
        {
            if (characterControllers != null && characterIndex >= 0 && characterIndex < characterControllers.CharacterCount)
            {
                characterControllers.RemoveCharacterByIndex(characterIndex);
            }
        }
    }
}