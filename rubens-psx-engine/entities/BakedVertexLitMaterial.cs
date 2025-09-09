using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rubens_psx_engine.entities
{
    /// <summary>
    /// Material for baked vertex-lit PS1-style shaders
    /// </summary>
    public class BakedVertexLitMaterial : Material
    {
        // PS1-style shader parameters
        public float VertexJitterAmount { get; set; } = 2.0f;
        public float AffineAmount { get; set; } = 1.0f;
        public bool EnableAffineMapping { get; set; } = true;
        
        // Baked lighting parameters
        public float BakedLightIntensity { get; set; } = 1.5f;
        public Vector3 TintColor { get; set; } = Vector3.One;
        
        public BakedVertexLitMaterial(string texturePath = null) 
            : base("shaders/surface/BakedVertexLit", texturePath)
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
            
            // Set baked lighting parameters
            effect.Parameters["BakedLightIntensity"]?.SetValue(BakedLightIntensity);
            effect.Parameters["TintColor"]?.SetValue(TintColor);
            
            // Set texture if available
            if (texture != null)
            {
                effect.Parameters["Texture"]?.SetValue(texture);
            }
        }
    }
}