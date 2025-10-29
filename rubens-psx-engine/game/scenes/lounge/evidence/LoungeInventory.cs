using System;

namespace anakinsoft.game.scenes.lounge.evidence
{
    /// <summary>
    /// Represents an item that can be collected
    /// </summary>
    public class InventoryItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public InventoryItem(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Simple inventory system - can only hold 1 item at a time
    /// </summary>
    public class LoungeInventory
    {
        private InventoryItem currentItem = null;
        private InteractableItem currentItemSource = null; // Track which interactable this item came from

        public bool HasItem => currentItem != null;
        public InventoryItem CurrentItem => currentItem;
        public InteractableItem CurrentItemSource => currentItemSource;

        public event Action<InteractableItem> OnItemSwappedOut; // Fires when item is returned to world

        /// <summary>
        /// Pick up an item (returns current item to world if holding one)
        /// </summary>
        public void PickUpItem(InventoryItem item, InteractableItem source)
        {
            // If already holding an item, return it to the world
            if (currentItem != null && currentItemSource != null)
            {
                Console.WriteLine($"Inventory: Returning {currentItem.Name} to world, picking up {item.Name}");
                OnItemSwappedOut?.Invoke(currentItemSource);
            }
            else
            {
                Console.WriteLine($"Inventory: Picked up {item.Name}");
            }

            currentItem = item;
            currentItemSource = source;
        }

        /// <summary>
        /// Drop current item
        /// </summary>
        public void DropItem()
        {
            if (currentItem != null)
            {
                Console.WriteLine($"Inventory: Dropped {currentItem.Name}");
                currentItem = null;
                currentItemSource = null;
            }
        }

        /// <summary>
        /// Check if holding a specific item
        /// </summary>
        public bool HasItemById(string itemId)
        {
            return currentItem != null && currentItem.Id == itemId;
        }

        /// <summary>
        /// Clear inventory
        /// </summary>
        public void Clear()
        {
            currentItem = null;
            currentItemSource = null;
        }

        /// <summary>
        /// Get item display text for UI
        /// </summary>
        public string GetDisplayText()
        {
            if (!HasItem)
                return "";

            return $"Holding: {currentItem.Name}";
        }
    }
}
