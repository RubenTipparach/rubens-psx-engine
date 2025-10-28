using Microsoft.Xna.Framework;
using rubens_psx_engine;
using rubens_psx_engine.Extensions;
using rubens_psx_engine.system;
using rubens_psx_engine.entities;
using anakinsoft.system;
using anakinsoft.system.physics;
using anakinsoft.entities;
using anakinsoft.utilities;
using BepuPhysics;
using BepuPhysics.Collidables;
using System;
using System.Linq;

namespace rubens_psx_engine
{
    /// <summary>
    /// Helper class for loading and initializing characters in The Lounge scene
    /// </summary>
    public class LoungeCharacterLoader
    {
        private readonly PhysicsSystem physicsSystem;
        private readonly InteractionSystem interactionSystem;
        private readonly float levelScale;

        public LoungeCharacterLoader(PhysicsSystem physics, InteractionSystem interaction, float scale)
        {
            physicsSystem = physics;
            interactionSystem = interaction;
            levelScale = scale;
        }

        /// <summary>
        /// Create a character with model, collider, and interaction
        /// </summary>
        public CharacterInstance CreateCharacter(
            string name,
            Vector3 position,
            Quaternion rotation,
            Vector3 cameraInteractionPos,
            Vector3 cameraLookAt,
            string modelPath,
            string materialTexture,
            float scale,
            Vector3 colliderSize,
            bool isSitting = false)
        {
            Console.WriteLine($"\n========================================");
            Console.WriteLine($"LOADING CHARACTER: {name}");
            Console.WriteLine($"========================================");

            var instance = new CharacterInstance();

            // Create interactable
            instance.Interaction = new InteractableCharacter(name, position, cameraInteractionPos, cameraLookAt);
            interactionSystem.RegisterInteractable(instance.Interaction);

            // Create physics collider
            var colliderCenter = CreateCollider(position, colliderSize, out var staticHandle);
            instance.Interaction.SetStaticHandle(staticHandle);
            instance.ColliderCenter = colliderCenter;
            instance.ColliderSize = colliderSize;

            Console.WriteLine($"Character created at position: {position}");
            Console.WriteLine($"Interaction camera position: {cameraInteractionPos}");
            Console.WriteLine($"Collider at {colliderCenter} (size: {colliderSize.X}x{colliderSize.Y}x{colliderSize.Z})");

            // Create character model
            var material = new UnlitSkinnedMaterial(materialTexture, "shaders/surface/SkinnedVertexLit", useDefault: false);
            material.AmbientColor = new Vector3(0.2f, 0.2f, 0.3f);
            material.EmissiveColor = new Vector3(0.1f, 0.1f, 0.15f);
            material.LightDirection = Vector3.Normalize(new Vector3(0.3f, -1, -0.5f));
            material.LightColor = new Vector3(1.0f, 0.95f, 0.9f);
            material.LightIntensity = 0.9f;

            instance.Model = new SkinnedRenderingEntity(modelPath, material);
            instance.Model.Position = position;
            instance.Model.Scale = Vector3.One * scale * levelScale;
            instance.Model.Rotation = rotation;
            instance.Model.IsVisible = true;

            // Play idle animation
            if (instance.Model is SkinnedRenderingEntity skinned)
            {
                var skinData = skinned.GetSkinningData();
                if (skinData != null && skinData.AnimationClips.Count > 0)
                {
                    var firstClipName = skinData.AnimationClips.Keys.First();
                    skinned.PlayAnimation(firstClipName, loop: true);
                }
            }

            Console.WriteLine($"========================================\n");
            return instance;
        }

        /// <summary>
        /// Create physics collider for character
        /// </summary>
        private Vector3 CreateCollider(Vector3 position, Vector3 size, out StaticHandle handle)
        {
            var boxShape = new Box(size.X, size.Y, size.Z);
            var shapeIndex = physicsSystem.Simulation.Shapes.Add(boxShape);

            // Move the collider up so the bottom touches the floor
            var colliderCenter = position + new Vector3(0, size.Y / 2f, 0);

            // Create static body
            handle = physicsSystem.Simulation.Statics.Add(new StaticDescription(
                colliderCenter.ToVector3N(),
                Quaternion.Identity.ToQuaternionN(),
                shapeIndex));

            return colliderCenter;
        }
    }

    /// <summary>
    /// Represents a complete character instance with all components
    /// </summary>
    public class CharacterInstance
    {
        public InteractableCharacter Interaction { get; set; }
        public SkinnedRenderingEntity Model { get; set; }
        public Vector3 ColliderCenter { get; set; }
        public Vector3 ColliderSize { get; set; }
    }
}
