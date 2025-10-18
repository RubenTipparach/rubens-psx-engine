using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rubens_psx_engine.entities
{
    /// <summary>
    /// Material for unlit PS1-style shaders
    /// </summary>
    public class UnlitMaterial : Material
    {
        // PS1-style shader parameters
        public float VertexJitterAmount { get; set; } = 2.0f;
        public float AffineAmount { get; set; } = 1.0f;
        public bool EnableAffineMapping { get; set; } = true;
        public float Brightness { get; set; } = 1.0f;

        // Fog parameters
        public bool FogEnabled { get; set; } = false;
        public bool FogUseExponential { get; set; } = false;
        public Vector3 FogColor { get; set; } = new Vector3(0.5f, 0.5f, 0.5f);
        public float FogStart { get; set; } = 50.0f;
        public float FogEnd { get; set; } = 200.0f;
        public float FogDensity { get; set; } = 0.01f;
        
        public UnlitMaterial(string texturePath = null) 
            : base("shaders/surface/Unlit", texturePath)
        {
        }

        public override void Apply(Camera camera, Matrix worldMatrix)
        {
            if (effect == null) return;

            // Set standard matrices
            effect.Parameters["World"]?.SetValue(worldMatrix);
            effect.Parameters["View"]?.SetValue(camera.View);
            effect.Parameters["Projection"]?.SetValue(camera.Projection);
            
            // Set PS1-style parameters
            effect.Parameters["VertexJitterAmount"]?.SetValue(VertexJitterAmount);
            effect.Parameters["AffineAmount"]?.SetValue(AffineAmount);
            effect.Parameters["EnableAffineMapping"]?.SetValue(EnableAffineMapping);
            effect.Parameters["Brightness"]?.SetValue(Brightness);

            // Set fog parameters
            effect.Parameters["FogEnabled"]?.SetValue(FogEnabled);
            effect.Parameters["FogUseExponential"]?.SetValue(FogUseExponential);
            effect.Parameters["FogColor"]?.SetValue(FogColor);
            effect.Parameters["FogStart"]?.SetValue(FogStart);
            effect.Parameters["FogEnd"]?.SetValue(FogEnd);
            effect.Parameters["FogDensity"]?.SetValue(FogDensity);

            // Set texture if available
            if (texture != null)
            {
                effect.Parameters["Texture"]?.SetValue(texture);
            }
        }
    }
}