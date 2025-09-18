using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace rubens_psx_engine.system.lighting
{
    public class PointLight
    {
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Color Color { get; set; }
        public float Range { get; set; }
        public float Intensity { get; set; }
        public bool IsEnabled { get; set; }

        // Optional properties for advanced lighting
        public float LinearAttenuation { get; set; }
        public float QuadraticAttenuation { get; set; }

        // For animated/pulsing lights
        public bool IsPulsing { get; set; }
        public float PulseSpeed { get; set; }
        public float PulseMinIntensity { get; set; }
        public float PulseMaxIntensity { get; set; }
        private float pulseTime;

        // For flickering lights (like fire)
        public bool IsFlickering { get; set; }
        public float FlickerSpeed { get; set; }
        public float FlickerIntensity { get; set; }
        private Random flickerRandom;

        public PointLight(string name = "PointLight")
        {
            Name = name;
            Position = Vector3.Zero;
            Color = Color.White;
            Range = 50.0f;
            Intensity = 1.0f;
            IsEnabled = true;

            // Default attenuation (constant = 1 is implicit)
            LinearAttenuation = 0.09f;
            QuadraticAttenuation = 0.032f;

            // Pulse settings
            IsPulsing = false;
            PulseSpeed = 1.0f;
            PulseMinIntensity = 0.5f;
            PulseMaxIntensity = 1.0f;
            pulseTime = 0.0f;

            // Flicker settings
            IsFlickering = false;
            FlickerSpeed = 10.0f;
            FlickerIntensity = 0.2f;
            flickerRandom = new Random();
        }

        public void Update(GameTime gameTime)
        {
            if (!IsEnabled) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update pulsing
            if (IsPulsing)
            {
                pulseTime += deltaTime * PulseSpeed;
                float pulse = (float)(Math.Sin(pulseTime) * 0.5 + 0.5);
                Intensity = MathHelper.Lerp(PulseMinIntensity, PulseMaxIntensity, pulse);
            }

            // Update flickering
            if (IsFlickering)
            {
                float flicker = (float)(flickerRandom.NextDouble() * 2.0 - 1.0) * FlickerIntensity;
                Intensity = Math.Max(0, Intensity + flicker * deltaTime * FlickerSpeed);
            }
        }

        public float GetAttenuationAt(Vector3 worldPosition)
        {
            if (!IsEnabled) return 0.0f;

            float distance = Vector3.Distance(Position, worldPosition);

            // If beyond range, no light contribution
            if (distance > Range) return 0.0f;

            // Calculate attenuation using standard formula
            float attenuation = 1.0f / (1.0f + LinearAttenuation * distance + QuadraticAttenuation * distance * distance);

            // Apply range falloff
            float rangeFalloff = 1.0f - (distance / Range);
            rangeFalloff = Math.Max(0, rangeFalloff);

            return attenuation * rangeFalloff * Intensity;
        }

        public Vector3 GetLightDirection(Vector3 worldPosition)
        {
            return Vector3.Normalize(Position - worldPosition);
        }

        // Factory methods for common light types
        public static PointLight CreateTorch(Vector3 position)
        {
            return new PointLight("Torch")
            {
                Position = position,
                Color = new Color(1.0f, 0.6f, 0.2f), // Orange
                Range = 20.0f,
                Intensity = 1.5f,
                IsFlickering = true,
                FlickerSpeed = 8.0f,
                FlickerIntensity = 0.3f
            };
        }

        public static PointLight CreateLamp(Vector3 position)
        {
            return new PointLight("Lamp")
            {
                Position = position,
                Color = new Color(1.0f, 0.95f, 0.8f), // Warm white
                Range = 30.0f,
                Intensity = 1.0f
            };
        }

        public static PointLight CreateMagicOrb(Vector3 position, Color color)
        {
            return new PointLight("Magic Orb")
            {
                Position = position,
                Color = color,
                Range = 25.0f,
                Intensity = 2.0f,
                IsPulsing = true,
                PulseSpeed = 2.0f,
                PulseMinIntensity = 1.0f,
                PulseMaxIntensity = 2.5f
            };
        }

        public static PointLight CreateExplosion(Vector3 position)
        {
            return new PointLight("Explosion")
            {
                Position = position,
                Color = new Color(1.0f, 0.8f, 0.3f),
                Range = 50.0f,
                Intensity = 5.0f
            };
        }
    }
}