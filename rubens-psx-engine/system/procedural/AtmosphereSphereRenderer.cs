using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine.system.procedural
{
    public class AtmosphereSphereRenderer : IDisposable
    {
        private GraphicsDevice graphicsDevice;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private int primitiveCount;
        private float radius;
        private int latSegments;
        private int lonSegments;

        // Chunk-based rendering for frustum culling
        private struct SphereChunk
        {
            public int StartIndex;
            public int PrimitiveCount;
            public Vector3 Center;
            public float BoundingRadius;
        }
        private List<SphereChunk> chunks;

        public float Radius => radius;

        public AtmosphereSphereRenderer(GraphicsDevice device, float radius, int subdivisions = 64)
        {
            this.graphicsDevice = device;
            this.radius = radius;
            this.latSegments = subdivisions;
            this.lonSegments = subdivisions * 2;

            GenerateSphere();
        }

        private void GenerateSphere()
        {
            int vertexCount = (latSegments + 1) * (lonSegments + 1);
            var vertices = new VertexPositionNormalTexture[vertexCount];

            int vertexIndex = 0;
            for (int lat = 0; lat <= latSegments; lat++)
            {
                float theta = lat * MathF.PI / latSegments;
                float sinTheta = MathF.Sin(theta);
                float cosTheta = MathF.Cos(theta);

                for (int lon = 0; lon <= lonSegments; lon++)
                {
                    float phi = lon * 2 * MathF.PI / lonSegments;
                    float sinPhi = MathF.Sin(phi);
                    float cosPhi = MathF.Cos(phi);

                    Vector3 normal = new Vector3(
                        cosPhi * sinTheta,
                        cosTheta,
                        sinPhi * sinTheta
                    );

                    Vector3 position = normal * radius;

                    Vector2 texCoord = new Vector2(
                        (float)lon / lonSegments,
                        (float)lat / latSegments
                    );

                    vertices[vertexIndex++] = new VertexPositionNormalTexture(
                        position, normal, texCoord
                    );
                }
            }

            // Generate indices and chunks for frustum culling
            int indexCount = latSegments * lonSegments * 6;
            var indices = new short[indexCount];
            chunks = new List<SphereChunk>();

            // Create chunks: divide sphere into 8x4 chunks (longitude x latitude)
            int chunkLonDiv = 8;
            int chunkLatDiv = 4;
            int chunkLonSize = lonSegments / chunkLonDiv;
            int chunkLatSize = latSegments / chunkLatDiv;

            int index = 0;

            for (int chunkLat = 0; chunkLat < chunkLatDiv; chunkLat++)
            {
                for (int chunkLon = 0; chunkLon < chunkLonDiv; chunkLon++)
                {
                    int startLat = chunkLat * chunkLatSize;
                    int endLat = Math.Min(startLat + chunkLatSize, latSegments);
                    int startLon = chunkLon * chunkLonSize;
                    int endLon = Math.Min(startLon + chunkLonSize, lonSegments);

                    int chunkStartIndex = index;
                    Vector3 chunkCenter = Vector3.Zero;
                    int chunkVertCount = 0;

                    for (int lat = startLat; lat < endLat; lat++)
                    {
                        for (int lon = startLon; lon < endLon; lon++)
                        {
                            int current = lat * (lonSegments + 1) + lon;
                            int next = current + lonSegments + 1;

                            // First triangle (inverted winding for inside-out rendering)
                            indices[index++] = (short)current;
                            indices[index++] = (short)(current + 1);
                            indices[index++] = (short)next;

                            // Second triangle
                            indices[index++] = (short)(current + 1);
                            indices[index++] = (short)(next + 1);
                            indices[index++] = (short)next;

                            // Calculate chunk center
                            chunkCenter += vertices[current].Position;
                            chunkVertCount++;
                        }
                    }

                    if (chunkVertCount > 0)
                    {
                        chunkCenter /= chunkVertCount;

                        chunks.Add(new SphereChunk
                        {
                            StartIndex = chunkStartIndex,
                            PrimitiveCount = (index - chunkStartIndex) / 3,
                            Center = chunkCenter,
                            BoundingRadius = radius * 0.5f // Conservative bounding sphere
                        });
                    }
                }
            }

            primitiveCount = indexCount / 3;

            vertexBuffer = new VertexBuffer(
                graphicsDevice,
                typeof(VertexPositionNormalTexture),
                vertices.Length,
                BufferUsage.WriteOnly
            );
            vertexBuffer.SetData(vertices);

            indexBuffer = new IndexBuffer(
                graphicsDevice,
                IndexElementSize.SixteenBits,
                indices.Length,
                BufferUsage.WriteOnly
            );
            indexBuffer.SetData(indices);
        }

        public void Draw(GraphicsDevice device)
        {
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, primitiveCount);
        }

        // Draw with frustum culling for better performance
        public void DrawWithFrustumCulling(GraphicsDevice device, BoundingFrustum frustum)
        {
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;

            int culledChunks = 0;
            int drawnPrimitives = 0;

            foreach (var chunk in chunks)
            {
                BoundingSphere chunkBounds = new BoundingSphere(chunk.Center, chunk.BoundingRadius);

                // Frustum culling check
                if (frustum.Contains(chunkBounds) != ContainmentType.Disjoint)
                {
                    device.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        chunk.StartIndex,
                        chunk.PrimitiveCount
                    );
                    drawnPrimitives += chunk.PrimitiveCount;
                }
                else
                {
                    culledChunks++;
                }
            }
        }

        public void Dispose()
        {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();
        }
    }
}
