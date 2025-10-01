using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine.system.procedural
{
    /// <summary>
    /// Represents a single chunk of the planet surface with its own LOD level
    /// </summary>
    public class PlanetChunk : IDisposable
    {
        public Vector3 LocalUp { get; private set; }
        public Vector2 ChunkOffset { get; private set; }
        public float ChunkSize { get; private set; }
        public int LODLevel { get; private set; }
        public BoundingSphere Bounds { get; private set; }

        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private int primitiveCount;
        private GraphicsDevice graphicsDevice;
        private ProceduralPlanetGenerator planetGenerator;
        private float planetRadius;
        private Vector3 centerPosition;

        public PlanetChunk(GraphicsDevice gd, ProceduralPlanetGenerator generator, Vector3 localUp, Vector2 offset, float size, int lodLevel, float radius)
        {
            graphicsDevice = gd;
            planetGenerator = generator;
            LocalUp = localUp;
            ChunkOffset = offset;
            ChunkSize = size;
            LODLevel = lodLevel;
            planetRadius = radius;

            GenerateMesh();
            CalculateBounds();
        }

        private void GenerateMesh()
        {
            // Resolution based on LOD level (higher LOD = more vertices)
            int resolution = GetResolutionForLOD(LODLevel);

            var vertices = new List<VertexPositionNormalTexture>();
            var indices = new List<int>();

            // Calculate perpendicular axes for this face
            Vector3 axisA = new Vector3(LocalUp.Y, LocalUp.Z, LocalUp.X);
            Vector3 axisB = Vector3.Cross(LocalUp, axisA);

            // Track center for backface culling
            Vector3 sumPosition = Vector3.Zero;

            // Generate grid of vertices
            for (int y = 0; y <= resolution; y++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    // Apply edge vertex skirt to prevent seams
                    // On borders, snap vertices to parent chunk's grid to match lower LOD neighbors
                    float xPercent = x / (float)resolution;
                    float yPercent = y / (float)resolution;

                    // Check if we're on an edge and should snap to coarser grid
                    bool onLeftEdge = (ChunkOffset.X > 0.0f && x == 0);
                    bool onRightEdge = (ChunkOffset.X + ChunkSize < 1.0f && x == resolution);
                    bool onBottomEdge = (ChunkOffset.Y > 0.0f && y == 0);
                    bool onTopEdge = (ChunkOffset.Y + ChunkSize < 1.0f && y == resolution);

                    // If on edge, snap to parent grid (reduce resolution by factor of 2)
                    if (onLeftEdge || onRightEdge)
                    {
                        // Round x to nearest parent grid point
                        int parentResolution = resolution / 2;
                        if (parentResolution > 0)
                        {
                            xPercent = MathF.Round(xPercent * parentResolution) / parentResolution;
                        }
                    }
                    if (onBottomEdge || onTopEdge)
                    {
                        // Round y to nearest parent grid point
                        int parentResolution = resolution / 2;
                        if (parentResolution > 0)
                        {
                            yPercent = MathF.Round(yPercent * parentResolution) / parentResolution;
                        }
                    }

                    // Map to cube face coordinates
                    Vector2 cubePos = ChunkOffset + new Vector2(xPercent, yPercent) * ChunkSize;

                    // Convert from [0,1] cube coordinates to sphere
                    Vector3 pointOnCube = LocalUp + (cubePos.X * 2f - 1f) * axisA + (cubePos.Y * 2f - 1f) * axisB;
                    Vector3 pointOnSphere = Vector3.Normalize(pointOnCube);

                    // Sample height from existing heightmap
                    float height = SampleHeightFromGenerator(pointOnSphere);

                    // Apply height offset (matching ProceduralPlanetGenerator scale)
                    float heightScale = planetRadius * 0.1f; // 10% of radius
                    Vector3 position = pointOnSphere * (planetRadius + (height - 0.5f) * heightScale * 2f);

                    sumPosition += position;

                    // Calculate UV for texturing
                    Vector2 uv = GetSphericalUV(pointOnSphere);

                    vertices.Add(new VertexPositionNormalTexture(position, pointOnSphere, uv));
                }
            }

            // Calculate center position for backface culling
            centerPosition = sumPosition / vertices.Count;

            // Generate indices for triangle list
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i0 = y * (resolution + 1) + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + (resolution + 1);
                    int i3 = i2 + 1;

                    indices.Add(i0);
                    indices.Add(i2);
                    indices.Add(i1);

                    indices.Add(i1);
                    indices.Add(i2);
                    indices.Add(i3);
                }
            }

            // Create buffers
            if (vertices.Count > 0)
            {
                vertexBuffer?.Dispose();
                vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), vertices.Count, BufferUsage.WriteOnly);
                vertexBuffer.SetData(vertices.ToArray());

                indexBuffer?.Dispose();
                indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
                indexBuffer.SetData(indices.ToArray());

                primitiveCount = indices.Count / 3;
            }
        }

        private float SampleHeightFromGenerator(Vector3 normalizedDirection)
        {
            // Use same UV calculation as ProceduralPlanetGenerator
            float theta = MathF.Atan2(normalizedDirection.X, normalizedDirection.Z);
            float phi = MathF.Asin(MathHelper.Clamp(normalizedDirection.Y, -1f, 1f));

            float u = 0.5f + theta / (2f * MathF.PI);
            float v = 0.5f + phi / MathF.PI;

            // Wrap u coordinate
            u = u - MathF.Floor(u);
            v = MathHelper.Clamp(v, 0f, 1f);

            // Sample from planet generator's heightmap
            return planetGenerator.SampleHeightAtUV(u, v);
        }

        private int GetResolutionForLOD(int lod)
        {
            // Reduced resolution for better performance
            // LOD 0 = 32x32, LOD 1 = 16x16, LOD 2 = 8x8, etc.
            return Math.Max(4, 32 >> lod);
        }

        private Vector2 GetSphericalUV(Vector3 spherePos)
        {
            float u = MathF.Atan2(spherePos.X, spherePos.Z) / (2.0f * MathF.PI) + 0.5f;
            float v = MathF.Asin(MathHelper.Clamp(spherePos.Y, -1.0f, 1.0f)) / MathF.PI + 0.5f;
            return new Vector2(u, v);
        }

        private void CalculateBounds()
        {
            // More accurate bounding sphere - use actual chunk size with some padding
            // The chunk covers ChunkSize fraction of the sphere surface
            float boundingRadius = planetRadius * ChunkSize * 2.5f; // Generous padding to avoid culling visible chunks
            Bounds = new BoundingSphere(centerPosition, boundingRadius);
        }

        public Vector3 GetCenterPosition() => centerPosition;

        public bool ShouldSubdivide(Vector3 cameraPosition)
        {
            float distanceToCamera = Vector3.Distance(centerPosition, cameraPosition);

            // Distance-based subdivision that creates smaller chunks near camera
            // Expanded radius for more gradual transitions and visible detail in distance
            float baseThreshold = planetRadius * 4.0f; // Expanded from 2.0 to 4.0 for more distant LOD transitions
            float lodMultiplier = MathF.Pow(0.5f, LODLevel); // Each level halves the threshold
            float threshold = baseThreshold * lodMultiplier;

            return distanceToCamera < threshold && LODLevel < 8; // Max LOD level 8 for extreme closeups
        }

        public void Draw(GraphicsDevice device)
        {
            if (vertexBuffer == null || indexBuffer == null)
                return;

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
