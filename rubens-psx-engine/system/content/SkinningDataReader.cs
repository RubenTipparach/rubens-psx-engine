using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using rubens_psx_engine.system.animation;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine.system.content
{
    /// <summary>
    /// Content type reader for SkinningData.
    /// This reads SkinningData that was serialized by the SkinnedModelPipeline
    /// and deserializes it into the runtime SkinningData type.
    /// </summary>
    public class SkinningDataReader : ContentTypeReader<SkinningData>
    {
        protected override SkinningData Read(ContentReader input, SkinningData existingInstance)
        {
            // Read animation clips dictionary
            int clipCount = input.ReadInt32();
            var animationClips = new Dictionary<string, AnimationClip>();

            for (int i = 0; i < clipCount; i++)
            {
                string clipName = input.ReadString();
                AnimationClip clip = input.ReadObject<AnimationClip>();
                animationClips.Add(clipName, clip);
            }

            // Read bind pose
            int bindPoseCount = input.ReadInt32();
            var bindPose = new List<Matrix>();
            for (int i = 0; i < bindPoseCount; i++)
            {
                bindPose.Add(input.ReadMatrix());
            }

            // Read inverse bind pose
            int inverseBindPoseCount = input.ReadInt32();
            var inverseBindPose = new List<Matrix>();
            for (int i = 0; i < inverseBindPoseCount; i++)
            {
                inverseBindPose.Add(input.ReadMatrix());
            }

            // Read skeleton hierarchy
            int hierarchyCount = input.ReadInt32();
            var skeletonHierarchy = new List<int>();
            for (int i = 0; i < hierarchyCount; i++)
            {
                skeletonHierarchy.Add(input.ReadInt32());
            }

            return new SkinningData(animationClips, bindPose, inverseBindPose, skeletonHierarchy);
        }
    }
}
