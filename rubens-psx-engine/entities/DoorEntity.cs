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
    /// Automatic sliding door entity with frame and proximity-based opening
    /// </summary>
    public class DoorEntity : IDisposable
    {
        // Door components
        private MultiMaterialRenderingEntity doorModel;
        private MultiMaterialRenderingEntity doorFrameModel;

        // Physics components
        private BodyHandle? doorBodyHandle;
        private StaticHandle? frameStaticHandle;
        private PhysicsSystem physicsSystem;

        // Door state
        private bool isOpen = false;
        private bool isMoving = false;
        private bool isLocked = false;
        private float currentOpenAmount = 0f;

        // Door parameters
        private Vector3 position;
        private Quaternion rotation;
        private Vector3 scale;

        // Opening parameters
        private float openSpeed = 3f; // Units per second
        private float openHeight = 30f; // How far the door slides up
        private float triggerDistance = 50f; // Distance to trigger opening
        private float closeDelay = 2f; // Seconds before door starts closing
        private float timeSinceLastTrigger = 10f;

        // Original door position for animation
        private Vector3 doorClosedPosition;
        private Vector3 doorOpenPosition;
        Vector3 physicsOffset = new Vector3(0, 20, 0);

        public Vector3 Position
        {
            get => position;
            set
            {
                position = value;
                UpdatePositions();
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

        public Vector3 Scale
        {
            get => scale;
            set
            {
                scale = value;
                UpdateScales();
            }
        }

        public bool IsOpen => isOpen;
        public bool IsMoving => isMoving;
        public bool IsLocked => isLocked;

        /// <summary>
        /// Creates a new door entity with automatic opening mechanism
        /// </summary>
        public DoorEntity(Vector3 position, Quaternion rotation, Vector3 scale,
            string doorModelPath, string doorFrameModelPath,
            Dictionary<int, Material> doorMaterials, Dictionary<int, Material> frameMaterials,
            PhysicsSystem physics)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.physicsSystem = physics;

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

            // Store door positions for animation
            doorClosedPosition = position;
            doorOpenPosition = position + Vector3.Up * openHeight * scale.Y;

            // Initialize physics colliders
            InitializePhysics();
        }

        /// <summary>
        /// Creates a new door entity with single material for each component
        /// </summary>
        public DoorEntity(Vector3 position, Quaternion rotation, Vector3 scale,
            string doorModelPath, string doorFrameModelPath,
            Material doorMaterial, Material frameMaterial,
            PhysicsSystem physics)
            : this(position, rotation, scale, doorModelPath, doorFrameModelPath,
                  new Dictionary<int, Material> { { 0, doorMaterial } },
                  new Dictionary<int, Material> { { 0, frameMaterial } },
                  physics)
        {
        }

        /// <summary>
        /// Alternative constructor with default models
        /// </summary>
        //public DoorEntity(Vector3 position, Quaternion rotation, Vector3 scale, PhysicsSystem physics)
        //    : this(position, rotation, scale,
        //          "models/level/door", "models/level/door_frame",
        //          "textures/door", "textures/door_frame",
        //          physics)
        //{
        //}


        private void InitializePhysics()
        {
            try
            {
                var simulation = physicsSystem.Simulation;

                // Create door collider (dynamic box that moves with the door)
                var doorBox = new Box(60f , 50f , 8f );
                var doorShapeIndex = simulation.Shapes.Add(doorBox);

                // Create door body (kinematic so we can control it)
                var doorDescription = BodyDescription.CreateKinematic(
                    new RigidPose(position.ToVector3N() + physicsOffset.ToVector3N(), rotation.ToQuaternionN()),
                    new CollidableDescription(doorShapeIndex, 0.1f),
                    new BodyActivityDescription(0.01f));

                doorBodyHandle = simulation.Bodies.Add(doorDescription);

                // Create doorframe collider (static mesh or compound of boxes)
                // Using a compound shape made of 3 boxes (top and two sides)
                var frameThickness = 2f;
                var frameWidth = 12f * scale.X;
                var frameHeight = 22f * scale.Y;

                // Side frames
                //var sideBox = new Box(frameThickness * scale.X, frameHeight, frameThickness * scale.Z);
                // var topBox = new Box(frameWidth, frameThickness * scale.Y, frameThickness * scale.Z);

                // Create compound shape for frame
                //var builder = new CompoundBuilder(physicsSystem.BufferPool, simulation.Shapes, 3);

                //// Left side
                //builder.Add(sideBox, new RigidPose(
                //    new System.Numerics.Vector3(-frameWidth/2, 0, 0),
                //    System.Numerics.Quaternion.Identity), 1);

                //// Right side
                //builder.Add(sideBox, new RigidPose(
                //    new System.Numerics.Vector3(frameWidth/2, 0, 0),
                //    System.Numerics.Quaternion.Identity), 1);

                //// Top
                //builder.Add(topBox, new RigidPose(
                //    new System.Numerics.Vector3(0, frameHeight/2, 0),
                //    System.Numerics.Quaternion.Identity), 1);

                //builder.BuildKinematicCompound(out var children);
                //var compound = new Compound(children);
                //builder.Dispose();

                //var frameShapeIndex = simulation.Shapes.Add(compound);

                // Create static frame
                //frameStaticHandle = simulation.Statics.Add(new StaticDescription(
                //    position.ToVector3N(),
                //    rotation.ToQuaternionN(),
                //    frameShapeIndex));

                Console.WriteLine($"Door entity physics initialized at {position}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create door physics: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the door state, handling automatic opening/closing
        /// </summary>
        public void Update(GameTime gameTime, Vector3 characterPosition)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Check distance to character
            float distance = Vector3.Distance(characterPosition, position);
            bool shouldBeOpen = distance < triggerDistance;

            // Update trigger timer
            if (shouldBeOpen)
            {
                timeSinceLastTrigger = 0f;
            }
            else
            {
                timeSinceLastTrigger += deltaTime;
            }

            // Determine if door should open or close (only if not locked)
            bool targetOpen = !isLocked && (shouldBeOpen || timeSinceLastTrigger < closeDelay);

            // Animate door
            if (targetOpen && !isOpen && !isLocked)
            {
                // Opening (only if not locked)
                isMoving = true;
                currentOpenAmount = Math.Min(currentOpenAmount + openSpeed * deltaTime, 1f);

                if (currentOpenAmount >= 1f)
                {
                    currentOpenAmount = 1f;
                    isOpen = true;
                    isMoving = false;
                }
            }
            else if (!targetOpen && isOpen)
            {
                // Closing
                isMoving = true;
                currentOpenAmount = Math.Max(currentOpenAmount - openSpeed * deltaTime, 0f);

                if (currentOpenAmount <= 0f)
                {
                    currentOpenAmount = 0f;
                    isOpen = false;
                    isMoving = false;
                }
            }

            // Update door position
            Vector3 targetPosition = Vector3.Lerp(doorClosedPosition, doorOpenPosition, currentOpenAmount);
            doorModel.Position = targetPosition;

            // Update physics body position
            if (doorBodyHandle.HasValue && physicsSystem?.Simulation != null)
            {
                var body = physicsSystem.Simulation.Bodies[doorBodyHandle.Value];
                body.Pose.Position = targetPosition.ToVector3N() + physicsOffset.ToVector3N();
                body.Awake = true;
            }
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
        /// Sets door opening parameters
        /// </summary>
        public void SetOpeningParameters(float speed, float height, float triggerDist, float closeDelaySeconds)
        {
            openSpeed = speed;
            openHeight = height;
            triggerDistance = triggerDist;
            closeDelay = closeDelaySeconds;

            // Recalculate open position
            doorOpenPosition = doorClosedPosition + Vector3.Up * openHeight * scale.Y;
        }

        private void UpdatePositions()
        {
            doorClosedPosition = position;
            doorOpenPosition = position + Vector3.Up * openHeight * scale.Y;

            // Update model positions
            doorModel.Position = Vector3.Lerp(doorClosedPosition, doorOpenPosition, currentOpenAmount);
            doorFrameModel.Position = position;

            // Update physics positions
            UpdatePhysicsTransforms();
        }

        private void UpdateRotations()
        {
            doorModel.Rotation = rotation;
            doorFrameModel.Rotation = rotation;
            UpdatePhysicsTransforms();
        }

        private void UpdateScales()
        {
            doorModel.Scale = scale;
            doorFrameModel.Scale = scale;
            // Note: Physics shapes don't easily rescale, would need recreation
        }

        private void UpdatePhysicsTransforms()
        {
            if (physicsSystem?.Simulation == null) return;

            // Update door body
            if (doorBodyHandle.HasValue)
            {
                var body = physicsSystem.Simulation.Bodies[doorBodyHandle.Value];
                body.Pose.Position = doorModel.Position.ToVector3N() + physicsOffset.ToVector3N();
                body.Pose.Orientation = rotation.ToQuaternionN();
                body.Awake = true;
            }

            // Frame is static, doesn't move after initial placement
        }

        /// <summary>
        /// Forces the door to open immediately
        /// </summary>
        public void ForceOpen()
        {
            isOpen = true;
            currentOpenAmount = 1f;
            doorModel.Position = doorOpenPosition;
            UpdatePhysicsTransforms();
        }

        /// <summary>
        /// Forces the door to close immediately
        /// </summary>
        public void ForceClose()
        {
            isOpen = false;
            currentOpenAmount = 0f;
            doorModel.Position = doorClosedPosition;
            UpdatePhysicsTransforms();
        }

        /// <summary>
        /// Locks the door, preventing it from opening automatically
        /// </summary>
        public void Lock()
        {
            isLocked = true;
            Console.WriteLine($"Door at {position} is now locked");
        }

        /// <summary>
        /// Unlocks the door, allowing it to function normally
        /// </summary>
        public void Unlock()
        {
            isLocked = false;
            Console.WriteLine($"Door at {position} is now unlocked");
        }

        /// <summary>
        /// Sets the lock state of the door
        /// </summary>
        /// <param name="locked">True to lock the door, false to unlock</param>
        public void SetLocked(bool locked)
        {
            isLocked = locked;
            Console.WriteLine($"Door at {position} is now {(locked ? "locked" : "unlocked")}");
        }

        public void Dispose()
        {
            // Remove physics bodies
            if (doorBodyHandle.HasValue && physicsSystem?.Simulation != null)
            {
                physicsSystem.Simulation.Bodies.Remove(doorBodyHandle.Value);
            }

            if (frameStaticHandle.HasValue && physicsSystem?.Simulation != null)
            {
                physicsSystem.Simulation.Statics.Remove(frameStaticHandle.Value);
            }
        }
    }
}