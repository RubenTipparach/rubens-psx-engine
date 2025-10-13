using anakinsoft.system.cameras;
using anakinsoft.utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.system;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Screen for The Lounge scene with FPS camera
    /// </summary>
    public class TheLoungeScreen : PhysicsScreen
    {
        FPSCamera fpsCamera;
        public Camera GetCamera { get { return fpsCamera; } }

        TheLoungeScene loungeScene;

        // Camera offset configuration
        public Vector3 CameraOffset = new Vector3(0, 17.5f, 0); // Y offset to mount camera above character center
        public Vector3 CameraLookOffset = new Vector3(0, -3, 0); // Additional offset for look direction

        public TheLoungeScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            fpsCamera = new FPSCamera(gd, new Vector3(0, 10, 0));

            loungeScene = new TheLoungeScene();
            SetScene(loungeScene); // Register scene with physics screen for automatic disposal

            // Create camera and set its rotation from the character's initial rotation
            fpsCamera.SetRotation(loungeScene.GetCharacterInitialRotation());

            // Hide mouse cursor for immersive FPS experience
            Globals.screenManager.IsMouseVisible = false;
        }

        public override void Update(GameTime gameTime)
        {
            // Update scene with camera for character movement
            loungeScene.UpdateWithCamera(gameTime, fpsCamera);

            // Mount FPS camera to character controller
            UpdateCameraMountedToCharacter();

            base.Update(gameTime);
        }

        private void UpdateCameraMountedToCharacter()
        {
            var character = loungeScene.GetCharacter();
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
                if (loungeScene.BoundingBoxRenderer != null)
                {
                    System.Console.WriteLine("TheLoungeScreen: L key pressed - toggling bounding boxes");
                    loungeScene.BoundingBoxRenderer.ToggleBoundingBoxes();

                    if (loungeScene.Physics?.Simulation?.BroadPhase != null)
                    {
                        var activeCount = loungeScene.Physics.Simulation.BroadPhase.ActiveTree.LeafCount;
                        var staticCount = loungeScene.Physics.Simulation.BroadPhase.StaticTree.LeafCount;
                        System.Console.WriteLine($"TheLoungeScreen: Physics bodies - Active: {activeCount}, Static: {staticCount}");
                    }
                }
                else
                {
                    System.Console.WriteLine("TheLoungeScreen: No BoundingBoxRenderer available");
                }
            }
        }

        public override void Draw2D(GameTime gameTime)
        {
            // Draw UI
            var spriteBatch = Globals.screenManager.getSpriteBatch;
            loungeScene.DrawUI(gameTime, fpsCamera, spriteBatch);
        }

        public override void Draw3D(GameTime gameTime)
        {
            // Draw the lounge scene
            loungeScene.Draw(gameTime, fpsCamera);
        }

        public override Color? GetBackgroundColor()
        {
            // Return the lounge scene's background color
            return loungeScene.BackgroundColor;
        }

        public override void ExitScreen()
        {
            // PhysicsScreen base class will automatically dispose physics resources
            base.ExitScreen();
        }

        public override void KillScreen()
        {
            // PhysicsScreen base class will automatically dispose physics resources
            base.KillScreen();
        }
    }
}
