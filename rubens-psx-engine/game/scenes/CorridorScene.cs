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
using rubens_psx_engine.Extensions;
using rubens_psx_engine.system.config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
        //MultiMaterialRenderingEntity corridorEntity;
        
        // Direct BepuPhysics meshes for corridors
        List<Mesh> corridorBepuMeshes;
        
        // Entity collections
        List<PhysicsEntity> bullets;
        PhysicsEntity ground;

        // Input handling
        bool mouseClick = false;
        KeyboardState previousKeyboard;
        
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

            // Initialize debug rendering (off by default)
            if (boundingBoxRenderer != null)
            {
                boundingBoxRenderer.ShowBoundingBoxes = false;
            }

            float intervals = 20;
            float jitter = 3;
            var affine = 0;
            var wall = new UnlitMaterial("textures/corridor_wall");
            wall.VertexJitterAmount = jitter;
            wall.Brightness = 1.0f; // Slightly darker
            wall.AffineAmount = affine;

            var floor = new UnlitMaterial("textures/floor_1");
            floor.VertexJitterAmount = jitter;
            floor.AffineAmount = affine;
            floor.Brightness = 1.0f; // Brighter
            //material2.LightDirection = Vector3.Normalize(new Vector3(0.5f, -1, 0.3f));

            var placeholder = new VertexLitMaterial("textures/prototype/wood");
            placeholder.VertexJitterAmount = jitter;
            placeholder.AffineAmount = affine;
            placeholder.AmbientColor = new Vector3(.7f, .7f, .7f);
            //placeholder.Brightness = 1.0f; // Brighter

            var rotateLevel = QuaternionExtensions.CreateFromYawPitchRollDegrees(180, 0, 0);

            // Lower Corridors
            CreateCorridorWithMaterialsAndPhysics(new Vector3(0, -4, 16) * intervals,
                Quaternion.Identity, wall, floor);

            CreateCorridorWithMaterialsAndPhysics(new Vector3(0, -4, 8) * intervals,
                Quaternion.Identity, wall, floor);

            CreateCorridorWithMaterialsAndPhysics(new Vector3(0, -4, 0) * intervals,
                Quaternion.Identity, wall, floor);

            CreateCorridorSlope(new Vector3(0, -4, -8) * intervals,
                rotateLevel, wall, floor);

            CreateCorridorSlope(new Vector3(0, -2, -16) * intervals,
                rotateLevel, wall, floor);

            CreateCorridorWithMaterialsAndPhysics(new Vector3(0, 0, -24) * intervals,
                Quaternion.Identity, wall, floor);

            // Upper corridors
            CreateStaticRoom(new Vector3(0, 0, -32) * intervals,
                rotateLevel, [wall, floor], "models/level/corridor_T_shape");
            CreateStaticRoom(new Vector3(-8, 0, -32) * intervals,
                Quaternion.Identity, [wall, floor], "models/level/corridor_T_shape");
            CreateStaticRoom(new Vector3(8, 0, -32) * intervals,
                Quaternion.Identity, [wall, floor], "models/level/corridor_T_shape");

            CreateStaticRoom(new Vector3(-16, 0, -32) * intervals,
                Quaternion.Identity, [wall, floor], "models/level/corridor_X_shape");


            // Main Engines
            CreateStaticRoom(new Vector3(0, -4, 28) * intervals,
                rotateLevel, [placeholder], "models/level/EngineRoom_1");

            CreateStaticRoom(new Vector3(0, -4, 28) * intervals,
                rotateLevel, [placeholder], "models/level/EngineRoom_2");

            // Main Facilities
            CreateStaticRoom(new Vector3(16, 0, -28) * intervals,
                rotateLevel, [placeholder], "models/level/medical_room");
            CreateStaticRoom(new Vector3(8, 0, -43) * intervals,
                rotateLevel, [placeholder], "models/level/crew_quarters");
            CreateStaticRoom(new Vector3(-9, 0, -54) * intervals,
                rotateLevel, [placeholder], "models/level/bathroom");
            CreateStaticRoom(new Vector3(-20, 0, -28) * intervals,
                rotateLevel, [placeholder], "models/level/lab");
            CreateStaticRoom(new Vector3(-16, -2, -48) * intervals,
                rotateLevel, [placeholder], "models/level/arborium");

            //bridge corridor
            CreateStaticRoom(new Vector3(-16, 0, -78) * intervals,
                Quaternion.Identity, [wall, floor], "models/level/corridor_X_shape");

            CreateStaticRoom(new Vector3(-16, -2, -48) * intervals,
                rotateLevel, [placeholder], "models/level/arborium");

            CreateStaticRoom(new Vector3(-16, -2, -48) * intervals,
                rotateLevel, [placeholder], "models/level/arborium");

            CreateStaticRoom(new Vector3(-24, 0, -78) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0),
                [wall, floor, wall], "models/level/corridor");
            CreateStaticRoom(new Vector3(-32, 0, -78) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0),
                [wall, floor, wall], "models/level/corridor");
            CreateStaticRoom(new Vector3(-8, 0, -78) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0),
                [wall, floor, wall],  "models/level/corridor");

            //CreateCorridorWithMaterialsAndPhysics(new Vector3(-24, 0, -78) * intervals,
            //    Quaternion.CreateFromYawPitchRoll(0, 0, 0), wall, floor);
            //CreateCorridorWithMaterialsAndPhysics(new Vector3(-32, 0, -78) * intervals,
            //    Quaternion.CreateFromYawPitchRoll(0, 0, 0), wall, floor);
            //CreateCorridorWithMaterialsAndPhysics(new Vector3(-8, 0, -78) * intervals,
            //    Quaternion.CreateFromYawPitchRoll(0, 0, 0), wall, floor);

            CreateStaticRoom(new Vector3(-4, 0, -78) * intervals,
                rotateLevel, [placeholder], "models/level/curved_hall");
            CreateCorridorWithMaterialsAndPhysics(new Vector3(16, 0, -102) * intervals,
                Quaternion.Identity, wall, floor);
            CreateCorridorSlope(new Vector3(16, 0, -110) * intervals,
                rotateLevel, wall, floor);
            CreateCorridorSlope(new Vector3(16, 2, -118) * intervals,
                rotateLevel, wall, floor);

            // bridge
            CreateStaticRoom(new Vector3(16, 4, -139) * intervals,
                rotateLevel, [placeholder], "models/level/bridge_prep_rooms");
            CreateStaticRoom(new Vector3(16, 4, -139) * intervals,
                rotateLevel, [placeholder], "models/level/bridge");
            CreateStaticRoom(new Vector3(16, 4, -126) * intervals,
                Quaternion.Identity, [wall, floor], "models/level/corridor_X_shape");
            // Create physics ground for collision (visible for testing)
            //CreatePhysicsGround();

            // Create test cube in the middle of the scene
            //CreateTestCube();

            // Create character
            CreateCharacter(new Vector3(0, -70, 160)); // Start at back of corridor
        }

        private void CreateStaticRoom(Vector3 offset, Quaternion rotation, Material[] mats, string mesh, bool  flipPhys = false)
        {
            // Create three different materials for the corridor channels using actual texture files

            // Create corridor entity with three material channels
            var loadedMats = new Dictionary<int, Material>();
            for(int i = 0; i < mats.Length; i++)
            {
                loadedMats.Add(i, mats[i]);
            }
            var entity = new MultiMaterialRenderingEntity(mesh, loadedMats);

            entity.Position = Vector3.Zero + offset;
            entity.Scale = Vector3.One * .2f;
            entity.Rotation = rotation;
            entity.IsVisible = true;

            // Add to rendering entities
            AddRenderingEntity(entity);

            // Create physics mesh for the corridor
            CreatePhysicsMesh(mesh, offset, rotation, 
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, -90, 0), Vector3.Zero);
        }


        private void CreateCorridorSlope(Vector3 offset, Quaternion rotation, Material wall, Material floor)
        {
            var mesh = "models/level/corridor_slope";
            // Create three different materials for the corridor channels using actual texture files
    

            // Create corridor entity with three material channels
             var corridorEntity = new MultiMaterialRenderingEntity(mesh,
                new Dictionary<int, Material>
                {
                    { 0, wall }, // Floor/walls
                    { 1, floor }, // Architectural details
                    { 2, wall }  // Decorative elements
                });

            corridorEntity.Position = Vector3.Zero + offset;
            corridorEntity.Scale = Vector3.One * .2f;
            corridorEntity.Rotation = rotation;
            corridorEntity.IsVisible = true;

            // Add to rendering entities
            AddRenderingEntity(corridorEntity);

            // Create physics mesh for the corridor
            CreatePhysicsMesh(mesh, offset, rotation, QuaternionExtensions.CreateFromYawPitchRollDegrees(0, -90, 0), Vector3.Zero);
        }


        private void CreateCorridorWithMaterialsAndPhysics(Vector3 offset, Quaternion rotation, Material wall, Material floor)
        {
            var mesh = "models/corridor_single";

            // Create corridor entity with three material channels
            var corridorEntity = new MultiMaterialRenderingEntity(mesh, 
                new Dictionary<int, Material>
                {
                    { 0, floor }, // Floor/walls
                    { 1, wall }, // Architectural details
                    { 2, wall }  // Decorative elements
                });

            corridorEntity.Position = Vector3.Zero + offset; 
            corridorEntity.Scale = Vector3.One * .2f;
            corridorEntity.IsVisible = true;
            
            // Add to rendering entities
            AddRenderingEntity(corridorEntity);

            // Create physics mesh for the corridor
            CreatePhysicsMesh(mesh, offset, rotation, QuaternionExtensions.CreateFromYawPitchRollDegrees(0, -90, 0), Vector3.Zero);
        }


        private void CreatePhysicsMesh(string mesh, Vector3 offset, Quaternion rotation,
            Quaternion rotationOffset, Vector3 physicsMeshOffset)
        {
            try
            {
                // Load the same model used for rendering
                var corridorModel = Globals.screenManager.Content.Load<Model>(mesh);
                
                // Use consistent scaling approach: visual scale * physics scale factor
                // Corridor uses visual scale of 0.1f, so physics scale = 0.1f * 10 = 1.0f
                var visualScale = Vector3.One * 0.2f; // Same scale as rendering entity
                var physicsScale = visualScale * 100; // Apply our learned scaling factor

                // Extract triangles and wireframe vertices for rendering
                var (triangles, wireframeVertices) = BepuMeshExtractor.ExtractTrianglesFromModel(corridorModel, physicsScale, physicsSystem);
                var bepuMesh = new Mesh(triangles, Vector3.One.ToVector3N(), physicsSystem.BufferPool);
                
                // Add the mesh shape to the simulation's shape collection
                var shapeIndex = physicsSystem.Simulation.Shapes.Add(bepuMesh);
                //rotate the rotation by this.
                var rotation2 = rotation * rotationOffset;
                // Create static body with the mesh shape
                var staticHandle = physicsSystem.Simulation.Statics.Add(new StaticDescription(
                    offset.ToVector3N(),
                    rotation2.ToQuaternionN(), 
                    shapeIndex));
                
                // Keep references for cleanup and wireframe rendering
                corridorBepuMeshes.Add(bepuMesh);
                meshTriangleVertices.Add(wireframeVertices);
                staticMeshTransforms.Add((offset, rotation2));


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
            ground = CreateGround(new Vector3(0, -50, 0), new Vector3(8000, 2, 8000), 
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
                minimumSpeculativeMargin: 0.5f, 
                mass: 0.1f, 
                maximumHorizontalForce: 100,
                maximumVerticalGlueForce: 1500,
                jumpVelocity: 0,
                speed: 80,
                maximumSlope: 40f.ToRadians());
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
            var keyboard = Keyboard.GetState();
            
            // Toggle physics wireframe rendering with L key
            if (keyboard.IsKeyDown(Keys.L) && !previousKeyboard.IsKeyDown(Keys.L))
            {
                // Toggle the bounding box renderer (which controls both bounding boxes and wireframes)
                if (boundingBoxRenderer != null)
                {
                   // boundingBoxRenderer.ShowBoundingBoxes = !boundingBoxRenderer.ShowBoundingBoxes;
                    Console.WriteLine($"Physics debug rendering: {(boundingBoxRenderer.ShowBoundingBoxes ? "ON" : "OFF")}");
                }
            }
            
            previousKeyboard = keyboard;
            
            // Mouse shooting - use camera direction
            if (Mouse.GetState().LeftButton == ButtonState.Pressed && !mouseClick)
            {
                if (characterActive && character.HasValue)
                {
                    var characterPos = character.Value.Body.Pose.Position.ToVector3();
                    
                    // Use forward direction (will be replaced by proper camera forward by screen)
                    var dir = Vector3N.UnitZ;
                    //ShootBullet(characterPos + new Vector3(0, 5, 0), dir);
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
            if (boundingBoxRenderer != null && boundingBoxRenderer.ShowBoundingBoxes)
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
        //public MultiMaterialRenderingEntity GetCorridor() => corridorEntity;
        
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