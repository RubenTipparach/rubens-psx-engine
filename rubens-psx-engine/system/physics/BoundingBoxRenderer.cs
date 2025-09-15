using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using System;
using System.Collections.Generic;
using System.Linq;
using anakinsoft.system.physics;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using BepuVector3 = System.Numerics.Vector3;

namespace rubens_psx_engine.system.physics
{
    /// <summary>
    /// Renders physics bounding boxes for debugging, similar to BepuPhysics demo renderer
    /// </summary>
    public class BoundingBoxRenderer
    {
        private GraphicsDevice graphicsDevice;
        private BasicEffect wireframeEffect;
        private List<VertexPositionColor> vertices;
        private List<short> indices;
        private bool showBoundingBoxes = false;
        
        public bool ShowBoundingBoxes 
        { 
            get => showBoundingBoxes; 

            set 
            {
                if (showBoundingBoxes != value)
                {
                    showBoundingBoxes = value;
                    //System.Console.WriteLine($"BoundingBoxRenderer: Bounding boxes {(showBoundingBoxes ? "ENABLED" : "DISABLED")}");
                    
                    if (!showBoundingBoxes)
                    {
                        // Clear vertices when disabled to save memory
                        vertices.Clear();
                        indices.Clear();
                        //System.Console.WriteLine("BoundingBoxRenderer: Cleared bounding box data");
                    }
                }
            }
        }
        
        /// <summary>
        /// Toggle bounding boxes on/off with console logging
        /// </summary>
        public void ToggleBoundingBoxes()
        {
            ShowBoundingBoxes = !ShowBoundingBoxes;
        }

        public BoundingBoxRenderer(GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;
            vertices = new List<VertexPositionColor>();
            indices = new List<short>();
            
            // Create wireframe effect for rendering lines
            wireframeEffect = new BasicEffect(graphicsDevice);
            wireframeEffect.VertexColorEnabled = true;
            wireframeEffect.LightingEnabled = false;
        }

        /// <summary>
        /// Extract bounding boxes from the physics simulation (mimics BepuPhysics BoundingBoxLineExtractor)
        /// </summary>
        public void ExtractBoundingBoxes(PhysicsSystem physicsSystem)
        {
            if (!showBoundingBoxes || physicsSystem?.Simulation == null)
            {
                vertices.Clear();
                indices.Clear();
                return;
            }

            // Check if the physics system is disposed
            try
            {
                if (physicsSystem.Simulation.BroadPhase == null)
                {
                    vertices.Clear();
                    indices.Clear();
                    return;
                }
            }
            catch (ObjectDisposedException)
            {
                vertices.Clear();
                indices.Clear();
                return;
            }

            vertices.Clear();
            indices.Clear();

            try
            {
                // Extract active bodies (green bounding boxes)
                ExtractBoundingBoxesFromTree(physicsSystem.Simulation.BroadPhase.ActiveTree, 
                                            physicsSystem.Simulation, true);
                
                // Extract static bodies (blue bounding boxes)  
                ExtractBoundingBoxesFromTree(physicsSystem.Simulation.BroadPhase.StaticTree, 
                                            physicsSystem.Simulation, false);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"BoundingBoxRenderer: Error extracting bounding boxes: {ex.Message}");
                vertices.Clear();
                indices.Clear();
            }
        }

        private unsafe void ExtractBoundingBoxesFromTree(BepuPhysics.Trees.Tree tree, Simulation simulation, bool isActive)
        {
            if (tree.LeafCount == 0) return;

            // Color scheme: Green for active bodies, Blue for static bodies
            Color boxColor = isActive ? Color.Green : Color.Blue;
            
            for (int i = 0; i < tree.LeafCount; i++)
            {
                try
                {
                    BepuVector3* min, max;
                    if (isActive)
                        simulation.BroadPhase.GetActiveBoundsPointers(i, out min, out max);
                    else
                        simulation.BroadPhase.GetStaticBoundsPointers(i, out min, out max);

                    // Convert from System.Numerics.Vector3 to Microsoft.Xna.Framework.Vector3
                    var minXna = new XnaVector3(min->X, min->Y, min->Z);
                    var maxXna = new XnaVector3(max->X, max->Y, max->Z);
                    
                    AddBoundingBoxLines(minXna, maxXna, boxColor);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"BoundingBoxRenderer: Error processing bounding box {i}: {ex.Message}");
                    break; // Stop processing if we encounter errors
                }
            }
        }

        /// <summary>
        /// Add wireframe lines for a bounding box (mimics BepuPhysics WriteBoundsLines)
        /// </summary>
        private void AddBoundingBoxLines(XnaVector3 min, XnaVector3 max, Color color)
        {
            // Calculate the 8 corners of the bounding box
            var v000 = new XnaVector3(min.X, min.Y, min.Z); // min
            var v001 = new XnaVector3(min.X, min.Y, max.Z);
            var v010 = new XnaVector3(min.X, max.Y, min.Z);
            var v011 = new XnaVector3(min.X, max.Y, max.Z);
            var v100 = new XnaVector3(max.X, min.Y, min.Z);
            var v101 = new XnaVector3(max.X, min.Y, max.Z);
            var v110 = new XnaVector3(max.X, max.Y, min.Z);
            var v111 = new XnaVector3(max.X, max.Y, max.Z); // max

            short vertexStart = (short)vertices.Count;

            // Add vertices
            vertices.Add(new VertexPositionColor(v000, color));
            vertices.Add(new VertexPositionColor(v001, color));
            vertices.Add(new VertexPositionColor(v010, color));
            vertices.Add(new VertexPositionColor(v011, color));
            vertices.Add(new VertexPositionColor(v100, color));
            vertices.Add(new VertexPositionColor(v101, color));
            vertices.Add(new VertexPositionColor(v110, color));
            vertices.Add(new VertexPositionColor(v111, color));

            // Add indices for the 12 edges of the cube (mimics BepuPhysics line generation)
            // Bottom face edges
            indices.Add((short)(vertexStart + 0)); indices.Add((short)(vertexStart + 1)); // min -> v001
            indices.Add((short)(vertexStart + 0)); indices.Add((short)(vertexStart + 2)); // min -> v010
            indices.Add((short)(vertexStart + 0)); indices.Add((short)(vertexStart + 4)); // min -> v100
            indices.Add((short)(vertexStart + 1)); indices.Add((short)(vertexStart + 3)); // v001 -> v011
            indices.Add((short)(vertexStart + 1)); indices.Add((short)(vertexStart + 5)); // v001 -> v101
            indices.Add((short)(vertexStart + 2)); indices.Add((short)(vertexStart + 3)); // v010 -> v011
            indices.Add((short)(vertexStart + 2)); indices.Add((short)(vertexStart + 6)); // v010 -> v110
            indices.Add((short)(vertexStart + 3)); indices.Add((short)(vertexStart + 7)); // v011 -> max
            indices.Add((short)(vertexStart + 4)); indices.Add((short)(vertexStart + 5)); // v100 -> v101
            indices.Add((short)(vertexStart + 4)); indices.Add((short)(vertexStart + 6)); // v100 -> v110
            indices.Add((short)(vertexStart + 5)); indices.Add((short)(vertexStart + 7)); // v101 -> max
            indices.Add((short)(vertexStart + 6)); indices.Add((short)(vertexStart + 7)); // v110 -> max
        }

        /// <summary>
        /// Render the bounding boxes using wireframe lines
        /// </summary>
        public void Draw(Camera camera)
        {
            if (!showBoundingBoxes || vertices.Count == 0 || indices.Count == 0)
                return;

            try
            {
                // Set up the effect matrices (using proven approach from WireframeCubeTestScene)
                wireframeEffect.World = Microsoft.Xna.Framework.Matrix.Identity;
                wireframeEffect.View = camera.View;
                wireframeEffect.Projection = camera.Projection;
                wireframeEffect.VertexColorEnabled = true;
                wireframeEffect.LightingEnabled = false;

                // Apply the effect first
                wireframeEffect.CurrentTechnique.Passes[0].Apply();

                // Method 1: Try DrawUserIndexedPrimitives first
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

                    
                    if (showBoundingBoxes) // Only log when actually showing
                    {
                        //System.Console.WriteLine($"BoundingBoxRenderer: Drew {indices.Count / 2} bounding box lines for {vertices.Count / 8} boxes");
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"BoundingBoxRenderer: Indexed primitives failed, using fallback. Error: {ex.Message}");
                    
                    // Method 2: Fallback to DrawUserPrimitives (proven to work)
                    try
                    {
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
                        
                        System.Console.WriteLine($"BoundingBoxRenderer: Fallback succeeded - drew {lineVertices.Count / 2} lines");
                    }
                    catch (Exception ex2)
                    {
                        System.Console.WriteLine($"BoundingBoxRenderer: Both methods failed: {ex2.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"BoundingBoxRenderer: Error during rendering setup: {ex.Message}");

            }
        }

        public void Dispose()
        {
            wireframeEffect?.Dispose();
        }
    }
}