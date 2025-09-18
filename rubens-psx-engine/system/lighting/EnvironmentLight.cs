using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace rubens_psx_engine.system.lighting
{
    public class EnvironmentLight
    {
        // Directional light properties
        public Vector3 DirectionalLightDirection { get; set; }
        public Color DirectionalLightColor { get; set; }
        public float DirectionalLightIntensity { get; set; }

        // Ambient light properties
        public Color AmbientLightColor { get; set; }
        public float AmbientLightIntensity { get; set; }

        // Fog properties (optional for atmosphere)
        public bool FogEnabled { get; set; }
        public Color FogColor { get; set; }
        public float FogStart { get; set; }
        public float FogEnd { get; set; }

        public EnvironmentLight()
        {
            // Default values for a typical outdoor scene
            DirectionalLightDirection = Vector3.Normalize(new Vector3(-0.5f, -1.0f, -0.5f));
            DirectionalLightColor = Color.White;
            DirectionalLightIntensity = 1.0f;

            AmbientLightColor = new Color(0.2f, 0.2f, 0.3f); // Slightly blue ambient
            AmbientLightIntensity = 1.0f;

            FogEnabled = false;
            FogColor = new Color(0.5f, 0.6f, 0.7f);
            FogStart = 50.0f;
            FogEnd = 200.0f;
        }

        public void ApplyToEffect(Effect effect)
        {
            // Apply directional light parameters
            effect.Parameters["LightDirection"]?.SetValue(DirectionalLightDirection);
            effect.Parameters["LightColor"]?.SetValue(DirectionalLightColor.ToVector3());
            effect.Parameters["LightIntensity"]?.SetValue(DirectionalLightIntensity);

            // Apply ambient light parameters
            effect.Parameters["AmbientColor"]?.SetValue(AmbientLightColor.ToVector3() * AmbientLightIntensity);

            // Apply fog parameters if supported
            if (FogEnabled)
            {
                effect.Parameters["FogEnabled"]?.SetValue(FogEnabled);
                effect.Parameters["FogColor"]?.SetValue(FogColor.ToVector3());
                effect.Parameters["FogStart"]?.SetValue(FogStart);
                effect.Parameters["FogEnd"]?.SetValue(FogEnd);
            }
        }

        // Preset lighting scenarios
        public static EnvironmentLight CreateDaylight()
        {
            return new EnvironmentLight
            {
                DirectionalLightDirection = Vector3.Normalize(new Vector3(-0.3f, -0.8f, -0.5f)),
                DirectionalLightColor = new Color(1.0f, 0.95f, 0.8f), // Warm sunlight
                DirectionalLightIntensity = 1.2f,
                AmbientLightColor = new Color(0.4f, 0.5f, 0.6f), // Sky blue ambient
                AmbientLightIntensity = 0.3f
            };
        }

        public static EnvironmentLight CreateSunset()
        {
            return new EnvironmentLight
            {
                DirectionalLightDirection = Vector3.Normalize(new Vector3(-0.7f, -0.2f, -0.7f)),
                DirectionalLightColor = new Color(1.0f, 0.6f, 0.3f), // Orange sunset
                DirectionalLightIntensity = 0.8f,
                AmbientLightColor = new Color(0.5f, 0.3f, 0.4f), // Purple/pink ambient
                AmbientLightIntensity = 0.4f
            };
        }

        public static EnvironmentLight CreateNight()
        {
            return new EnvironmentLight
            {
                DirectionalLightDirection = Vector3.Normalize(new Vector3(0, -1, 0)), // Moonlight from above
                DirectionalLightColor = new Color(0.4f, 0.4f, 0.6f), // Blue moonlight
                DirectionalLightIntensity = 0.3f,
                AmbientLightColor = new Color(0.1f, 0.1f, 0.2f), // Dark blue ambient
                AmbientLightIntensity = 0.5f
            };
        }

        public static EnvironmentLight CreateOvercast()
        {
            return new EnvironmentLight
            {
                DirectionalLightDirection = Vector3.Normalize(new Vector3(0, -1, 0)),
                DirectionalLightColor = Color.Gray,
                DirectionalLightIntensity = 0.6f,
                AmbientLightColor = new Color(0.5f, 0.5f, 0.5f),
                AmbientLightIntensity = 0.7f,
                FogEnabled = true,
                FogColor = new Color(0.7f, 0.7f, 0.7f),
                FogStart = 30.0f,
                FogEnd = 150.0f
            };
        }
    }
}