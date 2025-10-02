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
        private ChunkedPlanetRenderer chunkedPlanet;
        private WaterSphereRenderer waterSphere;
        private Effect planetShader;
        private bool useVertexColoring = false;
        private bool showWater = true;
        private bool useChunkedRenderer = true;
        private bool showWireframe = false;

        // Performance monitoring
        private int frameCount = 0;
        private double elapsedTime = 0;
        private float currentFPS = 0;
        private float minFPS = float.MaxValue;
        private float maxFPS = 0;

        // UI
        private List<Slider> sliders;
        private Texture2D pixelTexture;
        private bool showUI = true;
        private Rectangle regenerateButton;
        private bool regenerateButtonHovered = false;
        private Rectangle saveButton;
        private bool saveButtonHovered = false;

        // Shader parameters
        private float planetNormalMapStrength = 0.5f;
        private float planetDayNightTransition = 0.5f;
        private float planetSpecularIntensity = 0.1f;
        private float planetRotationSpeed = 0.0f;
        private float noiseScale = 10.0f;
        private float noiseStrength = 0.05f;
        private float waterUVScale = 1.0f;
        private float waterWaveFrequency = 1.0f;
        private float waterWaveAmplitude = 1.0f;
        private float waterNormalStrength = 1.0f;
        private float waterDistortion = 1.0f;
        private float waterScrollSpeed = 1.0f;

        // Rotation state
        private float planetRotationAngle = 0.0f;

        public AdvancedProceduralPlanetScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;

            // Create editor camera positioned to look at planet
            camera = new EditorCamera(gd, new Vector3(0, 15, 60));

            // Create procedural planet generator (old system) - 512 for fast startup
            planetGenerator = new ProceduralPlanetGenerator(gd, radius: 50f, heightmapResolution: 512);

            // Set planet context for height-based speed scaling
            camera.SetPlanetContext(planetGenerator, 50f);

            // Create chunked planet renderer (new LOD system)
            chunkedPlanet = new ChunkedPlanetRenderer(gd, planetGenerator, radius: 50f);

            // Create water sphere at ocean level (matches base planet radius at height 0)
            waterSphere = new WaterSphereRenderer(gd, 50, 3);

            // Load custom planet shader
            try
            {
                planetShader = Globals.screenManager.Content.Load<Effect>("shaders/surface/planet_modified");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to load Planet shader: {ex.Message} " + ex.StackTrace );
                planetShader = null;
            }

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

            // Regenerate button
            int buttonWidth = 150;
            int buttonHeight = 35;
            regenerateButton = new Rectangle(x + (sliderWidth - buttonWidth) / 2, startY, buttonWidth, buttonHeight);
            startY += buttonHeight + 10;

            // Save heightmap button
            saveButton = new Rectangle(x + (sliderWidth - buttonWidth) / 2, startY, buttonWidth, buttonHeight);
            startY += buttonHeight + 20;

            // Continent Frequency (needs regeneration)
            var continentSlider = new Slider(
                new Rectangle(x, startY, sliderWidth, sliderHeight),
                0.1f, 3.0f, planetGenerator.Parameters.ContinentFrequency,
                "Continent Freq", font);
            continentSlider.NeedsRegeneration = true;
            continentSlider.ValueChanged += value =>
            {
                planetGenerator.Parameters.ContinentFrequency = value;
            };
            sliders.Add(continentSlider);

            // Mountain Frequency (needs regeneration)
            var mountainSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing, sliderWidth, sliderHeight),
                0.5f, 8.0f, planetGenerator.Parameters.MountainFrequency,
                "Mountain Freq", font);
            mountainSlider.NeedsRegeneration = true;
            mountainSlider.ValueChanged += value =>
            {
                planetGenerator.Parameters.MountainFrequency = value;
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
                waterSphere.UpdateWaterLevel(value);
            };
            sliders.Add(oceanSlider);

            // Mountain Height (just changes scale, no regen) - 10x range
            var mountainHeightSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 3, sliderWidth, sliderHeight),
                0.1f, 10.0f, planetGenerator.Parameters.MountainHeight,
                "Mountain Height", font);
            mountainHeightSlider.ValueChanged += value =>
            {
                planetGenerator.Parameters.MountainHeight = value;
            };
            sliders.Add(mountainHeightSlider);

            // Planet Normal Map Strength
            var normalMapStrengthSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 4, sliderWidth, sliderHeight),
                -10.0f, 10.0f, 0.5f,
                "Normal Map Str", font);
            normalMapStrengthSlider.ValueChanged += value =>
            {
                planetNormalMapStrength = value;
            };
            sliders.Add(normalMapStrengthSlider);

            // Planet Day/Night Transition
            var dayNightSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 5, sliderWidth, sliderHeight),
                0.0f, 1.0f, 0.5f,
                "Day/Night Trans", font);
            dayNightSlider.ValueChanged += value =>
            {
                planetDayNightTransition = value;
            };
            sliders.Add(dayNightSlider);

            // Planet Specular Intensity
            var specularSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 6, sliderWidth, sliderHeight),
                0.0f, 1.0f, 0.1f,
                "Specular Intensity", font);
            specularSlider.ValueChanged += value =>
            {
                planetSpecularIntensity = value;
            };
            sliders.Add(specularSlider);

            // Planet Rotation Speed
            var rotationSpeedSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 7, sliderWidth, sliderHeight),
                -2.0f, 2.0f, 0.0f,
                "Rotation Speed", font);
            rotationSpeedSlider.ValueChanged += value =>
            {
                planetRotationSpeed = value;
            };
            sliders.Add(rotationSpeedSlider);

            // Noise Scale
            var noiseScaleSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 8, sliderWidth, sliderHeight),
                0.1f, 50.0f, 10.0f,
                "Noise Scale", font);
            noiseScaleSlider.ValueChanged += value =>
            {
                noiseScale = value;
            };
            sliders.Add(noiseScaleSlider);

            // Noise Strength
            var noiseStrengthSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 9, sliderWidth, sliderHeight),
                0.0f, 0.5f, 0.05f,
                "Noise Strength", font);
            noiseStrengthSlider.ValueChanged += value =>
            {
                noiseStrength = value;
            };
            sliders.Add(noiseStrengthSlider);

            // Water UV Scale
            var waterUVScaleSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 10, sliderWidth, sliderHeight),
                0.1f, 5.0f, 1.0f,
                "Water UV Scale", font);
            waterUVScaleSlider.ValueChanged += value =>
            {
                waterUVScale = value;
            };
            sliders.Add(waterUVScaleSlider);

            // Water Wave Frequency
            var waterFreqSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 11, sliderWidth, sliderHeight),
                0.1f, 5.0f, 1.0f,
                "Wave Frequency", font);
            waterFreqSlider.ValueChanged += value =>
            {
                waterWaveFrequency = value;
            };
            sliders.Add(waterFreqSlider);

            // Water Wave Amplitude
            var waterAmpSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 12, sliderWidth, sliderHeight),
                0.1f, 5.0f, 1.0f,
                "Wave Amplitude", font);
            waterAmpSlider.ValueChanged += value =>
            {
                waterWaveAmplitude = value;
            };
            sliders.Add(waterAmpSlider);

            // Water Normal Strength
            var waterNormalSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 13, sliderWidth, sliderHeight),
                0.0f, 3.0f, 1.0f,
                "Wave Normal Str", font);
            waterNormalSlider.ValueChanged += value =>
            {
                waterNormalStrength = value;
            };
            sliders.Add(waterNormalSlider);

            // Water Distortion
            var waterDistortionSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 14, sliderWidth, sliderHeight),
                0.0f, 3.0f, 1.0f,
                "Wave Distortion", font);
            waterDistortionSlider.ValueChanged += value =>
            {
                waterDistortion = value;
            };
            sliders.Add(waterDistortionSlider);

            // Water Scroll Speed
            var waterScrollSpeedSlider = new Slider(
                new Rectangle(x, startY + sliderSpacing * 15, sliderWidth, sliderHeight),
                0.0f, 5.0f, 1.0f,
                "Wave Scroll Speed", font);
            waterScrollSpeedSlider.ValueChanged += value =>
            {
                waterScrollSpeed = value;
            };
            sliders.Add(waterScrollSpeedSlider);
        }

        private void RegeneratePlanet()
        {
            planetGenerator.GeneratePlanet();

            // Clear chunk cache to force regeneration
            if (chunkedPlanet != null)
            {
                chunkedPlanet.ClearCache();
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Update FPS counter
            frameCount++;
            elapsedTime += gameTime.ElapsedGameTime.TotalSeconds;
            if (elapsedTime >= 1.0)
            {
                currentFPS = (float)(frameCount / elapsedTime);
                minFPS = Math.Min(minFPS, currentFPS);
                maxFPS = Math.Max(maxFPS, currentFPS);
                frameCount = 0;
                elapsedTime = 0;
            }

            // Update planet rotation
            planetRotationAngle += planetRotationSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update chunked planet LOD
            if (useChunkedRenderer && chunkedPlanet != null)
            {
                BoundingFrustum frustum = new BoundingFrustum(camera.View * camera.Projection);
                chunkedPlanet.UpdateChunks(camera.Position, frustum);
            }

            // Apply height collision for camera
            ApplyCameraHeightCollision();
        }

        private void ApplyCameraHeightCollision()
        {
            float planetRadius = 50f;
            float minHeightAboveTerrain = 0.5f; // Minimum distance above terrain

            // Get normalized position on sphere
            Vector3 direction = Vector3.Normalize(camera.Position);

            // Convert to spherical UV coordinates
            float u = MathF.Atan2(direction.X, direction.Z) / (2.0f * MathF.PI) + 0.5f;
            float v = MathF.Asin(MathHelper.Clamp(direction.Y, -1.0f, 1.0f)) / MathF.PI + 0.5f;

            // Sample height from heightmap (0-1 range)
            float heightSample = planetGenerator.SampleHeightAtUV(u, v);

            // Calculate terrain height at this position
            // Height sample is normalized (0-1), multiply by max terrain displacement
            float maxTerrainHeight = planetRadius * 0.3f; // Adjust based on your terrain settings
            float terrainHeight = planetRadius + (heightSample - 0.5f) * maxTerrainHeight;

            float currentDistance = camera.Position.Length();
            float minDistanceFromCenter = terrainHeight + minHeightAboveTerrain;

            if (currentDistance < minDistanceFromCenter)
            {
                // Push camera away to maintain minimum distance above terrain
                camera.Position = direction * minDistanceFromCenter;
            }
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

                // Handle UI buttons
                var mouseState = Mouse.GetState();
                var mousePos = new Point(mouseState.X, mouseState.Y);

                regenerateButtonHovered = regenerateButton.Contains(mousePos);
                saveButtonHovered = saveButton.Contains(mousePos);

                if (regenerateButtonHovered && InputManager.GetMouseClick(0))
                {
                    RegeneratePlanet();
                }

                if (saveButtonHovered && InputManager.GetMouseClick(0))
                {
                    planetGenerator.SaveHeightmapToDisk();
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

            // O key to toggle water sphere (O for Ocean)
            if (InputManager.GetKeyboardClick(Keys.O))
            {
                showWater = !showWater;
            }

            // C key to toggle between chunked and old renderer
            if (InputManager.GetKeyboardClick(Keys.C))
            {
                useChunkedRenderer = !useChunkedRenderer;
            }

            // W key to toggle wireframe
            if (InputManager.GetKeyboardClick(Keys.X))
            {
                showWireframe = !showWireframe;
            }
        }

        public override void Draw2D(GameTime gameTime)
        {
            if (!showUI) return;

            // Draw performance stats
            int activeChunkCount = useChunkedRenderer && chunkedPlanet != null ? chunkedPlanet.GetActiveChunkCount() : 0;
            string perfStats = $"FPS: {currentFPS:F1} (Min: {minFPS:F1}, Max: {maxFPS:F1})\n" +
                              $"Chunks: {activeChunkCount}\n" +
                              $"Renderer: {(useChunkedRenderer ? "Chunked LOD" : "Static Mesh")}\n" +
                              $"Wireframe: {(showWireframe ? "ON" : "OFF")}";

            Vector2 perfPosition = new Vector2(20, 20);
            getSpriteBatch.DrawString(Globals.fontNTR, perfStats, perfPosition + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, perfStats, perfPosition, Color.Lime);

            // Draw instructions
            string instructions = "Advanced Procedural Planet\n\n" +
                                "Hold Right Mouse + Move = Rotate camera\n" +
                                "WASD/QE = Move camera\n" +
                                "Mouse Wheel = Speed\n" +
                                "R = Random seed\n" +
                                "Tab = Toggle UI\n" +
                                "V = Toggle vertex/shader mode\n" +
                                "C = Toggle chunked LOD\n" +
                                "X = Toggle wireframe\n" +
                                "O = Toggle water sphere (Ocean)\n" +
                                "S = Save heightmap to disk\n" +
                                "ESC = Menu\n\n" +
                                "Features: Dynamic LOD chunks\n" +
                                "Backface culling\n" +
                                "Distance-based subdivision";

            Vector2 position = new Vector2(20, 120);
            getSpriteBatch.DrawString(Globals.fontNTR, instructions, position + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, instructions, position, Color.White);

            // Draw regenerate button
            Color regenButtonColor = regenerateButtonHovered ? Color.Orange : Color.DarkOrange;
            getSpriteBatch.Draw(pixelTexture, regenerateButton, regenButtonColor);

            string regenButtonText = "REGENERATE";
            Vector2 regenButtonTextSize = Globals.fontNTR.MeasureString(regenButtonText);
            Vector2 regenButtonTextPos = new Vector2(
                regenerateButton.X + (regenerateButton.Width - regenButtonTextSize.X) / 2,
                regenerateButton.Y + (regenerateButton.Height - regenButtonTextSize.Y) / 2
            );
            getSpriteBatch.DrawString(Globals.fontNTR, regenButtonText, regenButtonTextPos + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, regenButtonText, regenButtonTextPos, Color.White);

            // Draw save button
            Color saveButtonColor = saveButtonHovered ? Color.LightGreen : Color.DarkGreen;
            getSpriteBatch.Draw(pixelTexture, saveButton, saveButtonColor);

            string saveButtonText = "SAVE";
            Vector2 saveButtonTextSize = Globals.fontNTR.MeasureString(saveButtonText);
            Vector2 saveButtonTextPos = new Vector2(
                saveButton.X + (saveButton.Width - saveButtonTextSize.X) / 2,
                saveButton.Y + (saveButton.Height - saveButtonTextSize.Y) / 2
            );
            getSpriteBatch.DrawString(Globals.fontNTR, saveButtonText, saveButtonTextPos + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, saveButtonText, saveButtonTextPos, Color.White);

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

            // Draw renderer info
            string rendererText = $"Renderer: {(useChunkedRenderer ? $"Chunked LOD ({chunkedPlanet?.GetActiveChunkCount() ?? 0} chunks)" : "Static Mesh")}";
            Vector2 rendererPos = new Vector2(20, Globals.screenManager.Window.ClientBounds.Height - 90);
            getSpriteBatch.DrawString(Globals.fontNTR, rendererText, rendererPos + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, rendererText, rendererPos, Color.Cyan);

            // Draw current rendering mode
            string modeText = $"Mode: {(useVertexColoring ? "Vertex Colors" : "Shader")} | Wireframe: {(showWireframe ? "ON" : "OFF")}";
            Vector2 modePos = new Vector2(20, Globals.screenManager.Window.ClientBounds.Height - 60);
            getSpriteBatch.DrawString(Globals.fontNTR, modeText, modePos + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, modeText, modePos, Color.Orange);

            // Draw heightmap resolution info
            string resolutionText = $"Heightmap Resolution: {planetGenerator.HeightmapResolution}x{planetGenerator.HeightmapResolution}";
            Vector2 resolutionPos = new Vector2(20, Globals.screenManager.Window.ClientBounds.Height - 30);
            getSpriteBatch.DrawString(Globals.fontNTR, resolutionText, resolutionPos + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, resolutionText, resolutionPos, Color.Cyan);

            // Draw camera height above ground (bottom center)
            float heightAboveGround = camera.GetHeightAboveGround(planetGenerator, 50f);
            string heightText = $"Height: {heightAboveGround:F2}m";
            Vector2 heightTextSize = Globals.fontNTR.MeasureString(heightText);
            Vector2 heightPos = new Vector2(
                (Globals.screenManager.Window.ClientBounds.Width - heightTextSize.X) / 2,
                Globals.screenManager.Window.ClientBounds.Height - 40
            );
            getSpriteBatch.DrawString(Globals.fontNTR, heightText, heightPos + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, heightText, heightPos, Color.Yellow);
        }

        public override void Draw3D(GameTime gameTime)
        {
            var gd = Globals.screenManager.GraphicsDevice;
            gd.Clear(new Color(5, 5, 15));

            // Set render states
            gd.RasterizerState = RasterizerState.CullCounterClockwise;
            gd.DepthStencilState = DepthStencilState.Default;
            gd.BlendState = BlendState.Opaque;

            // Draw planet with rotation around Y axis
            Matrix world = Matrix.CreateRotationY(planetRotationAngle);

            if (planetShader != null)
            {
                // Set common shader parameters
                planetShader.Parameters["NormalMapStrength"]?.SetValue(planetNormalMapStrength);
                planetShader.Parameters["DayNightTransition"]?.SetValue(planetDayNightTransition);
                planetShader.Parameters["SpecularIntensity"]?.SetValue(planetSpecularIntensity);
                planetShader.Parameters["UseVertexColoring"]?.SetValue(useVertexColoring);
                planetShader.Parameters["NoiseScale"]?.SetValue(noiseScale);
                planetShader.Parameters["NoiseStrength"]?.SetValue(noiseStrength);

                // Always use chunked LOD renderer
                chunkedPlanet.Draw(gd, world, camera.View, camera.Projection, planetShader, showWireframe);
            }

            // Draw water sphere if enabled
            if (showWater)
            {
                waterSphere.Draw(gd, world, camera.View, camera.Projection, gameTime, planetGenerator.Parameters,
                    waterUVScale, waterWaveFrequency, waterWaveAmplitude, waterNormalStrength, waterDistortion, waterScrollSpeed);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                planetGenerator?.Dispose();
                chunkedPlanet?.Dispose();
                waterSphere?.Dispose();
                planetShader?.Dispose();
                pixelTexture?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}