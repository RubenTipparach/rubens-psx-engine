using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rubens_psx_engine.entities
{
    /// <summary>
    /// Material for vertex-lit PS1-style shaders
    /// </summary>
    public class VertexLitMaterial : Material
    {
        // PS1-style shader parameters
        public float VertexJitterAmount { get; set; } = 2.0f;
        public float AffineAmount { get; set; } = 1.0f;
        public bool EnableAffineMapping { get; set; } = true;
        
        // Lighting parameters
        public Vector3 LightDirection { get; set; } = Vector3.Normalize(new Vector3(1, -1, 1));
        public Vector3 LightColor { get; set; } = new Vector3(1.0f, 0.9f, 0.8f);
        public Vector3 AmbientColor { get; set; } = new Vector3(0.6f, 0.6f, 0.6f);
        public float LightIntensity { get; set; } = 0.5f;
        
        public VertexLitMaterial(string texturePath = null) 
            : base("shaders/surface/VertexLit", texturePath)
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
            
            // Set lighting parameters
            effect.Parameters["LightDirection"]?.SetValue(LightDirection);
            effect.Parameters["LightColor"]?.SetValue(LightColor);
            effect.Parameters["AmbientColor"]?.SetValue(AmbientColor);
            effect.Parameters["LightIntensity"]?.SetValue(LightIntensity);
            
            // Set texture if available
            if (texture != null)
            {
                effect.Parameters["Texture"]?.SetValue(texture);
            }
        }
    }
}