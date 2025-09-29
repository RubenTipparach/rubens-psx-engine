using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.entities;
using rubens_psx_engine.system.procedural;
using rubens_psx_engine;
using System;

namespace anakinsoft.game.scenes
{
    public class ProceduralPlanetTestScene : Scene
    {
        private ProceduralPlanet planet;
        private BasicEffect planetEffect;
        private float rotation = 0f;

        // Multiple planets for testing
        private ProceduralPlanet smallPlanet;
        private ProceduralPlanet largePlanet;

        public ProceduralPlanetTestScene() : base()
        {
        }

        public void Initialize(GraphicsDevice graphicsDevice)
        {

            // Create the main planet with medium detail
            planet = new ProceduralPlanet(graphicsDevice, radius: 15f, subdivisionLevel: 64);

            // Create a smaller, less detailed planet
            smallPlanet = new ProceduralPlanet(graphicsDevice, radius: 5f, subdivisionLevel: 32);

            // Create a larger, more detailed planet
            largePlanet = new ProceduralPlanet(graphicsDevice, radius: 25f, subdivisionLevel: 96);

            // Create a basic effect for rendering
            planetEffect = new BasicEffect(graphicsDevice);
            planetEffect.VertexColorEnabled = true;
            planetEffect.TextureEnabled = false;
            planetEffect.LightingEnabled = false; // Disable lighting since we don't have normals
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Rotate the planets
            rotation += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.2f;
        }

        public override void Draw(GameTime gameTime, Camera camera)
        {
            base.Draw(gameTime, camera);

            var graphicsDevice = Globals.screenManager.GraphicsDevice;

            // Set render states
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;

            // Draw main planet in the center
            Matrix worldMain = Matrix.CreateRotationY(rotation) * Matrix.CreateTranslation(Vector3.Zero);
            planet.Draw(graphicsDevice, worldMain, camera.View, camera.Projection, planetEffect);

            // Draw small planet to the left
            Matrix worldSmall = Matrix.CreateRotationY(-rotation * 1.5f) *
                               Matrix.CreateTranslation(new Vector3(-40, 5, 0));
            smallPlanet.Draw(graphicsDevice, worldSmall, camera.View, camera.Projection, planetEffect);

            // Draw large planet to the right and back
            Matrix worldLarge = Matrix.CreateRotationY(rotation * 0.7f) *
                               Matrix.CreateTranslation(new Vector3(60, -10, -30));
            largePlanet.Draw(graphicsDevice, worldLarge, camera.View, camera.Projection, planetEffect);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                planet?.Dispose();
                smallPlanet?.Dispose();
                largePlanet?.Dispose();
                planetEffect?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}