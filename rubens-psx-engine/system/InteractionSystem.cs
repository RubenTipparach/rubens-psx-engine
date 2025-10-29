using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using anakinsoft.entities;
using anakinsoft.system.physics;
using anakinsoft.utilities;
using anakinsoft.game.scenes.lounge.evidence;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using rubens_psx_engine.Extensions;
using rubens_psx_engine;

namespace anakinsoft.system
{
    /// <summary>
    /// Manages interaction with objects using BEPU physics raycasting
    /// </summary>
    public class InteractionSystem
    {
        private PhysicsSystem physicsSystem;
        private List<IInteractable> interactables;
        private IInteractable currentTarget;
        private KeyboardState previousKeyboard;

        // Raycast parameters
        private float maxRayDistance = 20f;
        private bool showDebugInfo = false;

        // UI components
        private SpriteFont font;
        private bool uiEnabled = true;

        public IInteractable CurrentTarget => currentTarget;
        public bool ShowDebugInfo { get => showDebugInfo; set => showDebugInfo = value; }
        public bool UIEnabled { get => uiEnabled; set => uiEnabled = value; }

        public InteractionSystem(PhysicsSystem physics)
        {
            physicsSystem = physics;
            interactables = new List<IInteractable>();
        }

        /// <summary>
        /// Registers an interactable object with the system
        /// </summary>
        public void RegisterInteractable(IInteractable interactable)
        {
            if (!interactables.Contains(interactable))
            {
                interactables.Add(interactable);
                Console.WriteLine($"Registered interactable at {interactable.Position}");
            }
        }

        /// <summary>
        /// Unregisters an interactable object from the system
        /// </summary>
        public void UnregisterInteractable(IInteractable interactable)
        {
            if (interactables.Remove(interactable))
            {
                if (currentTarget == interactable)
                {
                    currentTarget.IsTargeted = false;
                    currentTarget = null;
                }
                Console.WriteLine($"Unregistered interactable at {interactable.Position}");
            }
        }

        /// <summary>
        /// Updates the interaction system with mouse raycast and input handling
        /// </summary>
        public void Update(GameTime gameTime, Camera camera)
        {
            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();

            // Perform raycast to find targeted interactable
            PerformInteractionRaycast(camera);

            // Handle interaction input (E or F key)
            if ((keyboard.IsKeyDown(Keys.E) && !previousKeyboard.IsKeyDown(Keys.E)) ||
                (keyboard.IsKeyDown(Keys.F) && !previousKeyboard.IsKeyDown(Keys.F)))
            {
                if (currentTarget != null && currentTarget.CanInteract)
                {
                    currentTarget.OnInteract();
                }
            }

            // Toggle debug info
            if (keyboard.IsKeyDown(Keys.F3) && !previousKeyboard.IsKeyDown(Keys.F3))
            {
                showDebugInfo = !showDebugInfo;
                Console.WriteLine($"Interaction debug info: {(showDebugInfo ? "ON" : "OFF")}");
            }

            previousKeyboard = keyboard;
        }

        private void PerformInteractionRaycast(Camera camera)
        {
            var mouse = Mouse.GetState();
            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            // Create ray from camera through mouse position
            Vector3 nearPoint = viewport.Unproject(
                new Vector3(mouse.X, mouse.Y, 0),
                camera.Projection,
                camera.View,
                Matrix.Identity);

            Vector3 farPoint = viewport.Unproject(
                new Vector3(mouse.X, mouse.Y, 1),
                camera.Projection,
                camera.View,
                Matrix.Identity);

            Vector3 rayDirection = Vector3.Normalize(farPoint - nearPoint);
            var rayStart = nearPoint.ToVector3N();
            var rayDir = rayDirection.ToVector3N();

            // Clear current target first
            if (currentTarget != null)
            {
                currentTarget.IsTargeted = false;
                currentTarget = null;
            }

            // Perform BEPU raycast
            var hitHandler = new InteractionRayHitHandler();
            physicsSystem.Simulation.RayCast(rayStart, rayDir, maxRayDistance, ref hitHandler);

            if (hitHandler.Hit)
            {
                // Find the interactable object associated with this physics handle
                var hitInteractable = FindInteractableByStaticHandle(hitHandler.HitStaticHandle);
                if (hitInteractable != null)
                {
                    // Check if within interaction distance
                    float distance = Vector3.Distance(nearPoint, hitInteractable.Position);
                    if (distance <= hitInteractable.InteractionDistance)
                    {
                        currentTarget = hitInteractable;
                        currentTarget.IsTargeted = true;

                        //if (showDebugInfo)
                        //{
                        //    Console.WriteLine($"Targeting: {currentTarget.InteractionPrompt} - {currentTarget.InteractionDescription}");
                        //}
                    }
                }
            }
        }

        private IInteractable FindInteractableByStaticHandle(StaticHandle handle)
        {
            // Check all registered interactables to see if any match this physics handle
            foreach (var interactable in interactables)
            {
                // If it's an InteractableDoorEntity, check its static handle
                if (interactable is InteractableDoorEntity door)
                {
                    var doorHandle = door.GetStaticHandle();
                    if (doorHandle.HasValue && doorHandle.Value.Value == handle.Value)
                    {
                        return door;
                    }
                }
                // If it's an InteractableCharacter, check its static handle
                else if (interactable is InteractableCharacter character)
                {
                    var characterHandle = character.GetStaticHandle();
                    if (characterHandle.HasValue && characterHandle.Value.Value == handle.Value)
                    {
                        return character;
                    }
                }
                // If it's a SuspectsFile, check its static handle
                else if (interactable is SuspectsFile file)
                {
                    var fileHandle = file.GetStaticHandle();
                    if (fileHandle.HasValue && fileHandle.Value.Value == handle.Value)
                    {
                        return file;
                    }
                }
                // If it's an AutopsyReport, check its static handle
                else if (interactable is AutopsyReport report)
                {
                    var reportHandle = report.GetStaticHandle();
                    if (reportHandle.HasValue && reportHandle.Value.Value == handle.Value)
                    {
                        return report;
                    }
                }
                // Can be extended for other interactable types that have physics handles
            }

            return null;
        }

        /// <summary>
        /// Draws the interaction UI
        /// </summary>
        public void DrawUI(SpriteBatch spriteBatch, SpriteFont uiFont)
        {
            if (!uiEnabled || currentTarget == null)
                return;

            font = uiFont;
            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            // Draw interaction prompt in center of screen
            string promptText = currentTarget.InteractionPrompt;
            string descriptionText = currentTarget.InteractionDescription;

            var promptSize = font.MeasureString(promptText);
            var descriptionSize = font.MeasureString(descriptionText);

            // Position in center-bottom of screen
            var promptPos = new Vector2(
                (viewport.Width - promptSize.X) / 2,
                viewport.Height * 0.7f);

            var descriptionPos = new Vector2(
                (viewport.Width - descriptionSize.X) / 2,
                promptPos.Y + promptSize.Y + 5);

            // Draw with background for readability
            DrawTextWithBackground(spriteBatch, promptText, promptPos, Color.White, Color.Black);
            if (!string.IsNullOrEmpty(descriptionText))
            {
                DrawTextWithBackground(spriteBatch, descriptionText, descriptionPos, Color.Yellow, Color.Black);
            }

            // Debug info
            if (showDebugInfo)
            {
                string debugText = $"Interactables: {interactables.Count}\nDistance: {Vector3.Distance(Vector3.Zero, currentTarget.Position):F1}";
                spriteBatch.DrawString(font, debugText, new Vector2(10, 100), Color.Lime);
            }
        }

        private void DrawTextWithBackground(SpriteBatch spriteBatch, string text, Vector2 position, Color textColor, Color backgroundColor)
        {
            var textSize = font.MeasureString(text);
            var backgroundRect = new Rectangle(
                (int)(position.X - 5),
                (int)(position.Y - 2),
                (int)(textSize.X + 10),
                (int)(textSize.Y + 4));

            // Draw semi-transparent background
            var backgroundTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            backgroundTexture.SetData(new[] { backgroundColor });
            spriteBatch.Draw(backgroundTexture, backgroundRect, Color.Black * 0.7f);

            // Draw text
            spriteBatch.DrawString(font, text, position + Vector2.One, Color.Black); // Shadow
            spriteBatch.DrawString(font, text, position, textColor);
        }

        /// <summary>
        /// Gets all registered interactables
        /// </summary>
        public List<IInteractable> GetAllInteractables()
        {
            return new List<IInteractable>(interactables);
        }

        /// <summary>
        /// Finds interactables near a specific position
        /// </summary>
        public List<IInteractable> GetInteractablesNear(Vector3 position, float radius)
        {
            return interactables
                .Where(i => Vector3.Distance(i.Position, position) <= radius)
                .ToList();
        }

        /// <summary>
        /// Clears all registered interactables
        /// </summary>
        public void Clear()
        {
            if (currentTarget != null)
            {
                currentTarget.IsTargeted = false;
                currentTarget = null;
            }
            interactables.Clear();
        }
    }

    /// <summary>
    /// Ray hit handler specifically for interaction detection
    /// </summary>
    public struct InteractionRayHitHandler : IRayHitHandler
    {
        public bool Hit;
        public StaticHandle HitStaticHandle;
        public float HitDistance;

        public bool AllowTest(CollidableReference collidable)
        {
            // Only test static objects for interactions
            return collidable.Mobility == CollidableMobility.Static;
        }

        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return collidable.Mobility == CollidableMobility.Static;
        }

        public void OnRayHit(in RayData ray, ref float maximumT, float t, System.Numerics.Vector3 normal, CollidableReference collidable, int childIndex)
        {
            if (t < maximumT && collidable.Mobility == CollidableMobility.Static)
            {
                Hit = true;
                HitDistance = t;
                maximumT = t;
                HitStaticHandle = collidable.StaticHandle;
            }
        }
    }
}