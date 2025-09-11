using anakinsoft.system.physics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace anakinsoft.utilities
{
    public static class BepuMeshExtractor
    {

        // Dynamic boxes removed for simplified testing

        public static (Buffer<Triangle> triangles, List<Vector3> wireframeVertices) ExtractTrianglesFromModel(Model model, Vector3 scale, PhysicsSystem physicsSystem)
        {
            var trianglesList = new List<Triangle>();
            var wireframeVertices = new List<Vector3>();
            var scaleMatrix = Matrix.CreateScale(scale);

            foreach (var mesh in model.Meshes)
            {
                foreach (var meshPart in mesh.MeshParts)
                {
                    // Get vertex and index data
                    var vertexBuffer = meshPart.VertexBuffer;
                    var indexBuffer = meshPart.IndexBuffer;
                    var vertexDeclaration = meshPart.VertexBuffer.VertexDeclaration;

                    // Extract vertices (assuming VertexPositionNormalTexture)
                    var vertexCount = meshPart.NumVertices;
                    var vertices = new VertexPositionNormalTexture[vertexCount];
                    vertexBuffer.GetData(meshPart.VertexOffset * vertexDeclaration.VertexStride,
                        vertices, 0, vertexCount, vertexDeclaration.VertexStride);

                    // Extract indices
                    var indexCount = meshPart.PrimitiveCount * 3;
                    var indices = new int[indexCount];

                    if (indexBuffer.IndexElementSize == IndexElementSize.SixteenBits)
                    {
                        var shortIndices = new short[indexCount];
                        indexBuffer.GetData<short>(shortIndices, meshPart.StartIndex, indexCount);
                        for (int i = 0; i < shortIndices.Length; i++)
                            indices[i] = shortIndices[i];
                    }
                    else
                    {
                        indexBuffer.GetData<int>(indices, meshPart.StartIndex, indexCount);
                    }

                    // Create triangles and store wireframe vertices
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        var v1 = Vector3.Transform(vertices[indices[i]].Position, scaleMatrix);
                        var v2 = Vector3.Transform(vertices[indices[i + 1]].Position, scaleMatrix);
                        var v3 = Vector3.Transform(vertices[indices[i + 2]].Position, scaleMatrix);

                        // Store vertices for wireframe (in local space)
                        wireframeVertices.Add(v1);
                        wireframeVertices.Add(v2);
                        wireframeVertices.Add(v3);

                        trianglesList.Add(new Triangle(v1.ToVector3N(), v2.ToVector3N(), v3.ToVector3N()));
                    }
                }
            }

            // Create buffer and copy triangles
            physicsSystem.BufferPool.Take<Triangle>(trianglesList.Count, out var triangleBuffer);
            for (int i = 0; i < trianglesList.Count; i++)
            {
                triangleBuffer[i] = trianglesList[i];
            }

            return (triangleBuffer, wireframeVertices);
        }

    }
}
