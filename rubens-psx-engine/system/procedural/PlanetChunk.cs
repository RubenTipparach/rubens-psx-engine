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
        private ChunkNeighborInfo neighborInfo;

        public PlanetChunk(GraphicsDevice gd, ProceduralPlanetGenerator generator, Vector3 localUp, Vector2 offset, float size, int lodLevel, float radius, ChunkNeighborInfo neighbors = null)
        {
            graphicsDevice = gd;
            planetGenerator = generator;
            LocalUp = localUp;
            ChunkOffset = offset;
            ChunkSize = size;
            LODLevel = lodLevel;
            planetRadius = radius;
            neighborInfo = neighbors ?? new ChunkNeighborInfo();

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

                    // Apply height offset using slider value (height can be negative for underwater)
                    // Use the generator's MountainHeight parameter which is controlled by the slider
                    float heightScale = planetRadius * 0.1f * planetGenerator.Parameters.MountainHeight;
                    Vector3 position = pointOnSphere * (planetRadius + height * heightScale);

                    sumPosition += position;

                    // Calculate UV for texturing
                    Vector2 uv = GetSphericalUV(pointOnSphere);

                    vertices.Add(new VertexPositionNormalTexture(position, pointOnSphere, uv));
                }
            }

            // Calculate center position for backface culling
            centerPosition = sumPosition / vertices.Count;

            // Stitch edge vertices to match coarser neighbors (eliminates T-junctions)
            StitchEdgesToNeighbors(vertices, resolution);

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

            // Fix UV seams by duplicating vertices where triangles cross the UV wrap boundary
            FixUVSeams(ref vertices, ref indices);

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

        private void StitchEdgesToNeighbors(List<VertexPositionNormalTexture> vertices, int resolution)
        {
            // Stitch edges to neighbors with lower LOD by averaging vertex positions
            // This eliminates T-junctions: vertices that don't exist in the coarser neighbor
            // are moved to lie exactly on the line between their neighbors

            // Left edge (x = 0)
            if (neighborInfo.LeftNeighborLOD >= 0 && neighborInfo.LeftNeighborLOD < LODLevel)
            {
                int neighborRes = GetResolutionForLOD(neighborInfo.LeftNeighborLOD);
                int ratio = resolution / neighborRes;

                for (int y = 0; y <= resolution; y++)
                {
                    // Skip vertices that exist in both resolutions
                    if (y % ratio != 0)
                    {
                        int idx = y * (resolution + 1);
                        int prevIdx = (y / ratio) * ratio * (resolution + 1);
                        int nextIdx = ((y / ratio) + 1) * ratio * (resolution + 1);

                        // Average position with neighbors
                        var v = vertices[idx];
                        v.Position = (vertices[prevIdx].Position + vertices[nextIdx].Position) * 0.5f;
                        v.Normal = Vector3.Normalize((vertices[prevIdx].Normal + vertices[nextIdx].Normal) * 0.5f);
                        vertices[idx] = v;
                    }
                }
            }

            // Right edge (x = resolution)
            if (neighborInfo.RightNeighborLOD >= 0 && neighborInfo.RightNeighborLOD < LODLevel)
            {
                int neighborRes = GetResolutionForLOD(neighborInfo.RightNeighborLOD);
                int ratio = resolution / neighborRes;

                for (int y = 0; y <= resolution; y++)
                {
                    if (y % ratio != 0)
                    {
                        int idx = y * (resolution + 1) + resolution;
                        int prevIdx = (y / ratio) * ratio * (resolution + 1) + resolution;
                        int nextIdx = ((y / ratio) + 1) * ratio * (resolution + 1) + resolution;

                        var v = vertices[idx];
                        v.Position = (vertices[prevIdx].Position + vertices[nextIdx].Position) * 0.5f;
                        v.Normal = Vector3.Normalize((vertices[prevIdx].Normal + vertices[nextIdx].Normal) * 0.5f);
                        vertices[idx] = v;
                    }
                }
            }

            // Bottom edge (y = 0)
            if (neighborInfo.BottomNeighborLOD >= 0 && neighborInfo.BottomNeighborLOD < LODLevel)
            {
                int neighborRes = GetResolutionForLOD(neighborInfo.BottomNeighborLOD);
                int ratio = resolution / neighborRes;

                for (int x = 0; x <= resolution; x++)
                {
                    if (x % ratio != 0)
                    {
                        int idx = x;
                        int prevIdx = (x / ratio) * ratio;
                        int nextIdx = ((x / ratio) + 1) * ratio;

                        var v = vertices[idx];
                        v.Position = (vertices[prevIdx].Position + vertices[nextIdx].Position) * 0.5f;
                        v.Normal = Vector3.Normalize((vertices[prevIdx].Normal + vertices[nextIdx].Normal) * 0.5f);
                        vertices[idx] = v;
                    }
                }
            }

            // Top edge (y = resolution)
            if (neighborInfo.TopNeighborLOD >= 0 && neighborInfo.TopNeighborLOD < LODLevel)
            {
                int neighborRes = GetResolutionForLOD(neighborInfo.TopNeighborLOD);
                int ratio = resolution / neighborRes;

                for (int x = 0; x <= resolution; x++)
                {
                    if (x % ratio != 0)
                    {
                        int idx = resolution * (resolution + 1) + x;
                        int prevIdx = resolution * (resolution + 1) + (x / ratio) * ratio;
                        int nextIdx = resolution * (resolution + 1) + ((x / ratio) + 1) * ratio;

                        var v = vertices[idx];
                        v.Position = (vertices[prevIdx].Position + vertices[nextIdx].Position) * 0.5f;
                        v.Normal = Vector3.Normalize((vertices[prevIdx].Normal + vertices[nextIdx].Normal) * 0.5f);
                        vertices[idx] = v;
                    }
                }
            }
        }

        private float SampleHeightFromGenerator(Vector3 normalizedDirection)
        {
            // Use procedural sampling to avoid texture quantization artifacts
            return planetGenerator.SampleHeightAtPosition(normalizedDirection);
        }

        private int GetResolutionForLOD(int lod)
        {
            // Reduced resolution for better performance
            // LOD 0 = 32x32, LOD 1 = 16x16, LOD 2 = 8x8, etc.
            return Math.Max(4, 32 >> lod);
        }

        private Vector2 GetSphericalUV(Vector3 spherePos)
        {
            // Match heightmap generation exactly (ProceduralPlanetGenerator.cs lines 128-136)
            // X = sin(phi) * cos(theta), Y = cos(phi), Z = sin(phi) * sin(theta)
            // Invert: phi = acos(Y), theta = atan2(Z, X)

            float phi = MathF.Acos(MathHelper.Clamp(spherePos.Y, -1.0f, 1.0f)); // [0, PI]
            float theta = MathF.Atan2(spherePos.Z, spherePos.X); // [-PI, PI]

            // Convert back to UV
            float u = theta / (2.0f * MathF.PI); // [-0.5, 0.5]
            if (u < 0) u += 1.0f; // [0, 1]
            float v = phi / MathF.PI; // [0, 1]

            return new Vector2(u, v);
        }

        private void FixUVSeams(ref List<VertexPositionNormalTexture> vertices, ref List<int> indices)
        {
            // Fix UV seams by duplicating vertices where triangles cross the UV wrap boundary
            for (int i = 0; i < indices.Count; i += 3)
            {
                int i0 = indices[i];
                int i1 = indices[i + 1];
                int i2 = indices[i + 2];

                Vector2 uv0 = vertices[i0].TextureCoordinate;
                Vector2 uv1 = vertices[i1].TextureCoordinate;
                Vector2 uv2 = vertices[i2].TextureCoordinate;

                // Check if triangle crosses the UV seam (U wraps from 1 to 0)
                float maxU = MathF.Max(MathF.Max(uv0.X, uv1.X), uv2.X);
                float minU = MathF.Min(MathF.Min(uv0.X, uv1.X), uv2.X);
                bool crossesSeam = (maxU - minU) > 0.5f;

                if (crossesSeam)
                {
                    // Duplicate vertices that are on the wrong side of the seam
                    // Average U coordinate to determine which side of the seam we're on
                    float avgU = (uv0.X + uv1.X + uv2.X) / 3.0f;

                    // Fix vertex 0 if needed
                    if (MathF.Abs(uv0.X - avgU) > 0.4f)
                    {
                        var newVertex = vertices[i0];
                        var newUV = newVertex.TextureCoordinate;
                        newUV.X = uv0.X < 0.5f ? uv0.X + 1.0f : uv0.X - 1.0f;
                        newVertex.TextureCoordinate = newUV;
                        i0 = vertices.Count;
                        vertices.Add(newVertex);
                    }

                    // Fix vertex 1 if needed
                    if (MathF.Abs(uv1.X - avgU) > 0.4f)
                    {
                        var newVertex = vertices[i1];
                        var newUV = newVertex.TextureCoordinate;
                        newUV.X = uv1.X < 0.5f ? uv1.X + 1.0f : uv1.X - 1.0f;
                        newVertex.TextureCoordinate = newUV;
                        i1 = vertices.Count;
                        vertices.Add(newVertex);
                    }

                    // Fix vertex 2 if needed
                    if (MathF.Abs(uv2.X - avgU) > 0.4f)
                    {
                        var newVertex = vertices[i2];
                        var newUV = newVertex.TextureCoordinate;
                        newUV.X = uv2.X < 0.5f ? uv2.X + 1.0f : uv2.X - 1.0f;
                        newVertex.TextureCoordinate = newUV;
                        i2 = vertices.Count;
                        vertices.Add(newVertex);
                    }

                    // Update indices to point to new vertices
                    indices[i] = i0;
                    indices[i + 1] = i1;
                    indices[i + 2] = i2;
                }
            }
        }

        private void CalculateBounds()
        {
            // More accurate bounding sphere - use actual chunk size with some padding
            // The chunk covers ChunkSize fraction of the sphere surface
            float boundingRadius = planetRadius * ChunkSize * 2.5f; // Generous padding to avoid culling visible chunks
            Bounds = new BoundingSphere(centerPosition, boundingRadius);
        }

        public Vector3 GetCenterPosition() => centerPosition;

        public Vector3[] GetCornerPositions()
        {
            // Calculate the 4 corners of the chunk
            Vector3 axisA = new Vector3(LocalUp.Y, LocalUp.Z, LocalUp.X);
            Vector3 axisB = Vector3.Cross(LocalUp, axisA);

            Vector3[] corners = new Vector3[4];

            // Bottom-left (0, 0)
            Vector2 cubePos = ChunkOffset;
            Vector3 pointOnCube = LocalUp + (cubePos.X * 2f - 1f) * axisA + (cubePos.Y * 2f - 1f) * axisB;
            corners[0] = Vector3.Normalize(pointOnCube) * planetRadius;

            // Bottom-right (1, 0)
            cubePos = ChunkOffset + new Vector2(ChunkSize, 0);
            pointOnCube = LocalUp + (cubePos.X * 2f - 1f) * axisA + (cubePos.Y * 2f - 1f) * axisB;
            corners[1] = Vector3.Normalize(pointOnCube) * planetRadius;

            // Top-left (0, 1)
            cubePos = ChunkOffset + new Vector2(0, ChunkSize);
            pointOnCube = LocalUp + (cubePos.X * 2f - 1f) * axisA + (cubePos.Y * 2f - 1f) * axisB;
            corners[2] = Vector3.Normalize(pointOnCube) * planetRadius;

            // Top-right (1, 1)
            cubePos = ChunkOffset + new Vector2(ChunkSize, ChunkSize);
            pointOnCube = LocalUp + (cubePos.X * 2f - 1f) * axisA + (cubePos.Y * 2f - 1f) * axisB;
            corners[3] = Vector3.Normalize(pointOnCube) * planetRadius;

            return corners;
        }

        public bool ShouldSubdivide(Vector3 cameraPosition)
        {
            float distanceToCamera = Vector3.Distance(centerPosition, cameraPosition);

            // Calculate minimum chunk size to prevent too much detail
            // At LOD 0, size is 1.0 (entire cube face side)
            // Each subdivision halves the size
            float currentChunkSize = ChunkSize * planetRadius * 2.0f; // Approximate size in world units
            const float MIN_CHUNK_SIZE = 0.1f; // Minimum 0.1 meter chunks

            if (currentChunkSize <= MIN_CHUNK_SIZE)
            {
                return false; // Don't subdivide further
            }

            // Calculate height above ground for adaptive LOD
            float distanceFromPlanetCenter = cameraPosition.Length();
            float heightAboveSurface = distanceFromPlanetCenter - planetRadius;

            // Scale LOD distance based on height above ground
            // When close to ground: INCREASE detail (LARGER threshold means more chunks subdivide)
            // When far from ground: DECREASE detail (SMALLER threshold means fewer chunks subdivide)
            // Below heightThreshold: maintain maximum detail
            float heightOffset = 5.0f; // Offset to maintain max detail even below ground
            float heightThreshold = 15.0f; // Height where detail starts to reduce
            float adjustedHeight = heightAboveSurface + heightOffset;
            float heightFactor = MathHelper.Clamp(adjustedHeight / heightThreshold, 0.0f, 1.0f);
            // heightMultiplier ranges from 2.0 (at/below ground, max detail) to 1.0 (far away, normal detail)
            float heightMultiplier = 2.0f - heightFactor;

            // Distance-based subdivision that creates smaller chunks near camera
            float baseThreshold = planetRadius * 8.0f * heightMultiplier; // Multiply by height factor
            float lodMultiplier = MathF.Pow(0.5f, LODLevel); // Each level halves the threshold
            float threshold = baseThreshold * lodMultiplier;

            return distanceToCamera < threshold && LODLevel < 12; // Increased max LOD for ground-level detail
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
