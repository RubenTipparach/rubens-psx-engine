using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace anakinsoft.game.scenes.lounge.evidence
{
    /// <summary>
    /// Manages a grid-based evidence table for organizing items
    /// </summary>
    public class EvidenceTable
    {
        // Table configuration
        public Vector3 TableCenter { get; private set; }
        public Vector3 TableSize { get; private set; }
        public int GridRows { get; private set; }
        public int GridColumns { get; private set; }

        // Grid slots
        private EvidenceSlot[,] slots;
        private Dictionary<string, EvidenceSlot> occupiedSlots;

        public EvidenceTable(Vector3 center, Vector3 size, int rows, int columns)
        {
            TableCenter = center;
            TableSize = size;
            GridRows = rows;
            GridColumns = columns;

            slots = new EvidenceSlot[rows, columns];
            occupiedSlots = new Dictionary<string, EvidenceSlot>();

            InitializeGrid();
        }

        private void InitializeGrid()
        {
            // Calculate slot size
            float slotWidth = TableSize.X / GridColumns;
            float slotDepth = TableSize.Z / GridRows;

            // Calculate starting position (top-left corner of table)
            float startX = TableCenter.X - (TableSize.X / 2f) + (slotWidth / 2f);
            float startZ = TableCenter.Z - (TableSize.Z / 2f) + (slotDepth / 2f);
            float tableY = TableCenter.Y;

            // Create grid slots
            for (int row = 0; row < GridRows; row++)
            {
                for (int col = 0; col < GridColumns; col++)
                {
                    Vector3 slotPosition = new Vector3(
                        startX + (col * slotWidth),
                        tableY,
                        startZ + (row * slotDepth)
                    );

                    slots[row, col] = new EvidenceSlot(row, col, slotPosition, new Vector3(slotWidth, TableSize.Y, slotDepth));
                }
            }

            Console.WriteLine($"Evidence Table initialized at {TableCenter}");
            Console.WriteLine($"Grid: {GridRows}x{GridColumns}, Slot size: {slotWidth}x{slotDepth}");
        }

        /// <summary>
        /// Place an item in a specific grid slot
        /// </summary>
        public bool PlaceItem(string itemId, int row, int col, object item = null)
        {
            if (row < 0 || row >= GridRows || col < 0 || col >= GridColumns)
            {
                Console.WriteLine($"Invalid slot coordinates: ({row}, {col})");
                return false;
            }

            var slot = slots[row, col];

            if (slot.IsOccupied)
            {
                Console.WriteLine($"Slot ({row}, {col}) is already occupied by {slot.ItemId}");
                return false;
            }

            slot.ItemId = itemId;
            slot.Item = item;
            slot.IsOccupied = true;
            occupiedSlots[itemId] = slot;

            Console.WriteLine($"Placed {itemId} at slot ({row}, {col}), position: {slot.Position}");
            return true;
        }

        /// <summary>
        /// Place an item in the first available slot
        /// </summary>
        public bool PlaceItemAuto(string itemId, object item = null)
        {
            for (int row = 0; row < GridRows; row++)
            {
                for (int col = 0; col < GridColumns; col++)
                {
                    if (!slots[row, col].IsOccupied)
                    {
                        return PlaceItem(itemId, row, col, item);
                    }
                }
            }

            Console.WriteLine($"No available slots for {itemId}");
            return false;
        }

        /// <summary>
        /// Remove an item from the table
        /// </summary>
        public bool RemoveItem(string itemId)
        {
            if (!occupiedSlots.ContainsKey(itemId))
            {
                Console.WriteLine($"Item {itemId} not found on table");
                return false;
            }

            var slot = occupiedSlots[itemId];
            slot.IsOccupied = false;
            slot.ItemId = null;
            slot.Item = null;
            occupiedSlots.Remove(itemId);

            Console.WriteLine($"Removed {itemId} from slot ({slot.Row}, {slot.Column})");
            return true;
        }

        /// <summary>
        /// Get the world position for a specific slot
        /// </summary>
        public Vector3 GetSlotPosition(int row, int col)
        {
            if (row < 0 || row >= GridRows || col < 0 || col >= GridColumns)
                return TableCenter;

            return slots[row, col].Position;
        }

        /// <summary>
        /// Get slot information for an item
        /// </summary>
        public EvidenceSlot GetSlotForItem(string itemId)
        {
            return occupiedSlots.ContainsKey(itemId) ? occupiedSlots[itemId] : null;
        }

        /// <summary>
        /// Check if a slot is occupied
        /// </summary>
        public bool IsSlotOccupied(int row, int col)
        {
            if (row < 0 || row >= GridRows || col < 0 || col >= GridColumns)
                return true;

            return slots[row, col].IsOccupied;
        }

        /// <summary>
        /// Get all occupied slots
        /// </summary>
        public List<EvidenceSlot> GetOccupiedSlots()
        {
            return new List<EvidenceSlot>(occupiedSlots.Values);
        }

        /// <summary>
        /// Get all slots for debug visualization
        /// </summary>
        public EvidenceSlot[,] GetAllSlots()
        {
            return slots;
        }

        /// <summary>
        /// Get table bounds for collision/interaction
        /// </summary>
        public BoundingBox GetTableBounds()
        {
            Vector3 min = TableCenter - (TableSize / 2f);
            Vector3 max = TableCenter + (TableSize / 2f);
            return new BoundingBox(min, max);
        }
    }

    /// <summary>
    /// Represents a single slot on the evidence table
    /// </summary>
    public class EvidenceSlot
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Size { get; set; }
        public bool IsOccupied { get; set; }
        public string ItemId { get; set; }
        public object Item { get; set; }

        public EvidenceSlot(int row, int col, Vector3 position, Vector3 size)
        {
            Row = row;
            Column = col;
            Position = position;
            Size = size;
            IsOccupied = false;
            ItemId = null;
            Item = null;
        }

        public override string ToString()
        {
            return $"Slot[{Row},{Column}] at {Position} - {(IsOccupied ? $"Occupied by {ItemId}" : "Empty")}";
        }
    }
}
