using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using anakinsoft.entities;
using BepuPhysics;
using System;

namespace anakinsoft.game.scenes.lounge.evidence
{
    /// <summary>
    /// Autopsy report that shows forensic findings from the pathologist
    /// </summary>
    public class AutopsyReport : InteractableObject
    {
        public string ReportTitle { get; private set; }
        public string ReportContent { get; set; }
        public BoundingBox BoundingBox { get; private set; }

        // Physics handle for raycast detection
        private StaticHandle? staticHandle;

        public event Action<AutopsyReport> OnReportOpened;

        public AutopsyReport(
            string title,
            Vector3 position,
            Vector3 size)
        {
            ReportTitle = title;
            this.position = position;
            ReportContent = "";
            interactionDistance = 100f;
            interactionPrompt = "[E] Review Autopsy Report";

            // Create bounding box for hover highlight
            Vector3 halfSize = size / 2f;
            BoundingBox = new BoundingBox(position - halfSize, position + halfSize);
        }

        /// <summary>
        /// Handle interaction - open the report
        /// </summary>
        protected override void OnInteractAction()
        {
            Console.WriteLine($"Opening {ReportTitle}");
            OnReportOpened?.Invoke(this);
        }

        /// <summary>
        /// Override InteractionPrompt based on file state
        /// </summary>
        public override string InteractionPrompt
        {
            get
            {
                // Show disabled message if not interactable
                if (!CanInteract)
                    return "Autopsy Report - Talk to pathologist first";

                return "[E/F] Review Autopsy Report";
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
    }
}
