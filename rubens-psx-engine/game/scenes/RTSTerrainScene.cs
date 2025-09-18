using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProceduralTerrain;
using rubens_psx_engine;
using rubens_psx_engine.entities;
using rubens_psx_engine.system.ui;
using rubens_psx_engine.system.lighting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace rubens_psx_engine.game.scenes
{
    public class RTSTerrainScene : IDisposable
    {
        private TerrainData terrainData;
        private TerrainRenderer terrainRenderer;
        private RTSCamera rtsCamera;
        private Minimap minimap;
        private Effect terrainEffect;
        private Texture2D grassTexture;

        // Lighting system
        private LightProcessor lightProcessor;
        private EnvironmentLight environmentLight;
        private PointLight mouseLight;

        // Example static lights
        private List<PointLight> sceneLights;

        public RTSTerrainScene(RTSCamera camera)
        {
            rtsCamera = camera;
            InitializeLighting();
            LoadContent();
        }

        private void InitializeLighting()
        {
            // Create light processor
            lightProcessor = new LightProcessor(maxPointLightsPerMaterial: 8);

            // Create environment light with tweakable values
            environmentLight = new EnvironmentLight();

            // Tweak these values to adjust the main lighting
            environmentLight.DirectionalLightDirection = Vector3.Normalize(new Vector3(-0.5f, -1.0f, -0.5f));
            environmentLight.DirectionalLightColor = new Color(1.0f, 0.95f, 0.85f); // Warm white
            environmentLight.DirectionalLightIntensity = 0.5f;

            environmentLight.AmbientLightColor = new Color(0.3f, 0.35f, 0.4f); // Cool ambient
            environmentLight.AmbientLightIntensity = 0.1f;

            // Optional fog for atmosphere
            environmentLight.FogEnabled = false;
            environmentLight.FogColor = new Color(0.6f, 0.7f, 0.8f);
            environmentLight.FogStart = 50.0f;
            environmentLight.FogEnd = 100.0f;

            lightProcessor.SetEnvironmentLight(environmentLight);

            // Create mouse light
            mouseLight = new PointLight("MouseLight")
            {
                Color = Color.Yellow,
                Range = 10.0f,
                Intensity = 3.0f,
                IsEnabled = true
            };
            lightProcessor.AddPointLight(mouseLight);
        }

        private void CreateSceneLights()
        {
            sceneLights = new List<PointLight>();

            // Add some example lights around the terrain
            float terrainWidth = terrainData.Width * terrainData.Scale;
            float terrainHeight = terrainData.Height * terrainData.Scale;

            // Corner torches
            var torch1 = PointLight.CreateTorch(new Vector3(10, 10, 10));
            lightProcessor.AddPointLight(torch1);
            sceneLights.Add(torch1);

            var torch2 = PointLight.CreateTorch(new Vector3(terrainWidth - 10, 10, 10));
            lightProcessor.AddPointLight(torch2);
            sceneLights.Add(torch2);

            var torch3 = PointLight.CreateTorch(new Vector3(10, 10, terrainHeight - 10));
            lightProcessor.AddPointLight(torch3);
            sceneLights.Add(torch3);

            var torch4 = PointLight.CreateTorch(new Vector3(terrainWidth - 10, 10, terrainHeight - 10));
            lightProcessor.AddPointLight(torch4);
            sceneLights.Add(torch4);

            // Central magic orb
            var magicOrb = PointLight.CreateMagicOrb(
                new Vector3(terrainWidth / 2, 20, terrainHeight / 2),
                Color.Cyan
            );
            lightProcessor.AddPointLight(magicOrb);
            sceneLights.Add(magicOrb);

            // Some lamps along one edge
            for (int i = 0; i < 3; i++)
            {
                float x = (i + 1) * terrainWidth / 4;
                var lamp = PointLight.CreateLamp(new Vector3(x, 15, 5));
                lightProcessor.AddPointLight(lamp);
                sceneLights.Add(lamp);
            }
        }

        public void LoadContent()
        {
            LoadTerrainAssets();
            GenerateTerrain();
            
            // Set camera initial position to center of terrain
            Vector3 terrainCenter = new Vector3(
                terrainData.Width * terrainData.Scale * 0.5f, 
                0, 
                terrainData.Height * terrainData.Scale * 0.5f
            );
            rtsCamera.FocusOn(terrainCenter);
            rtsCamera.Height = 80.0f;
            
            // Set terrain bounds for camera movement
            rtsCamera.SetTerrainBounds(
                terrainData.Width * terrainData.Scale, 
                terrainData.Height * terrainData.Scale
            );

            // Create minimap in top-right corner
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            int minimapSize = 200;
            minimap = new Minimap(
                gd,
                Globals.screenManager.getSpriteBatch,
                minimapSize, minimapSize,
                gd.Viewport.Width - minimapSize - 40,
                gd.Viewport.Height - minimapSize - 40
            );
            minimap.SetTerrain(terrainData);
            minimap.SetCamera(rtsCamera);
        }

        private void LoadTerrainAssets()
        {
            terrainEffect = Globals.screenManager.Content.Load<Effect>("shaders/surface/VertexLitStandard");
            grassTexture = Globals.screenManager.Content.Load<Texture2D>("textures/prototype/grass");
            
            terrainRenderer = new TerrainRenderer(Globals.screenManager.getGraphicsDevice.GraphicsDevice, terrainEffect);
        }

        private void GenerateTerrain()
        {
            terrainData = new TerrainData(64, 64);
            terrainData.Scale = 2.0f;
            terrainData.HeightScale = 8.0f;
            
            terrainData.GenerateTerrain(
                seed: 12345,
                noiseScale: 8.0f,
                octaves: 4,
                persistence: 0.5f
            );

            terrainRenderer.LoadTerrain(terrainData, grassTexture);

            // Create scene lights after terrain is loaded
            //CreateSceneLights();
        }

        public void Update(GameTime gameTime)
        {
            rtsCamera.Update(gameTime);

            HandleTerrainInteraction();
            UpdateMousePointLight();
            UpdateLighting(gameTime);

            // Update all lights
            lightProcessor.Update(gameTime);
        }

        private void UpdateLighting(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();

            // Press 1-4 to switch between lighting presets
            if (keyboardState.IsKeyDown(Keys.D1))
            {
                environmentLight = EnvironmentLight.CreateDaylight();
                lightProcessor.SetEnvironmentLight(environmentLight);
                System.Console.WriteLine("Switched to Daylight lighting");
            }
            else if (keyboardState.IsKeyDown(Keys.D2))
            {
                environmentLight = EnvironmentLight.CreateSunset();
                lightProcessor.SetEnvironmentLight(environmentLight);
                System.Console.WriteLine("Switched to Sunset lighting");
            }
            else if (keyboardState.IsKeyDown(Keys.D3))
            {
                environmentLight = EnvironmentLight.CreateNight();
                lightProcessor.SetEnvironmentLight(environmentLight);
                System.Console.WriteLine("Switched to Night lighting");
            }
            else if (keyboardState.IsKeyDown(Keys.D4))
            {
                environmentLight = EnvironmentLight.CreateOvercast();
                lightProcessor.SetEnvironmentLight(environmentLight);
                System.Console.WriteLine("Switched to Overcast lighting");
            }

            // Toggle scene lights with L key
            if (keyboardState.IsKeyDown(Keys.L) && sceneLights != null)
            {
                foreach (var light in sceneLights)
                {
                    light.IsEnabled = !light.IsEnabled;
                }
                System.Console.WriteLine($"Scene lights: {(sceneLights.FirstOrDefault()?.IsEnabled == true ? "ON" : "OFF")}");
            }

            // Toggle mouse light with M key
            if (keyboardState.IsKeyDown(Keys.M))
            {
                mouseLight.IsEnabled = !mouseLight.IsEnabled;
                System.Console.WriteLine($"Mouse light: {(mouseLight.IsEnabled ? "ON" : "OFF")}");
            }

            // Tweak directional light intensity with Up/Down arrows
            if (keyboardState.IsKeyDown(Keys.Up))
            {
                environmentLight.DirectionalLightIntensity = Math.Min(2.0f, environmentLight.DirectionalLightIntensity + 0.01f);
            }
            else if (keyboardState.IsKeyDown(Keys.Down))
            {
                environmentLight.DirectionalLightIntensity = Math.Max(0.0f, environmentLight.DirectionalLightIntensity - 0.01f);
            }

            // Tweak ambient light intensity with Left/Right arrows
            if (keyboardState.IsKeyDown(Keys.Right))
            {
                environmentLight.AmbientLightIntensity = Math.Min(1.0f, environmentLight.AmbientLightIntensity + 0.01f);
            }
            else if (keyboardState.IsKeyDown(Keys.Left))
            {
                environmentLight.AmbientLightIntensity = Math.Max(0.0f, environmentLight.AmbientLightIntensity - 0.01f);
            }
        }

        private void UpdateMousePointLight()
        {
            var mouseState = Mouse.GetState();
            Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
            Vector3 worldPosition = rtsCamera.ScreenToWorld(mousePosition, 0);

            // Calculate terrain bounds
            float terrainWidth = terrainData.Width * terrainData.Scale;
            float terrainDepth = terrainData.Height * terrainData.Scale;

            // Clamp world position to terrain bounds
            float clampedX = MathHelper.Clamp(worldPosition.X, 0, terrainWidth);
            float clampedZ = MathHelper.Clamp(worldPosition.Z, 0, terrainDepth);

            // Get terrain height at clamped position
            float terrainHeightValue;
            try
            {
                terrainHeightValue = terrainData.GetHeightAt(clampedX, clampedZ);

                // Fallback if height is invalid
                if (float.IsNaN(terrainHeightValue) || float.IsInfinity(terrainHeightValue))
                {
                    terrainHeightValue = 0.0f;
                }
            }
            catch
            {
                // If GetHeightAt fails, use default height
                terrainHeightValue = 0.0f;
            }

            // Update mouse light position (use original world position, not clamped)
            mouseLight.Position = new Vector3(worldPosition.X, terrainHeightValue + 5.0f, worldPosition.Z);

            // Keep light enabled
            mouseLight.IsEnabled = true;

            // Debug output every 30 frames (roughly twice per second)
            if (System.Environment.TickCount % 500 < 17) // Approximate frame timing
            {
                System.Console.WriteLine($"Mouse light: Pos({mouseLight.Position.X:F1}, {mouseLight.Position.Y:F1}, {mouseLight.Position.Z:F1}) " +
                                       $"Range({mouseLight.Range}) Intensity({mouseLight.Intensity}) Enabled({mouseLight.IsEnabled})");
            }
        }

        private void HandleTerrainInteraction()
        {
            var mouseState = Mouse.GetState();
            var keyboardState = Keyboard.GetState();

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
                Vector3 worldPosition = rtsCamera.ScreenToWorld(mousePosition, 0);
                
                float terrainHeight = terrainData.GetHeightAt(worldPosition.X, worldPosition.Z);
                worldPosition.Y = terrainHeight;
                
                System.Console.WriteLine($"Clicked terrain at: {worldPosition}, Height: {terrainHeight}");
            }

            if (keyboardState.IsKeyDown(Keys.Space))
            {
                //GenerateTerrain();
            }

            if (keyboardState.IsKeyDown(Keys.C))
            {
                Vector3 terrainCenter = new Vector3(
                    terrainData.Width * terrainData.Scale * 0.5f, 
                    0, 
                    terrainData.Height * terrainData.Scale * 0.5f
                );
                rtsCamera.FocusOn(terrainCenter);
            }
        }

        public void Draw(GameTime gameTime)
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            
            gd.Clear(new Color(0.0f,0.021f,0.1f,0));
            gd.RasterizerState = RasterizerState.CullNone;
            gd.DepthStencilState = DepthStencilState.Default;

            // Apply environment lighting
            environmentLight.ApplyToEffect(terrainEffect);

            // Apply all point lights (don't filter by distance for terrain)
            var allActiveLights = new List<PointLight>();
            if (mouseLight.IsEnabled) allActiveLights.Add(mouseLight);
            if (sceneLights != null)
            {
                allActiveLights.AddRange(sceneLights.Where(l => l.IsEnabled));
            }

            // Apply up to 8 lights to the terrain shader
            var positions = new Vector3[8];
            var colors = new Vector3[8];
            var ranges = new float[8];
            var intensities = new float[8];

            int lightCount = Math.Min(allActiveLights.Count, 8);
            for (int i = 0; i < lightCount; i++)
            {
                var light = allActiveLights[i];
                positions[i] = light.Position;
                colors[i] = light.Color.ToVector3();
                ranges[i] = light.Range;
                intensities[i] = light.Intensity;
            }

            terrainEffect.Parameters["ActivePointLights"]?.SetValue(lightCount);
            terrainEffect.Parameters["PointLightPositions"]?.SetValue(positions);
            terrainEffect.Parameters["PointLightColors"]?.SetValue(colors);
            terrainEffect.Parameters["PointLightRanges"]?.SetValue(ranges);
            terrainEffect.Parameters["PointLightIntensities"]?.SetValue(intensities);

            // Set texture tiling (10x10 as requested)
            terrainEffect.Parameters["TextureTiling"]?.SetValue(new Vector2(10.0f, 10.0f));

            terrainRenderer.Render(
                world: Matrix.Identity,
                view: rtsCamera.View,
                projection: rtsCamera.Projection,
                lightDirection: environmentLight.DirectionalLightDirection
            );
        }

        public void DrawUI(GameTime gameTime)
        {
            minimap?.Draw();
            DrawLightingDebugInfo();
        }

        private void DrawLightingDebugInfo()
        {
            var spriteBatch = Globals.screenManager.getSpriteBatch;
            var font = Globals.screenManager.Content.Load<SpriteFont>("fonts/Arial");

            int y = 10;
            int lineHeight = 20;
            Color textColor = Color.White;

            // Draw lighting info
            spriteBatch.DrawString(font, "=== LIGHTING CONTROLS ===", new Vector2(10, y), textColor);
            y += lineHeight;

            spriteBatch.DrawString(font, "1-4: Lighting Presets (Day/Sunset/Night/Overcast)", new Vector2(10, y), Color.Yellow);
            y += lineHeight;

            spriteBatch.DrawString(font, $"Directional Intensity: {environmentLight.DirectionalLightIntensity:F2} (Up/Down)", new Vector2(10, y), textColor);
            y += lineHeight;

            spriteBatch.DrawString(font, $"Ambient Intensity: {environmentLight.AmbientLightIntensity:F2} (Left/Right)", new Vector2(10, y), textColor);
            y += lineHeight;

            spriteBatch.DrawString(font, $"Dir Light Color: R:{environmentLight.DirectionalLightColor.R} G:{environmentLight.DirectionalLightColor.G} B:{environmentLight.DirectionalLightColor.B}", new Vector2(10, y), textColor);
            y += lineHeight;

            spriteBatch.DrawString(font, $"Ambient Color: R:{environmentLight.AmbientLightColor.R} G:{environmentLight.AmbientLightColor.G} B:{environmentLight.AmbientLightColor.B}", new Vector2(10, y), textColor);
            y += lineHeight;

            y += lineHeight; // Extra space
            spriteBatch.DrawString(font, $"Active Point Lights: {lightProcessor.ActivePointLights}/{lightProcessor.TotalPointLights}", new Vector2(10, y), Color.Cyan);
            y += lineHeight;

            spriteBatch.DrawString(font, "L: Toggle Scene Lights | M: Toggle Mouse Light", new Vector2(10, y), Color.Yellow);
            y += lineHeight;

            spriteBatch.DrawString(font, "Space: Regenerate Terrain | C: Center Camera", new Vector2(10, y), Color.Gray);
        }

        public void Dispose()
        {
            terrainRenderer?.Dispose();
            minimap?.Dispose();
        }
    }
}