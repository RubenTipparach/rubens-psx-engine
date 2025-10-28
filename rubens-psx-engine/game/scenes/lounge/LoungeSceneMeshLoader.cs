using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine;
using rubens_psx_engine.Extensions;
using rubens_psx_engine.entities;
using anakinsoft.system.physics;
using anakinsoft.utilities;
using BepuPhysics;
using BepuPhysics.Collidables;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine
{
    /// <summary>
    /// Helper class for loading static meshes and physics colliders in The Lounge scene
    /// Handles all geometry, furniture, and debug visualization
    /// </summary>
    public class LoungeSceneMeshLoader
    {
        private readonly float levelScale;
        private readonly PhysicsSystem physicsSystem;
        private readonly Action<RenderingEntity> addRenderingEntityCallback;

        private readonly List<RenderingEntity> entities = new List<RenderingEntity>();

        // Physics mesh data for wireframe rendering and cleanup
        private readonly List<Mesh> bepuMeshes = new List<Mesh>();
        private readonly List<List<Vector3>> meshTriangleVertices = new List<List<Vector3>>();
        private readonly List<(Vector3 position, Quaternion rotation)> staticMeshTransforms = new List<(Vector3, Quaternion)>();

        public LoungeSceneMeshLoader(float scale, PhysicsSystem physics, Action<RenderingEntity> addEntityCallback)
        {
            levelScale = scale;
            physicsSystem = physics;
            addRenderingEntityCallback = addEntityCallback;
        }

        /// <summary>
        /// Get physics mesh data for wireframe rendering
        /// </summary>
        public (List<Mesh> meshes, List<List<Vector3>> vertices, List<(Vector3, Quaternion)> transforms) GetPhysicsMeshData()
        {
            return (bepuMeshes, meshTriangleVertices, staticMeshTransforms);
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
        /// Load all lounge geometry (walls, floor, ceiling, windows, doors)
        /// </summary>
        public void LoadAllLoungeGeometry(float jitter = 3f, float affine = 0f)
        {
            Console.WriteLine("Loading lounge geometry...");

            // Create unlit materials for each lounge texture
            var ceilingMat = new UnlitMaterial("textures/Lounge/Cieling");
            ceilingMat.VertexJitterAmount = jitter;
            ceilingMat.Brightness = 1.2f;
            ceilingMat.AffineAmount = affine;

            var doorMat = new UnlitMaterial("textures/Lounge/door");
            doorMat.VertexJitterAmount = jitter;
            doorMat.Brightness = 1.2f;
            doorMat.AffineAmount = affine;

            var floorMat = new UnlitMaterial("textures/Lounge/floor_1");
            floorMat.VertexJitterAmount = jitter;
            floorMat.AffineAmount = affine;
            floorMat.Brightness = 1.2f;

            var wall1Mat = new UnlitMaterial("textures/Lounge/wall_1");
            wall1Mat.VertexJitterAmount = jitter;
            wall1Mat.Brightness = 1.2f;
            wall1Mat.AffineAmount = affine;

            var wall2Mat = new UnlitMaterial("textures/Lounge/wall_2");
            wall2Mat.VertexJitterAmount = jitter;
            wall2Mat.Brightness = 1.2f;
            wall2Mat.AffineAmount = affine;

            var windowMat = new UnlitMaterial("textures/Lounge/window");
            windowMat.VertexJitterAmount = jitter;
            windowMat.Brightness = 1.2f;
            windowMat.AffineAmount = affine;

            // Load all geometry meshes
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { ceilingMat }, "models/lounge/Ceiling2");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { doorMat }, "models/lounge/Door");
            CreateStaticMesh(new Vector3(0, 0, 0), QuaternionExtensions.CreateFromYawPitchRollDegrees(180, 0, 0), new[] { doorMat }, "models/lounge/Door");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { floorMat }, "models/lounge/Lounge_floor");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { wall1Mat }, "models/lounge/Lounge_wall");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { wall2Mat }, "models/lounge/Wall_L");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { wall2Mat }, "models/lounge/Wall_R");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { windowMat }, "models/lounge/Window");

            Console.WriteLine("Lounge geometry loaded");
        }

        /// <summary>
        /// Load all furniture (bar, tables, chairs, booths)
        /// </summary>
        public void LoadFurniture(float jitter = 3f, float affine = 0f)
        {
            Console.WriteLine("Loading furniture...");

            // Create furniture materials
            var barMat = new UnlitMaterial("textures/Lounge/Bar");
            barMat.VertexJitterAmount = jitter;
            barMat.Brightness = 1.2f;
            barMat.AffineAmount = affine;

            var chairMat = new UnlitMaterial("textures/Lounge/chair");
            chairMat.VertexJitterAmount = jitter;
            chairMat.Brightness = 1.2f;
            chairMat.AffineAmount = affine;

            var tableMat = new UnlitMaterial("textures/Lounge/table");
            tableMat.VertexJitterAmount = jitter;
            tableMat.Brightness = 1.2f;
            tableMat.AffineAmount = affine;

            var boothMat = new UnlitMaterial("textures/Lounge/booth");
            boothMat.VertexJitterAmount = jitter;
            boothMat.Brightness = 1.2f;
            boothMat.AffineAmount = affine;

            // Position scale for furniture placement
            var posScale = 10f;

            // Load bar pieces
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { barMat }, "models/lounge/furnitures/lounge_bar");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { barMat }, "models/lounge/furnitures/lounge_bar_2");

            // Load bar stools
            CreateStaticMesh(new Vector3(-1.70852f, 0, -3.29662f) * posScale,
                Quaternion.Identity, new[] { chairMat }, "models/lounge/furnitures/lounge_high_chair");
            CreateStaticMesh(new Vector3(-1.70852f, 0, -2) * posScale,
                Quaternion.Identity, new[] { chairMat }, "models/lounge/furnitures/lounge_high_chair");

            // Load lounge chairs
            CreateStaticMesh(new Vector3(-2.91486f, 0, 2.17103f) * posScale,
                Quaternion.Identity, new[] { chairMat }, "models/lounge/furnitures/lounge_chair");
            CreateStaticMesh(new Vector3(-2.91486f, 0, 3.41485f) * posScale,
                Quaternion.Identity, new[] { chairMat }, "models/lounge/furnitures/lounge_chair");

            // Load tables
            CreateStaticMesh(new Vector3(-1.28593f, 0, 3.11644f) * posScale,
                Quaternion.Identity, new[] { tableMat }, "models/lounge/furnitures/lounge_table");
            CreateStaticMesh(new Vector3(2.05432f, 0, 3.11644f) * posScale,
                Quaternion.Identity, new[] { tableMat }, "models/lounge/furnitures/lounge_table");

            // Load booths
            CreateStaticMesh(new Vector3(0.137007f, 0, 2.8772f) * posScale,
                Quaternion.Identity, new[] { boothMat }, "models/lounge/furnitures/lounge_boot");
            CreateStaticMesh(new Vector3(0.137007f, 0, 2.8772f) * posScale,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), new[] { boothMat }, "models/lounge/furnitures/lounge_boot");
            CreateStaticMesh(new Vector3(3.18364f, 0, 2.8772f) * posScale,
                Quaternion.Identity, new[] { boothMat }, "models/lounge/furnitures/lounge_boot");

            // Load bar shelves
            CreateStaticMesh(new Vector3(-3.82676f, 0, 2.89935f) * posScale,
                Quaternion.Identity, new[] { barMat }, "models/lounge/furnitures/lounge_bar_stelf");
            CreateStaticMesh(new Vector3(3.68024f, 0, 2.89935f) * posScale,
                Quaternion.Identity, new[] { barMat }, "models/lounge/furnitures/lounge_bar_stelf");

            Console.WriteLine("Furniture loaded");
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
        /// Create a static mesh with physics collider
        /// </summary>
        private void CreateStaticMesh(Vector3 offset, Quaternion rotation, Material[] mats, string mesh)
        {
            // Create entity with material channels
            var loadedMats = new Dictionary<int, Material>();
            for (int i = 0; i < mats.Length; i++)
            {
                loadedMats.Add(i, mats[i]);
            }
            var entity = new MultiMaterialRenderingEntity(mesh, loadedMats);

            entity.Position = Vector3.Zero + offset;
            entity.Scale = Vector3.One * 0.2f * levelScale;
            entity.Rotation = rotation;
            entity.IsVisible = true;

            // Add to rendering entities
            entities.Add(entity);
            addRenderingEntityCallback(entity);

            // Create physics mesh for the model
            CreatePhysicsMesh(mesh, offset, rotation,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, -90, 0), Vector3.Zero);
        }

        /// <summary>
        /// Create physics collision mesh from model
        /// </summary>
        private void CreatePhysicsMesh(string mesh, Vector3 offset, Quaternion rotation,
            Quaternion rotationOffset, Vector3 physicsMeshOffset)
        {
            try
            {
                // Load the same model used for rendering
                var model = Globals.screenManager.Content.Load<Model>(mesh);

                // Use consistent scaling approach: visual scale * physics scale factor
                var visualScale = Vector3.One * 0.2f * levelScale; // Same scale as rendering entity
                var physicsScale = visualScale * 100; // Apply scaling factor

                // Extract triangles and wireframe vertices for rendering
                var (triangles, wireframeVertices) = BepuMeshExtractor.ExtractTrianglesFromModel(model, physicsScale, physicsSystem);
                var bepuMesh = new Mesh(triangles, Vector3.One.ToVector3N(), physicsSystem.BufferPool);

                // Add the mesh shape to the simulation's shape collection
                var shapeIndex = physicsSystem.Simulation.Shapes.Add(bepuMesh);
                var rotation2 = rotation * rotationOffset;

                // Create static body with the mesh shape
                var staticHandle = physicsSystem.Simulation.Statics.Add(new StaticDescription(
                    offset.ToVector3N(),
                    rotation2.ToQuaternionN(),
                    shapeIndex));

                // Keep references for cleanup and wireframe rendering
                bepuMeshes.Add(bepuMesh);
                meshTriangleVertices.Add(wireframeVertices);
                staticMeshTransforms.Add((offset, rotation2));

                Console.WriteLine($"Created lounge physics mesh at position: {offset} with physics scale: {physicsScale}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to create lounge physics mesh: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                // Continue without physics mesh for this model
            }
        }

        /// <summary>
        /// Draw wireframes for all static mesh collision geometry
        /// </summary>
        public void DrawStaticMeshWireframes(Camera camera)
        {
            var graphicsDevice = Globals.screenManager.GraphicsDevice;

            // Create a basic effect for wireframe rendering
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = camera.View;
            basicEffect.Projection = camera.Projection;
            basicEffect.World = Matrix.Identity;

            // Draw wireframes for each static mesh
            for (int meshIndex = 0; meshIndex < bepuMeshes.Count; meshIndex++)
            {
                if (meshIndex < meshTriangleVertices.Count && meshIndex < staticMeshTransforms.Count)
                {
                    var triangleVertices = meshTriangleVertices[meshIndex];
                    var (position, rotation) = staticMeshTransforms[meshIndex];

                    if (triangleVertices != null && triangleVertices.Count > 0)
                    {
                        // Create transform matrix for this static mesh
                        var worldMatrix = Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position);

                        // Create wireframe edges from triangles
                        var wireframeVertices = new List<VertexPositionColor>();

                        for (int i = 0; i < triangleVertices.Count; i += 3)
                        {
                            if (i + 2 < triangleVertices.Count)
                            {
                                // Apply world transform to vertices
                                var v1 = Vector3.Transform(triangleVertices[i], worldMatrix);
                                var v2 = Vector3.Transform(triangleVertices[i + 1], worldMatrix);
                                var v3 = Vector3.Transform(triangleVertices[i + 2], worldMatrix);

                                // Create the three edges of the triangle
                                // Edge 1: v1 to v2
                                wireframeVertices.Add(new VertexPositionColor(v1, Color.Yellow));
                                wireframeVertices.Add(new VertexPositionColor(v2, Color.Yellow));

                                // Edge 2: v2 to v3
                                wireframeVertices.Add(new VertexPositionColor(v2, Color.Yellow));
                                wireframeVertices.Add(new VertexPositionColor(v3, Color.Yellow));

                                // Edge 3: v3 to v1
                                wireframeVertices.Add(new VertexPositionColor(v3, Color.Yellow));
                                wireframeVertices.Add(new VertexPositionColor(v1, Color.Yellow));
                            }
                        }

                        if (wireframeVertices.Count > 0)
                        {
                            try
                            {
                                // Apply the effect and draw the wireframe lines
                                basicEffect.CurrentTechnique.Passes[0].Apply();
                                graphicsDevice.DrawUserPrimitives(
                                    PrimitiveType.LineList,
                                    wireframeVertices.ToArray(),
                                    0,
                                    wireframeVertices.Count / 2);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error drawing wireframe: {ex.Message}");
                            }
                        }
                    }
                }
            }

            basicEffect.Dispose();
        }

        /// <summary>
        /// Clear all loaded entities
        /// </summary>
        public void Clear()
        {
            entities.Clear();
            bepuMeshes.Clear();
            meshTriangleVertices.Clear();
            staticMeshTransforms.Clear();
        }
    }
}
