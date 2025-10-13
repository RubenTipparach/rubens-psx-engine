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
        private List<(string name, string id)> scenes;
        private const float ButtonStartY = 180f;
        private const float ButtonSpacing = 50f;
        private const float ButtonX = 100f;

        public SceneSelectionMenu()
        {
            sceneButtons = new List<Button>();

            // Define scenes with their display names and scene IDs
            scenes = new List<(string name, string id)>
            {
                ("Third Person Sandbox", "thirdPersonSandbox"),
                ("FPS Sandbox", "fpsSandbox"),
                ("RTS Terrain Demo", "rtsTerrainTest"),
                ("Graphics Test Scene", "graphicsTest"),
                ("Corridor Multi-Material Scene", "corridor"),
                ("The Lounge", "theLounge"),
                ("Wireframe Cube Test", "wireframeTest"),
                ("Static Mesh Demo", "staticMeshDemo"),
                ("Interactive Test Scene", "interactiveTest"),
                ("BEPU Physics Interaction", "bepuInteraction"),
                ("Procedural Planet Test", "proceduralPlanet"),
                ("Advanced Planet Editor", "advancedPlanetEditor"),
                ("Icosahedron Planet Editor", "icosahedronPlanetEditor"),
                ("Advanced Procedural Planet", "advancedProceduralPlanet")
            };

            // Create buttons dynamically based on position in list
            for (int i = 0; i < scenes.Count; i++)
            {
                var scene = scenes[i];
                var button = new Button(scene.name, (sender, args) => LoadScene(scene.id));
                button.SetPosition(new Vector2(ButtonX, ButtonStartY + (i * ButtonSpacing)));
                sceneButtons.Add(button);
            }

            // Back button positioned after all scene buttons
            backButton = new Button("Back", (sender, args) => ExitScreen());
            backButton.SetPosition(new Vector2(ButtonX, ButtonStartY + (scenes.Count * ButtonSpacing) + ButtonSpacing));
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

            // Handle number key shortcuts based on button order
            if (InputManager.GetKeyboardClick(Keys.D1) && scenes.Count > 0)
                LoadScene(scenes[0].id);
            else if (InputManager.GetKeyboardClick(Keys.D2) && scenes.Count > 1)
                LoadScene(scenes[1].id);
            else if (InputManager.GetKeyboardClick(Keys.D3) && scenes.Count > 2)
                LoadScene(scenes[2].id);
            else if (InputManager.GetKeyboardClick(Keys.D4) && scenes.Count > 3)
                LoadScene(scenes[3].id);
            else if (InputManager.GetKeyboardClick(Keys.D5) && scenes.Count > 4)
                LoadScene(scenes[4].id);
            else if (InputManager.GetKeyboardClick(Keys.D6) && scenes.Count > 5)
                LoadScene(scenes[5].id);
            else if (InputManager.GetKeyboardClick(Keys.D7) && scenes.Count > 6)
                LoadScene(scenes[6].id);
            else if (InputManager.GetKeyboardClick(Keys.D8) && scenes.Count > 7)
                LoadScene(scenes[7].id);
            else if (InputManager.GetKeyboardClick(Keys.D9) && scenes.Count > 8)
                LoadScene(scenes[8].id);
            else if (InputManager.GetKeyboardClick(Keys.D0) && scenes.Count > 9)
                LoadScene(scenes[9].id);
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
            string instructions = "Press number keys (1-9, 0) or click buttons to select a scene\nESC to go back";
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