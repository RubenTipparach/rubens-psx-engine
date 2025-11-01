using System;

namespace anakinsoft.game.scenes.lounge.evidence
{
    /// <summary>
    /// Simple evidence inventory - can only hold 1 evidence document at a time
    /// When picking up a new item, the current item is returned to its table position
    /// </summary>
    public class EvidenceInventory
    {
        private EvidenceDocument currentDocument = null;

        public bool HasDocument => currentDocument != null;
        public EvidenceDocument CurrentDocument => currentDocument;

        public event Action<EvidenceDocument> OnDocumentSwappedOut; // Fires when document is returned to table

        /// <summary>
        /// Pick up an evidence document (returns current document to table if holding one)
        /// </summary>
        public void PickUpDocument(EvidenceDocument document)
        {
            // If already holding a document, return it to the table
            if (currentDocument != null)
            {
                Console.WriteLine($"[EvidenceInventory] Returning {currentDocument.Name} to table, picking up {document.Name}");
                currentDocument.ReturnToWorld();
                OnDocumentSwappedOut?.Invoke(currentDocument);
            }
            else
            {
                Console.WriteLine($"[EvidenceInventory] Picked up {document.Name}");
            }

            currentDocument = document;
        }

        /// <summary>
        /// Drop current document (return it to table)
        /// </summary>
        public void DropDocument()
        {
            if (currentDocument != null)
            {
                Console.WriteLine($"[EvidenceInventory] Dropped {currentDocument.Name}");
                currentDocument.ReturnToWorld();
                OnDocumentSwappedOut?.Invoke(currentDocument);
                currentDocument = null;
            }
        }

        /// <summary>
        /// Check if holding a specific document
        /// </summary>
        public bool HasDocumentById(string evidenceId)
        {
            return currentDocument != null && currentDocument.EvidenceId == evidenceId;
        }

        /// <summary>
        /// Clear inventory without returning to world
        /// </summary>
        public void Clear()
        {
            currentDocument = null;
        }

        /// <summary>
        /// Get document display text for UI
        /// </summary>
        public string GetDisplayText()
        {
            if (!HasDocument)
                return "";

            return $"Holding: {currentDocument.Name}";
        }
    }
}
