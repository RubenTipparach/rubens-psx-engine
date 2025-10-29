using Microsoft.Xna.Framework;
using anakinsoft.entities;
using System;

namespace anakinsoft.game.scenes.lounge.evidence
{
    /// <summary>
    /// Represents an item that can be picked up in the world
    /// </summary>
    public class InteractableItem : InteractableObject
    {
        public string Name { get; private set; }
        public InventoryItem Item { get; private set; }
        public bool IsCollected { get; private set; }

        public event Action<InteractableItem> OnItemCollected;

        public InteractableItem(
            string name,
            Vector3 position,
            Vector3 cameraInteractionPosition,
            Vector3 cameraInteractionLookAt,
            InventoryItem item)
        {
            Name = name;
            this.position = position;
            Item = item;
            IsCollected = false;
            interactionDistance = 100f;
        }

        /// <summary>
        /// Collect this item
        /// </summary>
        public void Collect()
        {
            if (IsCollected)
            {
                Console.WriteLine($"InteractableItem: {Name} already collected");
                return;
            }

            IsCollected = true;
            Console.WriteLine($"InteractableItem: Collected {Name}");
            OnItemCollected?.Invoke(this);
        }

        /// <summary>
        /// Handle interaction - collect the item
        /// </summary>
        protected override void OnInteractAction()
        {
            Collect();
        }

        /// <summary>
        /// Override InteractionPrompt to update based on collection state
        /// </summary>
        public override string InteractionPrompt
        {
            get
            {
                if (IsCollected)
                    return "";  // Don't show prompt if already collected

                return $"[E] Pick up {Name}";
            }
        }
    }
}
