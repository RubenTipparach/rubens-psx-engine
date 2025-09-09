using anakinsoft.system.physics;
using Microsoft.Xna.Framework;
using rubens_psx_engine.entities;
using System;
using System.Collections.Generic;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Graphics test scene with PS1-style shader demonstration - extends the generic Scene class
    /// </summary>
    public class GraphicsTestScene : Scene
    {
        public GraphicsTestScene() : base()
        {
            // Initialize physics system (optional for basic scene)
            anakinsoft.system.character.CharacterControllers characters = null;
            physicsSystem = new PhysicsSystem(ref characters);
            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();

            // Create basic entities for testing
            CreateBasicGeometry();
        }

        private void CreateBasicGeometry()
        {
            // Create a simple ground platform with basic material
            var groundCube = CreateBox(
                position: new Vector3(0, -50, 0), 
                size: new Vector3(100, 2, 100), 
                mass: 0f, // Static
                isStatic: true,
                modelPath: "models/cube",
                texturePath: "textures/prototype/concrete"
            );
            groundCube.Scale = new Vector3(5f, 0.1f, 5f); // Large ground platform
            groundCube.Color = new Vector3(0.8f, 0.8f, 0.8f); // Light gray
            groundCube.IsVisible = true;

            // Create three cubes with different PS1-style materials for shader demonstration
            var positions = new Vector3[]
            {
                new Vector3(-50, 0, 0),  // Left - Unlit
                new Vector3(0, 0, 0),    // Center - Vertex Lit
                new Vector3(50, 0, 0)    // Right - Baked Vertex Lit
            };

            // Unlit cube
            var unlitMaterial = new UnlitMaterial("textures/prototype/grass");
            unlitMaterial.AffineAmount = 0;
            var unlitCube = CreateSphereWithMaterial(positions[0], unlitMaterial, new Vector3(2f));
            unlitCube.Color = new Vector3(1.0f, 1.0f, 1.0f);
            
            // Vertex lit cube
            var vertexLitMaterial = new VertexLitMaterial("textures/prototype/fire");
            vertexLitMaterial.AffineAmount = 0;
            var vertexLitCube = CreateSphereWithMaterial(positions[1], vertexLitMaterial, new Vector3(2f));
            vertexLitCube.Color = new Vector3(1.0f, 1.0f, 1.0f);
            
            // Baked vertex lit cube
            var bakedLitMaterial = new BakedVertexLitMaterial("textures/prototype/gold");
            bakedLitMaterial.AffineAmount = 0;
            var bakedLitCube = CreateSphereWithMaterial(positions[2], bakedLitMaterial, new Vector3(2f));
            bakedLitCube.Color = new Vector3(1.0f, 1.0f, 1.0f);

            // Add corridor multi-material model for testing
            CreateCorridorWithMaterials();

            // Create a simple test cube in center
            CreateTestCube();
        }

        private void CreateCorridorWithMaterials()
        {
            // Create three different materials for the corridor channels using actual texture files
            var material1 = new UnlitMaterial("textures/prototype/prototype_512x512_blue1");
            material1.VertexJitterAmount = 1.0f;
            material1.AffineAmount = 0.8f;
            
            var material2 = new VertexLitMaterial("textures/prototype/prototype_512x512_green1");  
            material2.VertexJitterAmount = 1.2f;
            material2.AffineAmount = 0.6f;
            material2.LightDirection = Vector3.Normalize(new Vector3(0.5f, -1, 0.3f));
            
            var material3 = new BakedVertexLitMaterial("textures/prototype/prototype_512x512_orange");
            material3.VertexJitterAmount = 0.8f;
            material3.AffineAmount = 0.9f;
            material3.BakedLightIntensity = 1.2f;

            // Create corridor entity with three material channels, offset to the side
            var corridorEntity = new MultiMaterialRenderingEntity("models/corridor_single", 
                new Dictionary<int, Material>
                {
                    { 0, material1 }, // Floor/walls
                    { 1, material2 }, // Architectural details
                    { 2, material3 }  // Decorative elements
                });

            corridorEntity.Position = new Vector3(100, -40, 0); // Offset to the side
            corridorEntity.Scale = Vector3.One;
            corridorEntity.IsVisible = true;
            
            // Add to rendering entities
            AddRenderingEntity(corridorEntity);
        }

        private void CreateTestCube()
        {
            // Create a simple test cube with unlit material in the center
            var testMaterial = new UnlitMaterial("textures/prototype/brick");
            testMaterial.VertexJitterAmount = 2.0f;
            testMaterial.AffineAmount = 1.0f;
            
            var testCube = CreateBoxWithMaterial(new Vector3(0, 10, 0), testMaterial, new Vector3(3f));
            testCube.Color = new Vector3(0.2f, 1.0f, 0.5f); // Green color
            testCube.IsVisible = true;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            // Basic scene doesn't need much custom update logic
            // The base Scene class handles physics and entity updates
        }
    }
}