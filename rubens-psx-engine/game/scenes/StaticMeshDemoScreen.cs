using anakinsoft.system.cameras;
using anakinsoft.utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.system;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Screen for demonstrating StaticMesh functionality
    /// Shows how to create immovable collision geometry from 3D models
    /// </summary>
    public class StaticMeshDemoScreen : PhysicsScreen
    {
        FPSCamera fpsCamera;
        public Camera GetCamera { get { return fpsCamera; } }

        StaticMeshDemoScene staticMeshScene;

        // Camera offset configuration for character following
        public Vector3 CameraOffset = new Vector3(0, 15, 0); // Y offset to mount camera above character center
        public Vector3 CameraLookOffset = new Vector3(0, -3, 0); // Additional offset for look direction

        public StaticMeshDemoScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            fpsCamera = new FPSCamera(gd, new Vector3(0, 20, 100));
            fpsCamera.Position = new Vector3(0, 20, 100); // Start elevated to see the demo area

            staticMeshScene = new StaticMeshDemoScene();
            SetScene(staticMeshScene); // Register scene with physics screen for automatic disposal
            
            // Hide mouse cursor for immersive FPS experience
            Globals.screenManager.IsMouseVisible = false;
        }

        public override void Update(GameTime gameTime)
        {
            // Update scene with camera for character movement
            staticMeshScene.UpdateWithCamera(gameTime, fpsCamera);

            // Mount FPS camera to character controller
            UpdateCameraMountedToCharacter();

            base.Update(gameTime);
        }

        private void UpdateCameraMountedToCharacter()
        {
            var character = staticMeshScene.GetCharacter();
            if (character.HasValue)
            {
                // Get character position and orientation
                var characterPos = character.Value.Body.Pose.Position.ToVector3();
                var characterOrientation = character.Value.Body.Pose.Orientation.ToQuaternion();
                
                // Apply camera offset relative to character center
                var offsetInWorldSpace = Vector3.Transform(CameraOffset, Matrix.CreateFromQuaternion(characterOrientation));
                
                // Set camera position to character center + offset
                fpsCamera.Position = characterPos + offsetInWorldSpace;
                
                // Optional: Add additional look offset for targeting
                var lookOffsetInWorldSpace = Vector3.Transform(CameraLookOffset, Matrix.CreateFromQuaternion(characterOrientation));
                // Note: FPS camera handles its own look direction via mouse input
            }
        }

        public override void UpdateInput(GameTime gameTime)
        {
            if (!Globals.screenManager.IsActive)
                return;
            
            fpsCamera.Update(gameTime);

            if (InputManager.GetKeyboardClick(Keys.Escape))
            {
                Globals.screenManager.AddScreen(new PauseMenu());
            }

            // Add F1 key to switch to scene selection (only if enabled in config)
            if (InputManager.GetKeyboardClick(Keys.F1) && rubens_psx_engine.system.SceneManager.IsSceneMenuEnabled())
            {
                Globals.screenManager.AddScreen(new SceneSelectionMenu());
            }

            // Handle L key to toggle bounding box visualization
            if (InputManager.GetKeyboardClick(Keys.L))
            {
                if (staticMeshScene.BoundingBoxRenderer != null)
                {
                    System.Console.WriteLine("StaticMeshDemoScreen: L key pressed - toggling bounding boxes");
                    staticMeshScene.BoundingBoxRenderer.ToggleBoundingBoxes();
                    
                    if (staticMeshScene.Physics?.Simulation?.BroadPhase != null)
                    {
                        var activeCount = staticMeshScene.Physics.Simulation.BroadPhase.ActiveTree.LeafCount;
                        var staticCount = staticMeshScene.Physics.Simulation.BroadPhase.StaticTree.LeafCount;
                        System.Console.WriteLine($"StaticMeshDemoScreen: Physics bodies - Active: {activeCount}, Static: {staticCount}");
                    }
                }
                else
                {
                    System.Console.WriteLine("StaticMeshDemoScreen: No BoundingBoxRenderer available");
                }
            }

            // Display help with H key
            if (InputManager.GetKeyboardClick(Keys.H))
            {
                System.Console.WriteLine("StaticMesh Demo Controls:");
                System.Console.WriteLine("WASD = Move character");
                System.Console.WriteLine("Mouse = Look around");
                System.Console.WriteLine("Left Click = Shoot bullets");
                System.Console.WriteLine("B = Spawn dynamic box");
                System.Console.WriteLine("L = Toggle bounding boxes");
                System.Console.WriteLine("H = Show this help");
                System.Console.WriteLine("ESC = Menu");
                System.Console.WriteLine("F1 = Scene selection");
                System.Console.WriteLine($"Static meshes in scene: {staticMeshScene.GetStaticMeshCount()}");
            }
        }

        public override void Draw2D(GameTime gameTime)
        {
            // Draw StaticMesh demo UI with detailed information
            string message = "StaticMesh Demo Scene\n\n";
            message += "Simple test with two static mesh cubes\n";
            message += "Walk into the red and blue cubes to test collision\n\n";
            message += "Controls:\n";
            message += "WASD = Move, Mouse = Look\n";
            message += "Left Click = Shoot, B = Spawn box\n";
            message += "L = Bounding boxes, H = Help\n";
            message += "ESC = Menu, F1 = Scene select\n\n";
            message += $"Static meshes: {staticMeshScene.GetStaticMeshCount()}\n";
            message += $"Dynamic boxes: {staticMeshScene.GetBoxes().Count}\n";
            message += $"Bullets: {staticMeshScene.GetBullets().Count}";
            
            Vector2 messageSize = Globals.fontNTR.MeasureString(message);
            
            // Position message in top-left corner
            Vector2 position = new Vector2(20, 20);
            
            // Draw text with better visibility
            getSpriteBatch.DrawString(Globals.fontNTR, message, position + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, message, position, Color.White);

            // Draw additional info in bottom-right corner
            var character = staticMeshScene.GetCharacter();
            if (character.HasValue)
            {
                var charPos = character.Value.Body.Pose.Position.ToVector3();
                string posInfo = $"Character Position:\nX: {charPos.X:F1}\nY: {charPos.Y:F1}\nZ: {charPos.Z:F1}";
                Vector2 posSize = Globals.fontNTR.MeasureString(posInfo);
                Vector2 posPosition = new Vector2(
                    Globals.screenManager.Window.ClientBounds.Width - posSize.X - 20,
                    Globals.screenManager.Window.ClientBounds.Height - posSize.Y - 20
                );
                
                getSpriteBatch.DrawString(Globals.fontNTR, posInfo, posPosition + Vector2.One, Color.Black);
                getSpriteBatch.DrawString(Globals.fontNTR, posInfo, posPosition, Color.Cyan);
            }
        }

        public override void Draw3D(GameTime gameTime)
        {
            // Draw the static mesh demo scene
            staticMeshScene.Draw(gameTime, fpsCamera);
        }

        public override Color? GetBackgroundColor()
        {
            // Return the scene's background color (default)
            return staticMeshScene.BackgroundColor;
        }

        public override void ExitScreen()
        {
            // Restore mouse visibility when exiting
            //Globals.screenManager.IsMouseVisible = true;
            
            // PhysicsScreen base class will automatically dispose physics resources
            base.ExitScreen();
        }

        public override void KillScreen()
        {
            // Restore mouse visibility when killing
            //Globals.screenManager.IsMouseVisible = true;
            
            // PhysicsScreen base class will automatically dispose physics resources
            base.KillScreen();
        }
    }
}