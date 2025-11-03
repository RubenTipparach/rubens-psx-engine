using System;
using Microsoft.Xna.Framework;

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
        public float[] StartPosition { get; set; } = { 0f, 0f, 7000f }; // X, Y, Z

        // End position (near window)
        public float[] EndPosition { get; set; } = { 0, 0f, 3000f }; // X, Y, Z

        // Helper methods to convert arrays to Vector3
        public Vector3 GetStartPosition()
        {
            return new Vector3(StartPosition[0], StartPosition[1], StartPosition[2]);
        }

        public Vector3 GetEndPosition()
        {
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
    /// Manager for accessing Odysseus ship configuration (hardcoded values)
    /// </summary>
    public static class OdysseusShipConfigManager
    {
        private static OdysseusShipConfig config;

        public static OdysseusShipConfig Config
        {
            get
            {
                if (config == null)
                {
                    config = new OdysseusShipConfig();
                    Console.WriteLine("[OdysseusShipConfig] Using hardcoded configuration values");
                }
                return config;
            }
        }
    }
}
