using Microsoft.Xna.Framework;
using anakinsoft.entities;
using BepuPhysics;
using System;
using rubens_psx_engine.entities;

namespace anakinsoft.game.scenes.lounge.evidence
{
    /// <summary>
    /// Generic evidence document that can be examined
    /// </summary>
    public class EvidenceDocument : InteractableObject
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string EvidenceId { get; private set; }
        public BoundingBox BoundingBox { get; private set; }
        public Vector3 VisualScale { get; private set; }
        public Vector3 ColliderSize { get; private set; }
        public bool IsCollected { get; private set; }
        public int TableRow { get; private set; }
        public int TableColumn { get; private set; }
        public Vector3 OriginalPosition { get; private set; }

        // Physics handle for raycast detection
        private StaticHandle? staticHandle;

        // Reference to visual entity for showing/hiding
        private RenderingEntity visual;

        public event Action<EvidenceDocument> OnDocumentExamined;

        public EvidenceDocument(
            string name,
            string description,
            string evidenceId,
            Vector3 position,
            Vector3 visualScale,
            float levelScale,
            int tableRow,
            int tableColumn)
        {
            Name = name;
            Description = description;
            EvidenceId = evidenceId;
            this.position = position;
            OriginalPosition = position;
            TableRow = tableRow;
            TableColumn = tableColumn;
            interactionDistance = 100f;
            interactionPrompt = $"[E] Examine {name}";

            // Store visual scale
            VisualScale = visualScale * levelScale;

            // Calculate base size from visual scale
            var baseSize = new Vector3(visualScale.X * 10f, visualScale.Y * 10f, visualScale.Z * 10f) * levelScale;

            // Collider is 1.2x for easier interaction
            ColliderSize = baseSize * 1.2f;

            // Bounding box is slightly larger to make it visible outside geometry
            var boundingBoxSize = baseSize * 2.1f;
            Vector3 halfSize = boundingBoxSize / 2f;
            BoundingBox = new BoundingBox(position - halfSize, position + halfSize);
        }

        /// <summary>
        /// Handle interaction - examine the document (read-only, no pickup)
        /// </summary>
        protected override void OnInteractAction()
        {
            // Evidence is read-only - can only hover and read description
            // Actual evidence presentation happens through dialogue system
            Console.WriteLine($"[EvidenceDocument] Examining {Name} (read-only)");
        }

        /// <summary>
        /// Override InteractionPrompt
        /// </summary>
        public override string InteractionPrompt
        {
            get
            {
                if (!CanInteract)
                    return $"{Name} - Not available yet";

                return $"{Name}";
            }
        }

        /// <summary>
        /// Override InteractionDescription to show evidence description
        /// </summary>
        public override string InteractionDescription
        {
            get
            {
                if (!CanInteract)
                    return "";

                return Description;
            }
        }

        /// <summary>
        /// Sets the physics static handle for this document
        /// </summary>
        public void SetStaticHandle(StaticHandle handle)
        {
            staticHandle = handle;
        }

        /// <summary>
        /// Gets the physics static handle for this document
        /// </summary>
        public StaticHandle? GetStaticHandle()
        {
            return staticHandle;
        }

        /// <summary>
        /// Sets the visual entity reference for this document
        /// </summary>
        public void SetVisual(RenderingEntity visualEntity)
        {
            visual = visualEntity;
        }

        /// <summary>
        /// Return document to world (when placed back on table)
        /// </summary>
        public void ReturnToWorld()
        {
            IsCollected = false;

            // Show visual when returned to table (after being swapped out)
            if (visual != null)
            {
                visual.Position = OriginalPosition;
                visual.IsVisible = true;
            }

            // Reset position
            position = OriginalPosition;

            Console.WriteLine($"[EvidenceDocument] Returned {Name} to world at position {OriginalPosition}");
        }
    }
}
