using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine.system.animation
{
    /// <summary>
    /// The animation player is responsible for decoding bone position matrices from an animation clip.
    /// </summary>
    public class AnimationPlayer
    {
        // Information about the currently playing animation clip
        private AnimationClip currentClipValue;
        private TimeSpan currentTimeValue;
        private int currentKeyframe;
        private bool isLooping;
        private bool isPlaying;

        // Current animation transform matrices
        private Matrix[] boneTransforms;
        private Matrix[] worldTransforms;
        private Matrix[] skinTransforms;

        // Backlink to the bind pose and skeleton hierarchy data
        private SkinningData skinningDataValue;

        /// <summary>
        /// Constructs a new animation player.
        /// </summary>
        public AnimationPlayer(SkinningData skinningData)
        {
            if (skinningData == null)
                throw new ArgumentNullException("skinningData");

            skinningDataValue = skinningData;

            boneTransforms = new Matrix[skinningData.BindPose.Count];
            worldTransforms = new Matrix[skinningData.BindPose.Count];
            skinTransforms = new Matrix[skinningData.BindPose.Count];

            isPlaying = false;
        }

        /// <summary>
        /// Gets the current animation clip being decoded.
        /// </summary>
        public AnimationClip CurrentClip
        {
            get { return currentClipValue; }
        }

        /// <summary>
        /// Gets the current play position.
        /// </summary>
        public TimeSpan CurrentTime
        {
            get { return currentTimeValue; }
        }

        /// <summary>
        /// Starts decoding the specified animation clip.
        /// </summary>
        public void StartClip(AnimationClip clip, bool loop = true)
        {
            if (clip == null)
                throw new ArgumentNullException("clip");

            currentClipValue = clip;
            currentTimeValue = TimeSpan.Zero;
            currentKeyframe = 0;
            isLooping = loop;
            isPlaying = true;

            // Initialize bone transforms to the bind pose
            skinningDataValue.BindPose.CopyTo(boneTransforms);
        }

        /// <summary>
        /// Stops the currently playing animation.
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
            currentClipValue = null;
        }

        /// <summary>
        /// Advances the current animation position.
        /// </summary>
        public void Update(TimeSpan time, bool relativeToCurrentTime, Matrix rootTransform)
        {
            if (!isPlaying || currentClipValue == null)
                return;

            // Update the animation position
            if (relativeToCurrentTime)
            {
                time += currentTimeValue;

                // If we reached the end, loop back to the start or stop
                while (time >= currentClipValue.Duration)
                {
                    if (isLooping)
                    {
                        time -= currentClipValue.Duration;

                        // Reset to the beginning if we're looping
                        if ((time < TimeSpan.Zero) || (time >= currentClipValue.Duration))
                            time = TimeSpan.Zero;

                        currentKeyframe = 0;
                    }
                    else
                    {
                        // Stop at the end if not looping
                        time = currentClipValue.Duration;
                        isPlaying = false;
                        break;
                    }
                }
            }

            if ((time < TimeSpan.Zero) || (time >= currentClipValue.Duration))
                throw new ArgumentOutOfRangeException("time");

            // If the position moved backwards, reset the keyframe index
            if (time < currentTimeValue)
            {
                currentKeyframe = 0;
                skinningDataValue.BindPose.CopyTo(boneTransforms);
            }

            currentTimeValue = time;

            // Read keyframe matrices
            List<Keyframe> keyframes = currentClipValue.Keyframes;

            while (currentKeyframe < keyframes.Count)
            {
                Keyframe keyframe = keyframes[currentKeyframe];

                // Stop when we've read up to the current time position
                if (keyframe.Time > currentTimeValue)
                    break;

                // Use this keyframe
                boneTransforms[keyframe.Bone] = keyframe.Transform;

                currentKeyframe++;
            }

            // Root bone transforms
            if (boneTransforms.Length > 0)
            {
                worldTransforms[0] = boneTransforms[0] * rootTransform;
            }

            // Child bone transforms
            for (int bone = 1; bone < worldTransforms.Length; bone++)
            {
                int parentBone = skinningDataValue.SkeletonHierarchy[bone];

                worldTransforms[bone] = boneTransforms[bone] * worldTransforms[parentBone];
            }

            // Compute the final skin transforms
            for (int bone = 0; bone < skinTransforms.Length; bone++)
            {
                skinTransforms[bone] = skinningDataValue.InverseBindPose[bone] * worldTransforms[bone];
            }
        }

        /// <summary>
        /// Gets the current bone transform matrices, relative to their parent bones.
        /// </summary>
        public Matrix[] GetBoneTransforms()
        {
            return boneTransforms;
        }

        /// <summary>
        /// Gets the current bone transform matrices, in absolute format.
        /// </summary>
        public Matrix[] GetWorldTransforms()
        {
            return worldTransforms;
        }

        /// <summary>
        /// Gets the current bone transform matrices, skinned ready for rendering.
        /// </summary>
        public Matrix[] GetSkinTransforms()
        {
            return skinTransforms;
        }
    }
}
