using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rubens_psx_engine.entities.materials
{
    public class VertexLitUnitsMaterial : Material
    {
        public Vector3 DiffuseColor { get; set; } = Vector3.One;
        public float Alpha { get; set; } = 1.0f;
        public new Texture2D Texture { get; set; }

        private new Effect effect;
        private EffectParameter worldParameter;
        private EffectParameter viewParameter;
        private EffectParameter projectionParameter;
        private EffectParameter diffuseColorParameter;
        private EffectParameter textureParameter;

        public VertexLitUnitsMaterial() : base(null, null)
        {
            LoadEffect();
        }

        private void LoadEffect()
        {
            try
            {
                effect = Globals.screenManager.Content.Load<Effect>("shaders/VertexLitStandard");

                worldParameter = effect.Parameters["World"];
                viewParameter = effect.Parameters["View"];
                projectionParameter = effect.Parameters["Projection"];
                diffuseColorParameter = effect.Parameters["DiffuseColor"];
                textureParameter = effect.Parameters["MainTexture"];
            }
            catch
            {
                // Fallback to basic effect if custom shader not available
                var graphicsDevice = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
                effect = new BasicEffect(graphicsDevice)
                {
                    VertexColorEnabled = false,
                    TextureEnabled = true
                };
            }
        }

        public override void Apply(Camera camera, Matrix world)
        {
            if (effect == null)
            {
                System.Console.WriteLine("VertexLitUnitsMaterial: ERROR - effect is null!");
                return;
            }

            //System.Console.WriteLine($"VertexLitUnitsMaterial: Applying effect {effect.GetType().Name}");

            if (effect is BasicEffect basicEffect)
            {
                // Use BasicEffect - FORCE SOLID COLOR RENDERING FOR DEBUG
                basicEffect.World = world;
                basicEffect.View = camera.View;
                basicEffect.Projection = camera.Projection;
                basicEffect.DiffuseColor = Vector3.One; // Force white/bright color
                basicEffect.Alpha = 1.0f; // Force fully opaque
                basicEffect.TextureEnabled = false; // Disable texture completely
                basicEffect.LightingEnabled = false; // Disable lighting
                basicEffect.VertexColorEnabled = false; // Disable vertex colors

                //System.Console.WriteLine($"BasicEffect: FORCED white color, no texture, no lighting");

                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                }
            }
            else
            {
                // Use custom effect
                worldParameter?.SetValue(world);
                viewParameter?.SetValue(camera.View);
                projectionParameter?.SetValue(camera.Projection);
                diffuseColorParameter?.SetValue(new Vector4(DiffuseColor, Alpha));

                if (Texture != null)
                    textureParameter?.SetValue(Texture);

                //System.Console.WriteLine($"Custom effect: World set, View set, Projection set, DiffuseColor={DiffuseColor}, Texture={Texture != null}");

                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                }
            }
        }

        public Effect GetEffect()
        {
            return effect;
        }

        public void Dispose()
        {
            effect?.Dispose();
        }
    }
}