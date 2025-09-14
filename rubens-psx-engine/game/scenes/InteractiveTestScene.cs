using anakinsoft.system.character;
using anakinsoft.system.physics;
using anakinsoft.utilities;
using Demos.Demos.Characters;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.entities;
using rubens_psx_engine.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Vector3N = System.Numerics.Vector3;
using QuaternionN = System.Numerics.Quaternion;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Test scene for raycasting and trigger volumes
    /// </summary>
    public class InteractiveTestScene : Scene
    {
        // Character system
        CharacterControllers characters;
        CharacterInput? character;
        bool characterActive;

        // Interactive cube data
        public class CubeData
        {
            public PhysicsEntity Entity { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public bool IsHighlighted { get; set; }
        }

        List<CubeData> interactiveCubes;
        CubeData hoveredCube;

        // Trigger area
        Vector3 triggerAreaCenter;
        Vector3 triggerAreaSize;
        bool isInsideTriggerArea;
        
        // UI text
        string statusText = "";
        
        // Input
        MouseState previousMouse;
        KeyboardState previousKeyboard;

        public InteractiveTestScene() : base()
        {
            // Initialize character system and physics
            characters = null;
            physicsSystem = new PhysicsSystem(ref characters);
            
            interactiveCubes = new List<CubeData>();
            
            // Set trigger area
            triggerAreaCenter = new Vector3(0, 5, -20);
            triggerAreaSize = new Vector3(20, 10, 20);
            
            // Set background color
            BackgroundColor = Color.DarkSlateGray;
            
            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();

            // Create ground
            var ground = CreateGround(new Vector3(0, -2f, 0), new Vector3(200, 1, 200), 
                "models/cube", "textures/prototype/concrete");
            ground.IsVisible = true;
            ground.Scale = new Vector3(10f, 0.05f, 10f);
            ground.Color = new Vector3(0.3f, 0.3f, 0.3f);

            // Create interactive cubes with data
            CreateInteractiveCubes();

            // Create visual representation of trigger area (transparent box)
            CreateTriggerAreaVisual();

            // Create character
            CreateCharacter(new Vector3(0, 5, 30));
        }

        private void CreateInteractiveCubes()
        {
            // Create a grid of interactive cubes
            string[] names = { "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta" };
            string[] descriptions = {
                "Primary test object",
                "Secondary system module",
                "Tertiary data node",
                "Quaternary processor",
                "Fifth element container",
                "Sixth dimension portal",
                "Seventh seal keeper",
                "Eighth wonder artifact"
            };
            
            int index = 0;
            for (int x = -15; x <= 15; x += 10)
            {
                for (int z = -15; z <= 15; z += 10)
                {
                    if (index >= names.Length) break;
                    
                    var position = new Vector3(x, 5, z);
                    var cube = CreateBox(position, new Vector3(4, 4, 4), 10f, false, 
                        "models/cube", "textures/prototype/brick");
                    
                    cube.Scale = new Vector3(0.2f);
                    cube.Color = new Vector3(0.5f, 0.7f, 1.0f);
                    
                    var cubeData = new CubeData
                    {
                        Entity = cube,
                        Name = names[index % names.Length],
                        Description = descriptions[index % descriptions.Length]
                    };
                    
                    interactiveCubes.Add(cubeData);
                    index++;
                }
            }
        }

        private void CreateTriggerAreaVisual()
        {
            // Create a semi-transparent visual representation of the trigger area
            var triggerVisual = new RenderingEntity("models/cube", "textures/prototype/concrete");
            triggerVisual.Position = triggerAreaCenter;
            triggerVisual.Scale = triggerAreaSize / 20f; // Cube model is 20 units
            triggerVisual.Color = new Vector3(0.2f, 1.0f, 0.2f) * 0.5f; // Make it darker to simulate transparency
            AddRenderingEntity(triggerVisual);
        }

        void CreateCharacter(Vector3 position)
        {
            characterActive = true;
            character = new CharacterInput(characters, position.ToVector3N(), 
                new Capsule(0.5f * 10, 1 * 10),
                minimumSpeculativeMargin: 0.1f, 
                mass: 1f, 
                maximumHorizontalForce: 100,
                maximumVerticalGlueForce: 10,
                jumpVelocity: 50,
                speed: 40,
                maximumSlope: 45f.ToRadians());
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            // Handle input
            HandleInput();
            
            // Check trigger area
            CheckTriggerArea();
        }

        private void HandleInput()
        {
            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();
            
            // Toggle bounding boxes with L
            if (keyboard.IsKeyDown(Keys.L) && !previousKeyboard.IsKeyDown(Keys.L))
            {
                if (boundingBoxRenderer != null)
                {
                    boundingBoxRenderer.ShowBoundingBoxes = !boundingBoxRenderer.ShowBoundingBoxes;
                    Console.WriteLine($"Bounding boxes: {(boundingBoxRenderer.ShowBoundingBoxes ? "ON" : "OFF")}");
                }
            }
            
            previousKeyboard = keyboard;
            previousMouse = mouse;
        }

        private void CheckTriggerArea()
        {
            if (characterActive && character.HasValue)
            {
                var characterPos = character.Value.Body.Pose.Position.ToVector3();
                
                // Check if character is inside trigger area (AABB test)
                var min = triggerAreaCenter - triggerAreaSize / 2;
                var max = triggerAreaCenter + triggerAreaSize / 2;
                
                bool wasInside = isInsideTriggerArea;
                isInsideTriggerArea = characterPos.X >= min.X && characterPos.X <= max.X &&
                                    characterPos.Y >= min.Y && characterPos.Y <= max.Y &&
                                    characterPos.Z >= min.Z && characterPos.Z <= max.Z;
                
                // Trigger enter/exit events
                if (isInsideTriggerArea && !wasInside)
                {
                    Console.WriteLine("Entered trigger area!");
                    statusText = "INSIDE TRIGGER AREA";
                }
                else if (!isInsideTriggerArea && wasInside)
                {
                    Console.WriteLine("Exited trigger area!");
                    statusText = "";
                }
            }
        }

        public void UpdateWithCamera(GameTime gameTime, Camera camera)
        {
            // Update the scene normally first
            Update(gameTime);

            // Perform raycasting from mouse position
            PerformMouseRaycast(camera);

            // Update character with camera
            if (characterActive && character.HasValue)
            {
                character.Value.UpdateCharacterGoals(Keyboard.GetState(), camera, (float)gameTime.ElapsedGameTime.TotalSeconds);
            }
        }

        private void PerformMouseRaycast(Camera camera)
        {
            // Get mouse position
            var mouse = Mouse.GetState();
            var viewport = Globals.screenManager.GraphicsDevice.Viewport;
            
            // Convert mouse position to ray
            Vector3 nearPoint = viewport.Unproject(
                new Vector3(mouse.X, mouse.Y, 0),
                camera.Projection,
                camera.View,
                Microsoft.Xna.Framework.Matrix.Identity);
                
            Vector3 farPoint = viewport.Unproject(
                new Vector3(mouse.X, mouse.Y, 1),
                camera.Projection,
                camera.View,
                Microsoft.Xna.Framework.Matrix.Identity);
            
            Vector3 rayDirection = Vector3.Normalize(farPoint - nearPoint);
            
            // Reset all highlights
            foreach (var cube in interactiveCubes)
            {
                cube.IsHighlighted = false;
                cube.Entity.Color = new Vector3(0.5f, 0.7f, 1.0f); // Default color
            }
            hoveredCube = null;
            
            // Perform raycast against all cubes
            float closestDistance = float.MaxValue;
            CubeData closestCube = null;
            
            foreach (var cube in interactiveCubes)
            {
                // Simple AABB ray intersection test
                var cubePos = cube.Entity.Position;
                var cubeSize = cube.Entity.Scale * 20f; // Cube model is 20 units
                
                var min = cubePos - cubeSize / 2;
                var max = cubePos + cubeSize / 2;
                
                // Ray-AABB intersection
                float? distance = RayIntersectsAABB(nearPoint, rayDirection, min, max);
                
                if (distance.HasValue && distance.Value < closestDistance)
                {
                    closestDistance = distance.Value;
                    closestCube = cube;
                }
            }
            
            // Highlight the closest cube
            if (closestCube != null)
            {
                closestCube.IsHighlighted = true;
                closestCube.Entity.Color = new Vector3(1.0f, 1.0f, 0.2f); // Yellow highlight
                hoveredCube = closestCube;
            }
        }

        private float? RayIntersectsAABB(Vector3 rayOrigin, Vector3 rayDirection, Vector3 boxMin, Vector3 boxMax)
        {
            float tMin = 0.0f;
            float tMax = float.MaxValue;

            for (int i = 0; i < 3; i++)
            {
                float origin = i == 0 ? rayOrigin.X : (i == 1 ? rayOrigin.Y : rayOrigin.Z);
                float direction = i == 0 ? rayDirection.X : (i == 1 ? rayDirection.Y : rayDirection.Z);
                float min = i == 0 ? boxMin.X : (i == 1 ? boxMin.Y : boxMin.Z);
                float max = i == 0 ? boxMax.X : (i == 1 ? boxMax.Y : boxMax.Z);

                if (Math.Abs(direction) < 0.00001f)
                {
                    if (origin < min || origin > max)
                        return null;
                }
                else
                {
                    float t1 = (min - origin) / direction;
                    float t2 = (max - origin) / direction;

                    if (t1 > t2)
                    {
                        float temp = t1;
                        t1 = t2;
                        t2 = temp;
                    }

                    tMin = Math.Max(tMin, t1);
                    tMax = Math.Min(tMax, t2);

                    if (tMin > tMax)
                        return null;
                }
            }

            return tMin;
        }

        public override void Draw(GameTime gameTime, Camera camera)
        {
            // Draw all entities
            base.Draw(gameTime, camera);
        }
        
        public void DrawUI(GameTime gameTime, Camera camera, SpriteBatch spriteBatch)
        {
            // Draw UI text at full resolution
            DrawUIText(gameTime, camera, spriteBatch);
        }

        private void DrawUIText(GameTime gameTime, Camera camera, SpriteBatch spriteBatch)
        {
            var font = Globals.fontNTR;
            
            // Draw instructions in top-left corner
            string instructions = "Interactive Test Scene - Raycasting & Triggers\n\nWASD = Move, Mouse = Look around\nHover mouse over cubes to see info\nWalk into green area to trigger\nL = Toggle bounding boxes";
            spriteBatch.DrawString(font, instructions, new Vector2(20, 20) + Vector2.One, Color.Black);
            spriteBatch.DrawString(font, instructions, new Vector2(20, 20), Color.White);
            
            // Draw hovered cube info
            if (hoveredCube != null)
            {
                var worldPos = hoveredCube.Entity.Position + new Vector3(0, 5, 0);
                var screenPos = Project3DToScreen(worldPos, camera);
                
                if (screenPos.HasValue)
                {
                    string info = $">>> {hoveredCube.Name} <<<\n{hoveredCube.Description}";
                    var textSize = font.MeasureString(info);
                    var textPos = screenPos.Value - textSize / 2;
                    
                    // Keep text on screen
                    var screenViewport = Globals.screenManager.GraphicsDevice.Viewport;
                    textPos.X = Microsoft.Xna.Framework.MathHelper.Clamp(textPos.X, 10, screenViewport.Width - textSize.X - 10);
                    textPos.Y = Microsoft.Xna.Framework.MathHelper.Clamp(textPos.Y, 10, screenViewport.Height - textSize.Y - 10);
                    
                    // Draw text with shadow for better visibility
                    spriteBatch.DrawString(font, info, textPos + Vector2.One * 2, Color.Black);
                    spriteBatch.DrawString(font, info, textPos, Color.Yellow);
                }
            }
            
            // Draw trigger area status
            if (isInsideTriggerArea)
            {
                var screenCenter = new Vector2(
                    Globals.screenManager.GraphicsDevice.Viewport.Width / 2,
                    120);
                
                var message = "*** INSIDE TRIGGER AREA ***";
                var textSize = font.MeasureString(message);
                var textPos = screenCenter - textSize / 2;
                
                // Draw with shadow
                spriteBatch.DrawString(font, message, textPos + Vector2.One * 3, Color.Black);
                spriteBatch.DrawString(font, message, textPos, Color.LimeGreen);
            }
            
            // Draw cube count info in bottom right
            var cubeInfo = $"Interactive Cubes: {interactiveCubes.Count}";
            var cubeTextSize = font.MeasureString(cubeInfo);
            var viewport = Globals.screenManager.GraphicsDevice.Viewport;
            var bottomRightPos = new Vector2(viewport.Width - cubeTextSize.X - 20, viewport.Height - cubeTextSize.Y - 20);
            
            spriteBatch.DrawString(font, cubeInfo, bottomRightPos + Vector2.One, Color.Black);
            spriteBatch.DrawString(font, cubeInfo, bottomRightPos, Color.Gray);
        }

        private Vector2? Project3DToScreen(Vector3 worldPosition, Camera camera)
        {
            var viewport = Globals.screenManager.GraphicsDevice.Viewport;
            var screenPos = viewport.Project(worldPosition, camera.Projection, camera.View, Microsoft.Xna.Framework.Matrix.Identity);
            
            // Check if point is in front of camera
            if (screenPos.Z < 0 || screenPos.Z > 1)
                return null;
                
            return new Vector2(screenPos.X, screenPos.Y);
        }
    }
}