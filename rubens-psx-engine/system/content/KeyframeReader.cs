using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using rubens_psx_engine.system.animation;
using System;

namespace rubens_psx_engine.system.content
{
    /// <summary>
    /// Content type reader for Keyframe.
    /// </summary>
    public class KeyframeReader : ContentTypeReader<Keyframe>
    {
        protected override Keyframe Read(ContentReader input, Keyframe existingInstance)
        {
            // Read bone index
            int bone = input.ReadInt32();

            // Read time
            TimeSpan time = input.ReadObject<TimeSpan>();

            // Read transform matrix
            Matrix transform = input.ReadMatrix();

            return new Keyframe(bone, time, transform);
        }
    }
}
