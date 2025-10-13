using anakinsoft.entities;
using anakinsoft.system;
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
using Matrix = Microsoft.Xna.Framework.Matrix;
using Vector3N = System.Numerics.Vector3;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// The Lounge scene with models from models/lounge and first person controller
    /// </summary>
    public class TheLoungeScene : Scene
    {
        // Character system
        CharacterControllers characters;
        CharacterInput? character;
        bool characterActive;
        float characterLoadDelay = 1.0f; // Delay in seconds before character physics activate
        float timeSinceLoad = 0f;
        Quaternion characterInitialRotation = Quaternion.Identity; // Initial camera rotation

        // Direct BepuPhysics meshes for models
        List<Mesh> loungeBepuMeshes;

        // Collision mesh wireframe data
        List<List<Vector3>> meshTriangleVertices; // Store triangle vertices for wireframe
        List<(Vector3 position, Quaternion rotation)> staticMeshTransforms; // Store mesh transforms

        // Input handling
        KeyboardState previousKeyboard;

        public TheLoungeScene() : base()
        {
            // Initialize character system and physics
            characters = null; // Will be initialized in physics system
            physicsSystem = new PhysicsSystem(ref characters);

            loungeBepuMeshes = new List<Mesh>();
            meshTriangleVertices = new List<List<Vector3>>();
            staticMeshTransforms = new List<(Vector3 position, Quaternion rotation)>();

            // Set black background for lounge scene
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

            // Create first person character at starting position
            CreateCharacter(new Vector3(0, 0, 0), Quaternion.Identity);

            float jitter = 3;
            var affine = 0;

            // Create unlit materials for each lounge texture
            var ceilingMat = new UnlitMaterial("textures/Lounge/Cieling");
            ceilingMat.VertexJitterAmount = jitter;
            ceilingMat.Brightness = 1.2f;
            ceilingMat.AffineAmount = affine;

            var doorMat = new UnlitMaterial("textures/Lounge/door");
            doorMat.VertexJitterAmount = jitter;
            doorMat.Brightness = 1.2f;
            doorMat.AffineAmount = affine;

            var floorMat = new UnlitMaterial("textures/Lounge/floor_1");
            floorMat.VertexJitterAmount = jitter;
            floorMat.AffineAmount = affine;
            floorMat.Brightness = 1.2f;

            var wall1Mat = new UnlitMaterial("textures/Lounge/wall_1");
            wall1Mat.VertexJitterAmount = jitter;
            wall1Mat.Brightness = 1.2f;
            wall1Mat.AffineAmount = affine;

            var wall2Mat = new UnlitMaterial("textures/Lounge/wall_2");
            wall2Mat.VertexJitterAmount = jitter;
            wall2Mat.Brightness = 1.2f;
            wall2Mat.AffineAmount = affine;

            // Load all models from models/lounge with colliders
            // Position them in the scene - you can adjust these positions as needed
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { ceilingMat }, "models/lounge/Ceiling2");
            //CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { doorMat }, "models/lounge/Door");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { floorMat }, "models/lounge/Lounge_floor");
            //CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { wall1Mat }, "models/lounge/Lounge_wall");
            //CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { wall2Mat }, "models/lounge/Wall_L");
            //CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { wall2Mat }, "models/lounge/Wall_R");
            //CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { wall1Mat }, "models/lounge/Window");
        }

        private void CreateStaticMesh(Vector3 offset, Quaternion rotation, Material[] mats, string mesh)
        {
            // Create corridor entity with material channels
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

            // Create physics mesh for the model
            CreatePhysicsMesh(mesh, offset, rotation,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, -90, 0), Vector3.Zero);
        }

        private void CreatePhysicsMesh(string mesh, Vector3 offset, Quaternion rotation,
            Quaternion rotationOffset, Vector3 physicsMeshOffset)
        {
            try
            {
                // Load the same model used for rendering
                var model = Globals.screenManager.Content.Load<Model>(mesh);

                // Use consistent scaling approach: visual scale * physics scale factor
                var visualScale = Vector3.One * 0.2f; // Same scale as rendering entity
                var physicsScale = visualScale * 100; // Apply our learned scaling factor

                // Extract triangles and wireframe vertices for rendering
                var (triangles, wireframeVertices) = BepuMeshExtractor.ExtractTrianglesFromModel(model, physicsScale, physicsSystem);
                var bepuMesh = new Mesh(triangles, Vector3.One.ToVector3N(), physicsSystem.BufferPool);

                // Add the mesh shape to the simulation's shape collection
                var shapeIndex = physicsSystem.Simulation.Shapes.Add(bepuMesh);
                // Rotate the rotation by this
                var rotation2 = rotation * rotationOffset;

                // Create static body with the mesh shape
                var staticHandle = physicsSystem.Simulation.Statics.Add(new StaticDescription(
                    offset.ToVector3N(),
                    rotation2.ToQuaternionN(),
                    shapeIndex));

                // Keep references for cleanup and wireframe rendering
                loungeBepuMeshes.Add(bepuMesh);
                meshTriangleVertices.Add(wireframeVertices);
                staticMeshTransforms.Add((offset, rotation2));

                Console.WriteLine($"Created lounge physics mesh at position: {offset} with physics scale: {physicsScale}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to create lounge physics mesh: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                // Continue without physics mesh for this model
            }
        }

        void CreateCharacter(Vector3 position, Quaternion rotation)
        {
            characterActive = true;
            character = new CharacterInput(characters, position.ToVector3N(),
                new Capsule(0.5f * 10, 1 * 10),
                minimumSpeculativeMargin: 1f,
                mass: 0.1f,
                maximumHorizontalForce: 100,
                maximumVerticalGlueForce: 1500,
                jumpVelocity: 0,
                speed: 80,
                maximumSlope: 40f.ToRadians());

            // Store the initial rotation for the camera
            characterInitialRotation = rotation;
        }

        public override void Update(GameTime gameTime)
        {
            // Update load timer first
            timeSinceLoad += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // During load delay, don't update base (which updates physics)
            // This prevents character from falling through floor while level loads
            if (timeSinceLoad <= characterLoadDelay)
            {
                // Skip physics update during load delay
                // Just update rendering entities
                foreach (var entity in renderingEntities)
                {
                    entity.Update(gameTime);
                }
            }
            else
            {
                // After load delay, update normally (includes physics)
                base.Update(gameTime);
            }

            // Apply global mouse visibility state
            Globals.screenManager.IsMouseVisible = Globals.shouldShowMouse;

            // Only handle input and movement if not in menu
            if (!Globals.IsInMenuState())
            {
                // Handle input
                HandleInput();
            }
        }

        public void UpdateWithCamera(GameTime gameTime, Camera camera)
        {
            // Update the scene normally first
            Update(gameTime);

            // Only update character movement if not in menu
            if (!Globals.IsInMenuState())
            {
                // Update character with camera (for movement direction based on camera look)
                // Only allow character movement after load delay to prevent falling through floor
                if (characterActive && character.HasValue && timeSinceLoad > characterLoadDelay)
                {
                    character.Value.UpdateCharacterGoals(Keyboard.GetState(), camera, (float)gameTime.ElapsedGameTime.TotalSeconds);
                }
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
                    boundingBoxRenderer.ShowBoundingBoxes = !boundingBoxRenderer.ShowBoundingBoxes;
                    Console.WriteLine($"Physics debug rendering: {(boundingBoxRenderer.ShowBoundingBoxes ? "ON" : "OFF")}");
                }
            }

            previousKeyboard = keyboard;
        }

        public override void Draw(GameTime gameTime, Camera camera)
        {
            // Draw all entities using the base scene drawing
            base.Draw(gameTime, camera);

            // Draw wireframe visualization of static mesh collision geometry if enabled
            if (boundingBoxRenderer != null && boundingBoxRenderer.ShowBoundingBoxes)
            {
                DrawStaticMeshWireframes(gameTime, camera);
            }
        }

        /// <summary>
        /// Draws the UI elements
        /// </summary>
        public void DrawUI(GameTime gameTime, Camera camera, SpriteBatch spriteBatch)
        {
            var font = Globals.fontNTR;
            if (font != null)
            {
                // Show loading indicator during character load delay
                if (timeSinceLoad <= characterLoadDelay)
                {
                    string loadingText = "Loading The Lounge...";
                    var textSize = font.MeasureString(loadingText);
                    var screenCenter = new Vector2(
                        Globals.screenManager.GraphicsDevice.Viewport.Width / 2f,
                        Globals.screenManager.GraphicsDevice.Viewport.Height / 2f
                    );

                    // Draw loading text with fade effect
                    float fadeAlpha = 1.0f - (timeSinceLoad / characterLoadDelay);
                    spriteBatch.DrawString(font, loadingText,
                        screenCenter - textSize / 2f,
                        Color.White * fadeAlpha);
                }
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
            for (int meshIndex = 0; meshIndex < loungeBepuMeshes.Count; meshIndex++)
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
        public bool IsCharacterActive() => characterActive;
        public CharacterInput? GetCharacter() => character;
        public Quaternion GetCharacterInitialRotation() => characterInitialRotation;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // BepuPhysics meshes are cleaned up by the physics system
                loungeBepuMeshes.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
