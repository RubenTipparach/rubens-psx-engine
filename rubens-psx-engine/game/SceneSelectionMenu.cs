using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine.system.config;
using System.Collections.Generic;

namespace rubens_psx_engine
{
    public class SceneSelectionMenu : rubens_psx_engine.system.MenuScreen
    {
        private List<Button> sceneButtons;
        private Button backButton;

        public SceneSelectionMenu()
        {
            sceneButtons = new List<Button>();

            // Create buttons for each scene
            var thirdPersonButton = new Button("Third Person Sandbox", (sender, args) => LoadScene("thirdPersonSandbox"));
            thirdPersonButton.SetPosition(new Vector2(100, 180));
            sceneButtons.Add(thirdPersonButton);

            var fpsButton = new Button("FPS Sandbox", (sender, args) => LoadScene("fpsSandbox"));
            fpsButton.SetPosition(new Vector2(100, 240));
            sceneButtons.Add(fpsButton);

            var basicButton = new Button("Basic Scene", (sender, args) => LoadScene("basic"));
            basicButton.SetPosition(new Vector2(100, 300));
            sceneButtons.Add(basicButton);

            var cameraTestButton = new Button("Camera Test Scene", (sender, args) => LoadScene("cameraTest"));
            cameraTestButton.SetPosition(new Vector2(100, 360));
            sceneButtons.Add(cameraTestButton);

            var thirdPersonHallwayButton = new Button("Third Person Hallway", (sender, args) => LoadScene("thirdPersonHallway"));
            thirdPersonHallwayButton.SetPosition(new Vector2(100, 420));
            sceneButtons.Add(thirdPersonHallwayButton);

            var graphicsTestButton = new Button("Graphics Test Scene", (sender, args) => LoadScene("graphicsTest"));
            graphicsTestButton.SetPosition(new Vector2(100, 480));
            sceneButtons.Add(graphicsTestButton);

            var corridorButton = new Button("Corridor Multi-Material Scene", (sender, args) => LoadScene("corridor"));
            corridorButton.SetPosition(new Vector2(100, 540));
            sceneButtons.Add(corridorButton);

            // Back button
            backButton = new Button("Back", (sender, args) => ExitScreen());
            backButton.SetPosition(new Vector2(100, 600));
        }

        private void LoadScene(string sceneType)
        {
            // Clear all screens first
            Globals.screenManager.ExitAllScreens();

            // Load the appropriate scene using SceneManager
            Screen newScene = rubens_psx_engine.system.SceneManager.CreateScene(sceneType);

            Globals.screenManager.AddScreen(newScene);
        }

        public override void UpdateInput(GameTime gameTime)
        {
            if (!Globals.screenManager.IsActive)
                return;

            // Handle escape key
            if (InputManager.GetKeyboardClick(Keys.Escape))
            {
                ExitScreen();
                return;
            }

            // Update buttons
            foreach (var button in sceneButtons)
            {
                button.Update(gameTime);
            }
            backButton.Update(gameTime);

            // Handle number key shortcuts
            if (InputManager.GetKeyboardClick(Keys.D1))
            {
                LoadScene("thirdPersonSandbox");
            }
            else if (InputManager.GetKeyboardClick(Keys.D2))
            {
                LoadScene("fpsSandbox");
            }
            else if (InputManager.GetKeyboardClick(Keys.D3))
            {
                LoadScene("basic");
            }
            else if (InputManager.GetKeyboardClick(Keys.D4))
            {
                LoadScene("cameraTest");
            }
            else if (InputManager.GetKeyboardClick(Keys.D5))
            {
                LoadScene("thirdPersonHallway");
            }
            else if (InputManager.GetKeyboardClick(Keys.D6))
            {
                LoadScene("graphicsTest");
            }
            else if (InputManager.GetKeyboardClick(Keys.D7))
            {
                LoadScene("corridor");
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Draw2D(GameTime gameTime)
        {
            // Draw title
            string title = "Scene Selection";
            Vector2 titleSize = Globals.fontNTR.MeasureString(title);
            Vector2 titlePosition = new Vector2(
                Globals.screenManager.Window.ClientBounds.Width / 2 - titleSize.X / 2,
                50
            );

            // Draw title with outline
            getSpriteBatch.DrawString(Globals.fontNTR, title, titlePosition + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, title, titlePosition, Color.White);

            // Draw instructions
            string instructions = "Press 1-7 for scenes or use mouse to click buttons\n1=Third Person  2=FPS  3=Basic  4=Camera Test  5=Third Person Hallway  6=Graphics Test  7=Corridor\nESC to go back";
            Vector2 instructionsSize = Globals.fontNTR.MeasureString(instructions);
            Vector2 instructionsPosition = new Vector2(
                Globals.screenManager.Window.ClientBounds.Width / 2 - instructionsSize.X / 2,
                120
            );

            getSpriteBatch.DrawString(Globals.fontNTR, instructions, instructionsPosition + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, instructions, instructionsPosition, Color.Gray);

            // Draw buttons
            foreach (var button in sceneButtons)
            {
                button.Draw2D(gameTime);
            }
            backButton.Draw2D(gameTime);

            // Draw current config info
            var config = RenderingConfigManager.Config.Scene;
            string configInfo = $"Current default scene: {config.DefaultScene}";
            Vector2 configPosition = new Vector2(20, Globals.screenManager.Window.ClientBounds.Height - 60);
            
            getSpriteBatch.DrawString(Globals.fontNTR, configInfo, configPosition + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, configInfo, configPosition, Color.Yellow);
        }
    }
}