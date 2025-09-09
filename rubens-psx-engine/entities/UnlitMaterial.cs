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
            
            // Set texture if available
            if (texture != null)
            {
                effect.Parameters["Texture"]?.SetValue(texture);
            }
        }
    }
}