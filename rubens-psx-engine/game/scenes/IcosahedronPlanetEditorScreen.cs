using anakinsoft.system.cameras;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.system;
using rubens_psx_engine.system.procedural;
using rubens_psx_engine.system.ui;
using System;
using System.Collections.Generic;

namespace anakinsoft.game.scenes
{
    public class IcosahedronPlanetEditorScreen : PhysicsScreen
    {
        private EditorCamera camera;
        public Camera GetCamera => camera;

        public override bool? OverridePostProcessing()
        {
            return false; // Disable post-processing for planet scenes
        }

        private AdvancedPlanetRenderer planet;
        private BasicEffect fallbackEffect;
        private BasicEffect vertexColorEffect;
        private PlanetGenerationParams planetParams;

        // UI
        private List<Slider> sliders;
        private Texture2D pixelTexture;
        private bool showUI = true;

        public IcosahedronPlanetEditorScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;

            // Create editor camera positioned to look at planet
            camera = new EditorCamera(gd, new Vector3(0, 15, 60));

            // Initialize planet parameters
            planetParams = new PlanetGenerationParams();

            // Create icosahedron-based planet with lower subdivision for better performance
            planet = new AdvancedPlanetRenderer(gd, radius: 20f, subdivisionLevel: 4);

            // Create fallback effect for textured rendering
            fallbackEffect = new BasicEffect(gd);
            fallbackEffect.VertexColorEnabled = false;
            fallbackEffect.TextureEnabled = true;
            fallbackEffect.LightingEnabled = true;
            fallbackEffect.EnableDefaultLighting();
            fallbackEffect.PreferPerPixelLighting = true;

            // Create vertex color effect for height visualization
            vertexColorEffect = new BasicEffect(gd);
            vertexColorEffect.VertexColorEnabled = true;
            vertexColorEffect.TextureEnabled = false;
            vertexColorEffect.LightingEnabled = false;

            // Create UI
            CreateUI(gd);
        }

        private void CreateUI(GraphicsDevice gd)
        {
            // Create pixel texture for UI
            pixelTexture = new Texture2D(gd, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            sliders = new List<Slider>();
            var font = Globals.fontNTR;

            int startY = 50;
            int sliderHeight = 20;
            int sliderSpacing = 50;
            int sliderWidth = 200;
            int x = Globals.screenManager.Window.ClientBounds.Width - sliderWidth - 20;

            // Continent Frequency
            var continentSlider = new Slider(
                new Rectangle(x, startY, sliderWidth, sliderHeight),
                0.1f, 1.5f, planetParams.ContinentFrequency,
                "Continent Freq", font);
            continentSlider.ValueChanged += value =>
            {
                planetParams.ContinentFrequency = value;
                RegeneratePlanet();
            };
            sliders.Add(continentSlider);

            // Mountain Frequency
            var mountainSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing, sliderWidth, sliderHeight),
                0.5f, 5.0f, planetParams.MountainFrequency,
                "Mountain Freq", font);
            mountainSlider.ValueChanged += value =>
            {
                planetParams.MountainFrequency = value;
                RegeneratePlanet();
            };
            sliders.Add(mountainSlider);

            // Detail Frequency
            var detailSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 2, sliderWidth, sliderHeight),
                1.0f, 10.0f, planetParams.DetailFrequency,
                "Detail Freq", font);
            detailSlider.ValueChanged += value =>
            {
                planetParams.DetailFrequency = value;
                RegeneratePlanet();
            };
            sliders.Add(detailSlider);

            // Ocean Level
            var oceanSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 3, sliderWidth, sliderHeight),
                0.1f, 0.8f, planetParams.OceanLevel,
                "Ocean Level", font);
            oceanSlider.ValueChanged += value =>
            {
                planetParams.OceanLevel = value;
                RegeneratePlanet();
            };
            sliders.Add(oceanSlider);

            // Mountain Height
            var mountainHeightSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 4, sliderWidth, sliderHeight),
                0.1f, 1.0f, planetParams.MountainHeight,
                "Mountain Height", font);
            mountainHeightSlider.ValueChanged += value =>
            {
                planetParams.MountainHeight = value;
                RegeneratePlanet();
            };
            sliders.Add(mountainHeightSlider);

            // Continent Height
            var continentHeightSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 5, sliderWidth, sliderHeight),
                0.05f, 0.3f, planetParams.ContinentHeight,
                "Continent Height", font);
            continentHeightSlider.ValueChanged += value =>
            {
                planetParams.ContinentHeight = value;
                RegeneratePlanet();
            };
            sliders.Add(continentHeightSlider);

            // Polar Cutoff
            var polarSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 6, sliderWidth, sliderHeight),
                0.5f, 0.9f, planetParams.PolarCutoff,
                "Polar Ice Cutoff", font);
            polarSlider.ValueChanged += value =>
            {
                planetParams.PolarCutoff = value;
                RegeneratePlanet();
            };
            sliders.Add(polarSlider);
        }

        private void RegeneratePlanet()
        {
            planet.RegenerateWithParams(planetParams);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void UpdateInput(GameTime gameTime)
        {
            if (!Globals.screenManager.IsActive)
                return;

            var keyboardState = Keyboard.GetState();

            // Update camera
            camera.Update(gameTime);

            // Update UI
            if (showUI)
            {
                foreach (var slider in sliders)
                {
                    slider.Update(gameTime);
                }
            }

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

            // R key to randomize seed and regenerate
            if (InputManager.GetKeyboardClick(Keys.R))
            {
                planetParams.Seed = new Random().Next();
                RegeneratePlanet();
            }

            // Tab to toggle UI
            if (InputManager.GetKeyboardClick(Keys.Tab))
            {
                showUI = !showUI;
            }

            // V key to toggle vertex coloring mode
            if (InputManager.GetKeyboardClick(Keys.V))
            {
                planet.UseVertexColoring = !planet.UseVertexColoring;
            }

            // Number keys for presets
            if (InputManager.GetKeyboardClick(Keys.D1))
            {
                LoadPreset("Earth-like");
            }
            else if (InputManager.GetKeyboardClick(Keys.D2))
            {
                LoadPreset("Desert");
            }
            else if (InputManager.GetKeyboardClick(Keys.D3))
            {
                LoadPreset("Ocean");
            }
            else if (InputManager.GetKeyboardClick(Keys.D4))
            {
                LoadPreset("Mountainous");
            }
        }

        private void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case "Earth-like":
                    planetParams.ContinentFrequency = 0.5f;
                    planetParams.MountainFrequency = 2.0f;
                    planetParams.DetailFrequency = 4.0f;
                    planetParams.OceanLevel = 0.4f;
                    planetParams.MountainHeight = 0.5f;
                    planetParams.ContinentHeight = 0.1f;
                    planetParams.PolarCutoff = 0.7f;
                    break;

                case "Desert":
                    planetParams.ContinentFrequency = 0.4f;
                    planetParams.MountainFrequency = 1.5f;
                    planetParams.DetailFrequency = 6.0f;
                    planetParams.OceanLevel = 0.2f;
                    planetParams.MountainHeight = 0.3f;
                    planetParams.ContinentHeight = 0.15f;
                    planetParams.PolarCutoff = 0.9f;
                    break;

                case "Ocean":
                    planetParams.ContinentFrequency = 0.3f;
                    planetParams.MountainFrequency = 1.0f;
                    planetParams.DetailFrequency = 3.0f;
                    planetParams.OceanLevel = 0.7f;
                    planetParams.MountainHeight = 0.8f;
                    planetParams.ContinentHeight = 0.05f;
                    planetParams.PolarCutoff = 0.6f;
                    break;

                case "Mountainous":
                    planetParams.ContinentFrequency = 0.6f;
                    planetParams.MountainFrequency = 3.0f;
                    planetParams.DetailFrequency = 8.0f;
                    planetParams.OceanLevel = 0.3f;
                    planetParams.MountainHeight = 0.9f;
                    planetParams.ContinentHeight = 0.2f;
                    planetParams.PolarCutoff = 0.8f;
                    break;
            }

            UpdateSlidersFromParams();
            RegeneratePlanet();
        }

        private void UpdateSlidersFromParams()
        {
            sliders[0].Value = planetParams.ContinentFrequency;
            sliders[1].Value = planetParams.MountainFrequency;
            sliders[2].Value = planetParams.DetailFrequency;
            sliders[3].Value = planetParams.OceanLevel;
            sliders[4].Value = planetParams.MountainHeight;
            sliders[5].Value = planetParams.ContinentHeight;
            sliders[6].Value = planetParams.PolarCutoff;
        }

        public override void Draw2D(GameTime gameTime)
        {
            if (!showUI) return;

            // Draw instructions
            string instructions = "Icosahedron Planet Editor\\n\\n" +
                                "Hold Right Mouse + Move = Rotate camera\\n" +
                                "WASD/QE = Move camera\\n" +
                                "Mouse Wheel = Speed\\n" +
                                "R = Random seed\\n" +
                                "Tab = Toggle UI\\n" +
                                "V = Toggle vertex/shader mode\\n" +
                                "1-4 = Presets\\n" +
                                "ESC = Menu";

            Vector2 position = new Vector2(20, 20);
            getSpriteBatch.DrawString(Globals.fontNTR, instructions, position + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, instructions, position, Color.White);

            // Draw sliders
            foreach (var slider in sliders)
            {
                slider.Draw(getSpriteBatch, pixelTexture);
            }

            // Draw current seed
            string seedText = $"Seed: {planetParams.Seed}";
            Vector2 seedPos = new Vector2(Globals.screenManager.Window.ClientBounds.Width - 250,
                                         Globals.screenManager.Window.ClientBounds.Height - 30);
            getSpriteBatch.DrawString(Globals.fontNTR, seedText, seedPos + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, seedText, seedPos, Color.Yellow);

            // Draw subdivision info
            string subdivisionText = $"Subdivision Level: {planet.SubdivisionLevel}";
            Vector2 subdivisionPos = new Vector2(20, Globals.screenManager.Window.ClientBounds.Height - 60);
            getSpriteBatch.DrawString(Globals.fontNTR, subdivisionText, subdivisionPos + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, subdivisionText, subdivisionPos, Color.Cyan);

            // Draw current rendering mode
            string modeText = $"Rendering Mode: {(planet.UseVertexColoring ? "Vertex Colors (Height Map)" : "Planet Shader")}";
            Vector2 modePos = new Vector2(20, Globals.screenManager.Window.ClientBounds.Height - 30);
            getSpriteBatch.DrawString(Globals.fontNTR, modeText, modePos + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, modeText, modePos, Color.Orange);
        }

        public override void Draw3D(GameTime gameTime)
        {
            var gd = Globals.screenManager.GraphicsDevice;
            gd.Clear(new Color(5, 5, 15));

            // Set render states
            gd.RasterizerState = RasterizerState.CullCounterClockwise;
            gd.DepthStencilState = DepthStencilState.Default;
            gd.BlendState = BlendState.Opaque;

            // Draw planet
            Matrix world = Matrix.CreateRotationY((float)gameTime.TotalGameTime.TotalSeconds * 0.1f);

            // Use appropriate effect based on rendering mode
            if (planet.UseVertexColoring)
            {
                // Use vertex color effect for height visualization
                vertexColorEffect.World = world;
                vertexColorEffect.View = camera.View;
                vertexColorEffect.Projection = camera.Projection;
                planet.Draw(gd, world, camera.View, camera.Projection, vertexColorEffect);
            }
            else
            {
                // Use textured effect for planet shader with heightmap
                fallbackEffect.World = world;
                fallbackEffect.View = camera.View;
                fallbackEffect.Projection = camera.Projection;
                fallbackEffect.Texture = planet.HeightmapTexture;  // Use heightmap for terrain shading
                fallbackEffect.DiffuseColor = Vector3.One;
                planet.Draw(gd, world, camera.View, camera.Projection, fallbackEffect);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                planet?.Dispose();
                fallbackEffect?.Dispose();
                vertexColorEffect?.Dispose();
                pixelTexture?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}