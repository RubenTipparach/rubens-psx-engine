using Microsoft.Xna.Framework;
using anakinsoft.entities;
using anakinsoft.system;
using rubens_psx_engine.entities;
using rubens_psx_engine.Extensions;
using System;
using System.Linq;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// Data container for a character in The Lounge scene
    /// Stores the interactable character, model, and collider dimensions
    /// </summary>
    public class LoungeCharacterData
    {
        public string Name { get; set; }
        public InteractableCharacter Interaction { get; set; }
        public SkinnedRenderingEntity Model { get; set; }

        // Collider dimensions (shared between physics and debug visualization)
        public float ColliderWidth { get; set; }
        public float ColliderHeight { get; set; }
        public float ColliderDepth { get; set; }
        public Vector3 ColliderCenter { get; set; }

        public LoungeCharacterData(string name)
        {
            Name = name;
        }

        public bool IsSpawned => Interaction != null && Model != null;
    }

    /// <summary>
    /// Configuration for spawning a character
    /// </summary>
    public class CharacterSpawnConfig
    {
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 CameraPosition { get; set; }
        public Vector3 CameraLookAt { get; set; }
        public string ModelPath { get; set; } = "models/characters/alien-2";
        public string TexturePath { get; set; } = "textures/prototype/grass";
        public float Scale { get; set; } = 0.25f;
        public float RotationYaw { get; set; } = -90f;
        public float RotationPitch { get; set; } = 0f;
        public float RotationRoll { get; set; } = 0f;
        public float ColliderWidth { get; set; } = 15f;
        public float ColliderHeight { get; set; } = 30f;
        public float ColliderDepth { get; set; } = 15f;

        // Lighting settings
        public Vector3 AmbientColor { get; set; } = new Vector3(0.1f, 0.1f, 0.2f);
        public Vector3 EmissiveColor { get; set; } = new Vector3(0.1f, 0.1f, 0.2f);
        public Vector3 LightDirection { get; set; } = new Vector3(0.3f, -1, -0.5f);
        public Vector3 LightColor { get; set; } = new Vector3(1.0f, 0.95f, 0.9f);
        public float LightIntensity { get; set; } = 0.9f;

        /// <summary>
        /// Create config from YAML CharacterConfig
        /// </summary>
        public static CharacterSpawnConfig FromYaml(CharacterConfig yamlConfig, float levelScale)
        {
            return new CharacterSpawnConfig
            {
                Name = yamlConfig.name,
                Position = yamlConfig.position.ToVector3() * levelScale,
                CameraPosition = yamlConfig.camera_position.ToVector3() * levelScale,
                CameraLookAt = yamlConfig.camera_look_at.ToVector3(),
                ModelPath = yamlConfig.model,
                Scale = yamlConfig.scale,
                RotationYaw = yamlConfig.rotation.yaw,
                RotationPitch = yamlConfig.rotation.pitch,
                RotationRoll = yamlConfig.rotation.roll,
                ColliderWidth = yamlConfig.collider.width * levelScale,
                ColliderHeight = yamlConfig.collider.height * levelScale,
                ColliderDepth = yamlConfig.collider.depth * levelScale
            };
        }
    }
}
