using anakinsoft.system.cameras;
using anakinsoft.utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.system;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Screen for the corridor scene with FPS camera and multi-material rendering
    /// </summary>
    public class CorridorScreen : PhysicsScreen
    {
        FPSCamera fpsCamera;
        public Camera GetCamera { get { return fpsCamera; } }

        CorridorScene corridorScene;

        // Camera offset configuration
        public Vector3 CameraOffset = new Vector3(0, 17.5f, 0); // Y offset to mount camera above character center
        public Vector3 CameraLookOffset = new Vector3(0, -3, 0); // Additional offset for look direction

        public CorridorScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            fpsCamera = new FPSCamera(gd, new Vector3(0, 10, 100));

            corridorScene = new CorridorScene();
            SetScene(corridorScene); // Register scene with physics screen for automatic disposal

            // Create camera and set its rotation from the character's initial rotation
            //fpsCamera.Position = new Vector3(0, 10, 100); // Start at back of corridor
            fpsCamera.SetRotation(corridorScene.GetCharacterInitialRotation());

            // Hide mouse cursor for immersive FPS experience
            Globals.screenManager.IsMouseVisible = false;
        }

        public override void Update(GameTime gameTime)
        {
            // Update scene with camera for character movement
            corridorScene.UpdateWithCamera(gameTime, fpsCamera);

            // Mount FPS camera to character controller
            UpdateCameraMountedToCharacter();

            base.Update(gameTime);
        }

        private void UpdateCameraMountedToCharacter()
        {
            var character = corridorScene.GetCharacter();
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
            if (InputManager.GetKeyboardClick(Keys.F1) && SceneManager.IsSceneMenuEnabled())
            {
                Globals.screenManager.AddScreen(new SceneSelectionMenu());
            }

            // Handle L key to toggle bounding box visualization
            if (InputManager.GetKeyboardClick(Keys.L))
            {
                if (corridorScene.BoundingBoxRenderer != null)
                {
                    System.Console.WriteLine("CorridorScreen: L key pressed - toggling bounding boxes");
                    corridorScene.BoundingBoxRenderer.ToggleBoundingBoxes();
                    
                    if (corridorScene.Physics?.Simulation?.BroadPhase != null)
                    {
                        var activeCount = corridorScene.Physics.Simulation.BroadPhase.ActiveTree.LeafCount;
                        var staticCount = corridorScene.Physics.Simulation.BroadPhase.StaticTree.LeafCount;
                        System.Console.WriteLine($"CorridorScreen: Physics bodies - Active: {activeCount}, Static: {staticCount}");
                    }
                }
                else
                {
                    System.Console.WriteLine("CorridorScreen: No BoundingBoxRenderer available");
                }
            }
        }

        public override void Draw2D(GameTime gameTime)
        {
            // Draw interaction UI for doors and objects
            var spriteBatch = Globals.screenManager.getSpriteBatch;
            corridorScene.DrawUI(gameTime, fpsCamera, spriteBatch);

            // Minimal UI for immersive corridor experience
            // Only show essential controls briefly or on key press

            // Optionally, you could add a minimal crosshair or status display here
        }

        public override void Draw3D(GameTime gameTime)
        {
            // Draw the corridor scene with multi-material rendering
            corridorScene.Draw(gameTime, fpsCamera);
        }

        public override Color? GetBackgroundColor()
        {
            // Return the corridor scene's background color
            return corridorScene.BackgroundColor;
        }

        public override void ExitScreen()
        {
            // Restore mouse visibility when exiting corridor screen
            //Globals.screenManager.IsMouseVisible = true;
            
            // PhysicsScreen base class will automatically dispose physics resources
            base.ExitScreen();
        }

        public override void KillScreen()
        {
            // Restore mouse visibility when killing corridor screen
            //Globals.screenManager.IsMouseVisible = true;
            
            // PhysicsScreen base class will automatically dispose physics resources
            base.KillScreen();
        }
    }
}