using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine;
using rubens_psx_engine.entities;
using anakinsoft.system;
using anakinsoft.entities;
using anakinsoft.game.scenes.lounge.characters;
using anakinsoft.game.scenes.lounge.evidence;
using System;
using System.Collections.Generic;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Handles all debug visualization rendering for The Lounge scene
    /// Including wireframes, colliders, interaction points, and evidence table grid
    /// </summary>
    public class LoungeDebugVisualizer
    {
        public void DrawStaticMeshWireframes(List<List<Vector3>> meshTriangleVertices,
            List<(Vector3 position, Quaternion rotation)> staticMeshTransforms, Camera camera)
        {
            if (meshTriangleVertices == null || staticMeshTransforms == null) return;

            var graphicsDevice = Globals.screenManager.GraphicsDevice;
            var basicEffect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                View = camera.View,
                Projection = camera.Projection,
                World = Matrix.Identity
            };

            // Draw wireframes for each static mesh
            for (int meshIndex = 0; meshIndex < meshTriangleVertices.Count; meshIndex++)
            {
                if (meshIndex < staticMeshTransforms.Count)
                {
                    var triangleVertices = meshTriangleVertices[meshIndex];
                    var (position, rotation) = staticMeshTransforms[meshIndex];

                    if (triangleVertices != null && triangleVertices.Count > 0)
                    {
                        // Create transform matrix for this static mesh
                        var worldMatrix = Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position);

                        // Create wireframe edges from triangles
                        var wireframeVertices = new List<VertexPositionColor>();

                        for (int i = 0; i < triangleVertices.Count; i += 3)
                        {
                            if (i + 2 < triangleVertices.Count)
                            {
                                // Apply world transform to vertices
                                var v1 = Vector3.Transform(triangleVertices[i], worldMatrix);
                                var v2 = Vector3.Transform(triangleVertices[i + 1], worldMatrix);
                                var v3 = Vector3.Transform(triangleVertices[i + 2], worldMatrix);

                                // Create the three edges of the triangle
                                wireframeVertices.Add(new VertexPositionColor(v1, Color.Yellow));
                                wireframeVertices.Add(new VertexPositionColor(v2, Color.Yellow));
                                wireframeVertices.Add(new VertexPositionColor(v2, Color.Yellow));
                                wireframeVertices.Add(new VertexPositionColor(v3, Color.Yellow));
                                wireframeVertices.Add(new VertexPositionColor(v3, Color.Yellow));
                                wireframeVertices.Add(new VertexPositionColor(v1, Color.Yellow));
                            }
                        }

                        if (wireframeVertices.Count > 0)
                        {
                            try
                            {
                                basicEffect.CurrentTechnique.Passes[0].Apply();
                                graphicsDevice.DrawUserPrimitives(
                                    PrimitiveType.LineList,
                                    wireframeVertices.ToArray(),
                                    0,
                                    wireframeVertices.Count / 2);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error drawing static mesh wireframe: {ex.Message}");
                            }
                        }
                    }
                }
            }

            basicEffect.Dispose();
        }

        public void DrawCharacterWireframe(SkinnedRenderingEntity character, Camera camera, Color color)
        {
            if (character?.Model == null) return;

            var graphicsDevice = Globals.screenManager.GraphicsDevice;
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = camera.View;
            basicEffect.Projection = camera.Projection;
            basicEffect.World = Matrix.Identity;

            var worldMatrix = character.GetWorldMatrix();

            // Draw bounding box
            var boundingBox = GetModelBoundingBox(character.Model, worldMatrix);
            DrawBoundingBox(boundingBox, basicEffect, graphicsDevice, color);

            // Draw mesh wireframe
            DrawModelWireframe(character.Model, worldMatrix, basicEffect, graphicsDevice, color);

            basicEffect.Dispose();
        }

        public void DrawCharacterCollider(LoungeCharacterData characterData, Camera camera, InteractionSystem interactionSystem)
        {
            if (characterData?.Interaction == null) return;

            var graphicsDevice = Globals.screenManager.GraphicsDevice;
            var basicEffect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                View = camera.View,
                Projection = camera.Projection,
                World = Matrix.Identity
            };

            // Check if character is being targeted
            bool isTargeted = interactionSystem?.CurrentTarget == characterData.Interaction;
            Color colliderColor = isTargeted ? Color.Yellow : Color.Cyan;

            // Use the exact same dimensions and position as the physics collider
            DrawDebugBox(characterData.ColliderCenter, characterData.ColliderWidth,
                characterData.ColliderHeight, characterData.ColliderDepth,
                colliderColor, basicEffect, graphicsDevice);

            basicEffect.Dispose();
        }

        public void DrawInteractionDebugVisualization(InteractableCharacter character, EvidenceTable evidenceTable, Camera camera)
        {
            if (character == null) return;

            var graphicsDevice = Globals.screenManager.GraphicsDevice;
            var basicEffect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                View = camera.View,
                Projection = camera.Projection,
                World = Matrix.Identity
            };

            var vertices = new List<VertexPositionColor>();

            // Draw sphere at character position (Green)
            DrawDebugSphere(character.Position, 5f, Color.Green, vertices);

            // Draw sphere at interaction camera position (Cyan)
            DrawDebugSphere(character.CameraInteractionPosition, 5f, Color.Cyan, vertices);

            // Draw line from camera position to look-at position (Yellow)
            vertices.Add(new VertexPositionColor(character.CameraInteractionPosition, Color.Yellow));
            vertices.Add(new VertexPositionColor(character.CameraInteractionLookAt, Color.Yellow));

            // Draw sphere at look-at position (Magenta)
            DrawDebugSphere(character.CameraInteractionLookAt, 3f, Color.Magenta, vertices);

            // Draw evidence table grid
            if (evidenceTable != null)
                DrawEvidenceTableGrid(evidenceTable, vertices);

            if (vertices.Count > 0)
            {
                try
                {
                    basicEffect.CurrentTechnique.Passes[0].Apply();
                    graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.LineList,
                        vertices.ToArray(),
                        0,
                        vertices.Count / 2);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error drawing interaction debug: {ex.Message}");
                }
            }

            basicEffect.Dispose();
        }

        public void DrawSuspectsFileBox(SuspectsFile file, Camera camera)
        {
            if (file == null) return;

            var graphicsDevice = Globals.screenManager.GraphicsDevice;
            var basicEffect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                View = camera.View,
                Projection = camera.Projection,
                World = Matrix.Identity
            };

            // Draw bounding box - yellow when targeted, white otherwise
            Color boxColor = file.IsTargeted ? Color.Yellow : Color.White;
            DrawBoundingBox(file.BoundingBox, basicEffect, graphicsDevice, boxColor);

            basicEffect.Dispose();
        }

        public void DrawAutopsyReportBox(AutopsyReport report, Camera camera)
        {
            if (report == null) return;

            var graphicsDevice = Globals.screenManager.GraphicsDevice;
            var basicEffect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                View = camera.View,
                Projection = camera.Projection,
                World = Matrix.Identity
            };

            // Draw bounding box - yellow when targeted, white otherwise
            Color boxColor = report.IsTargeted ? Color.Yellow : Color.White;
            DrawBoundingBox(report.BoundingBox, basicEffect, graphicsDevice, boxColor);

            basicEffect.Dispose();
        }

        private Microsoft.Xna.Framework.BoundingBox GetModelBoundingBox(Model model, Matrix worldMatrix)
        {
            // Initialize with max/min values
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    // Get vertex data
                    var vertexBuffer = part.VertexBuffer;
                    var vertexDeclaration = vertexBuffer.VertexDeclaration;
                    var vertexSize = vertexDeclaration.VertexStride;
                    var vertexCount = part.NumVertices;

                    byte[] vertexData = new byte[vertexCount * vertexSize];
                    vertexBuffer.GetData(
                        part.VertexOffset * vertexSize,
                        vertexData,
                        0,
                        vertexCount * vertexSize);

                    // Find position element offset
                    int positionOffset = 0;
                    foreach (var element in vertexDeclaration.GetVertexElements())
                    {
                        if (element.VertexElementUsage == VertexElementUsage.Position)
                        {
                            positionOffset = element.Offset;
                            break;
                        }
                    }

                    // Extract positions and transform them
                    Matrix meshTransform = transforms[mesh.ParentBone.Index] * worldMatrix;
                    for (int i = 0; i < vertexCount; i++)
                    {
                        Vector3 position = new Vector3(
                            BitConverter.ToSingle(vertexData, i * vertexSize + positionOffset),
                            BitConverter.ToSingle(vertexData, i * vertexSize + positionOffset + 4),
                            BitConverter.ToSingle(vertexData, i * vertexSize + positionOffset + 8));

                        Vector3 transformedPosition = Vector3.Transform(position, meshTransform);

                        min = Vector3.Min(min, transformedPosition);
                        max = Vector3.Max(max, transformedPosition);
                    }
                }
            }

            return new Microsoft.Xna.Framework.BoundingBox(min, max);
        }

        private void DrawBoundingBox(Microsoft.Xna.Framework.BoundingBox box, BasicEffect effect, GraphicsDevice graphicsDevice, Color color)
        {
            Vector3[] corners = box.GetCorners();
            var vertices = new List<VertexPositionColor>();

            // Bottom face
            vertices.Add(new VertexPositionColor(corners[0], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[3], color));
            vertices.Add(new VertexPositionColor(corners[3], color));
            vertices.Add(new VertexPositionColor(corners[0], color));

            // Top face
            vertices.Add(new VertexPositionColor(corners[4], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[7], color));
            vertices.Add(new VertexPositionColor(corners[7], color));
            vertices.Add(new VertexPositionColor(corners[4], color));

            // Vertical edges
            vertices.Add(new VertexPositionColor(corners[0], color));
            vertices.Add(new VertexPositionColor(corners[4], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[3], color));
            vertices.Add(new VertexPositionColor(corners[7], color));

            effect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.DrawUserPrimitives(
                PrimitiveType.LineList,
                vertices.ToArray(),
                0,
                vertices.Count / 2);
        }

        private void DrawModelWireframe(Model model, Matrix worldMatrix, BasicEffect effect, GraphicsDevice graphicsDevice, Color color)
        {
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                Matrix meshWorld = transforms[mesh.ParentBone.Index] * worldMatrix;

                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    // Get vertex and index data
                    var vertexBuffer = part.VertexBuffer;
                    var indexBuffer = part.IndexBuffer;
                    var vertexDeclaration = vertexBuffer.VertexDeclaration;
                    var vertexSize = vertexDeclaration.VertexStride;

                    // Read vertex data
                    byte[] vertexData = new byte[part.NumVertices * vertexSize];
                    vertexBuffer.GetData(
                        part.VertexOffset * vertexSize,
                        vertexData,
                        0,
                        part.NumVertices * vertexSize);

                    // Find position offset
                    int positionOffset = 0;
                    foreach (var element in vertexDeclaration.GetVertexElements())
                    {
                        if (element.VertexElementUsage == VertexElementUsage.Position)
                        {
                            positionOffset = element.Offset;
                            break;
                        }
                    }

                    // Read positions
                    Vector3[] positions = new Vector3[part.NumVertices];
                    for (int i = 0; i < part.NumVertices; i++)
                    {
                        positions[i] = new Vector3(
                            BitConverter.ToSingle(vertexData, i * vertexSize + positionOffset),
                            BitConverter.ToSingle(vertexData, i * vertexSize + positionOffset + 4),
                            BitConverter.ToSingle(vertexData, i * vertexSize + positionOffset + 8));
                    }

                    // Read index data
                    var indexElementSize = indexBuffer.IndexElementSize == IndexElementSize.SixteenBits ? 2 : 4;
                    byte[] indexData = new byte[part.PrimitiveCount * 3 * indexElementSize];
                    indexBuffer.GetData(
                        part.StartIndex * indexElementSize,
                        indexData,
                        0,
                        part.PrimitiveCount * 3 * indexElementSize);

                    // Build wireframe lines
                    var wireframeVertices = new List<VertexPositionColor>();
                    for (int i = 0; i < part.PrimitiveCount * 3; i += 3)
                    {
                        int idx0, idx1, idx2;
                        if (indexElementSize == 2)
                        {
                            idx0 = BitConverter.ToUInt16(indexData, i * 2) - part.VertexOffset;
                            idx1 = BitConverter.ToUInt16(indexData, (i + 1) * 2) - part.VertexOffset;
                            idx2 = BitConverter.ToUInt16(indexData, (i + 2) * 2) - part.VertexOffset;
                        }
                        else
                        {
                            idx0 = BitConverter.ToInt32(indexData, i * 4) - part.VertexOffset;
                            idx1 = BitConverter.ToInt32(indexData, (i + 1) * 4) - part.VertexOffset;
                            idx2 = BitConverter.ToInt32(indexData, (i + 2) * 4) - part.VertexOffset;
                        }

                        if (idx0 >= 0 && idx0 < positions.Length &&
                            idx1 >= 0 && idx1 < positions.Length &&
                            idx2 >= 0 && idx2 < positions.Length)
                        {
                            Vector3 v0 = Vector3.Transform(positions[idx0], meshWorld);
                            Vector3 v1 = Vector3.Transform(positions[idx1], meshWorld);
                            Vector3 v2 = Vector3.Transform(positions[idx2], meshWorld);

                            // Three edges of the triangle
                            wireframeVertices.Add(new VertexPositionColor(v0, color));
                            wireframeVertices.Add(new VertexPositionColor(v1, color));
                            wireframeVertices.Add(new VertexPositionColor(v1, color));
                            wireframeVertices.Add(new VertexPositionColor(v2, color));
                            wireframeVertices.Add(new VertexPositionColor(v2, color));
                            wireframeVertices.Add(new VertexPositionColor(v0, color));
                        }
                    }

                    if (wireframeVertices.Count > 0)
                    {
                        try
                        {
                            effect.CurrentTechnique.Passes[0].Apply();
                            graphicsDevice.DrawUserPrimitives(
                                PrimitiveType.LineList,
                                wireframeVertices.ToArray(),
                                0,
                                wireframeVertices.Count / 2);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error drawing character wireframe: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void DrawDebugBox(Vector3 center, float width, float height, float depth, Color color, BasicEffect effect, GraphicsDevice graphicsDevice)
        {
            var vertices = new List<VertexPositionColor>();

            // Calculate half extents
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;
            float halfDepth = depth / 2f;

            // Define 8 corners of the box
            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-halfWidth, -halfHeight, -halfDepth);
            corners[1] = center + new Vector3(halfWidth, -halfHeight, -halfDepth);
            corners[2] = center + new Vector3(halfWidth, -halfHeight, halfDepth);
            corners[3] = center + new Vector3(-halfWidth, -halfHeight, halfDepth);
            corners[4] = center + new Vector3(-halfWidth, halfHeight, -halfDepth);
            corners[5] = center + new Vector3(halfWidth, halfHeight, -halfDepth);
            corners[6] = center + new Vector3(halfWidth, halfHeight, halfDepth);
            corners[7] = center + new Vector3(-halfWidth, halfHeight, halfDepth);

            // Bottom face edges
            vertices.Add(new VertexPositionColor(corners[0], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[3], color));
            vertices.Add(new VertexPositionColor(corners[3], color));
            vertices.Add(new VertexPositionColor(corners[0], color));

            // Top face edges
            vertices.Add(new VertexPositionColor(corners[4], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[7], color));
            vertices.Add(new VertexPositionColor(corners[7], color));
            vertices.Add(new VertexPositionColor(corners[4], color));

            // Vertical edges
            vertices.Add(new VertexPositionColor(corners[0], color));
            vertices.Add(new VertexPositionColor(corners[4], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[3], color));
            vertices.Add(new VertexPositionColor(corners[7], color));

            if (vertices.Count > 0)
            {
                try
                {
                    effect.CurrentTechnique.Passes[0].Apply();
                    graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.LineList,
                        vertices.ToArray(),
                        0,
                        vertices.Count / 2);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error drawing debug box: {ex.Message}");
                }
            }
        }

        private void DrawDebugSphere(Vector3 center, float radius, Color color, List<VertexPositionColor> vertices)
        {
            const int segments = 16;
            float angleStep = Microsoft.Xna.Framework.MathHelper.TwoPi / segments;

            // Draw three circles (XY, XZ, YZ planes)
            // XY plane
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                Vector3 p1 = center + new Vector3(
                    (float)Math.Cos(angle1) * radius,
                    (float)Math.Sin(angle1) * radius,
                    0);
                Vector3 p2 = center + new Vector3(
                    (float)Math.Cos(angle2) * radius,
                    (float)Math.Sin(angle2) * radius,
                    0);

                vertices.Add(new VertexPositionColor(p1, color));
                vertices.Add(new VertexPositionColor(p2, color));
            }

            // XZ plane
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                Vector3 p1 = center + new Vector3(
                    (float)Math.Cos(angle1) * radius,
                    0,
                    (float)Math.Sin(angle1) * radius);
                Vector3 p2 = center + new Vector3(
                    (float)Math.Cos(angle2) * radius,
                    0,
                    (float)Math.Sin(angle2) * radius);

                vertices.Add(new VertexPositionColor(p1, color));
                vertices.Add(new VertexPositionColor(p2, color));
            }

            // YZ plane
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                Vector3 p1 = center + new Vector3(
                    0,
                    (float)Math.Cos(angle1) * radius,
                    (float)Math.Sin(angle1) * radius);
                Vector3 p2 = center + new Vector3(
                    0,
                    (float)Math.Cos(angle2) * radius,
                    (float)Math.Sin(angle2) * radius);

                vertices.Add(new VertexPositionColor(p1, color));
                vertices.Add(new VertexPositionColor(p2, color));
            }
        }

        private void DrawEvidenceTableGrid(EvidenceTable evidenceTable, List<VertexPositionColor> vertices)
        {
            if (evidenceTable == null)
                return;

            var slots = evidenceTable.GetAllSlots();
            int rows = evidenceTable.GridRows;
            int cols = evidenceTable.GridColumns;

            // Draw grid lines and slot markers
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var slot = slots[row, col];
                    Vector3 pos = slot.Position;
                    Vector3 size = slot.Size;

                    // Determine color based on occupancy
                    Color slotColor = slot.IsOccupied ? Color.Red : Color.Green;

                    // Draw slot boundary as a box outline
                    float halfWidth = size.X / 2f;
                    float halfDepth = size.Z / 2f;
                    float y = pos.Y;

                    // Bottom rectangle (at table surface)
                    Vector3 bl = new Vector3(pos.X - halfWidth, y, pos.Z - halfDepth); // Bottom-left
                    Vector3 br = new Vector3(pos.X + halfWidth, y, pos.Z - halfDepth); // Bottom-right
                    Vector3 tl = new Vector3(pos.X - halfWidth, y, pos.Z + halfDepth); // Top-left
                    Vector3 tr = new Vector3(pos.X + halfWidth, y, pos.Z + halfDepth); // Top-right

                    // Draw rectangle edges
                    vertices.Add(new VertexPositionColor(bl, slotColor));
                    vertices.Add(new VertexPositionColor(br, slotColor));

                    vertices.Add(new VertexPositionColor(br, slotColor));
                    vertices.Add(new VertexPositionColor(tr, slotColor));

                    vertices.Add(new VertexPositionColor(tr, slotColor));
                    vertices.Add(new VertexPositionColor(tl, slotColor));

                    vertices.Add(new VertexPositionColor(tl, slotColor));
                    vertices.Add(new VertexPositionColor(bl, slotColor));

                    // Draw center marker (small cross)
                    float markerSize = 2f;
                    vertices.Add(new VertexPositionColor(pos + new Vector3(-markerSize, 0, 0), Color.Yellow));
                    vertices.Add(new VertexPositionColor(pos + new Vector3(markerSize, 0, 0), Color.Yellow));

                    vertices.Add(new VertexPositionColor(pos + new Vector3(0, 0, -markerSize), Color.Yellow));
                    vertices.Add(new VertexPositionColor(pos + new Vector3(0, 0, markerSize), Color.Yellow));
                }
            }

            // Draw table boundary using vertex list version
            DrawDebugBoxToVertexList(evidenceTable.TableCenter, evidenceTable.TableSize.X,
                evidenceTable.TableSize.Y, evidenceTable.TableSize.Z, Color.Cyan, vertices);
        }

        private void DrawDebugBoxToVertexList(Vector3 center, float width, float height, float depth, Color color, List<VertexPositionColor> vertices)
        {
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;
            float halfDepth = depth / 2f;

            // Define 8 corners of the box
            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-halfWidth, -halfHeight, -halfDepth);
            corners[1] = center + new Vector3(halfWidth, -halfHeight, -halfDepth);
            corners[2] = center + new Vector3(halfWidth, halfHeight, -halfDepth);
            corners[3] = center + new Vector3(-halfWidth, halfHeight, -halfDepth);
            corners[4] = center + new Vector3(-halfWidth, -halfHeight, halfDepth);
            corners[5] = center + new Vector3(halfWidth, -halfHeight, halfDepth);
            corners[6] = center + new Vector3(halfWidth, halfHeight, halfDepth);
            corners[7] = center + new Vector3(-halfWidth, halfHeight, halfDepth);

            // Bottom face
            vertices.Add(new VertexPositionColor(corners[0], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[4], color));
            vertices.Add(new VertexPositionColor(corners[4], color));
            vertices.Add(new VertexPositionColor(corners[0], color));

            // Top face
            vertices.Add(new VertexPositionColor(corners[3], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[7], color));
            vertices.Add(new VertexPositionColor(corners[7], color));
            vertices.Add(new VertexPositionColor(corners[3], color));

            // Vertical edges
            vertices.Add(new VertexPositionColor(corners[0], color));
            vertices.Add(new VertexPositionColor(corners[3], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[4], color));
            vertices.Add(new VertexPositionColor(corners[7], color));
        }
    }
}
