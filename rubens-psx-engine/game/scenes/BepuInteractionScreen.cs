using anakinsoft.game.scenes;
using anakinsoft.system.cameras;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine.system;
using rubens_psx_engine;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Screen wrapper for BepuInteractionScene - BEPU Physics raycasting demonstration
    /// </summary>
    public class BepuInteractionScreen : PhysicsScreen
    {
        Camera camera;
        public Camera GetCamera { get { return camera; } }

        new BepuInteractionScene scene;

        public BepuInteractionScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            camera = new FPSCamera(gd, new Vector3(0, 5, 30));

            scene = new BepuInteractionScene();
            SetScene(scene);
        }

        public override void Update(GameTime gameTime)
        {
            scene.UpdateWithCamera(gameTime, camera);
            base.Update(gameTime);
        }

        public override void UpdateInput(GameTime gameTime)
        {
            if (!Globals.screenManager.IsActive)
                return;

            camera.Update(gameTime);

            if (InputManager.GetKeyboardClick(Keys.Escape))
            {
                Globals.screenManager.AddScreen(new PauseMenu());
            }

            // Add F1 key to switch to scene selection (only if enabled in config)
            if (InputManager.GetKeyboardClick(Keys.F1) && SceneManager.IsSceneMenuEnabled())
            {
                Globals.screenManager.AddScreen(new SceneSelectionMenu());
            }
        }

        public override void Draw2D(GameTime gameTime)
        {
            scene.DrawUI(gameTime, camera, getSpriteBatch);
        }

        public override void Draw3D(GameTime gameTime)
        {
            scene.Draw(gameTime, camera);
        }

        public override Color? GetBackgroundColor()
        {
            return scene.BackgroundColor;
        }
    }
}