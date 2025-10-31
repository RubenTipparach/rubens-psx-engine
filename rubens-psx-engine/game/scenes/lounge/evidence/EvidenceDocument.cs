using Microsoft.Xna.Framework;
using anakinsoft.entities;
using BepuPhysics;
using System;

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

        // Physics handle for raycast detection
        private StaticHandle? staticHandle;

        public event Action<EvidenceDocument> OnDocumentExamined;

        public EvidenceDocument(
            string name,
            string description,
            string evidenceId,
            Vector3 position,
            Vector3 visualScale,
            float levelScale)
        {
            Name = name;
            Description = description;
            EvidenceId = evidenceId;
            this.position = position;
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
        /// Handle interaction - examine the document
        /// </summary>
        protected override void OnInteractAction()
        {
            Console.WriteLine($"Examining {Name}");
            OnDocumentExamined?.Invoke(this);
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

                return $"[E] Examine {Name}";
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
    }
}
