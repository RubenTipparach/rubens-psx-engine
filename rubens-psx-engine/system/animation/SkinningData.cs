using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace rubens_psx_engine.system.animation
{
    /// <summary>
    /// Combines all the data needed to render and animate a skinned object.
    /// This is typically stored in the Tag property of the Model being animated.
    /// </summary>
    public class SkinningData
    {
        /// <summary>
        /// Gets a dictionary of animation clips. These are stored by name in a dictionary,
        /// so they can be looked up by name at runtime.
        /// </summary>
        public Dictionary<string, AnimationClip> AnimationClips { get; private set; }

        /// <summary>
        /// Gets the bind pose transforms. This is the skeleton in its default position,
        /// as exported from the modeling package.
        /// </summary>
        public List<Matrix> BindPose { get; private set; }

        /// <summary>
        /// Gets the inverse bind pose transforms. These are the inverse of the BindPose
        /// transforms, and are used to transform vertices from bone space into model space.
        /// </summary>
        public List<Matrix> InverseBindPose { get; private set; }

        /// <summary>
        /// Gets the skeleton hierarchy. This is a list of bone indices showing the
        /// parent of each bone. The root bone has a parent index of -1.
        /// </summary>
        public List<int> SkeletonHierarchy { get; private set; }

        /// <summary>
        /// Constructs a new skinning data object.
        /// </summary>
        public SkinningData(Dictionary<string, AnimationClip> animationClips,
                           List<Matrix> bindPose,
                           List<Matrix> inverseBindPose,
                           List<int> skeletonHierarchy)
        {
            AnimationClips = animationClips;
            BindPose = bindPose;
            InverseBindPose = inverseBindPose;
            SkeletonHierarchy = skeletonHierarchy;
        }
    }
}
