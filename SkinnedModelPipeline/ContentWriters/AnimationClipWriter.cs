using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using rubens_psx_engine.system.animation;

namespace SkinnedModelPipeline.ContentWriters
{
    [ContentTypeWriter]
    public class AnimationClipWriter : ContentTypeWriter<AnimationClip>
    {
        protected override void Write(ContentWriter output, AnimationClip value)
        {
            // Write duration
            output.WriteObject(value.Duration);

            // Write keyframes
            output.Write(value.Keyframes.Count);
            foreach (var keyframe in value.Keyframes)
            {
                output.WriteObject(keyframe);
            }
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "rubens_psx_engine.system.content.AnimationClipReader, derelict";
        }
    }
}
