using Microsoft.Xna.Framework;
using rubens_psx_engine;
using System;
using anakinsoft.system.cameras;
using rubens_psx_engine.Extensions;

namespace anakinsoft.system
{
    /// <summary>
    /// Manages smooth camera transitions between positions
    /// </summary>
    public class CameraTransitionSystem
    {
        private Camera activeCamera;
        private FPSCamera fpsCamera;
        private bool isTransitioning = false;
        private bool isInInteractionMode = false;

        // Transition state
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private Vector3 targetLookAt;
        private Vector3 returnPosition;
        private Quaternion returnRotation;

        private Quaternion startRotation;
        private Quaternion targetRotation;

        private float transitionProgress = 0f;
        private float transitionDuration = 1.0f; // Duration in seconds

        // Events
        public event Action OnTransitionToInteractionComplete;
        public event Action OnTransitionToPlayerComplete;

        public bool IsTransitioning => isTransitioning;
        public bool IsInInteractionMode => isInInteractionMode;

        public CameraTransitionSystem(Camera camera)
        {
            activeCamera = camera;
            fpsCamera = camera as FPSCamera;
        }

        /// <summary>
        /// Starts a transition to an interaction camera position
        /// </summary>
        public void TransitionToInteraction(Vector3 interactionPosition, Vector3 lookAtPosition, float duration = 1.0f)
        {
            if (isTransitioning)
            {
                Console.WriteLine("CameraTransition: Already transitioning, ignoring request");
                return;
            }

            // Store current camera state for return
            returnPosition = activeCamera.Position;
            returnRotation = activeCamera.GetRotation();

            // Set up transition
            startPosition = activeCamera.Position;
            startRotation = activeCamera.GetRotation();

            targetPosition = interactionPosition;
            targetLookAt = lookAtPosition;

            // Calculate target rotation: look from interactionPosition towards lookAtPosition
            // CreateLookAt creates a view matrix (from eye to target), we need to invert it for world rotation
            Matrix lookAtMatrix = Matrix.CreateLookAt(interactionPosition, lookAtPosition, Vector3.Up);
            targetRotation = Quaternion.CreateFromRotationMatrix(Matrix.Invert(lookAtMatrix));

            // Debug: print the look direction
            Vector3 lookDirection = Vector3.Normalize(lookAtPosition - interactionPosition);
            Console.WriteLine($"=== Camera Transition Setup ===");
            Console.WriteLine($"From: {interactionPosition} To: {lookAtPosition}");
            Console.WriteLine($"Look direction: {lookDirection}");

            // Test what the forward vector should be from the target rotation
            Vector3 targetForward = Vector3.Transform(Vector3.Forward, targetRotation);
            Console.WriteLine($"Target rotation forward vector: {targetForward}");
            Console.WriteLine($"Expected to match look direction: {lookDirection}");

            // Debug start rotation
            Vector3 startForward = Vector3.Transform(Vector3.Forward, startRotation);
            Console.WriteLine($"Start rotation forward vector: {startForward}");

            transitionDuration = duration;
            transitionProgress = 0f;

            isTransitioning = true;
            isInInteractionMode = false;

            Console.WriteLine($"CameraTransition: Starting transition to {interactionPosition} looking at {lookAtPosition}");

            // Debug: Print rotation info as euler angles
            Vector3 currentEuler = QuaternionToEulerAngles(activeCamera.GetRotation());
            Vector3 targetEuler = QuaternionToEulerAngles(targetRotation);

            Quaternion newRotation = Quaternion.Slerp(startRotation, targetRotation, 0);
            Vector3 interpolatedEuler = QuaternionToEulerAngles(newRotation);

            Console.WriteLine($"Camera Rotation Debug (t=0):");
            Console.WriteLine($"  Current Euler: Yaw={MathHelper.ToDegrees(currentEuler.X):F1}° Pitch={MathHelper.ToDegrees(currentEuler.Y):F1}° Roll={MathHelper.ToDegrees(currentEuler.Z):F1}°");
            Console.WriteLine($"  Target Euler:  Yaw={MathHelper.ToDegrees(targetEuler.X):F1}° Pitch={MathHelper.ToDegrees(targetEuler.Y):F1}° Roll={MathHelper.ToDegrees(targetEuler.Z):F1}°");
            Console.WriteLine($"  Interpolated:  Yaw={MathHelper.ToDegrees(interpolatedEuler.X):F1}° Pitch={MathHelper.ToDegrees(interpolatedEuler.Y):F1}° Roll={MathHelper.ToDegrees(interpolatedEuler.Z):F1}°");

        }

        /// <summary>
        /// Transitions back to the player's camera position
        /// </summary>
        public void TransitionBackToPlayer(float duration = 1.0f)
        {
            if (isTransitioning)
            {
                Console.WriteLine("CameraTransition: Already transitioning, ignoring request");
                return;
            }

            if (!isInInteractionMode)
            {
                Console.WriteLine("CameraTransition: Not in interaction mode, cannot return");
                return;
            }

            // Set up return transition
            startPosition = activeCamera.Position;
            startRotation = Quaternion.CreateFromRotationMatrix(activeCamera.View);

            targetPosition = returnPosition;
            targetRotation = returnRotation;

            transitionDuration = duration;
            transitionProgress = 0f;

            isTransitioning = true;

            Console.WriteLine($"CameraTransition: Returning to player position {returnPosition}");
        }

        /// <summary>
        /// Updates the camera transition
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!isTransitioning)
            {
                // If in interaction mode, keep camera looking at target
                if (isInInteractionMode && targetLookAt != Vector3.Zero && fpsCamera != null)
                {
                    fpsCamera.LookAt(targetLookAt);
                }
                return;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            transitionProgress += (deltaTime / transitionDuration);

            if (transitionProgress >= 1.0f)
            {
                // Transition complete
                transitionProgress = 1.0f;
            }

            // Smooth interpolation using smoothstep
            float t = transitionProgress;//SmoothStep(transitionProgress);

            // Interpolate position
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
            activeCamera.Position = newPosition;

            // Interpolate rotation and apply to camera
            Quaternion newRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            activeCamera.SetRotation(newRotation);

            // Debug: Print rotation info as euler angles
            Vector3 currentEuler = QuaternionToEulerAngles(activeCamera.GetRotation());
            Vector3 targetEuler = QuaternionToEulerAngles(targetRotation);
            Vector3 interpolatedEuler = QuaternionToEulerAngles(newRotation);

            Console.WriteLine($"Camera Rotation Debug (t={t:F2}):");
            Console.WriteLine($"  Current Euler: Yaw={MathHelper.ToDegrees(currentEuler.X):F1}° Pitch={MathHelper.ToDegrees(currentEuler.Y):F1}° Roll={MathHelper.ToDegrees(currentEuler.Z):F1}°");
            Console.WriteLine($"  Target Euler:  Yaw={MathHelper.ToDegrees(targetEuler.X):F1}° Pitch={MathHelper.ToDegrees(targetEuler.Y):F1}° Roll={MathHelper.ToDegrees(targetEuler.Z):F1}°");
            Console.WriteLine($"  Interpolated:  Yaw={MathHelper.ToDegrees(interpolatedEuler.X):F1}° Pitch={MathHelper.ToDegrees(interpolatedEuler.Y):F1}° Roll={MathHelper.ToDegrees(interpolatedEuler.Z):F1}°");

            if (transitionProgress >= 1.0f)
            {
                CompleteTransition();
            }
        }

        /// <summary>
        /// Gets the current look-at target during interaction transition
        /// </summary>
        public Vector3? GetInteractionLookAt()
        {
            if (isTransitioning && !isInInteractionMode)
            {
                return targetLookAt;
            }
            else if (isInInteractionMode)
            {
                return targetLookAt;
            }
            return null;
        }

        private void CompleteTransition()
        {
            isTransitioning = false;

            if (isInInteractionMode)
            {
                // Just completed return to player
                isInInteractionMode = false;
                OnTransitionToPlayerComplete?.Invoke();
                Console.WriteLine("CameraTransition: Returned to player control");
            }
            else
            {
                // Just completed transition to interaction
                isInInteractionMode = true;
                OnTransitionToInteractionComplete?.Invoke();
                Console.WriteLine("CameraTransition: Reached interaction position");
            }
        }

        /// <summary>
        /// Smoothstep interpolation for smoother transitions
        /// </summary>
        private float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Converts a quaternion to euler angles (yaw, pitch, roll) in YXZ order
        /// Returns Vector3 with X=Yaw, Y=Pitch, Z=Roll (to match typical camera usage)
        /// </summary>
        private Vector3 QuaternionToEulerAngles(Quaternion q)
        {
            // Convert to matrix first for more stable extraction
            Matrix m = Matrix.CreateFromQuaternion(q);

            Vector3 euler;

            // Extract yaw (Y-axis rotation) from forward vector
            Vector3 forward = new Vector3(m.M31, m.M32, m.M33);
            euler.X = (float)Math.Atan2(-forward.X, -forward.Z); // Yaw

            // Extract pitch (X-axis rotation) from forward vector
            euler.Y = (float)Math.Asin(forward.Y); // Pitch

            // Roll (Z-axis rotation) - usually 0 for FPS camera
            euler.Z = (float)Math.Atan2(m.M12, m.M22); // Roll

            return euler;
        }

        /// <summary>
        /// Immediately cancels any ongoing transition and returns to player
        /// </summary>
        public void CancelTransition()
        {
            if (isInInteractionMode)
            {
                activeCamera.Position = returnPosition;
                isInInteractionMode = false;
            }

            isTransitioning = false;
            transitionProgress = 0f;

            Console.WriteLine("CameraTransition: Cancelled");
        }
    }
}
