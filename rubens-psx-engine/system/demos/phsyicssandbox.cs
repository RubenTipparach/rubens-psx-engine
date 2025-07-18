﻿using anakinsoft.system.character;
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
    Model characterModel;

    Texture2D brickTexture;

    public List<BodyHandle> bullets;
    public List<BodyHandle> boxes;

    CharacterControllers characters;


    public PhysicsSandbox()
    {
        physics = new PhysicsSystem(ref characters);


        cubeModel = Globals.screenManager.Content.Load<Model>("models/cube");   // Must exist
        bulletModel = Globals.screenManager.Content.Load<Model>("models/sphere"); // Use a small sphere model
        brickTexture = Globals.screenManager.Content.Load<Texture2D>("textures/prototype/brick"); // Load a texture for the boxes
        characterModel = Globals.screenManager.Content.Load<Model>("models/capsule");
        //var ps1Effect = Globals.screenManager.Content.Load<Effect>("shaders/surface/Unlit");

        foreach (var m in cubeModel.Meshes)
        {
            foreach (var part in m.MeshParts)
            {
                //part.Effect = Globals.screenManager.Content.Load<Effect>("shaders/surface/Unlit");
                (part.Effect as BasicEffect).TextureEnabled = true;
                (part.Effect as BasicEffect).Texture = brickTexture;

                //part.Effect.Parameters["Texture"].SetValue(brickTexture);
                //    ps1Effect.Parameters["Texture"].SetValue(brickTexture);

                //part.Effect = ps1Effect;
                //ps1Effect.CurrentTechnique = ps1Effect.Techniques["Unlit"];

            }
        }

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
            var position = new Vector3N(1 + i * 1.2f * 20 - 10 + offset, 0, 0);
            SpawnBox(position);
        }

        for (int i = 0; i < 4; i++)
        {
            var position = new Vector3N(1 + i * 1.2f * 20 + offset, 20, 0);
            SpawnBox(position);
        }

        for (int i = 0; i < 3; i++)
        {
            var position = new Vector3N(1 + i * 1.2f * 20 + offset, 30, 0);
            SpawnBox(position);
        }

        CreateCharacter(new Vector3N(0, 2, 40));
    }

    bool characterActive;
    CharacterInput character;
    double time;

    void CreateCharacter(Vector3N position)
    {
        characterActive = true;
        character = new CharacterInput(characters, position, 
            new Capsule(0.5f*10, 1 * 10),
            minimumSpeculativeMargin: 0.1f, 
            mass: .1f, 
            maximumHorizontalForce: 200,
            maximumVerticalGlueForce: 10000,
            jumpVelocity: 100,
            speed: 40,
            maximumSlope: MathF.PI * 0.4f);
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
        var desc = BodyDescription.CreateConvexDynamic(new RigidPose(position), new BodyVelocity(Vector3N.Zero), 10, physics.Simulation.Shapes, shape);
        desc.Velocity.Linear = direction * 250f;
        var handle = physics.Simulation.Bodies.Add(desc);
        bullets.Add(handle);
    }
    bool mouseClick = false;

    public void Update(GameTime gameTime, Camera camera, KeyboardState input)
    {
        var gameTimePassed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        physics.Update(gameTimePassed);
        
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
            SpawnBox(new Vector3N(0, 5, -10));
        }

        //Console.WriteLine($"Supported: {characters.GetCharacterByBodyHandle(character.BodyHandle).Supported}");
        character.UpdateCharacterGoals(input, camera, gameTimePassed);
        character.UpdateCameraPosition(camera);

    }

    public void Draw(GameTime gameTime, Camera camera)
    {
        //var handles = bullets.Concat(boxes);
        foreach (var bodyHandle in boxes)
        {
            var bodyRef = physics.Simulation.Bodies.GetBodyReference(bodyHandle);
            var pose = bodyRef.Pose;

            foreach (ModelMesh mesh in cubeModel.Meshes)
            {
                // do this for custom effects
                //foreach (BasicEffect effect in mesh.Effects)
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


                    Matrix world = Matrix.CreateScale(1f) *
                                   Matrix.CreateFromQuaternion(new Quaternion(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W)) *
                                   Matrix.CreateTranslation(pose.Position);

                    cubeModel.Draw(world, camera.View, camera.Projection); // Choose model based on shape

                    var view = camera.View;
                    var projection = camera.Projection;
                    //ps1Effect.Parameters["WorldViewProj"]?.SetValue(world * view * projection);

                    //effect.Parameters["World"].SetValue(world);
                    //effect.Parameters["View"].SetValue(view);
                    //effect.Parameters["Projection"].SetValue(projection);
                    //effect.Parameters["Texture"].SetValue(brickTexture);

                }
                mesh.Draw();

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


        foreach (ModelMesh mesh in characterModel.Meshes)
        {

            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.LightingEnabled = true;
                effect.DiffuseColor = new Vector3(1, 1, 0);
                effect.DirectionalLight0.DiffuseColor = new Vector3(.7f, .7f, .7f);
                Vector3 lightAngle = new Vector3(20, -60, -60);
                lightAngle.Normalize();
                effect.DirectionalLight0.Direction = lightAngle;
                effect.AmbientLightColor = new Vector3(.3f, .3f, .3f);

            }

            var meshPos = character.Body.Pose.Position.ToVector3() - new Vector3(0, 10, 0);
            var meshOrientation = character.Body.Pose.Orientation.ToQuaternion();

            Matrix world = Matrix.CreateScale(.5f) *
                           Matrix.CreateFromQuaternion(meshOrientation) *
                           Matrix.CreateTranslation(meshPos);

            characterModel.Draw(world, camera.View, camera.Projection);
        }

        
    }
}
