using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine.system;
using rubens_psx_engine.system.vehicles;
using anakinsoft.system.cameras;
using anakinsoft.system.physics;
using anakinsoft.system.character;
using BepuUtilities.Memory;

namespace rubens_psx_engine.game.scenes
{
    public class TerrainScreen : PhysicsScreen
    {
        private TerrainScene terrainScene;
        private PodracerVehicle podracer;
        private VehicleCamera vehicleCamera;
        private PhysicsSystem physicsSystem;
        private CharacterControllers characterControllers;
        private BufferPool bufferPool;
        private Camera camera;

        public TerrainScreen()
        {
            var gdm = Globals.screenManager.getGraphicsDevice;
            var gd = gdm.GraphicsDevice;

            bufferPool = new BufferPool();
            characterControllers = new CharacterControllers(bufferPool);
            physicsSystem = new PhysicsSystem(ref characterControllers);

            terrainScene = new TerrainScene(physicsSystem);
            SetScene(terrainScene);
            terrainScene.Initialize();

            Vector3 startPosition = new Vector3(0, 20, 0);
            podracer = new PodracerVehicle(physicsSystem, startPosition);

            vehicleCamera = new VehicleCamera(gd, podracer);
            camera = vehicleCamera;
        }

        public override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            podracer?.Update(gameTime, keyboardState);

            scene?.Update(gameTime);
            camera?.Update(gameTime);

            base.Update(gameTime);
        }

        public override void UpdateInput(GameTime gameTime)
        {
            if (!Globals.screenManager.IsActive)
                return;

            HandleInput();
        }

        private void HandleInput()
        {
            var keyboardState = Keyboard.GetState();

            if (InputManager.GetKeyboardClick(Keys.Escape))
            {
                Globals.screenManager.AddScreen(new PauseMenu());
            }

            if (InputManager.GetKeyboardClick(Keys.F1) && SceneManager.IsSceneMenuEnabled())
            {
                Globals.screenManager.AddScreen(new SceneSelectionMenu());
            }

            if (InputManager.GetKeyboardClick(Keys.R))
            {
                vehicleCamera?.Reset();
            }

            if (InputManager.GetKeyboardClick(Keys.B))
            {
                if (terrainScene?.BoundingBoxRenderer != null)
                {
                    terrainScene.BoundingBoxRenderer.ShowBoundingBoxes =
                        !terrainScene.BoundingBoxRenderer.ShowBoundingBoxes;
                }
            }
        }

        public override void Draw3D(GameTime gameTime)
        {
            scene?.Draw(gameTime, camera);
            podracer?.Draw(gameTime, camera);
        }

        public override void Draw2D(GameTime gameTime)
        {
            DrawUI(gameTime);
        }

        private void DrawUI(GameTime gameTime)
        {
            string controlsText = "Podracer Terrain Scene\n\n" +
                "W/S - Accelerate/Brake\n" +
                "A/D - Steer\n" +
                "Shift - Boost\n" +
                "PageUp/PageDown - Camera Distance\n" +
                "Home/End - Camera Height\n" +
                "Right Mouse - Free Look\n" +
                "R - Reset Camera\n" +
                "B - Toggle Bounding Boxes\n" +
                "ESC - Pause\n" +
                "F1 - Scene Selection";

            Vector2 position = new Vector2(20, 20);
            getSpriteBatch.DrawString(Globals.fontNTR, controlsText, position + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, controlsText, position, Color.White);

            if (podracer != null)
            {
                float speed = podracer.Velocity.Length();
                string speedText = $"Speed: {speed:F1} m/s";
                Vector2 speedPos = new Vector2(Globals.screenManager.Window.ClientBounds.Width - 200, 20);
                getSpriteBatch.DrawString(Globals.fontNTR, speedText, speedPos + Vector2.One, Color.Black);
                getSpriteBatch.DrawString(Globals.fontNTR, speedText, speedPos, Color.Yellow);
            }
        }

        protected override void DisposePhysicsResources()
        {
            podracer?.RemoveFromPhysics();
            base.DisposePhysicsResources();
            characterControllers?.Dispose();
            bufferPool?.Clear();
        }
    }
}