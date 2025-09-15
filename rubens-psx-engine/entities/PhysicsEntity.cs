using Microsoft.Xna.Framework;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using anakinsoft.system.physics;
using anakinsoft.utilities;
using rubens_psx_engine.entities;
using System;
using Vector3N = System.Numerics.Vector3;
using QuaternionN = System.Numerics.Quaternion;

namespace rubens_psx_engine.entities
{
    /// <summary>
    /// Physics-enabled entity that extends RenderingEntity with physics simulation
    /// </summary>
    public class PhysicsEntity : RenderingEntity
    {
        protected PhysicsSystem physicsSystem;
        protected BodyHandle? bodyHandle;
        protected object physicsShape;
        
        // Physics properties
        public bool IsStatic { get; protected set; }
        public bool HasRigidBody { get; protected set; }
        public float Mass { get; protected set; }
        public float Friction { get; protected set; }
        public BodyVelocity Velocity { get; set; }

        public BodyHandle? BodyHandle => bodyHandle;

        public PhysicsEntity(PhysicsSystem physics, string modelPath, object shape, 
            float mass = 1f, bool isStatic = false, string texturePath = null, 
            string effectPath = "shaders/surface/Unlit", bool isShaded = true)
            : base(modelPath, texturePath, effectPath, isShaded)
        {
            physicsSystem = physics ?? throw new ArgumentNullException(nameof(physics));
            physicsShape = shape ?? throw new ArgumentNullException(nameof(shape));
            Mass = mass;
            IsStatic = isStatic;
            HasRigidBody = !isStatic && mass > 0;
            Friction = 0.1f;
            Velocity = new BodyVelocity();

            CreatePhysicsBody();
        }

        protected virtual void CreatePhysicsBody()
        {
            if (physicsSystem?.Simulation == null) return;

            try
            {
                TypedIndex shapeIndex;
                var pose = new RigidPose(Position.ToVector3N(), Rotation.ToQuaternionN());

                if (IsStatic)
                {
                    // Create static body
                    if (physicsShape is Box box)
                    {
                        shapeIndex = physicsSystem.Simulation.Shapes.Add(box);
                    }
                    else if (physicsShape is Sphere sphere)
                    {
                        shapeIndex = physicsSystem.Simulation.Shapes.Add(sphere);
                    }
                    else if (physicsShape is Capsule capsule)
                    {
                        shapeIndex = physicsSystem.Simulation.Shapes.Add(capsule);
                    }
                    else
                    {
                        throw new ArgumentException($"Unsupported shape type: {physicsShape.GetType()}");
                    }
                    
                    var collidable = new CollidableDescription(shapeIndex, Friction);
                    var staticDesc = BodyDescription.CreateKinematic(pose, collidable, new BodyActivityDescription(0.01f));
                    bodyHandle = physicsSystem.Simulation.Bodies.Add(staticDesc);
                }
                else
                {
                    // Create dynamic body
                    BodyDescription dynamicDesc;
                    if (physicsShape is Box box)
                    {
                        dynamicDesc = BodyDescription.CreateConvexDynamic(pose, Velocity, Mass, physicsSystem.Simulation.Shapes, box);
                    }
                    else if (physicsShape is Sphere sphere)
                    {
                        dynamicDesc = BodyDescription.CreateConvexDynamic(pose, Velocity, Mass, physicsSystem.Simulation.Shapes, sphere);
                    }
                    else if (physicsShape is Capsule capsule)
                    {
                        dynamicDesc = BodyDescription.CreateConvexDynamic(pose, Velocity, Mass, physicsSystem.Simulation.Shapes, capsule);
                    }
                    else
                    {
                        throw new ArgumentException($"Unsupported shape type: {physicsShape.GetType()}");
                    }
                    
                    bodyHandle = physicsSystem.Simulation.Bodies.Add(dynamicDesc);
                }
            }
            catch (Exception e)
            {
                Helpers.ErrorPopup($"Failed to create physics body for entity: {e.Message}");
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            SyncTransformFromPhysics();
        }

        protected virtual void SyncTransformFromPhysics()
        {
            if (!bodyHandle.HasValue || physicsSystem?.Simulation == null) return;

            try
            {
                var bodyRef = physicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle.Value);
                var pose = bodyRef.Pose;
                
                // Update transform from physics
                Position = pose.Position.ToVector3();
                Rotation = pose.Orientation.ToQuaternion();
            }
            catch (Exception)
            {
                // Handle case where body might have been removed
                bodyHandle = null;
            }
        }

        public virtual void SyncTransformToPhysics()
        {
            if (!bodyHandle.HasValue || physicsSystem?.Simulation == null) return;

            try
            {
                var bodyRef = physicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle.Value);
                bodyRef.Pose = new RigidPose(Position.ToVector3N(), Rotation.ToQuaternionN());
            }
            catch (Exception)
            {
                // Handle case where body might have been removed
                bodyHandle = null;
            }
        }

        public virtual void SetPhysicsPosition(Vector3 position)
        {
            Position = position;
            SyncTransformToPhysics();
        }

        public virtual void SetPhysicsRotation(Quaternion rotation)
        {
            Rotation = rotation;
            SyncTransformToPhysics();
        }

        public virtual void ApplyForce(Vector3N force)
        {
            if (!bodyHandle.HasValue || IsStatic || physicsSystem?.Simulation == null) return;

            try
            {
                var bodyRef = physicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle.Value);
                bodyRef.Velocity.Linear += force / Mass;
            }
            catch (Exception)
            {
                bodyHandle = null;
            }
        }

        public virtual void ApplyImpulse(Vector3N impulse)
        {
            if (!bodyHandle.HasValue || IsStatic || physicsSystem?.Simulation == null) return;

            try
            {
                var bodyRef = physicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle.Value);
                bodyRef.Velocity.Linear += impulse;
            }
            catch (Exception)
            {
                bodyHandle = null;
            }
        }

        public virtual void SetVelocity(Vector3N velocity)
        {
            if (!bodyHandle.HasValue || IsStatic || physicsSystem?.Simulation == null) return;

            try
            {
                var bodyRef = physicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle.Value);
                bodyRef.Velocity.Linear = velocity;
            }
            catch (Exception)
            {
                bodyHandle = null;
            }
        }

        public virtual Vector3N GetVelocity()
        {
            if (!bodyHandle.HasValue || physicsSystem?.Simulation == null) 
                return Vector3N.Zero;

            try
            {
                var bodyRef = physicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle.Value);
                return bodyRef.Velocity.Linear;
            }
            catch (Exception)
            {
                bodyHandle = null;
                return Vector3N.Zero;
            }
        }

        public virtual void RemoveFromPhysics()
        {
            if (!bodyHandle.HasValue || physicsSystem?.Simulation == null) return;

            try
            {
                physicsSystem.Simulation.Bodies.Remove(bodyHandle.Value);
                bodyHandle = null;
            }
            catch (Exception)
            {
                // Body might already be removed
                bodyHandle = null;
            }
        }

        public virtual bool IsPhysicsBodyValid()
        {
            return bodyHandle.HasValue && physicsSystem?.Simulation != null;
        }
    }

    // Factory methods for common physics entities
    public static class PhysicsEntityFactory
    {
        public static PhysicsEntity CreateBox(PhysicsSystem physics, Vector3 position, Vector3 size, 
            float mass = 1f, bool isStatic = false, string modelPath = "models/cube", 
            string texturePath = "textures/prototype/brick")
        {
            var shape = new Box(size.X, size.Y, size.Z);
            var entity = new PhysicsEntity(physics, modelPath, shape, mass, isStatic, texturePath);
            // Set position BEFORE creating physics body to avoid the bug where objects jump to origin
            entity.Position = position;
            entity.Scale = Vector3.One; // Model scale should be handled separately from physics scale
            // Now update the physics body position to match
            entity.SyncTransformToPhysics();
            return entity;
        }

        public static PhysicsEntity CreateSphere(PhysicsSystem physics, Vector3 position, float radius,
            float mass = 1f, bool isStatic = false, string modelPath = "models/sphere",
            string texturePath = null)
        {
            var shape = new Sphere(radius);
            var entity = new PhysicsEntity(physics, modelPath, shape, mass, isStatic, texturePath);
            entity.Position = position;
            entity.Scale = Vector3.One;
            entity.SyncTransformToPhysics();
            return entity;
        }

        public static PhysicsEntity CreateCapsule(PhysicsSystem physics, Vector3 position, float radius, float length,
            float mass = 1f, bool isStatic = false, string modelPath = "models/capsule",
            string texturePath = null)
        {
            var shape = new Capsule(radius, length);
            var entity = new PhysicsEntity(physics, modelPath, shape, mass, isStatic, texturePath);
            entity.Position = position;
            entity.Scale = Vector3.One;
            entity.SyncTransformToPhysics();
            return entity;
        }

        public static PhysicsEntity CreateGround(PhysicsSystem physics, Vector3 position, Vector3 size,
            string modelPath = "models/cube", string texturePath = "textures/prototype/concrete")
        {
            // CreateBox already handles the position correctly now
            return CreateBox(physics, position, size, 0f, true, modelPath, texturePath);
        }
    }
}