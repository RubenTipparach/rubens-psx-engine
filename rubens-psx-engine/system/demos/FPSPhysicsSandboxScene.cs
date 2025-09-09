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
/// FPS Physics sandbox scene - same as regular physics sandbox but designed for FPS camera
/// Maintains the same scale and functionality as the original PhysicsSandbox
/// </summary>
public class FPSPhysicsSandboxScene : Scene
{
    // Character system
    CharacterControllers characters;
    CharacterInput? character;
    bool characterActive;

    // Entity collections for specific types
    List<PhysicsEntity> bullets;
    List<PhysicsEntity> boxes;
    PhysicsEntity ground;

    // Input handling
    bool mouseClick = false;

    public FPSPhysicsSandboxScene() : base()
    {
        // Initialize character system and physics
        characters = null; // Will be initialized in physics system
        physicsSystem = new PhysicsSystem(ref characters);
        
        bullets = new List<PhysicsEntity>();
        boxes = new List<PhysicsEntity>();

        Initialize();
    }

    public override void Initialize()
    {
        base.Initialize();

        // Create ground using the same scale and position as original (1000x1x1000 at 0, -20, 0)
        ground = CreateGround(new Vector3(0, -20f, 0), new Vector3(1000, 1, 1000), 
            "models/cube", "textures/prototype/concrete");
        // Make ground visible with thin mesh that matches collision size
        ground.IsVisible = true;
        // If the cube model is 20 units (like the boxes), scale it to match 1000x1x1000 collision
        ground.Scale = new Vector3(50f, 0.05f, 50f); // 20 * 50 = 1000, very thin (0.05) height
        ground.Color = new Vector3(0.5f, 0.5f, 0.5f); // Gray color to distinguish from boxes

        // Create box tower - same positions as original but using entity system
        var offset = -50f;
        
        // First row (5 boxes)
        for (int i = 0; i < 5; i++)
        {
            var position = new Vector3(1 + i * 1.2f * 20 - 10 + offset, 0, 0);
            var box = CreateBoxEntity(position);
            boxes.Add(box);
        }

        // Second row (4 boxes)
        for (int i = 0; i < 4; i++)
        {
            var position = new Vector3(1 + i * 1.2f * 20 + offset, 20, 0);
            var box = CreateBoxEntity(position);
            boxes.Add(box);
        }

        // Third row (3 boxes)
        for (int i = 0; i < 3; i++)
        {
            var position = new Vector3(1 + i * 1.2f * 20 + offset, 30, 0);
            var box = CreateBoxEntity(position);
            boxes.Add(box);
        }

        // Create character
        CreateCharacter(new Vector3(0, 2, 40));
    }

    private PhysicsEntity CreateBoxEntity(Vector3 position)
    {
        // Create box with same physics scale as original (20x20x20)
        var box = CreateBox(position, new Vector3(20, 20, 20), 10f, false, "models/cube", "textures/prototype/brick");
        
        // Set visual scale to match original rendering (scale 1.0 - the cube model is already 20 units)
        box.Scale = Vector3.One;
        box.Color = new Vector3(1, 1, 0); // Yellow color like original
        
        return box;
    }

    private PhysicsEntity CreateBulletEntity(Vector3 position, Vector3N direction)
    {
        // Create bullet with same physics scale as original (radius 5)
        var bullet = CreateSphere(position, 5f, 10f, false, "models/sphere", null);
        
        // Set visual scale to match original (0.5f) - sphere model is likely 10 units diameter, so 0.5 makes it 5 unit radius
        bullet.Scale = new Vector3(0.5f);
        bullet.Color = new Vector3(1, 0, 0); // Red color for FPS bullets (different from third person)
        
        // Apply initial velocity
        bullet.SetVelocity(direction * 250f);
        
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
        Console.WriteLine("FPS bullet fired");
    }

    void SpawnBox(Vector3 position)
    {
        var box = CreateBoxEntity(position);
        boxes.Add(box);
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

        // Update character with camera (for movement direction based on camera look)
        if (characterActive && character.HasValue)
        {
            character.Value.UpdateCharacterGoals(Keyboard.GetState(), camera, (float)gameTime.ElapsedGameTime.TotalSeconds);
        }
    }

    private void HandleInput()
    {
        // Mouse shooting - use FPS camera direction instead of fixed direction
        if (Mouse.GetState().LeftButton == ButtonState.Pressed && !mouseClick)
        {
            // Get character position for bullet spawn
            if (characterActive && character.HasValue)
            {
                var characterPos = character.Value.Body.Pose.Position.ToVector3();
                
                // Use camera forward direction (FPS camera will be mounted on character)
                // For now, use a forward direction - the FPS screen will handle camera mounting
                var dir = Vector3N.UnitZ; // This will be replaced by proper camera forward in FPS mode
                ShootBullet(characterPos, dir);
            }
            mouseClick = true;
        }
        if (Mouse.GetState().LeftButton == ButtonState.Released)
        {
            mouseClick = false;
        }

        // Box spawning - spawn near character instead of fixed position
        if (Keyboard.GetState().IsKeyDown(Keys.B))
        {
            if (characterActive && character.HasValue)
            {
                var characterPos = character.Value.Body.Pose.Position.ToVector3();
                SpawnBox(characterPos + new Vector3(0, 10, -20)); // Spawn box in front of character
            }
        }
    }

    public override void Draw(GameTime gameTime, Camera camera)
    {
        // Draw all entities using the base scene drawing
        base.Draw(gameTime, camera);

        // Don't draw character in FPS mode - player shouldn't see their own character body
        // (In real FPS games you might draw hands/weapon, but for this sandbox we'll skip it)
    }

    // Utility methods for external access
    public List<PhysicsEntity> GetBullets() => bullets;
    public List<PhysicsEntity> GetBoxes() => boxes;
    public bool IsCharacterActive() => characterActive;
    public CharacterInput? GetCharacter() => character;
}