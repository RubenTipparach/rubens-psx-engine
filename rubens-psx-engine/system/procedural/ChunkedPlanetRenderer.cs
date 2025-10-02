using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine.system.procedural
{
    /// <summary>
    /// Chunk key for caching
    /// </summary>
    public struct ChunkKey : IEquatable<ChunkKey>
    {
        public Vector3 LocalUp;
        public Vector2 Offset;
        public float Size;
        public int LODLevel;

        public ChunkKey(Vector3 localUp, Vector2 offset, float size, int lodLevel)
        {
            LocalUp = localUp;
            Offset = offset;
            Size = size;
            LODLevel = lodLevel;
        }

        public bool Equals(ChunkKey other)
        {
            return LocalUp == other.LocalUp &&
                   Offset == other.Offset &&
                   MathF.Abs(Size - other.Size) < 0.0001f &&
                   LODLevel == other.LODLevel;
        }

        public override bool Equals(object obj) => obj is ChunkKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(LocalUp, Offset, Size, LODLevel);
    }

    /// <summary>
    /// Renders a planet using dynamic LOD chunks
    /// </summary>
    public class ChunkedPlanetRenderer : IDisposable
    {
        public float Radius { get; private set; }

        private GraphicsDevice graphicsDevice;
        private ProceduralPlanetGenerator planetGenerator;
        private List<PlanetChunk> activeChunks;
        private Dictionary<ChunkKey, PlanetChunk> chunkCache;
        private HashSet<ChunkKey> currentFrameChunks;

        // Performance limits
        private const int MAX_CHUNKS_PER_FRAME = 5000; // Limit total chunks to prevent performance issues
        private int chunksGeneratedThisFrame = 0;

        // Chunk prioritization
        private struct ChunkCandidate
        {
            public Vector3 LocalUp;
            public Vector2 Offset;
            public float Size;
            public int LODLevel;
            public float DistanceToCamera;
        }
        private List<ChunkCandidate> chunkCandidates;
        private Vector3 currentCameraPosition;

        // The 6 cube face directions
        private static readonly Vector3[] CubeFaces = new Vector3[]
        {
            Vector3.Up,
            Vector3.Down,
            Vector3.Left,
            Vector3.Right,
            Vector3.Forward,
            Vector3.Backward
        };

        public ChunkedPlanetRenderer(GraphicsDevice gd, ProceduralPlanetGenerator generator, float radius)
        {
            graphicsDevice = gd;
            planetGenerator = generator;
            Radius = radius;
            activeChunks = new List<PlanetChunk>();
            chunkCache = new Dictionary<ChunkKey, PlanetChunk>();
            currentFrameChunks = new HashSet<ChunkKey>();
            chunkCandidates = new List<ChunkCandidate>();
        }

        public void UpdateChunks(Vector3 cameraPosition, BoundingFrustum frustum)
        {
            // Clear active chunks list but don't dispose - we'll reuse from cache
            activeChunks.Clear();
            currentFrameChunks.Clear();
            chunksGeneratedThisFrame = 0; // Reset chunk counter
            chunkCandidates.Clear(); // Clear candidate list
            currentCameraPosition = cameraPosition;

            // PASS 1: Collect all chunk candidates
            foreach (var faceNormal in CubeFaces)
            {
                CollectChunkCandidatesRecursive(faceNormal, Vector2.Zero, 1.0f, 0, cameraPosition, frustum);
            }

            // PASS 2: Sort candidates by distance to camera (closest first)
            chunkCandidates.Sort((a, b) => a.DistanceToCamera.CompareTo(b.DistanceToCamera));

            // PASS 3: Generate chunks in priority order (closest first)
            foreach (var candidate in chunkCandidates)
            {
                if (chunksGeneratedThisFrame >= MAX_CHUNKS_PER_FRAME)
                    break; // Hit limit, stop generating

                GenerateChunk(candidate.LocalUp, candidate.Offset, candidate.Size, candidate.LODLevel);
            }

            // Remove unused chunks from cache
            var keysToRemove = new List<ChunkKey>();
            foreach (var kvp in chunkCache)
            {
                if (!currentFrameChunks.Contains(kvp.Key))
                {
                    kvp.Value.Dispose();
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                chunkCache.Remove(key);
            }
        }

        private void GenerateChunksRecursive(Vector3 localUp, Vector2 offset, float size, int lodLevel, Vector3 cameraPosition, BoundingFrustum frustum)
        {
            // Check if we've hit the chunk limit for this frame
            if (chunksGeneratedThisFrame >= MAX_CHUNKS_PER_FRAME)
            {
                return; // Stop generating more chunks to maintain performance
            }

            // Check cache first
            var key = new ChunkKey(localUp, offset, size, lodLevel);

            PlanetChunk chunk;
            if (chunkCache.TryGetValue(key, out chunk))
            {
                // Reuse cached chunk
                currentFrameChunks.Add(key);
            }
            else
            {
                // Create new chunk and cache it
                chunk = new PlanetChunk(graphicsDevice, planetGenerator, localUp, offset, size, lodLevel, Radius);
                chunkCache[key] = chunk;
                currentFrameChunks.Add(key);
            }

            chunksGeneratedThisFrame++; // Increment counter

            // DISABLED: Frustum culling for performance testing
            //if (frustum != null && frustum.Contains(chunk.Bounds) == ContainmentType.Disjoint)
            //{
            //    chunk.Dispose();
            //    return;
            //}

            // Check if camera is far enough to skip LOD and culling
            float distanceToCenter = Vector3.Distance(cameraPosition, Vector3.Zero);
            bool cameraFarAway = distanceToCenter > Radius * 3.0f; // 3x radius threshold

            // When camera is close, only cull chunks on the opposite hemisphere
            // Calculate camera direction from planet center
            Vector3 cameraDir = Vector3.Normalize(cameraPosition);

            // Check if chunk center is on the back hemisphere (more than 90 degrees away from camera direction)
            Vector3 chunkCenter = chunk.GetCenterPosition();
            Vector3 chunkDir = Vector3.Normalize(chunkCenter);
            float hemisphereAlignment = Vector3.Dot(cameraDir, chunkDir);

            // Only cull if chunk is clearly on the back hemisphere (dot < 0)
            if (!cameraFarAway && hemisphereAlignment < -0.2f)
            {
                // Chunk is on back hemisphere, check if it's fully hidden
                // Check all corners to see if any are visible
                Vector3[] corners = chunk.GetCornerPositions();
                bool anyPointVisible = false;

                // Check center first
                Vector3 toCameraDir = Vector3.Normalize(cameraPosition - chunkCenter);
                Vector3 centerNormal = Vector3.Normalize(chunkCenter);
                if (Vector3.Dot(toCameraDir, centerNormal) > -0.1f)
                {
                    anyPointVisible = true;
                }

                // Check corners if center wasn't visible
                if (!anyPointVisible)
                {
                    foreach (Vector3 corner in corners)
                    {
                        toCameraDir = Vector3.Normalize(cameraPosition - corner);
                        Vector3 cornerNormal = Vector3.Normalize(corner);

                        // Check if this corner faces the camera
                        if (Vector3.Dot(toCameraDir, cornerNormal) > -0.1f)
                        {
                            anyPointVisible = true;
                            break;
                        }
                    }
                }

                // If no points are visible, cull the chunk (don't add to active list)
                if (!anyPointVisible)
                {
                    return;
                }
            }

            // Check if we should subdivide this chunk
            if (!cameraFarAway && chunk.ShouldSubdivide(cameraPosition))
            {
                // Subdivide into 4 child chunks (don't dispose - chunk stays in cache)
                float childSize = size * 0.5f;
                int childLOD = lodLevel + 1;

                GenerateChunksRecursive(localUp, offset, childSize, childLOD, cameraPosition, frustum);
                GenerateChunksRecursive(localUp, offset + new Vector2(childSize, 0), childSize, childLOD, cameraPosition, frustum);
                GenerateChunksRecursive(localUp, offset + new Vector2(0, childSize), childSize, childLOD, cameraPosition, frustum);
                GenerateChunksRecursive(localUp, offset + new Vector2(childSize, childSize), childSize, childLOD, cameraPosition, frustum);
            }
            else
            {
                // Use this chunk
                activeChunks.Add(chunk);
            }
        }

        private void CollectChunkCandidatesRecursive(Vector3 localUp, Vector2 offset, float size, int lodLevel, Vector3 cameraPosition, BoundingFrustum frustum)
        {
            // Create temporary chunk to check subdivision
            Vector3 axisA = new Vector3(localUp.Y, localUp.Z, localUp.X);
            Vector3 axisB = Vector3.Cross(localUp, axisA);
            Vector3 centerPos = localUp * Radius + (axisA * (offset.X + size * 0.5f - 0.5f) + axisB * (offset.Y + size * 0.5f - 0.5f)) * Radius * 2.0f;

            float distanceToCamera = Vector3.Distance(centerPos, cameraPosition);

            // Check minimum chunk size (same as PlanetChunk.ShouldSubdivide)
            float currentChunkSize = size * Radius * 2.0f;
            const float MIN_CHUNK_SIZE = 0.1f;

            if (currentChunkSize <= MIN_CHUNK_SIZE)
            {
                // Add as candidate without subdividing
                chunkCandidates.Add(new ChunkCandidate
                {
                    LocalUp = localUp,
                    Offset = offset,
                    Size = size,
                    LODLevel = lodLevel,
                    DistanceToCamera = distanceToCamera
                });
                return;
            }

            // Calculate if should subdivide (same logic as PlanetChunk.ShouldSubdivide)
            float distanceFromPlanetCenter = cameraPosition.Length();
            float heightAboveSurface = distanceFromPlanetCenter - Radius;
            float heightThreshold = 2.0f; // Dense detail only below 2 units above ground
            float heightFactor = MathHelper.Clamp(heightAboveSurface / heightThreshold, 0.5f, 1.0f);
            float heightMultiplier = 1.0f / heightFactor;
            float baseThreshold = Radius * 8.0f * heightMultiplier;
            float lodMultiplier = MathF.Pow(0.5f, lodLevel);
            float threshold = baseThreshold * lodMultiplier;

            bool shouldSubdivide = distanceToCamera < threshold && lodLevel < 12;

            if (shouldSubdivide)
            {
                // Subdivide into 4 child chunks
                float childSize = size * 0.5f;
                int childLOD = lodLevel + 1;

                CollectChunkCandidatesRecursive(localUp, offset, childSize, childLOD, cameraPosition, frustum);
                CollectChunkCandidatesRecursive(localUp, offset + new Vector2(childSize, 0), childSize, childLOD, cameraPosition, frustum);
                CollectChunkCandidatesRecursive(localUp, offset + new Vector2(0, childSize), childSize, childLOD, cameraPosition, frustum);
                CollectChunkCandidatesRecursive(localUp, offset + new Vector2(childSize, childSize), childSize, childLOD, cameraPosition, frustum);
            }
            else
            {
                // Add this chunk as a candidate
                chunkCandidates.Add(new ChunkCandidate
                {
                    LocalUp = localUp,
                    Offset = offset,
                    Size = size,
                    LODLevel = lodLevel,
                    DistanceToCamera = distanceToCamera
                });
            }
        }

        private void GenerateChunk(Vector3 localUp, Vector2 offset, float size, int lodLevel)
        {
            var key = new ChunkKey(localUp, offset, size, lodLevel);

            PlanetChunk chunk;
            if (chunkCache.TryGetValue(key, out chunk))
            {
                // Reuse cached chunk
                currentFrameChunks.Add(key);
            }
            else
            {
                // Create new chunk and cache it
                chunk = new PlanetChunk(graphicsDevice, planetGenerator, localUp, offset, size, lodLevel, Radius);
                chunkCache[key] = chunk;
                currentFrameChunks.Add(key);
            }

            chunksGeneratedThisFrame++;
            activeChunks.Add(chunk);
        }

        public void Draw(GraphicsDevice device, Matrix world, Matrix view, Matrix projection, Effect shader, bool wireframe)
        {
            // Set common shader parameters
            shader.Parameters["World"]?.SetValue(world);
            shader.Parameters["View"]?.SetValue(view);
            shader.Parameters["Projection"]?.SetValue(projection);
            shader.Parameters["WorldInverseTranspose"]?.SetValue(Matrix.Transpose(Matrix.Invert(world)));
            shader.Parameters["HeightmapTexture"]?.SetValue(planetGenerator.HeightmapTexture);

            // Extract camera position from view matrix
            Matrix inverseView = Matrix.Invert(view);
            Vector3 cameraPosition = inverseView.Translation;
            shader.Parameters["CameraPosition"]?.SetValue(cameraPosition);

            // Set wireframe mode
            var previousRasterizer = device.RasterizerState;
            if (wireframe)
            {
                var wireframeState = new RasterizerState();
                wireframeState.FillMode = FillMode.WireFrame;
                wireframeState.CullMode = CullMode.None;
                device.RasterizerState = wireframeState;
            }

            // Draw all active chunks
            foreach (var chunk in activeChunks)
            {
                foreach (var pass in shader.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    chunk.Draw(device);
                }
            }

            // Restore rasterizer state
            if (wireframe)
            {
                device.RasterizerState = previousRasterizer;
            }
        }

        public int GetActiveChunkCount() => activeChunks.Count;

        public void ClearCache()
        {
            // Dispose all cached chunks
            foreach (var chunk in chunkCache.Values)
            {
                chunk.Dispose();
            }
            chunkCache.Clear();
            activeChunks.Clear();
            currentFrameChunks.Clear();
        }

        public void Dispose()
        {
            foreach (var chunk in activeChunks)
            {
                chunk.Dispose();
            }
            activeChunks.Clear();

            foreach (var chunk in chunkCache.Values)
            {
                chunk.Dispose();
            }
            chunkCache.Clear();
        }
    }
}
