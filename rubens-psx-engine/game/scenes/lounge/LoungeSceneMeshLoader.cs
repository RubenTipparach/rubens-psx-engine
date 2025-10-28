using Microsoft.Xna.Framework;
using rubens_psx_engine;
using rubens_psx_engine.Extensions;
using rubens_psx_engine.entities;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine
{
    /// <summary>
    /// Helper class for loading static meshes in The Lounge scene
    ///
    /// TODO: Refactor to centralize all mesh loading from TheLoungeScene
    /// TODO: Move CreateStaticMesh() method here from TheLoungeScene
    /// TODO: Move all CreateStaticMesh() calls from Initialize() to methods here
    /// TODO: Move physics mesh creation (CreatePhysicsMesh) here
    /// TODO: Move mesh collision wireframe data here (meshTriangleVertices, staticMeshTransforms)
    /// TODO: Move debug drawing methods here:
    ///       - DrawDebugBox
    ///       - DrawDebugSphere
    ///       - DrawEvidenceTableGrid
    /// TODO: Add method: LoadAllLoungeGeometry() to load walls, floor, ceiling, etc.
    /// TODO: Add method: LoadFurniture() to load bar, tables, chairs
    /// TODO: Add method: DrawDebugWireframes() to handle all debug visualization
    /// TODO: Consider separating physics mesh creation into LoungePhysicsHelper class
    /// </summary>
    public class LoungeSceneMeshLoader
    {
        private readonly float levelScale;
        private readonly List<RenderingEntity> entities = new List<RenderingEntity>();

        public LoungeSceneMeshLoader(float scale)
        {
            levelScale = scale;
        }

        /// <summary>
        /// Get all loaded entities
        /// </summary>
        public List<RenderingEntity> GetEntities() => entities;

        /// <summary>
        /// Load the main lounge room mesh
        /// </summary>
        public void LoadMainRoom()
        {
            Console.WriteLine("Loading main lounge room...");

            var loungeEntity = new RenderingEntity("models/lounge_16", "textures/lounge");
            loungeEntity.Position = Vector3.Zero;
            loungeEntity.Scale = Vector3.One * levelScale;
            loungeEntity.IsVisible = true;

            entities.Add(loungeEntity);
            Console.WriteLine("Main lounge room loaded");
        }

        /// <summary>
        /// Load a chair at specified position and rotation
        /// </summary>
        public RenderingEntity LoadChair(Vector3 position, float yawDegrees = 0f)
        {
            var chairEntity = new RenderingEntity("models/chair/chair", "models/chair/skin");
            chairEntity.Position = position;
            chairEntity.Scale = Vector3.One * levelScale;
            chairEntity.Rotation = QuaternionExtensions.CreateFromYawPitchRollDegrees(yawDegrees, 0, 0);
            chairEntity.IsVisible = true;

            entities.Add(chairEntity);
            return chairEntity;
        }

        /// <summary>
        /// Load multiple chairs in a pattern
        /// </summary>
        public void LoadChairSet(Vector3 startPosition, int count, Vector3 offset, float rotationIncrement = 0f)
        {
            Console.WriteLine($"Loading chair set: {count} chairs");

            for (int i = 0; i < count; i++)
            {
                var position = startPosition + (offset * i);
                var rotation = rotationIncrement * i;
                LoadChair(position, rotation);
            }

            Console.WriteLine($"Chair set loaded: {count} chairs");
        }

        /// <summary>
        /// Load bar counter furniture
        /// </summary>
        public void LoadBarCounter()
        {
            Console.WriteLine("Loading bar counter area...");
            // Add bar counter, bottles, glasses, etc.
            // TODO: Implement when bar assets are available
            Console.WriteLine("Bar counter area loaded");
        }

        /// <summary>
        /// Load decorative elements (lights, plants, etc.)
        /// </summary>
        public void LoadDecorations()
        {
            Console.WriteLine("Loading decorative elements...");
            // Add ambient lighting fixtures, plants, wall decorations, etc.
            // TODO: Implement when decoration assets are available
            Console.WriteLine("Decorative elements loaded");
        }

        /// <summary>
        /// Clear all loaded entities
        /// </summary>
        public void Clear()
        {
            entities.Clear();
        }
    }
}
