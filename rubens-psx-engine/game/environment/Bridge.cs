using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.entities;
using rubens_psx_engine.entities.materials;
using System.Collections.Generic;

namespace rubens_psx_engine.game.environment
{
    public class Bridge
    {
        private GraphicsDevice graphicsDevice;
        private VertexBuffer bridgeVertexBuffer;
        private IndexBuffer bridgeIndexBuffer;
        private VertexLitUnitsMaterial bridgeMaterial;

        public Vector3 Position { get; set; }
        public Vector3 EndPosition { get; set; }
        public float Width { get; set; } = 8.0f;
        public float Height { get; set; } = 2.0f;
        public float Thickness { get; set; } = 0.5f;

        // Bridge supports
        private List<Vector3> supportPositions;
        private PrimitiveCube supportMesh;

        public Bridge(GraphicsDevice device, Vector3 startPos, Vector3 endPos)
        {
            graphicsDevice = device;
            Position = startPos;
            EndPosition = endPos;
            supportPositions = new List<Vector3>();

            CreateBridgeMaterial();
            CreateBridgeGeometry();
            CreateSupports();
        }

        private void CreateBridgeMaterial()
        {
            // Create a brown/wood-colored material for the bridge
            bridgeMaterial = new VertexLitUnitsMaterial()
            {
                DiffuseColor = new Vector3(0.6f, 0.4f, 0.2f), // Brown wood color
                Alpha = 1.0f
            };

            // Create support mesh
            supportMesh = new PrimitiveCube(graphicsDevice);
        }

        private void CreateBridgeGeometry()
        {
            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
            List<int> indices = new List<int>();

            Vector3 direction = Vector3.Normalize(EndPosition - Position);
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.Up) * Width * 0.5f;
            Vector3 upVector = Vector3.Up * Height;

            // Bridge deck (top surface)
            Vector3 p1 = Position - perpendicular;
            Vector3 p2 = Position + perpendicular;
            Vector3 p3 = EndPosition - perpendicular;
            Vector3 p4 = EndPosition + perpendicular;

            // Top face
            int baseIndex = vertices.Count;
            vertices.Add(new VertexPositionNormalTexture(p1 + upVector, Vector3.Up, new Vector2(0, 0)));
            vertices.Add(new VertexPositionNormalTexture(p2 + upVector, Vector3.Up, new Vector2(1, 0)));
            vertices.Add(new VertexPositionNormalTexture(p3 + upVector, Vector3.Up, new Vector2(0, 1)));
            vertices.Add(new VertexPositionNormalTexture(p4 + upVector, Vector3.Up, new Vector2(1, 1)));

            // Top face indices
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 3);
            indices.Add(baseIndex + 2);

            // Bottom face
            baseIndex = vertices.Count;
            vertices.Add(new VertexPositionNormalTexture(p1, Vector3.Down, new Vector2(0, 0)));
            vertices.Add(new VertexPositionNormalTexture(p3, Vector3.Down, new Vector2(0, 1)));
            vertices.Add(new VertexPositionNormalTexture(p2, Vector3.Down, new Vector2(1, 0)));
            vertices.Add(new VertexPositionNormalTexture(p4, Vector3.Down, new Vector2(1, 1)));

            // Bottom face indices
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 3);
            indices.Add(baseIndex + 2);

            // Side faces
            CreateBridgeSide(vertices, indices, p1, p2, p1 + upVector, p2 + upVector, direction); // Front
            CreateBridgeSide(vertices, indices, p4, p3, p4 + upVector, p3 + upVector, -direction); // Back
            CreateBridgeSide(vertices, indices, p2, p4, p2 + upVector, p4 + upVector, perpendicular); // Right
            CreateBridgeSide(vertices, indices, p3, p1, p3 + upVector, p1 + upVector, -perpendicular); // Left

            // Create railings
            CreateRailing(vertices, indices, p1 + upVector, p3 + upVector, -perpendicular);
            CreateRailing(vertices, indices, p2 + upVector, p4 + upVector, perpendicular);

            // Create vertex buffer
            bridgeVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture),
                vertices.Count, BufferUsage.WriteOnly);
            bridgeVertexBuffer.SetData(vertices.ToArray());

            // Create index buffer
            bridgeIndexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits,
                indices.Count, BufferUsage.WriteOnly);
            bridgeIndexBuffer.SetData(indices.ToArray());
        }

        private void CreateBridgeSide(List<VertexPositionNormalTexture> vertices, List<int> indices,
            Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 normal)
        {
            int baseIndex = vertices.Count;

            vertices.Add(new VertexPositionNormalTexture(p1, normal, new Vector2(0, 1)));
            vertices.Add(new VertexPositionNormalTexture(p2, normal, new Vector2(1, 1)));
            vertices.Add(new VertexPositionNormalTexture(p3, normal, new Vector2(0, 0)));
            vertices.Add(new VertexPositionNormalTexture(p4, normal, new Vector2(1, 0)));

            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
        }

        private void CreateRailing(List<VertexPositionNormalTexture> vertices, List<int> indices,
            Vector3 start, Vector3 end, Vector3 normal)
        {
            float railingHeight = 1.5f;
            float railingThickness = 0.2f;

            Vector3 railingTop = Vector3.Up * railingHeight;
            Vector3 thicknessOffset = Vector3.Normalize(normal) * railingThickness;

            // Railing posts
            int numPosts = 5;
            for (int i = 0; i <= numPosts; i++)
            {
                float t = (float)i / numPosts;
                Vector3 postBase = Vector3.Lerp(start, end, t);
                Vector3 postTop = postBase + railingTop;

                // Create simple post geometry
                CreatePost(vertices, indices, postBase, postTop, thicknessOffset);
            }

            // Railing beam
            Vector3 beamStart = start + railingTop * 0.8f;
            Vector3 beamEnd = end + railingTop * 0.8f;
            CreateBeam(vertices, indices, beamStart, beamEnd, thicknessOffset);
        }

        private void CreatePost(List<VertexPositionNormalTexture> vertices, List<int> indices,
            Vector3 bottom, Vector3 top, Vector3 thickness)
        {
            // Simple post as a thin box
            Vector3[] corners = new Vector3[]
            {
                bottom - thickness, bottom + thickness,
                top - thickness, top + thickness
            };

            int baseIndex = vertices.Count;

            // Add vertices for post
            foreach (var corner in corners)
            {
                vertices.Add(new VertexPositionNormalTexture(corner, Vector3.Up, Vector2.Zero));
            }

            // Add simple indices for post faces
            int[] postIndices = new int[]
            {
                0, 2, 1, 1, 2, 3, // Front/back faces
                0, 1, 2, 1, 3, 2  // Additional faces
            };

            foreach (int index in postIndices)
            {
                indices.Add(baseIndex + index);
            }
        }

        private void CreateBeam(List<VertexPositionNormalTexture> vertices, List<int> indices,
            Vector3 start, Vector3 end, Vector3 thickness)
        {
            // Simple beam geometry
            Vector3 beamThickness = Vector3.Up * 0.1f;

            int baseIndex = vertices.Count;

            vertices.Add(new VertexPositionNormalTexture(start - thickness - beamThickness, Vector3.Up, Vector2.Zero));
            vertices.Add(new VertexPositionNormalTexture(start + thickness - beamThickness, Vector3.Up, Vector2.Zero));
            vertices.Add(new VertexPositionNormalTexture(end - thickness - beamThickness, Vector3.Up, Vector2.Zero));
            vertices.Add(new VertexPositionNormalTexture(end + thickness - beamThickness, Vector3.Up, Vector2.Zero));

            vertices.Add(new VertexPositionNormalTexture(start - thickness + beamThickness, Vector3.Up, Vector2.Zero));
            vertices.Add(new VertexPositionNormalTexture(start + thickness + beamThickness, Vector3.Up, Vector2.Zero));
            vertices.Add(new VertexPositionNormalTexture(end - thickness + beamThickness, Vector3.Up, Vector2.Zero));
            vertices.Add(new VertexPositionNormalTexture(end + thickness + beamThickness, Vector3.Up, Vector2.Zero));

            // Add beam indices
            int[] beamIndices = new int[]
            {
                0, 4, 1, 1, 4, 5,
                2, 3, 6, 3, 7, 6,
                0, 2, 4, 2, 6, 4,
                1, 5, 3, 3, 5, 7
            };

            foreach (int index in beamIndices)
            {
                indices.Add(baseIndex + index);
            }
        }

        private void CreateSupports()
        {
            supportPositions.Clear();

            float bridgeLength = Vector3.Distance(Position, EndPosition);
            int numSupports = (int)(bridgeLength / 15.0f) + 1; // Support every 15 units

            for (int i = 1; i < numSupports; i++)
            {
                float t = (float)i / numSupports;
                Vector3 supportPos = Vector3.Lerp(Position, EndPosition, t);
                supportPos.Y -= Height + 3.0f; // Place supports below bridge
                supportPositions.Add(supportPos);
            }
        }

        public void Draw(Matrix view, Matrix projection)
        {
            if (bridgeVertexBuffer == null || bridgeIndexBuffer == null) return;

            var camera = new BasicCamera { View = view, Projection = projection };

            // Draw main bridge
            bridgeMaterial.Apply(camera, Matrix.Identity);

            graphicsDevice.SetVertexBuffer(bridgeVertexBuffer);
            graphicsDevice.Indices = bridgeIndexBuffer;

            int indexCount = bridgeIndexBuffer.IndexCount;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, indexCount / 3);

            // Draw support pillars
            foreach (var supportPos in supportPositions)
            {
                Matrix supportWorld = Matrix.CreateScale(0.8f, 5.0f, 0.8f) * Matrix.CreateTranslation(supportPos);
                bridgeMaterial.Apply(camera, supportWorld);
                DrawCube(supportMesh);
            }
        }

        public bool IsOnBridge(Vector3 position)
        {
            // Check if position is on the bridge deck
            Vector3 toPos = position - Position;
            Vector3 bridgeDir = Vector3.Normalize(EndPosition - Position);
            float alongBridge = Vector3.Dot(toPos, bridgeDir);

            float bridgeLength = Vector3.Distance(Position, EndPosition);

            if (alongBridge < 0 || alongBridge > bridgeLength)
                return false;

            Vector3 closestPointOnBridge = Position + bridgeDir * alongBridge;
            float distanceFromBridge = Vector3.Distance(position, closestPointOnBridge);

            return distanceFromBridge <= Width * 0.5f &&
                   position.Y >= Position.Y &&
                   position.Y <= Position.Y + Height + 2.0f;
        }

        public void Dispose()
        {
            bridgeVertexBuffer?.Dispose();
            bridgeIndexBuffer?.Dispose();
            supportMesh?.Dispose();
        }

        private void DrawCube(PrimitiveCube cube)
        {
            var gd = graphicsDevice;
            gd.SetVertexBuffer(cube.VertexBuffer);
            gd.Indices = cube.IndexBuffer;
            gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, cube.IndexBuffer.IndexCount / 3);
        }

        // Helper camera class
        private class BasicCamera : Camera
        {
            public new Matrix View { get; set; }
            public new Matrix Projection { get; set; }

            public BasicCamera() : base(Globals.screenManager.getGraphicsDevice.GraphicsDevice)
            {
            }
        }
    }
}