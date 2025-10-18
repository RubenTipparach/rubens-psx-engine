using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.entities;

namespace rubens_psx_engine
{
    /// <summary>
    /// Skinned material with lighting support and customizable ambient color
    /// </summary>
    public class UnlitSkinnedMaterial : Material
    {
        private SkinnedEffect skinnedEffect;

        public float Brightness { get; set; } = 1.2f;

        // RGB ambient light color
        public Vector3 AmbientColor { get; set; } = new Vector3(0.6f, 0.6f, 0.6f);

        // Directional light properties
        public Vector3 LightDirection { get; set; } = Vector3.Normalize(new Vector3(0, -1, 0.5f));
        public Vector3 LightColor { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
        public float LightIntensity { get; set; } = 1.0f;

        public UnlitSkinnedMaterial(string texturePath = null)
            : base(null, texturePath)
        {
            // Create the SkinnedEffect directly
            skinnedEffect = new SkinnedEffect(Globals.screenManager.GraphicsDevice);
            skinnedEffect.PreferPerPixelLighting = false;
            skinnedEffect.DiffuseColor = new Vector3(Brightness);
            skinnedEffect.SpecularColor = Vector3.Zero;
            skinnedEffect.SpecularPower = 0;

            // Enable lighting
            skinnedEffect.EnableDefaultLighting();
            skinnedEffect.AmbientLightColor = AmbientColor;

            // Setup directional light
            skinnedEffect.DirectionalLight0.Enabled = true;
            skinnedEffect.DirectionalLight0.Direction = LightDirection;
            skinnedEffect.DirectionalLight0.DiffuseColor = LightColor * LightIntensity;
            skinnedEffect.DirectionalLight0.SpecularColor = Vector3.Zero;

            skinnedEffect.DirectionalLight1.Enabled = false;
            skinnedEffect.DirectionalLight2.Enabled = false;

            if (texture != null)
            {
                skinnedEffect.Texture = texture;
            }

            // Set the protected effect field from base class
            effect = skinnedEffect;
        }

        public override void Apply(Camera camera, Matrix worldMatrix)
        {
            if (skinnedEffect != null)
            {
                skinnedEffect.World = worldMatrix;
                skinnedEffect.View = camera.View;
                skinnedEffect.Projection = camera.Projection;

                // Apply lighting parameters
                skinnedEffect.AmbientLightColor = AmbientColor;
                skinnedEffect.DirectionalLight0.Direction = LightDirection;
                skinnedEffect.DirectionalLight0.DiffuseColor = LightColor * LightIntensity;
            }
        }

        /// <summary>
        /// Set the bone transforms for skinned animation
        /// </summary>
        public void SetBoneTransforms(Matrix[] boneTransforms)
        {
            if (skinnedEffect != null && boneTransforms != null)
            {
                skinnedEffect.SetBoneTransforms(boneTransforms);
            }
        }

        public new Texture2D GetTexture()
        {
            return texture;
        }

        public void SetTexture(Texture2D newTexture)
        {
            texture = newTexture;
            if (skinnedEffect != null)
            {
                skinnedEffect.Texture = texture;
            }
        }
    }
}
