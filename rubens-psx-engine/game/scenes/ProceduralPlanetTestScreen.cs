using anakinsoft.system.cameras;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.system;

namespace anakinsoft.game.scenes
{
    public class ProceduralPlanetTestScreen : PhysicsScreen
    {
        private Camera camera;
        public Camera GetCamera => camera;

        public override bool? OverridePostProcessing()
        {
            return false; // Disable post-processing for planet scenes
        }

        private ImprovedProceduralPlanetTestScene planetScene;

        public ProceduralPlanetTestScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;

            // Create camera positioned to see all planets
            camera = new FPSCamera(gd, new Vector3(0, 20, 100));

            // Create and initialize the planet test scene
            planetScene = new ImprovedProceduralPlanetTestScene();
            planetScene.Initialize(gd);
            SetScene(planetScene);
        }

        public override void Update(GameTime gameTime)
        {
            planetScene.Update(gameTime);
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
            if (InputManager.GetKeyboardClick(Keys.F1) && rubens_psx_engine.system.SceneManager.IsSceneMenuEnabled())
            {
                Globals.screenManager.AddScreen(new SceneSelectionMenu());
            }

            // R key to regenerate planets (for testing)
            if (InputManager.GetKeyboardClick(Keys.R))
            {
                // Regenerate planets with new seeds
                planetScene.RegeneratePlanets(Globals.screenManager.getGraphicsDevice.GraphicsDevice);
            }
        }

        public override void Draw2D(GameTime gameTime)
        {
            string message = $"Improved Procedural Planet\n\n" +
                           $"WASD + Mouse = Move camera\n" +
                           $"R = Regenerate planets\n" +
                           $"ESC = Menu\n" +
                           $"F1 = Scene selection\n\n" +
                           $"Features:\n" +
                           $"- Realistic continent generation\n" +
                           $"- Ocean/land/mountain biomes\n" +
                           $"- Polar ice caps & deserts\n" +
                           $"- Directional lighting\n" +
                           $"- Water specularity";

            Vector2 position = new Vector2(20, 20);

            getSpriteBatch.DrawString(Globals.fontNTR, message, position + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, message, position, Color.White);
        }

        public override void Draw3D(GameTime gameTime)
        {
            // Clear with a space-like background
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            gd.Clear(new Color(5, 5, 15));

            // Draw the planet scene
            planetScene.Draw(gameTime, camera);
        }
    }
}