using anakinsoft.system.physics;
using Microsoft.Xna.Framework;
using rubens_psx_engine.entities;
using System;

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
            var bakedLitMaterial = new UnlitMaterial("textures/prototype/gold");
            bakedLitMaterial.AffineAmount = 0;
            var bakedLitCube = CreateSphereWithMaterial(positions[2], bakedLitMaterial, new Vector3(2f));
            bakedLitCube.Color = new Vector3(1.0f, 1.0f, 1.0f);

            // Create a test sphere with unlit material
            //var sphereMaterial = new UnlitMaterial();
            //sphereMaterial.VertexJitterAmount = 1.0f; // Less jitter on sphere
            //var testSphere = CreateSphereWithMaterial(new Vector3(0, 15, -25), sphereMaterial, new Vector3(1.5f));
            //testSphere.Color = new Vector3(0.3f, 0.7f, 1.0f); // Blue
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            // Basic scene doesn't need much custom update logic
            // The base Scene class handles physics and entity updates
        }
    }
}