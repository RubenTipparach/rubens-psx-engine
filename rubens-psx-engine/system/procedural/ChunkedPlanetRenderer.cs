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

        // LOD target point (where detail is centered)
        public Vector3 LODTargetPoint { get; private set; }

        // True average radius of the planet (base radius + average terrain height)
        private float trueAverageRadius;
        private bool hasCalculatedTrueRadius = false;

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
            // Calculate true average radius once at startup
            if (!hasCalculatedTrueRadius)
            {
                CalculateTrueAverageRadius();
                hasCalculatedTrueRadius = true;
            }

            // Clear active chunks list but don't dispose - we'll reuse from cache
            activeChunks.Clear();
            currentFrameChunks.Clear();
            chunksGeneratedThisFrame = 0; // Reset chunk counter
            chunkCandidates.Clear(); // Clear candidate list
            currentCameraPosition = cameraPosition;

            // Calculate LOD target point: project camera to nearest point on sphere surface
            Vector3 directionFromCenter = Vector3.Normalize(cameraPosition);

            // Sample height using EXACT same method as vertices (PlanetChunk.cs line 102)
            float height = planetGenerator.SampleHeightAtPosition(directionFromCenter);

            // Apply EXACT same transformation as vertices (PlanetChunk.cs line 106-107)
            float heightScale = Radius * 0.1f * planetGenerator.Parameters.MountainHeight;
            float terrainSurfaceRadius = Radius + height * heightScale;

            // Final target point is at the actual terrain surface
            LODTargetPoint = directionFromCenter * terrainSurfaceRadius;

            // Calculate height above terrain (clamped to 0 minimum - no negative heights)
            float cameraDistanceFromCenter = cameraPosition.Length();
            float heightAboveSurface = MathF.Max(0.0f, cameraDistanceFromCenter - terrainSurfaceRadius);

            // PASS 1: Collect all chunk candidates
            foreach (var faceNormal in CubeFaces)
            {
                CollectChunkCandidatesRecursive(faceNormal, Vector2.Zero, 1.0f, 0, LODTargetPoint, frustum, heightAboveSurface);
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


        private void CollectChunkCandidatesRecursive(Vector3 localUp, Vector2 offset, float size, int lodLevel, Vector3 lodTargetPoint, BoundingFrustum frustum, float heightAboveSurface)
        {
            // Create chunk center position
            Vector3 axisA = new Vector3(localUp.Y, localUp.Z, localUp.X);
            Vector3 axisB = Vector3.Cross(localUp, axisA);
            Vector3 centerPos = localUp * Radius + (axisA * (offset.X + size * 0.5f - 0.5f) + axisB * (offset.Y + size * 0.5f - 0.5f)) * Radius * 2.0f;

            // Distance from chunk to LOD target point (where detail is centered)
            float distanceToTarget = Vector3.Distance(centerPos, lodTargetPoint);

            // Check minimum chunk size
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
                    DistanceToCamera = distanceToTarget
                });
                return;
            }

            // Calculate LOD threshold based on height above terrain
            // heightAboveSurface is already clamped to 0 minimum (no negative heights)
            // When at ground (height = 0): maximum detail
            // When far away: reduced detail
            float heightThreshold = 15.0f; // Height where detail starts to reduce
            float heightFactor = MathHelper.Clamp(heightAboveSurface / heightThreshold, 0.0f, 1.0f);
            // heightMultiplier ranges from 2.0 (at ground, max detail) to 1.0 (far away, normal detail)
            float heightMultiplier = 2.0f - heightFactor;

            float baseThreshold = Radius * 8.0f * heightMultiplier;
            float lodMultiplier = MathF.Pow(0.5f, lodLevel);
            float threshold = baseThreshold * lodMultiplier;

            bool shouldSubdivide = distanceToTarget < threshold && lodLevel < 12;

            if (shouldSubdivide)
            {
                // Subdivide into 4 child chunks
                float childSize = size * 0.5f;
                int childLOD = lodLevel + 1;

                CollectChunkCandidatesRecursive(localUp, offset, childSize, childLOD, lodTargetPoint, frustum, heightAboveSurface);
                CollectChunkCandidatesRecursive(localUp, offset + new Vector2(childSize, 0), childSize, childLOD, lodTargetPoint, frustum, heightAboveSurface);
                CollectChunkCandidatesRecursive(localUp, offset + new Vector2(0, childSize), childSize, childLOD, lodTargetPoint, frustum, heightAboveSurface);
                CollectChunkCandidatesRecursive(localUp, offset + new Vector2(childSize, childSize), childSize, childLOD, lodTargetPoint, frustum, heightAboveSurface);
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
                    DistanceToCamera = distanceToTarget
                });
            }
        }

        private void CalculateTrueAverageRadius()
        {
            // Sample 100 random points across the sphere to get average terrain height
            const int sampleCount = 100;
            float totalRadius = 0.0f;
            Random rand = new Random();

            for (int i = 0; i < sampleCount; i++)
            {
                // Generate random point on sphere using spherical coordinates
                float theta = (float)(rand.NextDouble() * Math.PI); // 0 to PI
                float phi = (float)(rand.NextDouble() * 2.0 * Math.PI); // 0 to 2PI

                float sinTheta = MathF.Sin(theta);
                Vector3 direction = new Vector3(
                    sinTheta * MathF.Cos(phi),
                    MathF.Cos(theta),
                    sinTheta * MathF.Sin(phi)
                );

                // Sample height using EXACT same method as vertices (PlanetChunk.cs line 102)
                float height = planetGenerator.SampleHeightAtPosition(direction);

                // Apply EXACT same transformation as vertices (PlanetChunk.cs line 106-107)
                float heightScale = Radius * 0.1f * planetGenerator.Parameters.MountainHeight;
                float terrainSurfaceRadius = Radius + height * heightScale;

                // Accumulate total radius
                totalRadius += terrainSurfaceRadius;
            }

            // Calculate average
            trueAverageRadius = totalRadius / sampleCount;

            System.Console.WriteLine($"Calculated true average planet radius: {trueAverageRadius:F2} (base: {Radius}, avg offset: {trueAverageRadius - Radius:F2})");
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
