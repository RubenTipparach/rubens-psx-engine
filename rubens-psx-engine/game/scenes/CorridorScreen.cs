using anakinsoft.system.cameras;
using anakinsoft.utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Screen for the corridor scene with FPS camera and multi-material rendering
    /// </summary>
    public class CorridorScreen : Screen
    {
        FPSCamera fpsCamera;
        public Camera GetCamera { get { return fpsCamera; } }

        CorridorScene corridorScene;

        // Camera offset configuration
        public Vector3 CameraOffset = new Vector3(0, 15, 0); // Y offset to mount camera above character center
        public Vector3 CameraLookOffset = new Vector3(0, -3, 0); // Additional offset for look direction

        public CorridorScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            fpsCamera = new FPSCamera(gd, new Vector3(0, 10, 100));
            fpsCamera.Position = new Vector3(0, 10, 100); // Start at back of corridor

            corridorScene = new CorridorScene();
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

            // Add F1 key to switch to scene selection
            if (InputManager.GetKeyboardClick(Keys.F1))
            {
                Globals.screenManager.AddScreen(new SceneSelectionMenu());
            }
        }

        public override void Draw2D(GameTime gameTime)
        {
            // Draw corridor scene UI
            string message = "Corridor Scene - Multi-Material Demo\n\n" +
                           "WASD = move\n" +
                           "Mouse = look\n" +
                           "Left Click = shoot\n" +
                           "ESC = menu\n" +
                           "F1 = scene selection\n\n" +
                           "Features:\n" +
                           "- Multi-material corridor model\n" +
                           "- 3 different PS1-style materials\n" +
                           "- FPS character controller";
            
            Vector2 position = new Vector2(20, 20);
            
            // Draw text with better visibility
            getSpriteBatch.DrawString(Globals.fontNTR, message, position + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, message, position, Color.White);

            // Draw material info
            string materialInfo = "Material Channels:\n" +
                                "Channel 0: Unlit (0_0.jpg)\n" +
                                "Channel 1: VertexLit (0_1.jpg)\n" +
                                "Channel 2: BakedLit (0_3.jpg)";
            
            Vector2 materialPosition = new Vector2(20, Globals.screenManager.Window.ClientBounds.Height - 120);
            
            getSpriteBatch.DrawString(Globals.fontNTR, materialInfo, materialPosition + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, materialInfo, materialPosition, Color.Yellow);
        }

        public override void Draw3D(GameTime gameTime)
        {
            // Draw the corridor scene with multi-material rendering
            corridorScene.Draw(gameTime, fpsCamera);
        }
    }
}