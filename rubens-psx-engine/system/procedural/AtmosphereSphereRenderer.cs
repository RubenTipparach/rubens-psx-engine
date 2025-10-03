using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

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

            // Generate indices
            int indexCount = latSegments * lonSegments * 6;
            var indices = new short[indexCount];
            int index = 0;

            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
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

        public void Dispose()
        {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();
        }
    }
}
