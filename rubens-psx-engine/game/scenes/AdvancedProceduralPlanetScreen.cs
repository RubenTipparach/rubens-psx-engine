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
using System.IO;

namespace anakinsoft.game.scenes
{
    public class AdvancedProceduralPlanetScreen : PhysicsScreen
    {
        private EditorCamera camera;
        public Camera GetCamera => camera;

        public override bool? OverridePostProcessing()
        {
            return false; // Disable post-processing for planet scenes
        }

        private ProceduralPlanetGenerator planetGenerator;
        private WaterSphereRenderer waterSphere;
        private BasicEffect heightmapEffect;
        private BasicEffect vertexColorEffect;
        private Effect planetShader;
        private bool useVertexColoring = false;
        private bool showWater = true;

        // UI
        private List<Slider> sliders;
        private Texture2D pixelTexture;
        private bool showUI = true;

        public AdvancedProceduralPlanetScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;

            // Create editor camera positioned to look at planet
            camera = new EditorCamera(gd, new Vector3(0, 15, 60));

            // Create procedural planet generator
            planetGenerator = new ProceduralPlanetGenerator(gd, radius: 20f, heightmapResolution: 1024);

            // Create water sphere at ocean level
            waterSphere = new WaterSphereRenderer(gd, 20f * 1.01f, 3); // Slightly larger than terrain

            // Create effects for different rendering modes
            vertexColorEffect = new BasicEffect(gd);
            vertexColorEffect.VertexColorEnabled = true;
            vertexColorEffect.TextureEnabled = false;
            vertexColorEffect.LightingEnabled = false;

            // Load custom planet shader
            planetShader = Globals.screenManager.Content.Load<Effect>("shaders/surface/Unlit");

     
            // Create UI
            CreateUI(gd);

            // Generate initial planet
            planetGenerator.GeneratePlanet();
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
                0.1f, 3.0f, planetGenerator.Parameters.ContinentFrequency,
                "Continent Freq", font);
            continentSlider.ValueChanged += value =>
            {
                planetGenerator.Parameters.ContinentFrequency = value;
                RegeneratePlanet();
            };
            sliders.Add(continentSlider);

            // Mountain Frequency
            var mountainSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing, sliderWidth, sliderHeight),
                0.5f, 8.0f, planetGenerator.Parameters.MountainFrequency,
                "Mountain Freq", font);
            mountainSlider.ValueChanged += value =>
            {
                planetGenerator.Parameters.MountainFrequency = value;
                RegeneratePlanet();
            };
            sliders.Add(mountainSlider);

            // Ocean Level (only affects water sphere, not terrain)
            var oceanSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 2, sliderWidth, sliderHeight),
                0.1f, 0.8f, planetGenerator.Parameters.OceanLevel,
                "Ocean Level", font);
            oceanSlider.ValueChanged += value =>
            {
                planetGenerator.Parameters.OceanLevel = value;
                // Only update water sphere, not terrain
                waterSphere.UpdateWaterLevel(value);
            };
            sliders.Add(oceanSlider);

            // Mountain Height
            var mountainHeightSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 3, sliderWidth, sliderHeight),
                0.1f, 1.0f, planetGenerator.Parameters.MountainHeight,
                "Mountain Height", font);
            mountainHeightSlider.ValueChanged += value =>
            {
                planetGenerator.Parameters.MountainHeight = value;
                RegeneratePlanet();
            };
            sliders.Add(mountainHeightSlider);
        }

        private void RegeneratePlanet()
        {
            planetGenerator.GeneratePlanet();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void UpdateInput(GameTime gameTime)
        {
            if (!Globals.screenManager.IsActive)
                return;

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

            // V key to toggle vertex coloring mode
            if (InputManager.GetKeyboardClick(Keys.V))
            {
                useVertexColoring = !useVertexColoring;
            }

            // R key to randomize seed and regenerate
            if (InputManager.GetKeyboardClick(Keys.R))
            {
                planetGenerator.Parameters.Seed = new Random().Next();
                RegeneratePlanet();
            }

            // Tab to toggle UI
            if (InputManager.GetKeyboardClick(Keys.Tab))
            {
                showUI = !showUI;
            }

            // S key to save heightmap to disk
            if (InputManager.GetKeyboardClick(Keys.S))
            {
                planetGenerator.SaveHeightmapToDisk();
            }

            // O key to toggle water sphere (O for Ocean)
            if (InputManager.GetKeyboardClick(Keys.O))
            {
                showWater = !showWater;
            }
        }

        public override void Draw2D(GameTime gameTime)
        {
            if (!showUI) return;

            // Draw instructions
            string instructions = "Advanced Procedural Planet\\n\\n" +
                                "Hold Right Mouse + Move = Rotate camera\\n" +
                                "WASD/QE = Move camera\\n" +
                                "Mouse Wheel = Speed\\n" +
                                "R = Random seed\\n" +
                                "Tab = Toggle UI\\n" +
                                "V = Toggle vertex/shader mode\\n" +
                                "O = Toggle water sphere (Ocean)\\n" +
                                "S = Save heightmap to disk\\n" +
                                "ESC = Menu\\n\\n" +
                                "Features: High-LOD terrain mesh\\n" +
                                "Custom terrain shader with gradients\\n" +
                                "Separate animated water sphere\\n" +
                                "Pure terrain heightmaps (no water data)";

            Vector2 position = new Vector2(20, 20);
            getSpriteBatch.DrawString(Globals.fontNTR, instructions, position + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, instructions, position, Color.White);

            // Draw sliders
            foreach (var slider in sliders)
            {
                slider.Draw(getSpriteBatch, pixelTexture);
            }

            // Draw current seed
            string seedText = $"Seed: {planetGenerator.Parameters.Seed}";
            Vector2 seedPos = new Vector2(Globals.screenManager.Window.ClientBounds.Width - 250,
                                         Globals.screenManager.Window.ClientBounds.Height - 30);
            getSpriteBatch.DrawString(Globals.fontNTR, seedText, seedPos + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, seedText, seedPos, Color.Yellow);

            // Draw current rendering mode
            string modeText = $"Rendering Mode: {(useVertexColoring ? "Vertex Colors (Height Map)" : (planetShader != null ? "Custom Planet Shader" : "Basic Heightmap Texture"))}";
            Vector2 modePos = new Vector2(20, Globals.screenManager.Window.ClientBounds.Height - 60);
            getSpriteBatch.DrawString(Globals.fontNTR, modeText, modePos + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, modeText, modePos, Color.Orange);

            // Draw heightmap resolution info
            string resolutionText = $"Heightmap Resolution: {planetGenerator.HeightmapResolution}x{planetGenerator.HeightmapResolution}";
            Vector2 resolutionPos = new Vector2(20, Globals.screenManager.Window.ClientBounds.Height - 30);
            getSpriteBatch.DrawString(Globals.fontNTR, resolutionText, resolutionPos + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, resolutionText, resolutionPos, Color.Cyan);
        }

        public override void Draw3D(GameTime gameTime)
        {
            var gd = Globals.screenManager.GraphicsDevice;
            gd.Clear(new Color(5, 5, 15));

            // Set render states
            gd.RasterizerState = RasterizerState.CullCounterClockwise;
            gd.DepthStencilState = DepthStencilState.Default;
            gd.BlendState = BlendState.Opaque;

            // Draw planet (no rotation)
            Matrix world = Matrix.Identity;

            if (useVertexColoring)
            {
                // Use vertex color effect for height visualization
                vertexColorEffect.World = world;
                vertexColorEffect.View = camera.View;
                vertexColorEffect.Projection = camera.Projection;
                planetGenerator.Draw(gd, world, camera.View, camera.Projection, vertexColorEffect, true);
            }
            else
            {
                // Use custom planet shader with heightmap
                planetShader.Parameters["World"].SetValue(world);
                planetShader.Parameters["View"].SetValue(camera.View);
                planetShader.Parameters["Projection"].SetValue(camera.Projection);
                planetShader.Parameters["WorldInverseTranspose"].SetValue(Matrix.Transpose(Matrix.Invert(world)));
                planetShader.Parameters["CameraPosition"].SetValue(camera.Position);
                planetShader.Parameters["HeightmapTexture"].SetValue(planetGenerator.HeightmapTexture);
                planetShader.Parameters["NormalMapTexture"].SetValue(planetGenerator.NormalMapTexture);

                planetGenerator.Draw(gd, world, camera.View, camera.Projection, planetShader, false);
            }

            // Draw water sphere if enabled
            if (showWater)
            {
                waterSphere.Draw(gd, world, camera.View, camera.Projection, gameTime, planetGenerator.Parameters);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                planetGenerator?.Dispose();
                waterSphere?.Dispose();
                heightmapEffect?.Dispose();
                vertexColorEffect?.Dispose();
                planetShader?.Dispose();
                pixelTexture?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}