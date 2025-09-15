using anakinsoft.system.cameras;
using anakinsoft.system.physics;
using anakinsoft.system.character;
using BepuPhysics;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine.system.controllers;
using System;
using System.Collections.Generic;
using Vector3N = System.Numerics.Vector3;
using rubens_psx_engine;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Test scene to compare FPS and Third Person camera behaviors
    /// Shows both controllers side by side with debug information
    /// </summary>
    public class CameraTestScene : Screen
    {
        Camera camera;
        public Camera GetCamera { get { return camera; } }

        // Physics system (shared between controllers)
        PhysicsSystem physicsSystem;
        CharacterControllers characterControllers;

        // Controller instances
        IPlayerController currentController;
        FPSController fpsController;
        ThirdPersonController thirdPersonController;

        // Test environment
        List<Entity> testGeometry;
        
        // UI and debugging
        private bool showDebugInfo = true;
        private string currentControllerType = "FPS";

        public CameraTestScene()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            
            // Create camera
            camera = new FPSCamera(gd, new Vector3(0, 2, 0));
            camera.Position = new Vector3(0, 2, 0);

            // Initialize physics system
            characterControllers = null;
            physicsSystem = new PhysicsSystem(ref characterControllers);

            // Create both controllers
            fpsController = new FPSController(physicsSystem, characterControllers);
            thirdPersonController = new ThirdPersonController();
            
            // Start with FPS controller
            currentController = fpsController;

            // Create test environment
            CreateTestEnvironment();
        }

        private void CreateTestEnvironment()
        {
            testGeometry = new List<Entity>();

            // Available textures for variety
            string[] textures = {
                "textures/prototype/brick",
                "textures/prototype/concrete", 
                "textures/prototype/prototype_512x512_blue1",
                "textures/prototype/prototype_512x512_green1",
                "textures/prototype/prototype_512x512_orange",
            };

            Random random = new Random(42);

            // Create a simple test room with reference geometry
            // Floor
            for (int x = -10; x <= 10; x += 4)
            {
                for (int z = -10; z <= 10; z += 4)
                {
                    var floor = new Entity("models/waterfall.xnb", textures[random.Next(textures.Length)], true);
                    floor.SetPosition(new Vector3(x, 0, z));
                    floor.SetScale(1.0f);
                    testGeometry.Add(floor);

                    // Add to physics
                    CreateStaticBlock(new Vector3N(x, 0, z), 4, 0.5f, 4);
                }
            }

            // Create some reference cubes at specific positions for camera testing
            var positions = new Vector3[]
            {
                new Vector3(0, 2, 5),   // In front
                new Vector3(5, 2, 0),   // To the right
                new Vector3(-5, 2, 0),  // To the left
                new Vector3(0, 2, -5),  // Behind
                new Vector3(0, 6, 0),   // Above
            };

            foreach (var pos in positions)
            {
                var cube = new Entity("models/waterfall.xnb", textures[random.Next(textures.Length)], true);
                cube.SetPosition(pos);
                cube.SetScale(1.0f);
                testGeometry.Add(cube);

                // Add to physics
                CreateStaticBlock(new Vector3N(pos.X, pos.Y, pos.Z), 2, 2, 2);
            }
        }

        private void CreateStaticBlock(Vector3N position, float width, float height, float depth)
        {
            var boxShape = new Box(width, height, depth);
            
            var staticDescription = BodyDescription.CreateKinematic(
                pose: new RigidPose(position),
                collidable: new CollidableDescription(physicsSystem.Simulation.Shapes.Add(boxShape), 0.1f),
                activity: new BodyActivityDescription(0.01f));
                
            physicsSystem.Simulation.Bodies.Add(staticDescription);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update physics
            physicsSystem.Update(dt);

            // Update current controller
            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();
            currentController.Update(gameTime, keyboard, mouse);

            // Update camera based on current controller
            currentController.UpdateCamera(camera);
            
            // Update camera matrices
            camera.Update(gameTime);

            base.Update(gameTime);
        }

        public override void UpdateInput(GameTime gameTime)
        {
            if (!Globals.screenManager.IsActive)
                return;

            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();

            // Toggle between controllers
            if (InputManager.GetKeyboardClick(Keys.Tab))
            {
                SwitchController();
            }

            // Toggle debug info
            if (InputManager.GetKeyboardClick(Keys.F2))
            {
                showDebugInfo = !showDebugInfo;
            }

            // Mouse lock for FPS controller
            if (InputManager.GetMouseClick(0))
            {
                currentController.SetMouseLocked(!currentController.IsMouseLocked());
            }

            if (InputManager.GetKeyboardClick(Keys.Escape))
            {
                Globals.screenManager.AddScreen(new PauseMenu());
            }

            // Add F1 key to switch to scene selection (only if enabled in config)
            if (InputManager.GetKeyboardClick(Keys.F1) && rubens_psx_engine.system.SceneManager.IsSceneMenuEnabled())
            {
                Globals.screenManager.AddScreen(new SceneSelectionMenu());
            }
        }

        private void SwitchController()
        {
            if (currentController == fpsController)
            {
                currentController = thirdPersonController;
                currentControllerType = "Third Person";
            }
            else
            {
                currentController = fpsController;
                currentControllerType = "FPS";
            }
        }

        public override void Draw2D(GameTime gameTime)
        {
            if (!showDebugInfo) return;

            // Debug information
            string debugInfo = $"Camera Test Scene - {currentControllerType} Controller\n\n";
            debugInfo += "Controls:\n";
            debugInfo += "TAB = Switch Controller\n";
            debugInfo += "F2 = Toggle Debug Info\n";
            debugInfo += "WASD = Move\n";
            
            if (currentController.IsMouseLocked())
            {
                debugInfo += "Mouse = Look (Click to unlock)\n";
            }
            else
            {
                debugInfo += "Click to lock mouse\n";
            }
            
            debugInfo += "ESC = Menu, F1 = Scene Selection\n\n";
            
            // Camera debug info
            debugInfo += $"Camera Position: {camera.Position:F2}\n";
            debugInfo += $"Camera Target: {camera.Target:F2}\n";
            debugInfo += $"Camera Forward: {camera.Forward:F2}\n";
            debugInfo += $"Camera Right: {camera.Right:F2}\n";
            debugInfo += $"Camera Up: {camera.Up:F2}\n\n";
            
            // Player position
            debugInfo += $"Player Position: {currentController.GetPosition():F2}\n";
            
            // Matrix debug info
            var view = camera.View;
            var proj = camera.Projection;
            debugInfo += $"View Matrix M11: {view.M11:F3}, M33: {view.M33:F3}\n";
            debugInfo += $"Projection Matrix M11: {proj.M11:F3}, M33: {proj.M33:F3}\n";

            Vector2 position = new Vector2(20, 20);
            
            // Draw debug text with background for readability
            getSpriteBatch.DrawString(Globals.fontNTR, debugInfo, position + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, debugInfo, position, Color.White);

            // Draw crosshair if mouse is locked
            if (currentController.IsMouseLocked())
            {
                var center = new Vector2(
                    Globals.screenManager.Window.ClientBounds.Width / 2,
                    Globals.screenManager.Window.ClientBounds.Height / 2
                );
                
                var crosshairSize = 10;
                var crosshairRect = new Rectangle((int)center.X - 1, (int)center.Y - crosshairSize, 2, crosshairSize * 2);
                getSpriteBatch.Draw(Globals.white, crosshairRect, Color.White);
                crosshairRect = new Rectangle((int)center.X - crosshairSize, (int)center.Y - 1, crosshairSize * 2, 2);
                getSpriteBatch.Draw(Globals.white, crosshairRect, Color.White);
            }
        }

        public override void Draw3D(GameTime gameTime)
        {
            // Draw all test geometry
            foreach (var entity in testGeometry)
            {
                entity.Draw3D(gameTime, camera);
            }
        }

        public override void ExitScreen()
        {
            // Clean up controllers
            fpsController?.Dispose();
            thirdPersonController?.Dispose();
            
            if (physicsSystem != null)
            {
                physicsSystem.ThreadDispatcher?.Dispose();
            }
            
            base.ExitScreen();
        }
    }
}