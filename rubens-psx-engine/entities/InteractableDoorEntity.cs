using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine;
using rubens_psx_engine.entities;
using rubens_psx_engine.Extensions;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using System;
using System.Collections.Generic;
using anakinsoft.utilities;
using anakinsoft.system.physics;

namespace anakinsoft.entities
{
    /// <summary>
    /// Interactive door that shows destination and can trigger travel/teleport actions
    /// </summary>
    public class InteractableDoorEntity : InteractableObject, IDisposable
    {
        // Visual components
        private MultiMaterialRenderingEntity doorModel;
        private MultiMaterialRenderingEntity doorFrameModel;

        // Physics components
        private StaticHandle? frameStaticHandle;
        private PhysicsSystem physicsSystem;

        // Door parameters
        private Quaternion rotation;
        private Vector3 scale;

        // Travel/Action parameters
        private string destinationName;
        private Vector3? teleportDestination;
        private Action<InteractableDoorEntity> customAction;

        public Vector3 Scale
        {
            get => scale;
            set
            {
                scale = value;
                UpdateScales();
            }
        }

        public Quaternion Rotation
        {
            get => rotation;
            set
            {
                rotation = value;
                UpdateRotations();
            }
        }

        public string DestinationName
        {
            get => destinationName;
            set
            {
                destinationName = value;
                UpdateInteractionText();
            }
        }

        public Vector3? TeleportDestination
        {
            get => teleportDestination;
            set => teleportDestination = value;
        }

        /// <summary>
        /// Gets the door model for external rendering
        /// </summary>
        public MultiMaterialRenderingEntity DoorModel => doorModel;

        /// <summary>
        /// Gets the door frame model for external rendering
        /// </summary>
        public MultiMaterialRenderingEntity DoorFrameModel => doorFrameModel;

        /// <summary>
        /// Creates a new interactable door entity
        /// </summary>
        public InteractableDoorEntity(Vector3 position, Quaternion rotation, Vector3 scale,
            string destinationName, string doorModelPath, string doorFrameModelPath,
            Dictionary<int, Material> doorMaterials, Dictionary<int, Material> frameMaterials,
            PhysicsSystem physics)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.destinationName = destinationName;
            this.physicsSystem = physics;
            this.interactionDistance = 150f; // Slightly longer range for doors

            // Create door model with materials
            doorModel = new MultiMaterialRenderingEntity(doorModelPath, doorMaterials);
            doorModel.Position = position;
            doorModel.Rotation = rotation;
            doorModel.Scale = scale;
            doorModel.IsVisible = true;

            // Create doorframe model with materials
            doorFrameModel = new MultiMaterialRenderingEntity(doorFrameModelPath, frameMaterials);
            doorFrameModel.Position = position;
            doorFrameModel.Rotation = rotation;
            doorFrameModel.Scale = scale;
            doorFrameModel.IsVisible = true;

            UpdateInteractionText();
            InitializePhysics();
        }

        /// <summary>
        /// Creates a new interactable door with single materials
        /// </summary>
        public InteractableDoorEntity(Vector3 position, Quaternion rotation, Vector3 scale,
            string destinationName, string doorModelPath, string doorFrameModelPath,
            Material doorMaterial, Material frameMaterial, PhysicsSystem physics)
            : this(position, rotation, scale, destinationName, doorModelPath, doorFrameModelPath,
                  new Dictionary<int, Material> { { 0, doorMaterial } },
                  new Dictionary<int, Material> { { 0, frameMaterial } },
                  physics)
        {
        }

        private void UpdateInteractionText()
        {
            if (!string.IsNullOrEmpty(destinationName))
            {
                interactionPrompt = "Press E to travel";
                interactionDescription = $"Destination: {destinationName}";
            }
            else
            {
                interactionPrompt = "Press E to interact";
                interactionDescription = "Special door";
            }
        }

        private void InitializePhysics()
        {
            try
            {
                var simulation = physicsSystem.Simulation;

                // Use same box collider dimensions and offset as regular DoorEntity
                var physicsOffset = new Vector3(0, 20, 0);
                var doorBox = new Box(60f, 50f, 8f);
                var frameShapeIndex = simulation.Shapes.Add(doorBox);

                // Create static frame for interaction detection (with same offset as DoorEntity)
                frameStaticHandle = simulation.Statics.Add(new StaticDescription(
                    position.ToVector3N() + physicsOffset.ToVector3N(),
                    rotation.ToQuaternionN(),
                    frameShapeIndex));

                Console.WriteLine($"Interactive door physics initialized at {position}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create interactive door physics: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets a teleport destination for this door
        /// </summary>
        public void SetTeleportDestination(Vector3 destination)
        {
            teleportDestination = destination;
        }

        /// <summary>
        /// Sets a custom action to be executed when interacting with this door
        /// </summary>
        public void SetCustomAction(Action<InteractableDoorEntity> action)
        {
            customAction = action;
        }

        /// <summary>
        /// Gets the static handle for physics interaction detection
        /// </summary>
        public StaticHandle? GetStaticHandle()
        {
            return frameStaticHandle;
        }

        protected override void OnInteractAction()
        {
            Console.WriteLine($"Interacting with door to: {destinationName}");

            // Execute custom action if set
            if (customAction != null)
            {
                customAction.Invoke(this);
                return;
            }

            // Default teleport behavior if destination is set
            if (teleportDestination.HasValue)
            {
                // This would need to be handled by the scene or game manager
                Console.WriteLine($"Teleporting to: {teleportDestination.Value}");
                // The actual teleportation would be handled externally
            }
        }

        protected override void OnTargetEnterAction()
        {
            //Console.WriteLine($"Targeting door to: {destinationName}");
        }

        protected override void OnTargetExitAction()
        {
            //Console.WriteLine($"No longer targeting door to: {destinationName}");
        }

        private void UpdateScales()
        {
            if (doorModel != null) doorModel.Scale = scale;
            if (doorFrameModel != null) doorFrameModel.Scale = scale;
        }

        private void UpdateRotations()
        {
            if (doorModel != null) doorModel.Rotation = rotation;
            if (doorFrameModel != null) doorFrameModel.Rotation = rotation;
        }

        public void Dispose()
        {
            // Remove physics bodies
            if (frameStaticHandle.HasValue && physicsSystem?.Simulation != null)
            {
                physicsSystem.Simulation.Statics.Remove(frameStaticHandle.Value);
            }
        }
    }

    /// <summary>
    /// Factory class for creating different types of interactive doors
    /// </summary>
    public static class InteractableDoorFactory
    {
        /// <summary>
        /// Creates a teleport door that moves the player to a specific location
        /// </summary>
        public static InteractableDoorEntity CreateTeleportDoor(Vector3 position, Quaternion rotation,
            Vector3 scale, string destinationName, Vector3 teleportDestination,
            Material doorMaterial, Material frameMaterial, PhysicsSystem physics,
            string doorModel = "models/level/door", string frameModel = "models/level/door_frame")
        {
            var door = new InteractableDoorEntity(position, rotation, scale, destinationName,
                doorModel, frameModel, doorMaterial, frameMaterial, physics);
            door.SetTeleportDestination(teleportDestination);
            return door;
        }

        /// <summary>
        /// Creates a custom action door that executes a specific function when interacted with
        /// </summary>
        public static InteractableDoorEntity CreateActionDoor(Vector3 position, Quaternion rotation,
            Vector3 scale, string actionName, Action<InteractableDoorEntity> action,
            Material doorMaterial, Material frameMaterial, PhysicsSystem physics,
            string doorModel = "models/level/door", string frameModel = "models/level/door_frame")
        {
            var door = new InteractableDoorEntity(position, rotation, scale, actionName,
                doorModel, frameModel, doorMaterial, frameMaterial, physics);
            door.SetCustomAction(action);
            return door;
        }

        /// <summary>
        /// Creates a scene transition door that changes to a different scene/level
        /// </summary>
        public static InteractableDoorEntity CreateSceneTransitionDoor(Vector3 position, Quaternion rotation,
            Vector3 scale, string sceneName, string sceneId,
            Material doorMaterial, Material frameMaterial, PhysicsSystem physics,
            string doorModel = "models/level/door", string frameModel = "models/level/door_frame")
        {
            var door = new InteractableDoorEntity(position, rotation, scale, sceneName,
                doorModel, frameModel, doorMaterial, frameMaterial, physics);

            door.SetCustomAction((doorEntity) =>
            {
                Console.WriteLine($"Transitioning to scene: {sceneId}");
                // Scene transition would be handled by the game manager
                // Globals.sceneManager.LoadScene(sceneId);
            });

            return door;
        }
    }
}