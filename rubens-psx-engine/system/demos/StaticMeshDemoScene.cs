using anakinsoft.system.character;
using anakinsoft.system.physics;
using anakinsoft.utilities;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;
using Demos.Demos.Characters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Vector3N = System.Numerics.Vector3;
using Matrix = Microsoft.Xna.Framework.Matrix;

/// <summary>
/// Dedicated scene for demonstrating StaticMesh functionality
/// Shows how to create immovable collision geometry from 3D models
/// </summary>
public class StaticMeshDemoScene : Scene
{
    // Character system
    CharacterControllers characters;
    CharacterInput? character;
    bool characterActive;

    // Entity collections
    List<PhysicsEntity> bullets;
    List<PhysicsEntity> boxes;
    PhysicsEntity ground;
    
    // Static mesh objects - the main focus of this demo  
    List<Mesh> bepuMeshes;
    List<RenderingEntity> staticMeshVisuals;
    List<List<Vector3>> meshTriangleVertices; // Store triangle vertices for wireframe
    
    // Store position and rotation info for wireframe rendering
    List<(Vector3 position, Quaternion rotation)> staticMeshTransforms;

    // Input handling
    bool mouseClick = false;
    bool spawnBoxKey = false;

    public StaticMeshDemoScene() : base()
    {
        // Initialize character system and physics
        characters = null; // Will be initialized in physics system
        physicsSystem = new PhysicsSystem(ref characters);
        
        bullets = new List<PhysicsEntity>();
        boxes = new List<PhysicsEntity>();
        bepuMeshes = new List<Mesh>();
        staticMeshVisuals = new List<RenderingEntity>();
        meshTriangleVertices = new List<List<Vector3>>();
        staticMeshTransforms = new List<(Vector3, Quaternion)>();

        Initialize();
    }

    public override void Initialize()
    {
        base.Initialize();

        // Create ground
        ground = CreateGround(new Vector3(0, -20f, 0), new Vector3(1000, 1, 1000), 
            "models/cube", "textures/prototype/concrete");
        ground.IsVisible = true;
        ground.Scale = new Vector3(50f, 0.05f, 50f);
        ground.Color = new Vector3(0.3f, 0.3f, 0.3f); // Dark gray

        // Create simple static mesh test - just two cubes for collision testing
        CreateSimpleStaticMeshTest();

        // Create character closer to test cubes
        CreateCharacter(new Vector3(0, 2, 40));
    }

    private void CreateSimpleStaticMeshTest()
    {
        // Just two simple static cubes for collision testing
        CreateStaticMeshCube(new Vector3(-30, 2, 20), Vector3.One, "Static Cube 1", Color.Red);
        CreateStaticMeshCube(new Vector3(30, 2, 20), Vector3.One, "Static Cube 2", Color.Blue);
    }

    private void CreateStaticMeshCube(Vector3 position, Vector3 scale, string name, Color color)
    {
        CreateStaticMeshCubeRotated(position, scale, Quaternion.Identity, name, color);
    }

    private void CreateStaticMeshCubeRotated(Vector3 position, Vector3 scale, Quaternion rotation, string name, Color color)
    {
        try
        {
            // Load the cube model
            var cubeModel = Globals.screenManager.Content.Load<Model>("models/cube");
            
            // Extract triangles directly and create BepuPhysics Mesh
            var (triangles, wireframeVertices) = ExtractTrianglesFromModel(cubeModel, scale * 10);
            var bepuMesh = new Mesh(triangles, scale.ToVector3N() * 10, physicsSystem.BufferPool);
            
            // Add the mesh shape to the simulation's shape collection
            var shapeIndex = physicsSystem.Simulation.Shapes.Add(bepuMesh);
            
            // Create static body with the mesh shape
            var staticHandle = physicsSystem.Simulation.Statics.Add(new StaticDescription(
                position.ToVector3N(), 
                rotation.ToQuaternionN(), 
                shapeIndex));
            
            // Store mesh data for wireframe rendering
            bepuMeshes.Add(bepuMesh);
            meshTriangleVertices.Add(wireframeVertices);
            staticMeshTransforms.Add((position, rotation));
            
            // Create visual representation
            var visualCube = new RenderingEntity("models/cube", "textures/prototype/concrete");
            visualCube.Position = position;
            visualCube.Scale = scale;
            visualCube.Rotation = rotation;
            visualCube.Color = color.ToVector3();
            visualCube.IsVisible = true;
            
            // Add to rendering system
            AddRenderingEntity(visualCube);
            staticMeshVisuals.Add(visualCube);
            
            Console.WriteLine($"Created static mesh '{name}' at {position} with scale {scale}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create static mesh '{name}': {ex.Message}");
        }
    }

    // Dynamic boxes removed for simplified testing
    
    private (Buffer<Triangle> triangles, List<Vector3> wireframeVertices) ExtractTrianglesFromModel(Model model, Vector3 scale)
    {
        var trianglesList = new List<Triangle>();
        var wireframeVertices = new List<Vector3>();
        var scaleMatrix = Matrix.CreateScale(scale);
        
        foreach (var mesh in model.Meshes)
        {
            foreach (var meshPart in mesh.MeshParts)
            {
                // Get vertex and index data
                var vertexBuffer = meshPart.VertexBuffer;
                var indexBuffer = meshPart.IndexBuffer;
                var vertexDeclaration = meshPart.VertexBuffer.VertexDeclaration;
                
                // Extract vertices (assuming VertexPositionNormalTexture)
                var vertexCount = meshPart.NumVertices;
                var vertices = new VertexPositionNormalTexture[vertexCount];
                vertexBuffer.GetData(meshPart.VertexOffset * vertexDeclaration.VertexStride, 
                    vertices, 0, vertexCount, vertexDeclaration.VertexStride);
                
                // Extract indices
                var indexCount = meshPart.PrimitiveCount * 3;
                var indices = new int[indexCount];
                
                if (indexBuffer.IndexElementSize == IndexElementSize.SixteenBits)
                {
                    var shortIndices = new short[indexCount];
                    indexBuffer.GetData<short>(shortIndices, meshPart.StartIndex, indexCount);
                    for (int i = 0; i < shortIndices.Length; i++)
                        indices[i] = shortIndices[i];
                }
                else
                {
                    indexBuffer.GetData<int>(indices, meshPart.StartIndex, indexCount);
                }
                
                // Create triangles and store wireframe vertices
                for (int i = 0; i < indices.Length; i += 3)
                {
                    var v1 = Vector3.Transform(vertices[indices[i]].Position, scaleMatrix);
                    var v2 = Vector3.Transform(vertices[indices[i + 1]].Position, scaleMatrix);
                    var v3 = Vector3.Transform(vertices[indices[i + 2]].Position, scaleMatrix);
                    
                    // Store vertices for wireframe (in local space)
                    wireframeVertices.Add(v1);
                    wireframeVertices.Add(v2);
                    wireframeVertices.Add(v3);
                    
                    trianglesList.Add(new Triangle(v1.ToVector3N(), v2.ToVector3N(), v3.ToVector3N()));
                }
            }
        }
        
        // Create buffer and copy triangles
        physicsSystem.BufferPool.Take<Triangle>(trianglesList.Count, out var triangleBuffer);
        for (int i = 0; i < trianglesList.Count; i++)
        {
            triangleBuffer[i] = trianglesList[i];
        }
        
        return (triangleBuffer, wireframeVertices);
    }

    private PhysicsEntity CreateBulletEntity(Vector3 position, Vector3N direction)
    {
        var bullet = CreateSphere(position, 5f, 10f, false, "models/sphere", null);
        bullet.Scale = new Vector3(0.5f);
        bullet.Color = new Vector3(1, 0.5f, 0); // Orange bullets
        bullet.SetVelocity(direction * 300f);
        bullets.Add(bullet);
        return bullet;
    }

    void CreateCharacter(Vector3 position)
    {
        characterActive = true;
        character = new CharacterInput(characters, position.ToVector3N(), 
            new Capsule(0.5f * 10, 1 * 10),
            minimumSpeculativeMargin: 0.1f, 
            mass: 0.1f, 
            maximumHorizontalForce: 200,
            maximumVerticalGlueForce: 10000,
            jumpVelocity: 100,
            speed: 40,
            maximumSlope: MathF.PI * 0.4f);
    }

    void ShootBullet(Vector3 position, Vector3N direction)
    {
        CreateBulletEntity(position, direction);
        Console.WriteLine("StaticMesh demo: bullet fired");
    }

    void SpawnBox(Vector3 position)
    {
        var box = CreateBox(position, new Vector3(20, 20, 20), 10f, false, "models/cube", "textures/prototype/brick");
        box.Scale = Vector3.One;
        box.Color = new Vector3(0, 1, 1); // Cyan for spawned boxes
        boxes.Add(box);
        Console.WriteLine("StaticMesh demo: box spawned");
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Handle input
        HandleInput();
    }

    public void UpdateWithCamera(GameTime gameTime, Camera camera)
    {
        // Update the scene normally first
        Update(gameTime);

        // Update character with camera
        if (characterActive && character.HasValue)
        {
            character.Value.UpdateCharacterGoals(Keyboard.GetState(), camera, (float)gameTime.ElapsedGameTime.TotalSeconds);
        }
    }

    private void HandleInput()
    {
        var keyState = Keyboard.GetState();
        var mouseState = Mouse.GetState();

        // Mouse shooting
        if (mouseState.LeftButton == ButtonState.Pressed && !mouseClick)
        {
            if (characterActive && character.HasValue)
            {
                var characterPos = character.Value.Body.Pose.Position.ToVector3();
                var dir = Vector3N.UnitZ; // Forward direction (camera will provide proper direction)
                ShootBullet(characterPos + new Vector3(0, 5, 0), dir);
            }
            mouseClick = true;
        }
        if (mouseState.LeftButton == ButtonState.Released)
        {
            mouseClick = false;
        }

        // Box spawning with B key
        if (keyState.IsKeyDown(Keys.B) && !spawnBoxKey)
        {
            if (characterActive && character.HasValue)
            {
                var characterPos = character.Value.Body.Pose.Position.ToVector3();
                SpawnBox(characterPos + new Vector3(0, 10, -10));
            }
            spawnBoxKey = true;
        }
        if (keyState.IsKeyUp(Keys.B))
        {
            spawnBoxKey = false;
        }
    }

    public override void Draw(GameTime gameTime, Camera camera)
    {
        // Draw all entities using the base scene drawing
        base.Draw(gameTime, camera);

        // Draw character if active
        if (characterActive && character.HasValue)
        {
            DrawCharacter(gameTime, camera);
        }
        
        // Draw wireframe visualization of static mesh collision geometry
        DrawStaticMeshWireframes(gameTime, camera);
    }

    private void DrawCharacter(GameTime gameTime, Camera camera)
    {
        var characterModel = Globals.screenManager.Content.Load<Model>("models/capsule");
        
        foreach (ModelMesh mesh in characterModel.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.LightingEnabled = true;
                effect.DiffuseColor = new Vector3(0, 1, 0); // Green character
                effect.DirectionalLight0.DiffuseColor = new Vector3(.7f, .7f, .7f);
                Vector3 lightAngle = new Vector3(20, -60, -60);
                lightAngle.Normalize();
                effect.DirectionalLight0.Direction = lightAngle;
                effect.AmbientLightColor = new Vector3(.3f, .3f, .3f);
            }

            var meshPos = character.Value.Body.Pose.Position.ToVector3() - new Vector3(0, 10, 0);
            var meshOrientation = character.Value.Body.Pose.Orientation.ToQuaternion();

            Matrix world = Matrix.CreateScale(.5f) *
                           Matrix.CreateFromQuaternion(meshOrientation) *
                           Matrix.CreateTranslation(meshPos);

            characterModel.Draw(world, camera.View, camera.Projection);
        }
    }
    
    private void DrawStaticMeshWireframes(GameTime gameTime, Camera camera)
    {
        var graphicsDevice = Globals.screenManager.GraphicsDevice;
        
        // Create a basic effect for wireframe rendering
        var basicEffect = new BasicEffect(graphicsDevice);
        basicEffect.VertexColorEnabled = true;
        basicEffect.View = camera.View;
        basicEffect.Projection = camera.Projection;
        basicEffect.World = Matrix.Identity;
        
        // Draw wireframes for each static mesh
        for (int meshIndex = 0; meshIndex < bepuMeshes.Count; meshIndex++)
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
                        // Edge 1: v1 to v2
                        wireframeVertices.Add(new VertexPositionColor(v1, Color.Yellow));
                        wireframeVertices.Add(new VertexPositionColor(v2, Color.Yellow));
                        
                        // Edge 2: v2 to v3
                        wireframeVertices.Add(new VertexPositionColor(v2, Color.Yellow));
                        wireframeVertices.Add(new VertexPositionColor(v3, Color.Yellow));
                        
                        // Edge 3: v3 to v1
                        wireframeVertices.Add(new VertexPositionColor(v3, Color.Yellow));
                        wireframeVertices.Add(new VertexPositionColor(v1, Color.Yellow));
                    }
                }
                
                if (wireframeVertices.Count > 0)
                {
                    try
                    {
                        // Apply the effect and draw the wireframe lines
                        basicEffect.CurrentTechnique.Passes[0].Apply();
                        graphicsDevice.DrawUserPrimitives(
                            PrimitiveType.LineList, 
                            wireframeVertices.ToArray(), 
                            0, 
                            wireframeVertices.Count / 2);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error drawing wireframe: {ex.Message}");
                    }
                }
            }
        }
        
        basicEffect.Dispose();
    }

    // Utility methods
    public List<PhysicsEntity> GetBullets() => bullets;
    public List<PhysicsEntity> GetBoxes() => boxes;
    public bool IsCharacterActive() => characterActive;
    public CharacterInput? GetCharacter() => character;
    public List<Mesh> GetBepuMeshes() => bepuMeshes;
    public int GetStaticMeshCount() => bepuMeshes.Count;
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // BepuPhysics meshes are cleaned up by the physics system
            bepuMeshes.Clear();
            staticMeshVisuals.Clear();
            meshTriangleVertices.Clear();
        }
        base.Dispose(disposing);
    }
}