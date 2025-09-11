using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.game.scenes;
using anakinsoft.system.cameras;
using System;

namespace rubens_psx_engine.game.scenes
{
    /// <summary>
    /// Screen for testing wireframe cube rendering
    /// </summary>
    public class WireframeCubeTestScreen : Screen
    {
        private Camera camera;
        private WireframeCubeTestScene testScene;
        private bool showDebugInfo = true;

        public WireframeCubeTestScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            
            // Create camera positioned to see the cubes
            camera = new FPSCamera(gd, new Vector3(50, 30, 50));
            
            // Create the test scene
            testScene = new WireframeCubeTestScene();
            testScene.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            // Update the scene
            testScene.Update(gameTime);
            
            base.Update(gameTime);
        }

        public override void UpdateInput(GameTime gameTime)
        {
            if (!Globals.screenManager.IsActive)
                return;

            // Update camera
            camera.Update(gameTime);

            // Handle escape key
            if (InputManager.GetKeyboardClick(Keys.Escape))
            {
                Globals.screenManager.AddScreen(new PauseMenu());
            }

            // Toggle debug info with D key
            if (InputManager.GetKeyboardClick(Keys.D))
            {
                showDebugInfo = !showDebugInfo;
                Console.WriteLine($"Debug info: {(showDebugInfo ? "ON" : "OFF")}");
            }
            
            // Test simple line drawing with L key
            if (InputManager.GetKeyboardClick(Keys.L))
            {
                Console.WriteLine("L key pressed - Testing line rendering");
            }
        }

        public override void Draw3D(GameTime gameTime)
        {
            // Draw the test scene with wireframe cubes
            testScene.Draw(gameTime, camera);
            
            // Additional test: Draw a single line directly here
            if (showDebugInfo)
            {
                DrawTestLine();
            }
        }

        private void DrawTestLine()
        {
            var graphicsDevice = Globals.screenManager.GraphicsDevice;
            
            // Create a simple effect for the test line
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = camera.View;
            basicEffect.Projection = camera.Projection;
            basicEffect.World = Matrix.Identity;
            
            // Define a simple line from origin upward
            var startPoint = new Vector3(0, 0, 0);
            var endPoint = new Vector3(0, 50, 0);
            
            // Create vertices for the line
            var vertices = new[] 
            { 
                new VertexPositionColor(startPoint, Color.Yellow),
                new VertexPositionColor(endPoint, Color.Yellow) 
            };
            
            try
            {
                // Apply the effect and draw the line
                basicEffect.CurrentTechnique.Passes[0].Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, 1);
                
                Console.WriteLine("DrawTestLine: Successfully drew test line");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DrawTestLine: Error - {ex.Message}");
            }
            
            basicEffect.Dispose();
        }

        public override void Draw2D(GameTime gameTime)
        {
            // Draw UI text
            string message = "Wireframe Cube Test Scene\n\n";
            message += "You should see several colored wireframe cubes\n";
            message += "White cube at center, colored cubes around it\n\n";
            message += "Controls:\n";
            message += "WASD = Move camera\n";
            message += "Mouse = Look around\n";
            message += "D = Toggle debug info\n";
            message += "L = Test line rendering\n";
            message += "ESC = Menu\n\n";
            message += $"Debug Info: {(showDebugInfo ? "ON" : "OFF")}\n";
            message += $"Camera Pos: {camera.Position:F1}";
            
            Vector2 position = new Vector2(20, 20);
            
            getSpriteBatch.DrawString(Globals.fontNTR, message, position + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, message, position, Color.White);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                testScene?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}