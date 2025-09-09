using anakinsoft.system.character;
using anakinsoft.system.physics;
using anakinsoft.utilities;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;
using Demos.Demos.Characters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Vector3N = System.Numerics.Vector3;
using Matrix = Microsoft.Xna.Framework.Matrix;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Corridor scene with multi-material corridor_single model and FPS character controller
    /// </summary>
    public class CorridorScene : Scene
    {
        // Character system
        CharacterControllers characters;
        CharacterInput? character;
        bool characterActive;

        // Multi-material corridor entity
        MultiMaterialRenderingEntity corridorEntity;
        
        // Entity collections
        List<PhysicsEntity> bullets;
        PhysicsEntity ground;

        // Input handling
        bool mouseClick = false;

        public CorridorScene() : base()
        {
            // Initialize character system and physics
            characters = null; // Will be initialized in physics system
            physicsSystem = new PhysicsSystem(ref characters);
            
            bullets = new List<PhysicsEntity>();

            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();

            // Create corridor with multiple materials
            CreateCorridorWithMaterials();
            
            // Create physics ground for collision (invisible, just for physics)
            CreatePhysicsGround();

            // Create character
            CreateCharacter(new Vector3(0, 10, 100)); // Start at back of corridor
        }

        private void CreateCorridorWithMaterials()
        {
            // Create three different materials for the corridor channels using actual texture files
            var material1 = new UnlitMaterial("textures/test/0_0");
            material1.VertexJitterAmount = 1.0f;
            material1.AffineAmount = 0.8f;
            
            var material2 = new VertexLitMaterial("textures/test/0_1");  
            material2.VertexJitterAmount = 1.2f;
            material2.AffineAmount = 0.6f;
            material2.LightDirection = Vector3.Normalize(new Vector3(0.5f, -1, 0.3f));
            
            var material3 = new BakedVertexLitMaterial("textures/test/0_3");
            material3.VertexJitterAmount = 0.8f;
            material3.AffineAmount = 0.9f;
            material3.BakedLightIntensity = 1.2f;

            // Create corridor entity with three material channels
            corridorEntity = new MultiMaterialRenderingEntity("models/corridor_single", 
                new Dictionary<int, Material>
                {
                    { 0, material1 }, // Floor/walls
                    { 1, material2 }, // Architectural details
                    { 2, material3 }  // Decorative elements
                });

            corridorEntity.Position = Vector3.Zero;
            corridorEntity.Scale = Vector3.One;
            corridorEntity.IsVisible = true;
            
            // Add to rendering entities
            AddRenderingEntity(corridorEntity);
        }

        private void CreatePhysicsGround()
        {
            // Create invisible ground plane for character physics
            ground = CreateGround(new Vector3(0, -10f, 0), new Vector3(200, 2, 400), 
                "models/cube", null);
            ground.IsVisible = false; // Invisible - corridor model provides visuals
            ground.Scale = new Vector3(10f, 0.1f, 20f);
        }

        void CreateCharacter(Vector3 position)
        {
            characterActive = true;
            character = new CharacterInput(characters, position.ToVector3N(), 
                new Capsule(0.5f * 10, 1 * 10),
                minimumSpeculativeMargin: 0.1f, 
                mass: 0.1f, 
                maximumHorizontalForce: 200,
                maximumVerticalGlueForce: 10000,
                jumpVelocity: 100,
                speed: 40,
                maximumSlope: MathF.PI * 0.4f);
        }

        private PhysicsEntity CreateBulletEntity(Vector3 position, Vector3N direction)
        {
            // Create bullet with physics
            var bullet = CreateSphere(position, 2f, 5f, false, "models/sphere", null);
            
            bullet.Scale = new Vector3(0.2f);
            bullet.Color = new Vector3(1, 0.3f, 0); // Orange bullets
            
            // Apply initial velocity
            bullet.SetVelocity(direction * 300f);
            
            bullets.Add(bullet);
            return bullet;
        }

        void ShootBullet(Vector3 position, Vector3N direction)
        {
            CreateBulletEntity(position, direction);
            Console.WriteLine("Corridor bullet fired");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Handle input
            HandleInput();
        }

        public void UpdateWithCamera(GameTime gameTime, Camera camera)
        {
            // Update the scene normally first
            Update(gameTime);

            // Update character with camera (for movement direction based on camera look)
            if (characterActive && character.HasValue)
            {
                character.Value.UpdateCharacterGoals(Keyboard.GetState(), camera, (float)gameTime.ElapsedGameTime.TotalSeconds);
            }
        }

        private void HandleInput()
        {
            // Mouse shooting - use camera direction
            if (Mouse.GetState().LeftButton == ButtonState.Pressed && !mouseClick)
            {
                if (characterActive && character.HasValue)
                {
                    var characterPos = character.Value.Body.Pose.Position.ToVector3();
                    
                    // Use forward direction (will be replaced by proper camera forward by screen)
                    var dir = Vector3N.UnitZ;
                    ShootBullet(characterPos + new Vector3(0, 5, 0), dir);
                }
                mouseClick = true;
            }
            if (Mouse.GetState().LeftButton == ButtonState.Released)
            {
                mouseClick = false;
            }
        }

        public override void Draw(GameTime gameTime, Camera camera)
        {
            // Draw all entities using the base scene drawing
            base.Draw(gameTime, camera);

            // The corridor entity will be drawn with its multi-material system
            // Character is not drawn in FPS mode
        }

        // Utility methods for external access
        public List<PhysicsEntity> GetBullets() => bullets;
        public bool IsCharacterActive() => characterActive;
        public CharacterInput? GetCharacter() => character;
        public MultiMaterialRenderingEntity GetCorridor() => corridorEntity;
    }
}