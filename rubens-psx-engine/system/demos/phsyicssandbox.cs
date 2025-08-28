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
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Vector3N = System.Numerics.Vector3;

public class PhysicsSandbox {

    // Use the new scene-based system
    PhysicsSandboxScene scene;

    public PhysicsSandbox()
    {
        scene = new PhysicsSandboxScene();
    }


    public void Update(GameTime gameTime, Camera camera, KeyboardState input)
    {
        scene.UpdateWithCamera(gameTime, camera);
        scene.UpdateCameraForCharacter(camera);
    }

    public void Draw(GameTime gameTime, Camera camera)
    {
        scene.Draw(gameTime, camera);
    }
}
