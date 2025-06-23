using anakinsoft.system.physics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Vector3N = System.Numerics.Vector3;

public class PhysicsSandbox {


    PhysicsSystem physics;
    Dictionary<BodyHandle, Matrix> bodyTransforms = new();
    Model cubeModel;
    Model bulletModel;


    public List<BodyHandle> bullets;
    public List<BodyHandle> boxes;
    public PhysicsSandbox()
    {
        physics = new PhysicsSystem();
        cubeModel = Globals.screenManager.Content.Load<Model>("models/cube");   // Must exist
        bulletModel = Globals.screenManager.Content.Load<Model>("models/sphere"); // Use a small sphere model

        // Create ground
        var groundDesc = new CollidableDescription(
            physics.Simulation.Shapes.Add(new Box(1000, 1, 1000)), 0.1f);

        //physics.Simulation.
        var groundHandle = physics.Simulation.Bodies.Add(
            BodyDescription.CreateKinematic(
                new RigidPose(new Vector3N(0, -20f, 0)), groundDesc, 
                new BodyActivityDescription(.1f)));
        // Add ground
        //var groundShape = new Box(50f, 1f, 50f);



        bullets = new();
        boxes = new();
        //SpawnBox(new Vector3N(0, 2, 0));
        //SpawnBox(new Vector3N(0, 10, 0));

        //SpawnBox(new Vector3N(0, 20, 0));
        // Add some boxes
        //var boxShape = new Box(1, 1, 1);
        //var shapeIndex = physics.Simulation.Shapes.Add(boxShape);

        var offset = -50f;
        for (int i = 0; i < 5; i++)
        {
            var position = new Vector3N( 1 + i * 1.2f * 20 -10 + offset, 0,0);
            SpawnBox(position);
        }

        for (int i = 0; i < 4; i++)
        {
            var position = new Vector3N( 1 + i * 1.2f * 20 + offset, 20 , 0);
            SpawnBox(position);
        }

        for (int i = 0; i < 3; i++)
        {
            var position = new Vector3N(1 + i * 1.2f * 20 + offset, 30, 0);
            SpawnBox(position);
        }
    }

    void SpawnBox(Vector3N position)
    {
        var shape = new Box(20, 20, 20);
        var collidable = new CollidableDescription(physics.Simulation.Shapes.Add(shape), 0.1f);
        var desc = BodyDescription.CreateConvexDynamic(new RigidPose(position), new BodyVelocity(Vector3N.Zero), 10, physics.Simulation.Shapes, shape);
        var handle = physics.Simulation.Bodies.Add(desc);
        boxes.Add(handle);
    }

    void ShootBullet(Vector3N position, Vector3N direction)
    {
        var shape = new Sphere(5);
        var collidable = new CollidableDescription(physics.Simulation.Shapes.Add(shape), 0.1f);
        var desc = BodyDescription.CreateConvexDynamic(new RigidPose(position), new BodyVelocity(Vector3N.Zero), 1, physics.Simulation.Shapes, shape);
        desc.Velocity.Linear = direction * 100f;
        var handle = physics.Simulation.Bodies.Add(desc);
        bullets.Add(handle);
    }
    bool mouseClick = false;
    public void Update(GameTime gameTime)
    {
        physics.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        if (Mouse.GetState().LeftButton == ButtonState.Pressed && !mouseClick)
        {
            var camPos = new Vector3N(0, 0, -20);
            var dir = Vector3N.Normalize(Vector3N.UnitZ); // does it need to be ngative?
            ShootBullet(camPos, dir);
            Console.WriteLine("spawn bullet");
            mouseClick = true;
        }
        if (Mouse.GetState().LeftButton == ButtonState.Released)
        {
            mouseClick = false;
        }

            if (Keyboard.GetState().IsKeyDown(Keys.B))
        {
            SpawnBox(new Vector3N(0, 5, 0));
        }

    }

    public void Draw(GameTime gameTime, Camera camera)
    {
        //var handles = bullets.Concat(boxes);

        foreach (ModelMesh mesh in cubeModel.Meshes)
        {
            // do this for custom effects
            foreach (BasicEffect effect in mesh.Effects)
            {
                bool shaded = true;
                effect.LightingEnabled = shaded;

                if (shaded)
                {
                    effect.DiffuseColor = new Vector3(1, 1, 0);
                    effect.DirectionalLight0.DiffuseColor = new Vector3(.7f, .7f, .7f);
                    Vector3 lightAngle = new Vector3(20, -60, -60);
                    lightAngle.Normalize();
                    effect.DirectionalLight0.Direction = lightAngle;
                    effect.AmbientLightColor = new Vector3(.3f, .3f, .3f);
                }

                foreach (var bodyHandle in boxes)
                {
                    var bodyRef = physics.Simulation.Bodies.GetBodyReference(bodyHandle);
                    var pose = bodyRef.Pose;

                    Matrix world = Matrix.CreateScale(1f) *
                                   Matrix.CreateFromQuaternion(new Quaternion(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W)) *
                                   Matrix.CreateTranslation(pose.Position);

                    cubeModel.Draw(world, camera.View, camera.Projection); // Choose model based on shape
                }

            }
        }

        foreach (ModelMesh mesh in bulletModel.Meshes)
        {
            // do this for custom effects
            foreach (BasicEffect effect in mesh.Effects)
            {
                bool shaded = true;
                effect.LightingEnabled = shaded;

                if (shaded)
                {
                    effect.DiffuseColor = new Vector3(1, 1, 0);
                    effect.DirectionalLight0.DiffuseColor = new Vector3(.7f, .7f, .7f);
                    Vector3 lightAngle = new Vector3(20, -60, -60);
                    lightAngle.Normalize();
                    effect.DirectionalLight0.Direction = lightAngle;
                    effect.AmbientLightColor = new Vector3(.3f, .3f, .3f);
                }

                foreach (var bodyHandle in bullets)
                {
                    var bodyRef = physics.Simulation.Bodies.GetBodyReference(bodyHandle);
                    var pose = bodyRef.Pose;

                    Matrix world = Matrix.CreateScale(.5f) *
                                   Matrix.CreateFromQuaternion(new Quaternion(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W)) *
                                   Matrix.CreateTranslation(pose.Position);

                    bulletModel.Draw(world, camera.View, camera.Projection); // Choose model based on shape
                }

            }
        }
    }
}
