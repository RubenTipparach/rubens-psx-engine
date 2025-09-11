using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using anakinsoft.utilities;
using Vector3N = System.Numerics.Vector3;

namespace anakinsoft.system.physics
{
    /// <summary>
    /// Converts XNA mesh data to BepuPhysics static mesh for collision detection.
    /// Provides easy interface to create static collision geometry from 3D models.
    /// </summary>
    public class StaticMesh : IDisposable
    {
        private Mesh bepuMesh;
        private TypedIndex shapeIndex;
        private BufferPool bufferPool;
        private bool disposed = false;
        
        // Store triangle data for wireframe visualization
        private List<Vector3> triangleVertices;

        /// <summary>
        /// Gets the BepuPhysics mesh shape index for use in simulation.
        /// </summary>
        public TypedIndex ShapeIndex => shapeIndex;

        /// <summary>
        /// Gets the underlying BepuPhysics mesh.
        /// </summary>
        public Mesh BepuMesh => bepuMesh;
        
        /// <summary>
        /// Gets the triangle vertices for wireframe visualization.
        /// Each group of 3 vertices represents one triangle.
        /// </summary>
        public IReadOnlyList<Vector3> TriangleVertices => triangleVertices;

        /// <summary>
        /// Creates a static mesh from XNA Model data.
        /// </summary>
        /// <param name="model">XNA Model containing mesh data</param>
        /// <param name="scale">Scale to apply to the mesh</param>
        /// <param name="simulation">Physics simulation to register the shape with</param>
        /// <param name="bufferPool">Buffer pool for memory management</param>
        public StaticMesh(Model model, Vector3 scale, Simulation simulation, BufferPool bufferPool)
        {
            this.bufferPool = bufferPool;
            triangleVertices = new List<Vector3>();
            var triangles = ExtractTrianglesFromModel(model, scale, bufferPool);
            CreateBepuMesh(triangles, scale.ToVector3N(), simulation, bufferPool);
        }

        /// <summary>
        /// Creates a static mesh from vertex and index arrays.
        /// </summary>
        /// <param name="vertices">Array of vertex positions</param>
        /// <param name="indices">Array of triangle indices (3 per triangle)</param>
        /// <param name="scale">Scale to apply to the mesh</param>
        /// <param name="simulation">Physics simulation to register the shape with</param>
        /// <param name="bufferPool">Buffer pool for memory management</param>
        public StaticMesh(Vector3[] vertices, int[] indices, Vector3 scale, Simulation simulation, BufferPool bufferPool)
        {
            this.bufferPool = bufferPool;
            triangleVertices = new List<Vector3>();
            var triangles = ExtractTrianglesFromArrays(vertices, indices, scale, bufferPool);
            CreateBepuMesh(triangles, scale.ToVector3N(), simulation, bufferPool);
        }

        /// <summary>
        /// Creates a static mesh from a ModelMesh (single mesh from a model).
        /// </summary>
        /// <param name="modelMesh">XNA ModelMesh containing geometry data</param>
        /// <param name="scale">Scale to apply to the mesh</param>
        /// <param name="simulation">Physics simulation to register the shape with</param>
        /// <param name="bufferPool">Buffer pool for memory management</param>
        public StaticMesh(ModelMesh modelMesh, Vector3 scale, Simulation simulation, BufferPool bufferPool)
        {
            this.bufferPool = bufferPool;
            triangleVertices = new List<Vector3>();
            var triangles = ExtractTrianglesFromModelMesh(modelMesh, scale, bufferPool);
            CreateBepuMesh(triangles, scale.ToVector3N(), simulation, bufferPool);
        }

        /// <summary>
        /// Adds this static mesh to the physics simulation at the specified position and orientation.
        /// </summary>
        /// <param name="simulation">Physics simulation to add to</param>
        /// <param name="position">World position for the static mesh</param>
        /// <param name="orientation">World orientation for the static mesh</param>
        /// <returns>Static handle for the created physics body</returns>
        public StaticHandle AddToSimulation(Simulation simulation, Vector3 position, Quaternion orientation)
        {
            var pose = new RigidPose(position.ToVector3N(), orientation.ToQuaternionN());
            var staticDescription = new StaticDescription(pose, shapeIndex);
            return simulation.Statics.Add(staticDescription);
        }

        /// <summary>
        /// Adds this static mesh to the physics simulation at origin with no rotation.
        /// </summary>
        /// <param name="simulation">Physics simulation to add to</param>
        /// <returns>Static handle for the created physics body</returns>
        public StaticHandle AddToSimulation(Simulation simulation)
        {
            return AddToSimulation(simulation, Vector3.Zero, Quaternion.Identity);
        }

        private Buffer<Triangle> ExtractTrianglesFromModel(Model model, Vector3 scale, BufferPool bufferPool)
        {
            var allTriangles = new List<Triangle>();

            foreach (var modelMesh in model.Meshes)
            {
                var meshTriangles = ExtractTrianglesFromModelMeshInternal(modelMesh, scale);
                allTriangles.AddRange(meshTriangles);
            }

            bufferPool.Take<Triangle>(allTriangles.Count, out var triangleBuffer);
            for (int i = 0; i < allTriangles.Count; i++)
            {
                triangleBuffer[i] = allTriangles[i];
            }

            return triangleBuffer;
        }

        private Buffer<Triangle> ExtractTrianglesFromModelMesh(ModelMesh modelMesh, Vector3 scale, BufferPool bufferPool)
        {
            var triangles = ExtractTrianglesFromModelMeshInternal(modelMesh, scale);
            
            bufferPool.Take<Triangle>(triangles.Count, out var triangleBuffer);
            for (int i = 0; i < triangles.Count; i++)
            {
                triangleBuffer[i] = triangles[i];
            }

            return triangleBuffer;
        }

        private List<Triangle> ExtractTrianglesFromModelMeshInternal(ModelMesh modelMesh, Vector3 scale)
        {
            var triangles = new List<Triangle>();

            foreach (var meshPart in modelMesh.MeshParts)
            {
                // Extract vertex data
                var vertexBuffer = meshPart.VertexBuffer;
                var indexBuffer = meshPart.IndexBuffer;

                // Get vertex data
                var vertices = new Vector3[meshPart.NumVertices];
                vertexBuffer.GetData<Vector3>(vertices, meshPart.VertexOffset, meshPart.NumVertices);

                // Get index data
                var indices = new int[meshPart.PrimitiveCount * 3];
                if (indexBuffer.IndexElementSize == IndexElementSize.SixteenBits)
                {
                    var shortIndices = new short[meshPart.PrimitiveCount * 3];
                    indexBuffer.GetData<short>(shortIndices, meshPart.StartIndex, meshPart.PrimitiveCount * 3);
                    for (int i = 0; i < shortIndices.Length; i++)
                    {
                        indices[i] = shortIndices[i];
                    }
                }
                else
                {
                    indexBuffer.GetData<int>(indices, meshPart.StartIndex, meshPart.PrimitiveCount * 3);
                }

                // Create triangles
                for (int i = 0; i < indices.Length; i += 3)
                {
                    var v1 = Vector3.Transform(vertices[indices[i]], Matrix.CreateScale(scale));
                    var v2 = Vector3.Transform(vertices[indices[i + 1]], Matrix.CreateScale(scale));
                    var v3 = Vector3.Transform(vertices[indices[i + 2]], Matrix.CreateScale(scale));

                    // Store vertices for wireframe visualization
                    triangleVertices.Add(v1);
                    triangleVertices.Add(v2);
                    triangleVertices.Add(v3);

                    triangles.Add(new Triangle(v1.ToVector3N(), v2.ToVector3N(), v3.ToVector3N()));
                }
            }

            return triangles;
        }

        private Buffer<Triangle> ExtractTrianglesFromArrays(Vector3[] vertices, int[] indices, Vector3 scale, BufferPool bufferPool)
        {
            if (indices.Length % 3 != 0)
                throw new ArgumentException("Index count must be divisible by 3 for triangle meshes.");

            var triangleCount = indices.Length / 3;
            bufferPool.Take<Triangle>(triangleCount, out var triangleBuffer);

            var scaleMatrix = Matrix.CreateScale(scale);

            for (int i = 0; i < triangleCount; i++)
            {
                var i1 = indices[i * 3];
                var i2 = indices[i * 3 + 1];
                var i3 = indices[i * 3 + 2];

                var v1 = Vector3.Transform(vertices[i1], scaleMatrix);
                var v2 = Vector3.Transform(vertices[i2], scaleMatrix);
                var v3 = Vector3.Transform(vertices[i3], scaleMatrix);

                // Store vertices for wireframe visualization
                triangleVertices.Add(v1);
                triangleVertices.Add(v2);
                triangleVertices.Add(v3);

                triangleBuffer[i] = new Triangle(v1.ToVector3N(), v2.ToVector3N(), v3.ToVector3N());
            }

            return triangleBuffer;
        }

        private void CreateBepuMesh(Buffer<Triangle> triangles, Vector3N scale, Simulation simulation, BufferPool bufferPool)
        {
            if (triangles.Length == 0)
                throw new ArgumentException("Cannot create mesh with zero triangles.");

            // Create the mesh using BepuPhysics
            // For large meshes, we could use DemoMeshHelper.CreateGiantMeshFast for better performance
            if (triangles.Length > 10000)
            {
                // Use fast creation for large meshes
                bepuMesh = new Mesh(triangles, scale, bufferPool);
            }
            else
            {
                // Use standard creation for smaller meshes
                bepuMesh = new Mesh(triangles, scale, bufferPool);
            }

            // Register the shape with the simulation
            shapeIndex = simulation.Shapes.Add(bepuMesh);
        }

        /// <summary>
        /// Creates a simple heightmap-based static mesh.
        /// Useful for terrain generation.
        /// </summary>
        /// <param name="heightData">2D array of height values</param>
        /// <param name="cellSize">Size of each grid cell</param>
        /// <param name="heightScale">Scale factor for height values</param>
        /// <param name="simulation">Physics simulation to register with</param>
        /// <param name="bufferPool">Buffer pool for memory management</param>
        /// <returns>StaticMesh representing the heightmap terrain</returns>
        public static StaticMesh CreateHeightmap(float[,] heightData, float cellSize, float heightScale, Simulation simulation, BufferPool bufferPool)
        {
            int width = heightData.GetLength(0);
            int height = heightData.GetLength(1);

            var vertices = new List<Vector3>();
            var indices = new List<int>();

            // Generate vertices
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var worldX = x * cellSize;
                    var worldZ = y * cellSize;
                    var worldY = heightData[x, y] * heightScale;
                    
                    vertices.Add(new Vector3(worldX, worldY, worldZ));
                }
            }

            // Generate triangles (two triangles per quad)
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int i = y * width + x;
                    
                    // First triangle
                    indices.Add(i);
                    indices.Add(i + width);
                    indices.Add(i + 1);
                    
                    // Second triangle
                    indices.Add(i + 1);
                    indices.Add(i + width);
                    indices.Add(i + width + 1);
                }
            }

            return new StaticMesh(vertices.ToArray(), indices.ToArray(), Vector3.One, simulation, bufferPool);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                bepuMesh.Dispose(bufferPool);
                disposed = true;
            }
        }
    }
}