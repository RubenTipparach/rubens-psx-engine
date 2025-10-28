using Microsoft.Xna.Framework;
using anakinsoft.entities;
using anakinsoft.system;
using rubens_psx_engine.entities;

namespace anakinsoft.game.scenes
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
}
