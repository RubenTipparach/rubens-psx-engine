using System;
using System.IO;
using Microsoft.Xna.Framework;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace rubens_psx_engine.system.config
{
    /// <summary>
    /// Configuration for Odysseus ship finale animation
    /// </summary>
    public class OdysseusShipConfig
    {
        // Ship model settings
        public float Scale { get; set; } = 2.0f;
        public float[] Rotation { get; set; } = { 0f, 0f, 0f }; // Yaw, Pitch, Roll in degrees

        // Animation settings
        public float ApproachDuration { get; set; } = 8.0f; // seconds

        // Start position (far away)
        public float[] StartPosition { get; set; } = { 0f, 50f, -8000f }; // X, Y, Z

        // End position (near window)
        public float[] EndPosition { get; set; } = { -100f, 30f, -400f }; // X, Y, Z

        // Helper methods to convert arrays to Vector3
        public Vector3 GetStartPosition()
        {
            if (StartPosition == null || StartPosition.Length != 3)
                return new Vector3(0f, 50f, -8000f);
            return new Vector3(StartPosition[0], StartPosition[1], StartPosition[2]);
        }

        public Vector3 GetEndPosition()
        {
            if (EndPosition == null || EndPosition.Length != 3)
                return new Vector3(-100f, 30f, -400f);
            return new Vector3(EndPosition[0], EndPosition[1], EndPosition[2]);
        }

        public Vector3 GetRotation()
        {
            if (Rotation == null || Rotation.Length != 3)
                return Vector3.Zero;
            return new Vector3(
                MathHelper.ToRadians(Rotation[0]), // Yaw
                MathHelper.ToRadians(Rotation[1]), // Pitch
                MathHelper.ToRadians(Rotation[2])  // Roll
            );
        }
    }

    /// <summary>
    /// Manager for loading and accessing Odysseus ship configuration
    /// </summary>
    public static class OdysseusShipConfigManager
    {
        private static OdysseusShipConfig config;
        private const string ConfigFileName = "odysseus_ship.yml";

        public static OdysseusShipConfig Config
        {
            get
            {
                if (config == null)
                {
                    LoadConfig();
                }
                return config;
            }
        }

        public static void LoadConfig()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Data", ConfigFileName);

            try
            {
                if (File.Exists(configPath))
                {
                    string yaml = File.ReadAllText(configPath);
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(UnderscoredNamingConvention.Instance)
                        .Build();

                    config = deserializer.Deserialize<OdysseusShipConfig>(yaml);
                    Console.WriteLine($"[OdysseusShipConfig] Loaded from {configPath}");
                }
                else
                {
                    Console.WriteLine($"[OdysseusShipConfig] Config file not found at {configPath}, using defaults");
                    config = new OdysseusShipConfig();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OdysseusShipConfig] Error loading config: {ex.Message}");
                config = new OdysseusShipConfig();
            }
        }

        public static void SaveConfig()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Data", ConfigFileName);

            try
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

                string yaml = serializer.Serialize(config ?? new OdysseusShipConfig());
                File.WriteAllText(configPath, yaml);
                Console.WriteLine($"[OdysseusShipConfig] Saved to {configPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OdysseusShipConfig] Error saving config: {ex.Message}");
            }
        }
    }
}
