using Microsoft.Xna.Framework;
using anakinsoft.system.physics;
using rubens_psx_engine.entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace rubens_psx_engine.entities
{
    /// <summary>
    /// Generic scene manager that handles collections of rendering and physics entities
    /// </summary>
    public class Scene
    {
        protected PhysicsSystem physicsSystem;
        protected List<RenderingEntity> renderingEntities;
        protected List<PhysicsEntity> physicsEntities;
        
        public IReadOnlyList<RenderingEntity> RenderingEntities => renderingEntities.AsReadOnly();
        public IReadOnlyList<PhysicsEntity> PhysicsEntities => physicsEntities.AsReadOnly();
        public PhysicsSystem Physics => physicsSystem;

        public Scene(PhysicsSystem physics = null)
        {
            physicsSystem = physics;
            renderingEntities = new List<RenderingEntity>();
            physicsEntities = new List<PhysicsEntity>();
        }

        public virtual void Initialize()
        {
            // Override in derived classes for scene-specific initialization
        }

        public virtual void Update(GameTime gameTime)
        {
            // Update all rendering entities
            foreach (var entity in renderingEntities.ToList()) // ToList to avoid modification during enumeration
            {
                entity.Update(gameTime);
            }

            // Update all physics entities
            foreach (var entity in physicsEntities.ToList())
            {
                entity.Update(gameTime);
            }

            // Update physics simulation
            physicsSystem?.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        public virtual void Draw(GameTime gameTime, Camera camera)
        {
            // Draw all rendering entities
            foreach (var entity in renderingEntities)
            {
                if (entity.IsVisible)
                {
                    entity.Draw(gameTime, camera);
                }
            }

            // Draw all physics entities
            foreach (var entity in physicsEntities)
            {
                if (entity.IsVisible)
                {
                    entity.Draw(gameTime, camera);
                }
            }
        }

        // Entity management methods
        public virtual T AddRenderingEntity<T>(T entity) where T : RenderingEntity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            renderingEntities.Add(entity);
            return entity;
        }

        public virtual T AddPhysicsEntity<T>(T entity) where T : PhysicsEntity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            physicsEntities.Add(entity);
            return entity;
        }

        public virtual bool RemoveRenderingEntity(RenderingEntity entity)
        {
            return renderingEntities.Remove(entity);
        }

        public virtual bool RemovePhysicsEntity(PhysicsEntity entity)
        {
            if (physicsEntities.Remove(entity))
            {
                entity.RemoveFromPhysics();
                return true;
            }
            return false;
        }

        public virtual void ClearAllEntities()
        {
            // Remove all physics entities from physics simulation
            foreach (var entity in physicsEntities)
            {
                entity.RemoveFromPhysics();
            }

            renderingEntities.Clear();
            physicsEntities.Clear();
        }

        // Utility methods for finding entities
        public virtual List<T> FindEntitiesOfType<T>() where T : RenderingEntity
        {
            var results = new List<T>();
            
            results.AddRange(renderingEntities.OfType<T>());
            results.AddRange(physicsEntities.OfType<T>());
            
            return results;
        }

        public virtual T FindFirstEntityOfType<T>() where T : RenderingEntity
        {
            return FindEntitiesOfType<T>().FirstOrDefault();
        }

        public virtual List<RenderingEntity> FindEntitiesInRadius(Vector3 center, float radius)
        {
            var results = new List<RenderingEntity>();
            float radiusSquared = radius * radius;

            foreach (var entity in renderingEntities)
            {
                if (Vector3.DistanceSquared(entity.Position, center) <= radiusSquared)
                {
                    results.Add(entity);
                }
            }

            foreach (var entity in physicsEntities)
            {
                if (Vector3.DistanceSquared(entity.Position, center) <= radiusSquared)
                {
                    results.Add(entity);
                }
            }

            return results;
        }

        // Factory methods for easy entity creation
        public virtual PhysicsEntity CreateBox(Vector3 position, Vector3 size, float mass = 1f, 
            bool isStatic = false, string modelPath = "models/cube", string texturePath = "textures/prototype/brick")
        {
            if (physicsSystem == null) 
                throw new InvalidOperationException("Physics system is required to create physics entities");

            var entity = PhysicsEntityFactory.CreateBox(physicsSystem, position, size, mass, isStatic, modelPath, texturePath);
            return AddPhysicsEntity(entity);
        }

        public virtual PhysicsEntity CreateSphere(Vector3 position, float radius, float mass = 1f,
            bool isStatic = false, string modelPath = "models/sphere", string texturePath = null)
        {
            if (physicsSystem == null)
                throw new InvalidOperationException("Physics system is required to create physics entities");

            var entity = PhysicsEntityFactory.CreateSphere(physicsSystem, position, radius, mass, isStatic, modelPath, texturePath);
            return AddPhysicsEntity(entity);
        }

        public virtual PhysicsEntity CreateCapsule(Vector3 position, float radius, float length, float mass = 1f,
            bool isStatic = false, string modelPath = "models/capsule", string texturePath = null)
        {
            if (physicsSystem == null)
                throw new InvalidOperationException("Physics system is required to create physics entities");

            var entity = PhysicsEntityFactory.CreateCapsule(physicsSystem, position, radius, length, mass, isStatic, modelPath, texturePath);
            return AddPhysicsEntity(entity);
        }

        public virtual PhysicsEntity CreateGround(Vector3 position, Vector3 size,
            string modelPath = "models/cube", string texturePath = "textures/prototype/concrete")
        {
            if (physicsSystem == null)
                throw new InvalidOperationException("Physics system is required to create physics entities");

            var entity = PhysicsEntityFactory.CreateGround(physicsSystem, position, size, modelPath, texturePath);
            return AddPhysicsEntity(entity);
        }

        public virtual RenderingEntity CreateRenderingEntity(Vector3 position, string modelPath, 
            string texturePath = null, string effectPath = "shaders/surface/Unlit", bool isShaded = true)
        {
            var entity = new RenderingEntity(modelPath, texturePath, effectPath, isShaded);
            entity.Position = position;
            return AddRenderingEntity(entity);
        }
    }
}