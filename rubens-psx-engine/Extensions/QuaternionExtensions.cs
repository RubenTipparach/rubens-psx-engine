using Microsoft.Xna.Framework;
using System;

namespace rubens_psx_engine.Extensions
{
    public static class QuaternionExtensions
    {
        /// <summary>
        /// Creates a quaternion from yaw, pitch, and roll angles specified in degrees.
        /// </summary>
        /// <param name="yawDegrees">Yaw angle in degrees</param>
        /// <param name="pitchDegrees">Pitch angle in degrees</param>
        /// <param name="rollDegrees">Roll angle in degrees</param>
        /// <returns>A quaternion representing the specified rotation</returns>
        public static Quaternion CreateFromYawPitchRollDegrees(float yawDegrees, float pitchDegrees, float rollDegrees)
        {
            float yawRadians = MathHelper.ToRadians(yawDegrees);
            float pitchRadians = MathHelper.ToRadians(pitchDegrees);
            float rollRadians = MathHelper.ToRadians(rollDegrees);
            
            return Quaternion.CreateFromYawPitchRoll(yawRadians, pitchRadians, rollRadians);
        }
    }
    
    public static class FloatExtensions
    {
        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="degrees">Angle in degrees</param>
        /// <returns>Angle in radians</returns>
        public static float ToRadians(this float degrees)
        {
            return MathHelper.ToRadians(degrees);
        }
    }
}