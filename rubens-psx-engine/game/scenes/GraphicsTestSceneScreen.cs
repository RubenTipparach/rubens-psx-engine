using anakinsoft.system.cameras;
using anakinsoft.game.scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Screen that displays the GraphicsTestScene
    /// </summary>
    public class GraphicsTestSceneScreen : Screen
    {
        Camera camera;
        public Camera GetCamera { get { return camera; } }

        GraphicsTestScene graphicsTestScene;

        public GraphicsTestSceneScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            
            // Create camera - using BasicCamera now
            camera = new anakinsoft.system.cameras.FPSCamera(gd, new Vector3(0, 5, 80));

            // Create and initialize the graphics test scene
            graphicsTestScene = new GraphicsTestScene();
        }

        public override void Update(GameTime gameTime)
        {
            // Update the scene
            graphicsTestScene.Update(gameTime);
            
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

            // Handle F1 key to switch to scene selection
            if (InputManager.GetKeyboardClick(Keys.F1))
            {
                Globals.screenManager.AddScreen(new SceneSelectionMenu());
            }

            // Handle L key to toggle bounding box visualization (mimics BepuPhysics demo)
            if (InputManager.GetKeyboardClick(Keys.L))
            {
                if (graphicsTestScene.BoundingBoxRenderer != null)
                {
                    System.Console.WriteLine("GraphicsTestSceneScreen: L key pressed - toggling bounding boxes");
                    graphicsTestScene.BoundingBoxRenderer.ToggleBoundingBoxes();
                    
                    if (graphicsTestScene.Physics?.Simulation?.BroadPhase != null)
                    {
                        var activeCount = graphicsTestScene.Physics.Simulation.BroadPhase.ActiveTree.LeafCount;
                        var staticCount = graphicsTestScene.Physics.Simulation.BroadPhase.StaticTree.LeafCount;
                        System.Console.WriteLine($"GraphicsTestSceneScreen: Physics bodies - Active: {activeCount}, Static: {staticCount}");
                    }
                }
                else
                {
                    System.Console.WriteLine("GraphicsTestSceneScreen: No BoundingBoxRenderer available");
                }
            }
        }

        public override void Draw2D(GameTime gameTime)
        {
            // Draw graphics test scene UI
            string bbStatus = graphicsTestScene.BoundingBoxRenderer?.ShowBoundingBoxes == true ? "ON" : "OFF";
            string message = $"Graphics Test Scene\n\nPS1-style shader demonstration\nESC = menu\nF1 = scene selection\nL = bounding boxes ({bbStatus})";
            Vector2 position = new Vector2(20, 20);
            
            getSpriteBatch.DrawString(Globals.fontNTR, message, position + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, message, position, Color.White);
        }

        public override void Draw3D(GameTime gameTime)
        {
            // Draw the scene
            graphicsTestScene.Draw(gameTime, camera);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose the scene to clean up physics resources
                graphicsTestScene?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}