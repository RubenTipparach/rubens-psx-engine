using Microsoft.Xna.Framework.Content;
using rubens_psx_engine.system.animation;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine.system.content
{
    /// <summary>
    /// Content type reader for AnimationClip.
    /// </summary>
    public class AnimationClipReader : ContentTypeReader<AnimationClip>
    {
        protected override AnimationClip Read(ContentReader input, AnimationClip existingInstance)
        {
            // Read duration
            TimeSpan duration = input.ReadObject<TimeSpan>();

            // Read keyframes
            int keyframeCount = input.ReadInt32();
            var keyframes = new List<Keyframe>();
            for (int i = 0; i < keyframeCount; i++)
            {
                keyframes.Add(input.ReadObject<Keyframe>());
            }

            return new AnimationClip(duration, keyframes);
        }
    }
}
