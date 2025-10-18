using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.system.animation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace rubens_psx_engine.entities
{
    /// <summary>
    /// Rendering entity with skinned mesh animation support
    /// </summary>
    public class SkinnedRenderingEntity : RenderingEntity
    {
        private AnimationPlayer animationPlayer;
        private SkinningData skinningData;

        public SkinnedRenderingEntity(string modelPath, Material material = null)
            : base(modelPath, null, null, true)
        {
            this.material = material;

            // Extract skinning data from the model
            if (model != null)
            {
                skinningData = model.Tag as SkinningData;
                if (skinningData != null)
                {
                    animationPlayer = new AnimationPlayer(skinningData);
                    Console.WriteLine($"SUCCESS: Loaded skinning data for {modelPath}");
                    Console.WriteLine($"  - Animation clips: {skinningData.AnimationClips.Count}");
                    Console.WriteLine($"  - Bones: {skinningData.BindPose.Count}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"WARNING: Model {modelPath} does not contain skinning data in Tag");
                    Console.WriteLine($"  Model will be rendered as static mesh");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }

            // Setup the material if provided
            if (material != null)
            {
                SetupSkinnedModelEffects();
            }
        }

        private Material material;

        protected override void SetupModelEffects()
        {
            // Don't use the default setup for skinned meshes
            // We'll use SetupSkinnedModelEffects instead
        }

        private void SetupSkinnedModelEffects()
        {
            if (model == null || material == null) return;

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    if (material.Effect != null)
                    {
                        part.Effect = material.Effect;
                    }
                }
            }
        }

        /// <summary>
        /// Play an animation clip by name
        /// </summary>
        public void PlayAnimation(string clipName, bool loop = true)
        {
            if (animationPlayer == null)
            {
                Console.WriteLine("Cannot play animation: AnimationPlayer is null");
                return;
            }

            if (skinningData == null)
            {
                Console.WriteLine("Cannot play animation: SkinningData is null");
                return;
            }

            var clip = skinningData.AnimationClips.FirstOrDefault(c => c.Key == clipName).Value;
            if (clip != null)
            {
                animationPlayer.StartClip(clip, loop);
            }
            else
            {
                Console.WriteLine($"Animation clip '{clipName}' not found. Available clips:");
                foreach (var availableClip in skinningData.AnimationClips.Keys)
                {
                    Console.WriteLine($"  - {availableClip}");
                }
            }
        }

        /// <summary>
        /// Stop the current animation
        /// </summary>
        public void StopAnimation()
        {
            animationPlayer?.Stop();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Update animation
            if (animationPlayer != null)
            {
                animationPlayer.Update(gameTime.ElapsedGameTime, true, Matrix.Identity);
            }
        }

        public override void Draw(GameTime gameTime, Camera camera)
        {
            if (!IsVisible || model == null) return;

            model.CopyAbsoluteBoneTransformsTo(transforms);
            Matrix world = GetWorldMatrix();

            // Get bone transforms for skinning if available, otherwise use identity transforms
            Matrix[] boneTransforms = null;
            if (animationPlayer != null)
            {
                boneTransforms = animationPlayer.GetSkinTransforms();
            }
            else if (skinningData != null)
            {
                // No animation player but we have skinning data - use bind pose
                boneTransforms = skinningData.BindPose.ToArray();
            }
            else
            {
                // No skinning data - use identity matrix for a single bone
                boneTransforms = new Matrix[] { Matrix.Identity };
            }

            foreach (ModelMesh mesh in model.Meshes)
            {
                Matrix meshWorld = transforms[mesh.ParentBone.Index] * world;

                foreach (Effect meshEffect in mesh.Effects)
                {
                    // Apply material if available
                    if (material != null)
                    {
                        material.Apply(camera, meshWorld);

                        // Set bone transforms AFTER applying material using the proper method
                        if (boneTransforms != null && material is UnlitSkinnedMaterial unlitSkinned)
                        {
                            unlitSkinned.SetBoneTransforms(boneTransforms);
                        }
                    }
                    else
                    {
                        // Fallback to basic matrix setup
                        if (meshEffect.Parameters["World"] != null)
                            meshEffect.Parameters["World"].SetValue(meshWorld);
                        if (meshEffect.Parameters["View"] != null)
                            meshEffect.Parameters["View"].SetValue(camera.View);
                        if (meshEffect.Parameters["Projection"] != null)
                            meshEffect.Parameters["Projection"].SetValue(camera.Projection);

                        // Set bone transforms for skinning
                        if (boneTransforms != null && meshEffect.Parameters["Bones"] != null)
                        {
                            meshEffect.Parameters["Bones"].SetValue(boneTransforms);
                        }
                    }
                }

                try
                {
                    mesh.Draw();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SkinnedRenderingEntity: ERROR drawing mesh {mesh.Name}: {ex.Message}");
                    Console.WriteLine($"  Stack trace: {ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Get the current animation player
        /// </summary>
        public AnimationPlayer GetAnimationPlayer()
        {
            return animationPlayer;
        }

        /// <summary>
        /// Get the skinning data
        /// </summary>
        public SkinningData GetSkinningData()
        {
            return skinningData;
        }

        /// <summary>
        /// Check if an animation clip exists
        /// </summary>
        public bool HasAnimationClip(string clipName)
        {
            return skinningData?.AnimationClips.ContainsKey(clipName) ?? false;
        }
    }
}
