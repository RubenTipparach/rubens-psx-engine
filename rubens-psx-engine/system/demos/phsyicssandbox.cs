using anakinsoft.system.physics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

using Vector3N = System.Numerics.Vector3;

using Matrix = Microsoft.Xna.Framework.Matrix;

public class PhysicsSandbox {


    PhysicsSystem physics;
    Dictionary<BodyHandle, Matrix> bodyTransforms = new();
    Model cubeModel;
    Model bulletModel;


    public PhysicsSandbox()
    {
        physics = new PhysicsSystem();
        cubeModel = Globals.screenManager.Content.Load<Model>("cube");   // Must exist
        bulletModel = Globals.screenManager.Content.Load<Model>("sphere"); // Use a small sphere model

        // Create ground
        var groundDesc = new CollidableDescription(
            physics.Simulation.Shapes.Add(new Box(100, 1, 100)), 0.1f);
        var groundHandle = physics.Simulation.Bodies.Add(
            BodyDescription.CreateKinematic(
                new RigidPose(new Vector3N(0, -0.5f, 0)), groundDesc, 
                new BodyActivityDescription(.1f)));

    }

    void SpawnBox(Vector3N position)
    {
        var shape = new Box(1, 1, 1);
        var collidable = new CollidableDescription(physics.Simulation.Shapes.Add(shape), 0.1f);
        var desc = BodyDescription.CreateDynamic(new RigidPose(position), new BodyInertia(), collidable, 1);
        var handle = physics.Simulation.Bodies.Add(desc);
    }

    void ShootBullet(Vector3N position, Vector3N direction)
    {
        var shape = new Sphere(0.2f);
        var collidable = new CollidableDescription(physics.Simulation.Shapes.Add(shape), 0.1f);
        var desc = BodyDescription.CreateDynamic(new RigidPose(position), new BodyInertia(), collidable, 1);
        desc.Velocity.Linear = direction * 30f;
        physics.Simulation.Bodies.Add(desc);
    }

    public void Update(GameTime gameTime)
    {
        physics.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
        {
            var camPos = new Vector3N(0, 2, 10);
            var dir = Vector3N.Normalize(Vector3N.UnitZ); // does it need to be ngative?
            ShootBullet(camPos, dir);
        }

        if (Keyboard.GetState().IsKeyDown(Keys.B))
        {
            SpawnBox(new Vector3N(0, 5, 0));
        }

    }

    public void Draw(GameTime gameTime, Camera camera)
    {
        foreach (var bodyHandle in physics.Simulation.Bodies.ActiveSet.Span)
        {
            var bodyRef = physics.Simulation.Bodies.GetBodyReference(bodyHandle);
            var pose = bodyRef.Pose;

            Matrix world = Matrix.CreateScale(1f) *
                           Matrix.CreateFromQuaternion(new Quaternion(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W)) *
                           Matrix.CreateTranslation(pose.Position);

            cubeModel.Draw(world, camera.View, camera.); // Choose model based on shape
        }

    }
}
