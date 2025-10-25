using Microsoft.Xna.Framework;
using rubens_psx_engine;
using System;
using anakinsoft.system.cameras;

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
            returnRotation = Quaternion.CreateFromRotationMatrix(activeCamera.View);

            // Set up transition
            startPosition = activeCamera.Position;
            startRotation = Quaternion.CreateFromRotationMatrix(activeCamera.View);

            targetPosition = interactionPosition;
            targetLookAt = lookAtPosition;

            // Calculate target rotation: look from interactionPosition towards lookAtPosition
            Vector3 lookDirection = Vector3.Normalize(lookAtPosition - interactionPosition);
            Matrix lookAtMatrix = Matrix.CreateLookAt(interactionPosition, lookAtPosition, Vector3.Up);
            targetRotation = Quaternion.CreateFromRotationMatrix(Matrix.Invert(lookAtMatrix));

            transitionDuration = duration;
            transitionProgress = 0f;

            isTransitioning = true;
            isInInteractionMode = false;

            Console.WriteLine($"CameraTransition: Starting transition to {interactionPosition} looking at {lookAtPosition}");
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
            transitionProgress += deltaTime / transitionDuration;

            if (transitionProgress >= 1.0f)
            {
                // Transition complete
                transitionProgress = 1.0f;
            }

            // Smooth interpolation using smoothstep
            float t = SmoothStep(transitionProgress);

            // Interpolate position
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
            activeCamera.Position = newPosition;

            // Interpolate the look-at target and apply rotation
            if (fpsCamera != null)
            {
                // Interpolate rotation by lerping the quaternion and extracting the direction
                Quaternion newRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                Vector3 forward = Vector3.Transform(Vector3.Forward, newRotation);
                Vector3 interpolatedTarget = activeCamera.Position + forward;
                fpsCamera.LookAt(interpolatedTarget);
            }

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
