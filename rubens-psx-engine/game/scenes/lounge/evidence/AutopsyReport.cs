using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using anakinsoft.entities;
using BepuPhysics;
using System;
using rubens_psx_engine.entities;

namespace anakinsoft.game.scenes.lounge.evidence
{
    /// <summary>
    /// Autopsy report that can be collected and delivered to the pathologist
    /// </summary>
    public class AutopsyReport : InteractableObject
    {
        public string ReportTitle { get; private set; }
        public string ReportContent { get; set; }
        public BoundingBox BoundingBox { get; private set; }
        public bool IsCollected { get; private set; }
        public bool IsTranscriptMode { get; private set; } // After round 1, becomes transcript viewer

        // Physics handle for raycast detection
        private StaticHandle? staticHandle;

        // Reference to visual entity for showing/hiding
        private RenderingEntity visual;

        public event Action<AutopsyReport> OnReportCollected;
        public event Action<AutopsyReport> OnTranscriptViewed;

        public AutopsyReport(
            string title,
            Vector3 position,
            Vector3 size)
        {
            ReportTitle = title;
            this.position = position;
            ReportContent = "";
            interactionDistance = 100f;
            interactionPrompt = "[E] Review Crime Scene Report";

            // Create bounding box for hover highlight
            Vector3 halfSize = size / 2f;
            BoundingBox = new BoundingBox(position - halfSize, position + halfSize);
        }

        /// <summary>
        /// Handle interaction - collect the report or view transcript
        /// </summary>
        protected override void OnInteractAction()
        {
            if (IsTranscriptMode)
            {
                // In transcript mode, show the pathologist's transcript
                Console.WriteLine($"Viewing transcript: {ReportTitle}");
                OnTranscriptViewed?.Invoke(this);
            }
            else if (!IsCollected)
            {
                // First time collection
                IsCollected = true;
                Console.WriteLine($"Collected {ReportTitle}");
                OnReportCollected?.Invoke(this);

                // Hide visual when collected
                if (visual != null)
                {
                    visual.IsVisible = false;
                }
            }
        }

        /// <summary>
        /// Override InteractionPrompt based on file state
        /// </summary>
        public override string InteractionPrompt
        {
            get
            {
                if (IsTranscriptMode)
                    return "[E] Review Autopsy Report";

                if (IsCollected)
                    return ""; // Don't show prompt if already collected

                // Show disabled message if not interactable
                if (!CanInteract)
                    return "Autopsy Report - Talk to pathologist first";

                return "[E] Pick up Autopsy Report";
            }
        }

        /// <summary>
        /// Sets the physics static handle for this report
        /// </summary>
        public void SetStaticHandle(StaticHandle handle)
        {
            staticHandle = handle;
        }

        /// <summary>
        /// Gets the physics static handle for this report
        /// </summary>
        public StaticHandle? GetStaticHandle()
        {
            return staticHandle;
        }

        /// <summary>
        /// Sets the visual entity reference for this report
        /// </summary>
        public void SetVisual(RenderingEntity visualEntity)
        {
            visual = visualEntity;
        }

        /// <summary>
        /// Convert to transcript mode (after round 1 starts)
        /// </summary>
        public void ConvertToTranscriptMode()
        {
            IsTranscriptMode = true;
            IsCollected = false; // Reset collected state
            CanInteract = true; // Make it interactable

            // Show visual again
            if (visual != null)
            {
                visual.IsVisible = true;
            }

            Console.WriteLine($"[AutopsyReport] Converted to transcript mode - always visible and interactable");
        }

        /// <summary>
        /// Return report to world (when swapped from inventory)
        /// </summary>
        public void ReturnToWorld()
        {
            if (IsTranscriptMode)
            {
                // In transcript mode, already visible and interactable
                return;
            }

            IsCollected = false;

            // Show visual again
            if (visual != null)
            {
                visual.IsVisible = true;
            }

            Console.WriteLine($"[AutopsyReport] Returned to world");
        }
    }
}
