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
        
        // Static physics meshes for corridors
        List<StaticMesh> corridorPhysicsMeshes;
        
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
            corridorPhysicsMeshes = new List<StaticMesh>();
            
            // Set black background for corridor scene
            BackgroundColor = Color.Black;
            Globals.screenManager.IsMouseVisible = false;
            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();

            // Create corridor with multiple materials and physics
            CreateCorridorWithMaterialsAndPhysics(Vector3.One);

            CreateCorridorWithMaterialsAndPhysics(Vector3.Forward * 80);

            CreateCorridorWithMaterialsAndPhysics(Vector3.Forward * 80 * 2);

            CreateCorridorWithMaterialsAndPhysics(Vector3.Forward * 80 * 3);

            CreateCorridorWithMaterialsAndPhysics(Vector3.Forward * 80* 4);

            // Create physics ground for collision (visible for testing)
            CreatePhysicsGround();
            
            // Create test cube in the middle of the scene
            //CreateTestCube();

            // Create character
            CreateCharacter(new Vector3(0, 10, 100)); // Start at back of corridor
        }

        private void CreateCorridorWithMaterialsAndPhysics(Vector3 offset)
        {
            var affine = 0;
            // Create three different materials for the corridor channels using actual texture files
            var cieling = new UnlitMaterial("textures/test/0_0");
            cieling.VertexJitterAmount = 4f;
            cieling.AffineAmount = affine;
            cieling.Brightness = 1.2f; // Slightly darker
            
            var floor = new UnlitMaterial("textures/test/0_1");
            floor.VertexJitterAmount = 4f;
            floor.AffineAmount = affine;
            floor.Brightness = 1.8f; // Brighter
            //material2.LightDirection = Vector3.Normalize(new Vector3(0.5f, -1, 0.3f));
            
            var material3 = new UnlitMaterial("textures/test/0_3");
            material3.VertexJitterAmount = 4f;
            material3.AffineAmount = affine;
            material3.Brightness = 1.2f; // Much brighter
            //material3.BakedLightIntensity = 1.2f;

            // Create corridor entity with three material channels
            corridorEntity = new MultiMaterialRenderingEntity("models/corridor_single", 
                new Dictionary<int, Material>
                {
                    { 0, floor }, // Floor/walls
                    { 1, cieling }, // Architectural details
                    { 2, cieling }  // Decorative elements
                });

            corridorEntity.Position = Vector3.Zero + offset; 
            corridorEntity.Scale = Vector3.One * .1f;
            corridorEntity.IsVisible = true;
            
            // Add to rendering entities
            AddRenderingEntity(corridorEntity);

            // Create physics mesh for the corridor
            CreateCorridorPhysicsMesh(offset);
        }

        private void CreateCorridorPhysicsMesh(Vector3 offset)
        {
            try
            {
                // Load the same model used for rendering
                var corridorModel = Globals.screenManager.Content.Load<Model>("models/corridor_single");
                
                // Create static mesh with the same scale as the rendering entity
                var corridorScale = Vector3.One * 0.1f; // Same scale as visual
                var staticMesh = new StaticMesh(corridorModel, corridorScale, 
                    physicsSystem.Simulation, physicsSystem.BufferPool);
                
                // Add to simulation at the same position as the visual
                staticMesh.AddToSimulation(physicsSystem.Simulation, offset, Quaternion.Identity);
                
                // Keep reference for cleanup
                corridorPhysicsMeshes.Add(staticMesh);
                
                Console.WriteLine($"Created corridor physics mesh at position: {offset}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create corridor physics mesh: {ex.Message}");
                // Continue without physics mesh for this corridor
            }
        }

        private void CreatePhysicsGround()
        {
            // Create visible ground plane for character physics and visual reference
            ground = CreateGround(new Vector3(0, -10f, 0), new Vector3(8000, 2, 8000), 
                "models/cube", "textures/prototype/concrete");
            ground.IsVisible = false; // Make it visible for testing
            ground.Scale = new Vector3(10f, 0.1f, 20f);
            ground.Color = new Vector3(0.5f, 0.5f, 0.5f); // Gray color
        }

        private void CreateTestCube()
        {
            // Create a simple test cube with unlit material in the center
            var testMaterial = new UnlitMaterial("textures/prototype/brick");
            testMaterial.VertexJitterAmount = 2.0f;
            testMaterial.AffineAmount = 1.0f;
            
            var testCube = CreateBoxWithMaterial(new Vector3(0, 10, 0), testMaterial, new Vector3(3f));
            testCube.Color = new Vector3(1.0f, 0.5f, 0.2f); // Orange color
            testCube.IsVisible = true;
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
            Globals.screenManager.IsMouseVisible = false;

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
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean up static meshes
                foreach (var staticMesh in corridorPhysicsMeshes)
                {
                    staticMesh?.Dispose();
                }
                corridorPhysicsMeshes.Clear();
            }
            base.Dispose(disposing);
        }
    }
}