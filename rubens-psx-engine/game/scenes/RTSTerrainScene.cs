using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProceduralTerrain;
using rubens_psx_engine;
using rubens_psx_engine.entities;
using rubens_psx_engine.system.ui;
using System;

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
        private Vector3 lightDirection;

        public RTSTerrainScene(RTSCamera camera)
        {
            rtsCamera = camera;
            lightDirection = Vector3.Normalize(new Vector3(-0.5f, -1.0f, -0.5f));
            LoadContent();
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
                gd.Viewport.Width - minimapSize - 10, 10
            );
            minimap.SetTerrain(terrainData);
            minimap.SetCamera(rtsCamera);
        }

        private void LoadTerrainAssets()
        {
            terrainEffect = Globals.screenManager.Content.Load<Effect>("shaders/surface/VertexLit");
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
        }

        public void Update(GameTime gameTime)
        {
            rtsCamera.Update(gameTime);
            
            HandleTerrainInteraction();
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
                GenerateTerrain();
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
            
            gd.Clear(Color.SkyBlue);
            gd.RasterizerState = RasterizerState.CullNone;
            gd.DepthStencilState = DepthStencilState.Default;

            terrainRenderer.Render(
                world: Matrix.Identity,
                view: rtsCamera.View,
                projection: rtsCamera.Projection,
                lightDirection: lightDirection
            );
        }

        public void DrawUI(GameTime gameTime)
        {
            minimap?.Draw();
        }

        public void Dispose()
        {
            terrainRenderer?.Dispose();
            minimap?.Dispose();
        }
    }
}