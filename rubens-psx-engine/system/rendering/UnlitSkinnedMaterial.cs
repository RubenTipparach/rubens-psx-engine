using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.entities;

namespace rubens_psx_engine
{
    /// <summary>
    /// Skinned material with lighting support and customizable ambient color.
    /// Supports both default SkinnedEffect and custom shader effects.
    /// </summary>
    public class UnlitSkinnedMaterial : Material
    {
        private SkinnedEffect skinnedEffect;
        private Effect customEffect;
        private bool useDefaultEffect;
        private Matrix[] boneTransforms;

        public float Brightness { get; set; } = 1.2f;

        // RGB ambient light color - this illuminates all surfaces equally
        public Vector3 AmbientColor { get; set; } = new Vector3(0.8f, 0.8f, 0.8f);

        // Emissive color - adds brightness to dark areas (unlit surfaces)
        public Vector3 EmissiveColor { get; set; } = new Vector3(0.3f, 0.3f, 0.3f);

        // Directional light properties
        public Vector3 LightDirection { get; set; } = Vector3.Normalize(new Vector3(0, -1, 0.5f));
        public Vector3 LightColor { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
        public float LightIntensity { get; set; } = 0.6f;

        /// <summary>
        /// Create a skinned material with optional custom shader
        /// </summary>
        /// <param name="texturePath">Path to the texture</param>
        /// <param name="customEffectPath">Path to custom shader effect (null to use default)</param>
        /// <param name="useDefault">If true, uses MonoGame's default SkinnedEffect regardless of customEffectPath</param>
        public UnlitSkinnedMaterial(string texturePath = null, string customEffectPath = null, bool useDefault = true)
            : base(null, texturePath)
        {
            useDefaultEffect = useDefault || string.IsNullOrEmpty(customEffectPath);

            if (useDefaultEffect)
            {
                // Create the default SkinnedEffect
                skinnedEffect = new SkinnedEffect(Globals.screenManager.GraphicsDevice);
                skinnedEffect.PreferPerPixelLighting = true;
                skinnedEffect.DiffuseColor = new Vector3(Brightness);
                skinnedEffect.EmissiveColor = EmissiveColor;
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
            else
            {
                // Load custom effect
                LoadEffect(customEffectPath);
                customEffect = effect;

                if (texture != null && customEffect != null)
                {
                    // Set texture on custom effect if it has a Texture parameter
                    var textureParam = customEffect.Parameters["Texture"];
                    if (textureParam != null)
                    {
                        textureParam.SetValue(texture);
                    }
                }
            }
        }

        public override void Apply(Camera camera, Matrix worldMatrix)
        {
            if (useDefaultEffect && skinnedEffect != null)
            {
                skinnedEffect.World = worldMatrix;
                skinnedEffect.View = camera.View;
                skinnedEffect.Projection = camera.Projection;

                // Apply lighting parameters
                skinnedEffect.AmbientLightColor = AmbientColor;
                skinnedEffect.EmissiveColor = EmissiveColor;
                skinnedEffect.DirectionalLight0.Direction = LightDirection;
                skinnedEffect.DirectionalLight0.DiffuseColor = LightColor * LightIntensity;
            }
            else if (customEffect != null)
            {
                // Apply custom effect parameters
                customEffect.Parameters["World"]?.SetValue(worldMatrix);
                customEffect.Parameters["View"]?.SetValue(camera.View);
                customEffect.Parameters["Projection"]?.SetValue(camera.Projection);

                // Lighting parameters for custom shader
                customEffect.Parameters["AmbientColor"]?.SetValue(AmbientColor);
                customEffect.Parameters["EmissiveColor"]?.SetValue(EmissiveColor);
                customEffect.Parameters["LightDirection"]?.SetValue(LightDirection);
                customEffect.Parameters["LightColor"]?.SetValue(LightColor * LightIntensity);

                // Apply bone transforms if available
                if (boneTransforms != null && boneTransforms.Length > 0)
                {
                    customEffect.Parameters["Bones"]?.SetValue(boneTransforms);
                }
            }
        }

        /// <summary>
        /// Set the bone transforms for skinned animation
        /// </summary>
        public void SetBoneTransforms(Matrix[] transforms)
        {
            boneTransforms = transforms;

            if (useDefaultEffect && skinnedEffect != null && transforms != null)
            {
                skinnedEffect.SetBoneTransforms(transforms);
            }
            else if (customEffect != null && transforms != null)
            {
                // For custom effects, store transforms to apply in Apply() method
                // This is because we can't call SetBoneTransforms on a generic Effect
                customEffect.Parameters["Bones"]?.SetValue(transforms);
            }
        }

        public new Texture2D GetTexture()
        {
            return texture;
        }

        public void SetTexture(Texture2D newTexture)
        {
            texture = newTexture;

            if (useDefaultEffect && skinnedEffect != null)
            {
                skinnedEffect.Texture = texture;
            }
            else if (customEffect != null)
            {
                customEffect.Parameters["Texture"]?.SetValue(texture);
            }
        }
    }
}
