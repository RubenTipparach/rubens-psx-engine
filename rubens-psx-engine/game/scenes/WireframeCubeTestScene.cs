using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.entities;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine.game.scenes
{
    /// <summary>
    /// Simple test scene that draws wireframe cubes to verify line rendering works
    /// </summary>
    public class WireframeCubeTestScene : Scene
    {
        private BasicEffect lineEffect;
        private List<VertexPositionColor> vertices;
        private List<short> indices;

        public WireframeCubeTestScene() : base(null)
        {
            vertices = new List<VertexPositionColor>();
            indices = new List<short>();
        }

        public override void Initialize()
        {
            base.Initialize();

            // Create the basic effect for line rendering
            if (Globals.screenManager?.GraphicsDevice != null)
            {
                lineEffect = new BasicEffect(Globals.screenManager.GraphicsDevice);
                lineEffect.VertexColorEnabled = true;
                lineEffect.LightingEnabled = false;
            }

            // Create a few test cubes at different positions
            CreateWireframeCube(Vector3.Zero, 10f, Color.White);
            CreateWireframeCube(new Vector3(30, 0, 0), 8f, Color.Red);
            CreateWireframeCube(new Vector3(-30, 0, 0), 8f, Color.Green);
            CreateWireframeCube(new Vector3(0, 20, 0), 5f, Color.Yellow);
            CreateWireframeCube(new Vector3(0, -20, 0), 12f, Color.Cyan);
            CreateWireframeCube(new Vector3(0, 0, 30), 7f, Color.Magenta);
        }

        private void CreateWireframeCube(Vector3 center, float size, Color color)
        {
            float halfSize = size / 2f;

            // Calculate the 8 corners of the cube
            var corners = new Vector3[]
            {
                center + new Vector3(-halfSize, -halfSize, -halfSize), // 0: min
                center + new Vector3(-halfSize, -halfSize, halfSize),  // 1
                center + new Vector3(-halfSize, halfSize, -halfSize),  // 2
                center + new Vector3(-halfSize, halfSize, halfSize),   // 3
                center + new Vector3(halfSize, -halfSize, -halfSize),  // 4
                center + new Vector3(halfSize, -halfSize, halfSize),   // 5
                center + new Vector3(halfSize, halfSize, -halfSize),   // 6
                center + new Vector3(halfSize, halfSize, halfSize)     // 7: max
            };

            short vertexStart = (short)vertices.Count;

            // Add vertices with color
            foreach (var corner in corners)
            {
                vertices.Add(new VertexPositionColor(corner, color));
            }

            // Add indices for the 12 edges of the cube
            // Bottom face edges
            indices.Add((short)(vertexStart + 0)); indices.Add((short)(vertexStart + 1));
            indices.Add((short)(vertexStart + 0)); indices.Add((short)(vertexStart + 2));
            indices.Add((short)(vertexStart + 0)); indices.Add((short)(vertexStart + 4));
            
            // Top face edges
            indices.Add((short)(vertexStart + 3)); indices.Add((short)(vertexStart + 7));
            indices.Add((short)(vertexStart + 2)); indices.Add((short)(vertexStart + 3));
            indices.Add((short)(vertexStart + 6)); indices.Add((short)(vertexStart + 7));
            
            // Vertical edges
            indices.Add((short)(vertexStart + 1)); indices.Add((short)(vertexStart + 3));
            indices.Add((short)(vertexStart + 1)); indices.Add((short)(vertexStart + 5));
            indices.Add((short)(vertexStart + 2)); indices.Add((short)(vertexStart + 6));
            indices.Add((short)(vertexStart + 4)); indices.Add((short)(vertexStart + 5));
            indices.Add((short)(vertexStart + 4)); indices.Add((short)(vertexStart + 6));
            indices.Add((short)(vertexStart + 5)); indices.Add((short)(vertexStart + 7));
        }

        public override void Draw(GameTime gameTime, Camera camera)
        {
            // First draw any regular entities
            base.Draw(gameTime, camera);

            // Now draw our wireframe cubes
            if (lineEffect != null && vertices.Count > 0 && indices.Count > 0)
            {
                var graphicsDevice = Globals.screenManager.GraphicsDevice;

                // Configure the effect with camera matrices
                lineEffect.World = Matrix.Identity;
                lineEffect.View = camera.View;
                lineEffect.Projection = camera.Projection;

                // Apply the effect
                lineEffect.CurrentTechnique.Passes[0].Apply();

                // Method 1: Using DrawUserIndexedPrimitives (what BoundingBoxRenderer uses)
                try
                {
                    graphicsDevice.DrawUserIndexedPrimitives(
                        PrimitiveType.LineList,
                        vertices.ToArray(),
                        0,
                        vertices.Count,
                        indices.ToArray(),
                        0,
                        indices.Count / 2
                    );
                    
                    Console.WriteLine($"WireframeCubeTestScene: Successfully drew {indices.Count / 2} lines using indexed primitives");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WireframeCubeTestScene: Error with indexed primitives: {ex.Message}");
                    
                    // Fallback - Method 2: Using DrawUserPrimitives (your example approach)
                    try
                    {
                        // Convert indexed data to line pairs for simple drawing
                        var lineVertices = new List<VertexPositionColor>();
                        for (int i = 0; i < indices.Count; i += 2)
                        {
                            lineVertices.Add(vertices[indices[i]]);
                            lineVertices.Add(vertices[indices[i + 1]]);
                        }
                        
                        graphicsDevice.DrawUserPrimitives(
                            PrimitiveType.LineList,
                            lineVertices.ToArray(),
                            0,
                            lineVertices.Count / 2
                        );
                        
                        Console.WriteLine($"WireframeCubeTestScene: Successfully drew {lineVertices.Count / 2} lines using simple primitives");
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"WireframeCubeTestScene: Error with simple primitives: {ex2.Message}");
                    }
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            // Optional: Rotate the cubes for visual effect
            // You could modify vertex positions here for animation
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lineEffect?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}