using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using rubens_psx_engine.system.animation;

namespace SkinnedModelPipeline.ContentWriters
{
    [ContentTypeWriter]
    public class SkinningDataWriter : ContentTypeWriter<SkinningData>
    {
        protected override void Write(ContentWriter output, SkinningData value)
        {
            // Write animation clips dictionary
            output.Write(value.AnimationClips.Count);
            foreach (var kvp in value.AnimationClips)
            {
                output.Write(kvp.Key);
                output.WriteObject(kvp.Value);
            }

            // Write bind pose
            output.Write(value.BindPose.Count);
            foreach (var matrix in value.BindPose)
            {
                output.Write(matrix);
            }

            // Write inverse bind pose
            output.Write(value.InverseBindPose.Count);
            foreach (var matrix in value.InverseBindPose)
            {
                output.Write(matrix);
            }

            // Write skeleton hierarchy
            output.Write(value.SkeletonHierarchy.Count);
            foreach (var boneIndex in value.SkeletonHierarchy)
            {
                output.Write(boneIndex);
            }
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            // Return the fully qualified name of the runtime reader
            return "rubens_psx_engine.system.content.SkinningDataReader, derelict";
        }
    }
}
