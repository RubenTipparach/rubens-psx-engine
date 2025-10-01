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

                // If no points are visible, cull the chunk
                if (!anyPointVisible)
                {
                    chunk.Dispose();
                    return;
                }
            }

            // Check if we should subdivide this chunk
            if (!cameraFarAway && chunk.ShouldSubdivide(cameraPosition))
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
