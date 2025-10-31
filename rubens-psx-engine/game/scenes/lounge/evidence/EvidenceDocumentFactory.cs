using Microsoft.Xna.Framework;
using anakinsoft.system;
using BepuPhysics.Collidables;
using rubens_psx_engine.entities;
using rubens_psx_engine.Extensions;
using anakinsoft.system.physics;

namespace anakinsoft.game.scenes.lounge.evidence
{
    /// <summary>
    /// Factory for creating evidence documents with all required components
    /// </summary>
    public class EvidenceDocumentFactory
    {
        private PhysicsSystem physicsSystem;
        private InteractionSystem interactionSystem;
        private EvidenceTable evidenceTable;
        private float levelScale;

        public EvidenceDocumentFactory(
            PhysicsSystem physicsSystem,
            InteractionSystem interactionSystem,
            EvidenceTable evidenceTable,
            float levelScale)
        {
            this.physicsSystem = physicsSystem;
            this.interactionSystem = interactionSystem;
            this.evidenceTable = evidenceTable;
            this.levelScale = levelScale;
        }

        /// <summary>
        /// Creates an evidence document with physics collider and visual representation
        /// </summary>
        public (EvidenceDocument document, RenderingEntity visual) CreateEvidenceDocument(
            string name,
            string description,
            string evidenceId,
            int tableRow,
            int tableColumn,
            Vector3 visualScale,
            string texturePath = "textures/prototype/concrete")
        {
            // Get position from evidence table
            Vector3 position = evidenceTable.GetSlotPosition(tableRow, tableColumn);

            // Create evidence document
            var document = new EvidenceDocument(
                name: name,
                description: description,
                evidenceId: evidenceId,
                position: position,
                visualScale: visualScale,
                levelScale: levelScale
            );

            // Register with interaction system
            interactionSystem.RegisterInteractable(document);

            // Place on evidence table
            evidenceTable.PlaceItem(evidenceId, tableRow, tableColumn, document);

            // Create physics collider
            var colliderShape = new Box(document.ColliderSize.X, document.ColliderSize.Y, document.ColliderSize.Z);
            var rotation = QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0);
            var staticHandle = physicsSystem.Simulation.Statics.Add(
                new BepuPhysics.StaticDescription(
                    new System.Numerics.Vector3(position.X, position.Y, position.Z),
                    new System.Numerics.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W),
                    physicsSystem.Simulation.Shapes.Add(colliderShape))
            );
            document.SetStaticHandle(staticHandle);

            // Create visual cube
            var visual = new RenderingEntity("models/cube", texturePath);
            visual.Position = position;
            visual.Scale = document.VisualScale;
            visual.Rotation = QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0);
            visual.IsVisible = true;

            System.Console.WriteLine($"{name} created at position: {position} with collider handle: {staticHandle.Value}");

            return (document, visual);
        }
    }
}
