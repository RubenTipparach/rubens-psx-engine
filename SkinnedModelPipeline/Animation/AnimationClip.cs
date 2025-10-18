using System;
using System.Collections.Generic;

namespace rubens_psx_engine.system.animation
{
    /// <summary>
    /// An animation clip is the runtime equivalent of an XNA Framework Content Pipeline AnimationContent object.
    /// It holds all the keyframes needed to describe a single animation.
    /// </summary>
    public class AnimationClip
    {
        /// <summary>
        /// Gets the total length of the animation.
        /// </summary>
        public TimeSpan Duration { get; private set; }

        /// <summary>
        /// Gets a combined list containing all the keyframes for all bones, sorted by time.
        /// </summary>
        public List<Keyframe> Keyframes { get; private set; }

        /// <summary>
        /// Constructs a new animation clip object.
        /// </summary>
        public AnimationClip(TimeSpan duration, List<Keyframe> keyframes)
        {
            Duration = duration;
            Keyframes = keyframes;
        }
    }
}
