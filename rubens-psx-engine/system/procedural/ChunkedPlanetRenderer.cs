using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine.system.procedural
{
    /// <summary>
    /// Renders a planet using dynamic LOD chunks
    /// </summary>
    public class ChunkedPlanetRenderer : IDisposable
    {
        public float Radius { get; private set; }

        private GraphicsDevice graphicsDevice;
        private ProceduralPlanetGenerator planetGenerator;
        private List<PlanetChunk> activeChunks;

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
        }

        public void UpdateChunks(Vector3 cameraPosition, BoundingFrustum frustum)
        {
            // Clear old chunks
            foreach (var chunk in activeChunks)
            {
                chunk.Dispose();
            }
            activeChunks.Clear();

            // Generate chunks for each cube face
            foreach (var faceNormal in CubeFaces)
            {
                GenerateChunksRecursive(faceNormal, Vector2.Zero, 1.0f, 0, cameraPosition, frustum);
            }
        }

        private void GenerateChunksRecursive(Vector3 localUp, Vector2 offset, float size, int lodLevel, Vector3 cameraPosition, BoundingFrustum frustum)
        {
            // Create chunk
            var chunk = new PlanetChunk(graphicsDevice, planetGenerator, localUp, offset, size, lodLevel, Radius);

            // DISABLED: Frustum culling for performance testing
            //if (frustum != null && frustum.Contains(chunk.Bounds) == ContainmentType.Disjoint)
            //{
            //    chunk.Dispose();
            //    return;
            //}

            // Check if chunk faces away from camera (backface culling)
            Vector3 chunkCenter = chunk.GetCenterPosition();
            Vector3 toCameraDir = Vector3.Normalize(cameraPosition - chunkCenter);
            Vector3 chunkNormal = Vector3.Normalize(chunkCenter);

            // If chunk faces away from camera, don't render it (with some tolerance for edge chunks)
            if (Vector3.Dot(toCameraDir, chunkNormal) < -0.2f)
            {
                chunk.Dispose();
                return;
            }

            // Check if we should subdivide this chunk
            if (chunk.ShouldSubdivide(cameraPosition))
            {
                chunk.Dispose();

                // Subdivide into 4 child chunks
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

        public void Dispose()
        {
            foreach (var chunk in activeChunks)
            {
                chunk.Dispose();
            }
            activeChunks.Clear();
        }
    }
}
