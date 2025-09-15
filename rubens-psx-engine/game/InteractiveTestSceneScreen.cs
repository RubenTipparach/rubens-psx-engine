using anakinsoft.game.scenes;
using anakinsoft.system.cameras;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine.system;

namespace rubens_psx_engine.game.scenes
{
    /// <summary>
    /// Screen wrapper for InteractiveTestScene
    /// </summary>
    public class InteractiveTestSceneScreen : PhysicsScreen
    {
        Camera camera;
        public Camera GetCamera { get { return camera; } }
        
        new InteractiveTestScene scene;

        public InteractiveTestSceneScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            camera = new FPSCamera(gd, new Vector3(0, 5, 40));
            
            scene = new InteractiveTestScene();
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
            
            // Handle escape for menu
            if (InputManager.GetKeyboardClick(Keys.Escape))
            {
                Globals.screenManager.AddScreen(new PauseMenu());
            }
            
            // Handle F1 for scene selection (only if enabled in config)
            if (InputManager.GetKeyboardClick(Keys.F1) && rubens_psx_engine.system.SceneManager.IsSceneMenuEnabled())
            {
                Globals.screenManager.AddScreen(new SceneSelectionMenu());
            }
        }

        public override void Draw2D(GameTime gameTime)
        {
            // Draw UI text at full resolution
            scene.DrawUI(gameTime, camera, getSpriteBatch);
        }

        public override void Draw3D(GameTime gameTime)
        {
            scene.Draw(gameTime, camera);
        }
    }
}