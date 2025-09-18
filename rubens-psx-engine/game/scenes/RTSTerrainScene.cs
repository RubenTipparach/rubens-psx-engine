using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.entities;
using rubens_psx_engine.system.ui;
using rubens_psx_engine.system.lighting;
using rubens_psx_engine.system.terrain;
using rubens_psx_engine.game.units;
using rubens_psx_engine.game.environment;
using System;
using System.Collections.Generic;
using System.Linq;

namespace rubens_psx_engine.game.scenes
{
    public class RTSTerrainScene : Scene
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

        // Game systems
        private UnitManager unitManager;
        private WaterSystem waterSystem;
        private Bridge bridge;

        // Center cube
        private RenderingEntity centerCube;

        // Test cube using working Scene methods
        private RenderingEntity testCube;

        // Debug wireframe rendering
        private BasicEffect wireframeEffect;
        private List<VertexPositionColor> wireframeVertices;
        private List<short> wireframeIndices;

        public RTSTerrainScene(RTSCamera camera) : base(null) // No physics needed for this scene
        {
            rtsCamera = camera;
            InitializeLighting();
            InitializeGameSystems();
            InitializeWireframe();
            LoadContent();
        }

        private void InitializeGameSystems()
        {
            unitManager = new UnitManager();
            waterSystem = new WaterSystem(Globals.screenManager.getGraphicsDevice.GraphicsDevice);
        }

        private void InitializeWireframe()
        {
            wireframeVertices = new List<VertexPositionColor>();
            wireframeIndices = new List<short>();

            wireframeEffect = new BasicEffect(Globals.screenManager.getGraphicsDevice.GraphicsDevice);
            wireframeEffect.VertexColorEnabled = true;
            wireframeEffect.LightingEnabled = false;
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
            minimap.SetUnitManager(unitManager);
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

            // Initialize water system with terrain
            waterSystem.GenerateRiverGeometry(terrainData);

            // Pass terrain data to unit manager
            unitManager.SetTerrainData(terrainData);

            // Create bridge across the river
            CreateBridge();

            // Create center cube
            CreateCenterCube();

            // Create test cube using known working method
            CreateTestCube();

            // Create some initial units
            CreateInitialUnits();

            // Create scene lights after terrain is loaded
            CreateSceneLights();
        }

        public override void Update(GameTime gameTime)
        {
            // Call base Scene update to handle all entities
            base.Update(gameTime);

            rtsCamera.Update(gameTime);

            HandleTerrainInteraction();
            UpdateMousePointLight();
            UpdateLighting(gameTime);

            // Update game systems
            unitManager.Update(gameTime, rtsCamera);
            waterSystem.Update(gameTime);

            // Update all lights
            lightProcessor.Update(gameTime);
        }

        private void UpdateLighting(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();

            // Press F1-F4 to switch between lighting presets (moved from 1-4 to avoid conflict with unit spawning)
            if (keyboardState.IsKeyDown(Keys.F1))
            {
                environmentLight = EnvironmentLight.CreateDaylight();
                lightProcessor.SetEnvironmentLight(environmentLight);
                System.Console.WriteLine("Switched to Daylight lighting");
            }
            else if (keyboardState.IsKeyDown(Keys.F2))
            {
                environmentLight = EnvironmentLight.CreateSunset();
                lightProcessor.SetEnvironmentLight(environmentLight);
                System.Console.WriteLine("Switched to Sunset lighting");
            }
            else if (keyboardState.IsKeyDown(Keys.F3))
            {
                environmentLight = EnvironmentLight.CreateNight();
                lightProcessor.SetEnvironmentLight(environmentLight);
                System.Console.WriteLine("Switched to Night lighting");
            }
            else if (keyboardState.IsKeyDown(Keys.F4))
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

        public override void Draw(GameTime gameTime, Camera camera)
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

            // Draw water
            waterSystem.Draw(Matrix.Identity, rtsCamera.View, rtsCamera.Projection);

            // Draw bridge
            bridge?.Draw(rtsCamera.View, rtsCamera.Projection);

            // Call base Scene draw to handle all RenderingEntity objects
            base.Draw(gameTime, rtsCamera);

            // Draw units
            unitManager.Draw(rtsCamera);

            // Draw debug wireframes
            DrawDebugWireframes();
        }

        // Keep the original Draw method for backward compatibility with existing callers
        public void Draw(GameTime gameTime)
        {
            Draw(gameTime, rtsCamera);
        }

        public void DrawUI(GameTime gameTime)
        {
            minimap?.Draw();
            DrawLightingDebugInfo();
            DrawGameUI();

            // Draw unit UI (labels and selection box)
            unitManager.DrawUI(rtsCamera);
        }

        private void DrawGameUI()
        {
            var spriteBatch = Globals.screenManager.getSpriteBatch;
            var font = Globals.screenManager.Content.Load<SpriteFont>("fonts/Arial");

            int y = 420; // Below lighting info
            int lineHeight = 20;
            Color textColor = Color.White;

            spriteBatch.DrawString(font, "=== UNIT CONTROLS ===", new Vector2(10, y), textColor);
            y += lineHeight;

            spriteBatch.DrawString(font, "1: Create Worker | 2: Create Soldier | 3: Create Tank", new Vector2(10, y), Color.Yellow);
            y += lineHeight;

            spriteBatch.DrawString(font, "Left Click: Select | Right Click: Move | Ctrl+Click: Multi-select", new Vector2(10, y), Color.Yellow);
            y += lineHeight;

            spriteBatch.DrawString(font, $"Units: {unitManager.Units.Count} | Selected: {unitManager.SelectedUnits.Count}", new Vector2(10, y), Color.Cyan);
            y += lineHeight;

            // Unit type counts
            int workers = unitManager.GetUnitCount(UnitType.Worker);
            int soldiers = unitManager.GetUnitCount(UnitType.Soldier);
            int tanks = unitManager.GetUnitCount(UnitType.Tank);
            spriteBatch.DrawString(font, $"Workers: {workers} | Soldiers: {soldiers} | Tanks: {tanks}", new Vector2(10, y), Color.LightGreen);
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

            spriteBatch.DrawString(font, "F1-F4: Lighting Presets (Day/Sunset/Night/Overcast)", new Vector2(10, y), Color.Yellow);
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

        private void CreateBridge()
        {
            // Create bridge at roughly 1/3 point across the terrain
            float terrainWidth = terrainData.Width * terrainData.Scale;
            float terrainDepth = terrainData.Height * terrainData.Scale;
            float bridgeX = terrainWidth * 0.33f;

            Vector3 bridgeStart = new Vector3(bridgeX, 2.0f, 0);
            Vector3 bridgeEnd = new Vector3(bridgeX, 2.0f, terrainDepth);

            bridge = new Bridge(Globals.screenManager.getGraphicsDevice.GraphicsDevice, bridgeStart, bridgeEnd);
        }

        private void CreateCenterCube()
        {
            // Calculate center of terrain
            float terrainWidth = terrainData.Width * terrainData.Scale;
            float terrainDepth = terrainData.Height * terrainData.Scale;
            Vector3 centerPosition = new Vector3(terrainWidth * 0.5f, 0, terrainDepth * 0.5f);

            // Get terrain height at center
            float terrainHeight = terrainData.GetHeightAt(centerPosition.X, centerPosition.Z);
            centerPosition.Y = terrainHeight + 2.0f; // Place cube 2 units above terrain

            System.Console.WriteLine($"Creating cube at position: {centerPosition}, terrain size: {terrainWidth}x{terrainDepth}");

            // Create cube entity using the SAME effect and texture as terrain and add to scene
            centerCube = CreateRenderingEntity(centerPosition, "models/cube", "textures/prototype/grass", "shaders/surface/VertexLitStandard");
            centerCube.Scale = new Vector3(1.0f, 1.0f, 1.0f); // Make it a proper 5x5x5 cube
            centerCube.Color = new Vector3(1.0f, 1.0f, 0.0f); // Bright yellow color for visibility
        }

        private void CreateTestCube()
        {
            // Create a test cube using the exact same method as GraphicsTestScene and add to scene
            Vector3 testPosition = new Vector3(terrainData.Width * terrainData.Scale * 0.7f, 15.0f, terrainData.Height * terrainData.Scale * 0.7f);
            testCube = CreateRenderingEntity(testPosition, "models/cube", "textures/white");
            testCube.Scale = new Vector3(1.0f, 1.0f, 1.0f); // Large for visibility
            testCube.Color = new Vector3(0.0f, 1.0f, 0.0f); // Bright green color
            testCube.IsVisible = true;

            System.Console.WriteLine($"Created test cube at position: {testCube.Position}, scale: {testCube.Scale}");
        }

        private void CreateInitialUnits()
        {
            float terrainWidth = terrainData.Width * terrainData.Scale;
            float terrainDepth = terrainData.Height * terrainData.Scale;

            // Create some units on one side of the river
            for (int i = 0; i < 3; i++)
            {
                Vector3 unitPos = new Vector3(
                    10 + i * 3,
                    0,
                    terrainDepth * 0.25f + i * 2
                );
                unitPos.Y = terrainData.GetHeightAt(unitPos.X, unitPos.Z) + 1.0f;
                unitManager.CreateUnit(UnitType.Worker, unitPos, Color.Blue, $"Worker {i + 1}");
            }

            // Create a soldier near the bridge
            Vector3 soldierPos = new Vector3(
                terrainWidth * 0.3f,
                0,
                terrainDepth * 0.2f
            );
            soldierPos.Y = terrainData.GetHeightAt(soldierPos.X, soldierPos.Z) + 1.0f;
            unitManager.CreateUnit(UnitType.Soldier, soldierPos, Color.Blue, "Guard");
        }

        private void DrawDebugWireframes()
        {
            wireframeVertices.Clear();
            wireframeIndices.Clear();

            // Draw bounding box for center cube
            if (centerCube != null)
            {
                var cubeWorldMatrix = centerCube.GetWorldMatrix();
                var actualBounds = GetModelBounds(centerCube.Model, cubeWorldMatrix);

                // Calculate expected bounds based on position and scale
                var expectedMin = centerCube.Position - centerCube.Scale * 0.5f;
                var expectedMax = centerCube.Position + centerCube.Scale * 0.5f;
                var expectedBounds = new BoundingBox(expectedMin, expectedMax);

                CreateWireframeBox(actualBounds, Color.Yellow);
                CreateWireframeBox(expectedBounds, Color.Orange); // Different color to distinguish

                //System.Console.WriteLine($"=== CENTER CUBE DEBUG ===");
                //System.Console.WriteLine($"Position: {centerCube.Position}");
                //System.Console.WriteLine($"Scale: {centerCube.Scale}");
                //System.Console.WriteLine($"EXPECTED bounds: Min:{expectedBounds.Min} Max:{expectedBounds.Max}");
                //System.Console.WriteLine($"ACTUAL mesh bounds: Min:{actualBounds.Min} Max:{actualBounds.Max}");
                //System.Console.WriteLine($"Bounds match: {Vector3.Distance(expectedBounds.Min, actualBounds.Min) < 0.1f && Vector3.Distance(expectedBounds.Max, actualBounds.Max) < 0.1f}");
                //System.Console.WriteLine();
            }

            // Draw bounding boxes for all units
            foreach (var unit in unitManager.Units)
            {
                var unitScale = unit.GetPublicUnitScale();
                var unitWorldMatrix = Matrix.CreateScale(unitScale) *
                                    Matrix.CreateTranslation(unit.Position);
                var actualBounds = GetModelBounds(unit.GetModel(), unitWorldMatrix);

                // Calculate expected bounds based on position and scale
                var expectedMin = unit.Position - Vector3.One * unitScale * 0.5f;
                var expectedMax = unit.Position + Vector3.One * unitScale * 0.5f;
                var expectedBounds = new BoundingBox(expectedMin, expectedMax);

                CreateWireframeBox(actualBounds, Color.Cyan);
                CreateWireframeBox(expectedBounds, Color.Lime); // Different color to distinguish

                //System.Console.WriteLine($"=== UNIT {unit.Name} DEBUG ===");
                //System.Console.WriteLine($"Type: {unit.Type}");
                //System.Console.WriteLine($"Position: {unit.Position}");
                //System.Console.WriteLine($"Scale: {unitScale}");
                //System.Console.WriteLine($"EXPECTED bounds: Min:{expectedBounds.Min} Max:{expectedBounds.Max}");
                //System.Console.WriteLine($"ACTUAL mesh bounds: Min:{actualBounds.Min} Max:{actualBounds.Max}");
                //System.Console.WriteLine($"Bounds match: {Vector3.Distance(expectedBounds.Min, actualBounds.Min) < 0.1f && Vector3.Distance(expectedBounds.Max, actualBounds.Max) < 0.1f}");
                //System.Console.WriteLine();
            }

            // Draw all wireframes
            if (wireframeVertices.Count > 0 && wireframeIndices.Count > 0)
            {
                var graphicsDevice = Globals.screenManager.getGraphicsDevice.GraphicsDevice;

                wireframeEffect.World = Matrix.Identity;
                wireframeEffect.View = rtsCamera.View;
                wireframeEffect.Projection = rtsCamera.Projection;

                wireframeEffect.CurrentTechnique.Passes[0].Apply();

                try
                {
                    graphicsDevice.DrawUserIndexedPrimitives(
                        PrimitiveType.LineList,
                        wireframeVertices.ToArray(),
                        0,
                        wireframeVertices.Count,
                        wireframeIndices.ToArray(),
                        0,
                        wireframeIndices.Count / 2
                    );
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Wireframe drawing error: {ex.Message}");
                }
            }
        }

        private BoundingBox GetModelBounds(Model model, Matrix worldMatrix)
        {
            if (model == null) return new BoundingBox();

            BoundingBox modelBounds = new BoundingBox();
            bool first = true;

            foreach (var mesh in model.Meshes)
            {
                // Convert BoundingSphere to BoundingBox manually
                var sphere = mesh.BoundingSphere;
                Vector3 min = sphere.Center - Vector3.One * sphere.Radius;
                Vector3 max = sphere.Center + Vector3.One * sphere.Radius;
                BoundingBox meshBounds = new BoundingBox(min, max);

                if (first)
                {
                    modelBounds = meshBounds;
                    first = false;
                }
                else
                {
                    modelBounds = BoundingBox.CreateMerged(modelBounds, meshBounds);
                }
            }

            // Transform bounding box to world space
            Vector3[] corners = new Vector3[8];
            modelBounds.GetCorners(corners);

            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = Vector3.Transform(corners[i], worldMatrix);
            }

            return BoundingBox.CreateFromPoints(corners);
        }

        private void CreateWireframeBox(BoundingBox bounds, Color color)
        {
            Vector3[] corners = new Vector3[8];
            bounds.GetCorners(corners);

            short vertexStart = (short)wireframeVertices.Count;

            // Add vertices
            foreach (var corner in corners)
            {
                wireframeVertices.Add(new VertexPositionColor(corner, color));
            }

            // Add indices for the 12 edges of the box
            // Bottom face edges (0,1,2,3)
            wireframeIndices.Add((short)(vertexStart + 0)); wireframeIndices.Add((short)(vertexStart + 1));
            wireframeIndices.Add((short)(vertexStart + 1)); wireframeIndices.Add((short)(vertexStart + 2));
            wireframeIndices.Add((short)(vertexStart + 2)); wireframeIndices.Add((short)(vertexStart + 3));
            wireframeIndices.Add((short)(vertexStart + 3)); wireframeIndices.Add((short)(vertexStart + 0));

            // Top face edges (4,5,6,7)
            wireframeIndices.Add((short)(vertexStart + 4)); wireframeIndices.Add((short)(vertexStart + 5));
            wireframeIndices.Add((short)(vertexStart + 5)); wireframeIndices.Add((short)(vertexStart + 6));
            wireframeIndices.Add((short)(vertexStart + 6)); wireframeIndices.Add((short)(vertexStart + 7));
            wireframeIndices.Add((short)(vertexStart + 7)); wireframeIndices.Add((short)(vertexStart + 4));

            // Vertical edges
            wireframeIndices.Add((short)(vertexStart + 0)); wireframeIndices.Add((short)(vertexStart + 4));
            wireframeIndices.Add((short)(vertexStart + 1)); wireframeIndices.Add((short)(vertexStart + 5));
            wireframeIndices.Add((short)(vertexStart + 2)); wireframeIndices.Add((short)(vertexStart + 6));
            wireframeIndices.Add((short)(vertexStart + 3)); wireframeIndices.Add((short)(vertexStart + 7));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                terrainRenderer?.Dispose();
                minimap?.Dispose();
                waterSystem?.Dispose();
                bridge?.Dispose();
                centerCube = null; // RenderingEntity doesn't have Dispose (managed by Scene)
                testCube = null; // RenderingEntity doesn't have Dispose (managed by Scene)
                unitManager?.Dispose();
                wireframeEffect?.Dispose();
            }

            // Call base Scene dispose
            base.Dispose(disposing);
        }
    }
}