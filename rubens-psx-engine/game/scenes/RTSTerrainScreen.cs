using ProceduralTerrain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.system;

namespace rubens_psx_engine.game.scenes
{
    /// <summary>
    /// Screen that displays the RTS Terrain Demo
    /// </summary>
    public class RTSTerrainScreen : Screen
    {
        private RTSCamera camera;
        private RTSTerrainScene rtsScene;

        public RTSTerrainScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            camera = new RTSCamera(Globals.screenManager.getGraphicsDevice);
            rtsScene = new RTSTerrainScene(camera);
        }

        public override void Update(GameTime gameTime)
        {
            if (InputManager.GetKeyboardClick(Keys.Escape))
            {
                ExitScreen();
                return;
            }

            camera.Update(gameTime);
            rtsScene.Update(gameTime);
        }

        public override void Draw2D(GameTime gameTime)
        {
            rtsScene.Draw(gameTime);

            // Draw UI elements (including minimap)
            rtsScene.DrawUI(gameTime);

            string instructions = "RTS Camera Demo\n" +
                                "WASD/Arrows: Pan camera horizontally\n" +
                                "Q/E: Zoom in/out\n" +
                                "Mouse Wheel: Zoom\n" +
                                "Middle Mouse: Pan\n" +
                                "Left Click: Get terrain height\n" +
                                "Space: Generate new terrain\n" +
                                "C: Center camera\n" +
                                "Escape: Exit\n\n" +
                                "Camera is fixed at 45 angle\n" +
                                "Check minimap (top-right)";

            getSpriteBatch.DrawString(Globals.fontNTR, instructions, new Vector2(10, 10), Color.White);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                rtsScene?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}