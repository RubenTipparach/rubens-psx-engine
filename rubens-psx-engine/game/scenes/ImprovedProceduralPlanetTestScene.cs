using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using rubens_psx_engine.entities;
using rubens_psx_engine.system.procedural;
using rubens_psx_engine;
using System;

namespace anakinsoft.game.scenes
{
    public class ImprovedProceduralPlanetTestScene : Scene
    {
        private ImprovedProceduralPlanet planet;
        private ImprovedProceduralPlanet moon;
        private ImprovedProceduralPlanet asteroidBelt;

        private Effect planetEffect;
        private BasicEffect basicEffect;

        private float rotation = 0f;
        private float moonOrbit = 0f;

        public ImprovedProceduralPlanetTestScene() : base()
        {
        }

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            // Create the main planet with high detail
            planet = new ImprovedProceduralPlanet(graphicsDevice, radius: 20f, subdivisionLevel: 128);

            // Create a moon with less detail
            moon = new ImprovedProceduralPlanet(graphicsDevice, radius: 5f, subdivisionLevel: 64, seed: 42);

            // Create smaller asteroid/planet
            asteroidBelt = new ImprovedProceduralPlanet(graphicsDevice, radius: 3f, subdivisionLevel: 32, seed: 123);

            // Try to load custom planet shader
            try
            {
                var content = Globals.screenManager.Content;
                planetEffect = content.Load<Effect>("shaders/PlanetShader");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to load planet shader: {ex.Message}");
                // Fall back to BasicEffect
                basicEffect = new BasicEffect(graphicsDevice);
                basicEffect.VertexColorEnabled = true;
                basicEffect.LightingEnabled = true;
                basicEffect.EnableDefaultLighting();
                basicEffect.PreferPerPixelLighting = true;
                basicEffect.SpecularColor = new Vector3(0.5f, 0.5f, 0.5f);
                basicEffect.SpecularPower = 32f;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Rotate the planets
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            rotation += deltaTime * 0.1f;
            moonOrbit += deltaTime * 0.3f;
        }

        public override void Draw(GameTime gameTime, Camera camera)
        {
            base.Draw(gameTime, camera);

            var graphicsDevice = Globals.screenManager.GraphicsDevice;

            // Set render states
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            graphicsDevice.BlendState = BlendState.Opaque;

            Effect effectToUse = planetEffect ?? (Effect)basicEffect;

            // Update shader parameters if using custom shader
            if (planetEffect != null)
            {
                planetEffect.Parameters["CameraPosition"]?.SetValue(camera.Position);
                planetEffect.Parameters["WorldInverseTranspose"]?.SetValue(Matrix.Invert(Matrix.Transpose(Matrix.Identity)));
            }

            // Draw main planet
            Matrix worldMain = Matrix.CreateRotationY(rotation) * Matrix.CreateTranslation(Vector3.Zero);
            planet.Draw(graphicsDevice, worldMain, camera.View, camera.Projection, effectToUse);

            // Draw moon orbiting the planet
            Matrix moonWorld =
                Matrix.CreateRotationY(moonOrbit * 2f) *
                Matrix.CreateTranslation(new Vector3(35, 5, 0)) *
                Matrix.CreateRotationY(moonOrbit);
            moon.Draw(graphicsDevice, moonWorld, camera.View, camera.Projection, effectToUse);

            // Draw distant asteroid
            Matrix asteroidWorld =
                Matrix.CreateRotationY(-rotation * 3f) *
                Matrix.CreateRotationX(rotation * 0.5f) *
                Matrix.CreateTranslation(new Vector3(-50, -10, -20));
            asteroidBelt.Draw(graphicsDevice, asteroidWorld, camera.View, camera.Projection, effectToUse);
        }

        public void RegeneratePlanets(GraphicsDevice graphicsDevice)
        {
            // Dispose old planets
            planet?.Dispose();
            moon?.Dispose();
            asteroidBelt?.Dispose();

            // Create new ones with random seeds
            var random = new Random();
            planet = new ImprovedProceduralPlanet(graphicsDevice, radius: 20f, subdivisionLevel: 128, seed: random.Next());
            moon = new ImprovedProceduralPlanet(graphicsDevice, radius: 5f, subdivisionLevel: 64, seed: random.Next());
            asteroidBelt = new ImprovedProceduralPlanet(graphicsDevice, radius: 3f, subdivisionLevel: 32, seed: random.Next());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                planet?.Dispose();
                moon?.Dispose();
                asteroidBelt?.Dispose();
                planetEffect?.Dispose();
                basicEffect?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}