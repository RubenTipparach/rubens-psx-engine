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
using System.Text;
using System.Threading.Tasks;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Vector3N = System.Numerics.Vector3;

public class FPSPhysicsSandbox 
{
    // Use the new scene-based system
    FPSPhysicsSandboxScene scene;
    public FPSPhysicsSandboxScene Scene { get { return scene; } }

    public FPSPhysicsSandbox()
    {
        scene = new FPSPhysicsSandboxScene();
    }

    public void Update(GameTime gameTime, Camera camera, KeyboardState input)
    {
        scene.UpdateWithCamera(gameTime, camera);
        // Don't update camera position - FPSSandboxScreen handles camera mounting
    }

    public void Draw(GameTime gameTime, Camera camera)
    {
        scene.Draw(gameTime, camera);
    }

    // Expose character for camera mounting
    public CharacterInput? GetCharacter()
    {
        return scene.GetCharacter();
    }
}