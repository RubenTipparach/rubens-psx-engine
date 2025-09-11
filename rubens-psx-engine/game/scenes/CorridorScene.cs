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
using rubens_psx_engine.system.config;
using System;
using System.Collections.Generic;
using System.Linq;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Vector3N = System.Numerics.Vector3;

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
        
        // Direct BepuPhysics meshes for corridors
        List<Mesh> corridorBepuMeshes;
        
        // Entity collections
        List<PhysicsEntity> bullets;
        PhysicsEntity ground;

        // Input handling
        bool mouseClick = false;
        
        // Collision mesh wireframe data
        List<List<Vector3>> meshTriangleVertices; // Store triangle vertices for wireframe
        List<(Vector3 position, Quaternion rotation)> staticMeshTransforms; // Store mesh transforms

        public CorridorScene() : base()
        {
            // Initialize character system and physics
            characters = null; // Will be initialized in physics system
            physicsSystem = new PhysicsSystem(ref characters);
            
            bullets = new List<PhysicsEntity>();
            corridorBepuMeshes = new List<Mesh>();
            meshTriangleVertices = new List<List<Vector3>>();
            staticMeshTransforms = new List<(Vector3 position, Quaternion rotation)>();
            
            // Set black background for corridor scene
            BackgroundColor = Color.Black;
            Globals.screenManager.IsMouseVisible = false;
            Initialize();

        }

        public override void Initialize()
        {
            base.Initialize();

            // Initialize debug rendering based on config
            if (boundingBoxRenderer != null)
            {
                boundingBoxRenderer.ShowBoundingBoxes = RenderingConfigManager.Config.Development.ShowStaticMeshDebug;
            }

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
            var cieling = new UnlitMaterial("textures/corridor_wall");
            cieling.VertexJitterAmount = 4f;
            cieling.AffineAmount = affine;
            cieling.Brightness = 1.2f; // Slightly darker
            
            var floor = new UnlitMaterial("textures/test/0_1");
            floor.VertexJitterAmount = 4f;
            floor.AffineAmount = affine;
            floor.Brightness = 1.8f; // Brighter
            //material2.LightDirection = Vector3.Normalize(new Vector3(0.5f, -1, 0.3f));
            
            var material3 = new UnlitMaterial("textures/corridor_wall");
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
                
                // Use consistent scaling approach: visual scale * physics scale factor
                // Corridor uses visual scale of 0.1f, so physics scale = 0.1f * 10 = 1.0f
                var visualScale = Vector3.One * 0.1f; // Same scale as rendering entity
                var physicsScale = visualScale * 10; // Apply our learned scaling factor

                // Extract triangles and wireframe vertices for rendering
                var (triangles, wireframeVertices) = BepuMeshExtractor.ExtractTrianglesFromModel(corridorModel, physicsScale, physicsSystem);
                var bepuMesh = new Mesh(triangles, physicsScale.ToVector3N(), physicsSystem.BufferPool);
                
                // Add the mesh shape to the simulation's shape collection
                var shapeIndex = physicsSystem.Simulation.Shapes.Add(bepuMesh);
                
                // Create static body with the mesh shape
                var staticHandle = physicsSystem.Simulation.Statics.Add(new StaticDescription(
                    offset.ToVector3N(), 
                    Quaternion.Identity.ToQuaternionN(), 
                    shapeIndex));
                
                // Keep references for cleanup and wireframe rendering
                corridorBepuMeshes.Add(bepuMesh);
                meshTriangleVertices.Add(wireframeVertices);
                staticMeshTransforms.Add((offset, Quaternion.Identity));


                Console.WriteLine($"Created corridor physics mesh at position: {offset} with physics scale: {physicsScale}");
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
            
            // Draw wireframe visualization of static mesh collision geometry if enabled
            if (RenderingConfigManager.Config.Development.ShowStaticMeshDebug)
            {
                DrawStaticMeshWireframes(gameTime, camera);
            }
        }

        private void DrawStaticMeshWireframes(GameTime gameTime, Camera camera)
        {
            var graphicsDevice = Globals.screenManager.GraphicsDevice;

            // Create a basic effect for wireframe rendering
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = camera.View;
            basicEffect.Projection = camera.Projection;
            basicEffect.World = Matrix.Identity;

            // Draw wireframes for each static mesh
            for (int meshIndex = 0; meshIndex < corridorBepuMeshes.Count; meshIndex++)
            {
                if (meshIndex < meshTriangleVertices.Count && meshIndex < staticMeshTransforms.Count)
                {
                    var triangleVertices = meshTriangleVertices[meshIndex];
                    var (position, rotation) = staticMeshTransforms[meshIndex];

                    if (triangleVertices != null && triangleVertices.Count > 0)
                    {
                        // Create transform matrix for this static mesh
                        var worldMatrix = Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position);

                        // Create wireframe edges from triangles
                        var wireframeVertices = new List<VertexPositionColor>();

                        for (int i = 0; i < triangleVertices.Count; i += 3)
                        {
                            if (i + 2 < triangleVertices.Count)
                            {
                                // Apply world transform to vertices
                                var v1 = Vector3.Transform(triangleVertices[i], worldMatrix);
                                var v2 = Vector3.Transform(triangleVertices[i + 1], worldMatrix);
                                var v3 = Vector3.Transform(triangleVertices[i + 2], worldMatrix);

                                // Create the three edges of the triangle
                                // Edge 1: v1 to v2
                                wireframeVertices.Add(new VertexPositionColor(v1, Color.Yellow));
                                wireframeVertices.Add(new VertexPositionColor(v2, Color.Yellow));

                                // Edge 2: v2 to v3
                                wireframeVertices.Add(new VertexPositionColor(v2, Color.Yellow));
                                wireframeVertices.Add(new VertexPositionColor(v3, Color.Yellow));

                                // Edge 3: v3 to v1
                                wireframeVertices.Add(new VertexPositionColor(v3, Color.Yellow));
                                wireframeVertices.Add(new VertexPositionColor(v1, Color.Yellow));
                            }
                        }

                        if (wireframeVertices.Count > 0)
                        {
                            try
                            {
                                // Apply the effect and draw the wireframe lines
                                basicEffect.CurrentTechnique.Passes[0].Apply();
                                graphicsDevice.DrawUserPrimitives(
                                    PrimitiveType.LineList,
                                    wireframeVertices.ToArray(),
                                    0,
                                    wireframeVertices.Count / 2);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error drawing wireframe: {ex.Message}");
                            }
                        }
                    }
                }
            }

            basicEffect.Dispose();
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
                // BepuPhysics meshes are cleaned up by the physics system
                corridorBepuMeshes.Clear();
            }
            base.Dispose(disposing);
        }
    }
}