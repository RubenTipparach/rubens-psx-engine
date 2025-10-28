using System;

namespace rubens_psx_engine
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

        public bool HasItem => currentItem != null;
        public InventoryItem CurrentItem => currentItem;

        /// <summary>
        /// Pick up an item (drops current item if holding one)
        /// </summary>
        public void PickUpItem(InventoryItem item)
        {
            if (currentItem != null)
            {
                Console.WriteLine($"Inventory: Dropped {currentItem.Name}, picked up {item.Name}");
            }
            else
            {
                Console.WriteLine($"Inventory: Picked up {item.Name}");
            }

            currentItem = item;
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
