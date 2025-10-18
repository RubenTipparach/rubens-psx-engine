using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using rubens_psx_engine.system.animation;

namespace SkinnedModelPipeline.ContentWriters
{
    [ContentTypeWriter]
    public class KeyframeWriter : ContentTypeWriter<Keyframe>
    {
        protected override void Write(ContentWriter output, Keyframe value)
        {
            // Write bone index
            output.Write(value.Bone);

            // Write time
            output.WriteObject(value.Time);

            // Write transform matrix
            output.Write(value.Transform);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "rubens_psx_engine.system.content.KeyframeReader, derelict";
        }
    }
}
