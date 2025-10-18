using Microsoft.Xna.Framework;
using System;

namespace rubens_psx_engine.system.animation
{
    /// <summary>
    /// Describes the position of a single bone at a single point in time.
    /// </summary>
    public class Keyframe
    {
        /// <summary>
        /// Gets the index of the target bone that is animated by this keyframe.
        /// </summary>
        public int Bone { get; private set; }

        /// <summary>
        /// Gets the time offset from the start of the animation to this keyframe.
        /// </summary>
        public TimeSpan Time { get; private set; }

        /// <summary>
        /// Gets the bone transform for this keyframe.
        /// </summary>
        public Matrix Transform { get; private set; }

        /// <summary>
        /// Constructs a new keyframe object.
        /// </summary>
        public Keyframe(int bone, TimeSpan time, Matrix transform)
        {
            Bone = bone;
            Time = time;
            Transform = transform;
        }
    }
}
