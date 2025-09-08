using anakinsoft.system.cameras;
using anakinsoft.system.physics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

using Vector3N = System.Numerics.Vector3;
namespace rubens_psx_engine
{
    public class ThirdPersonSandboxScreen : Screen
    {
        Camera camera;
        public Camera GetCamera { get { return camera; } }

        Entity chair;

        //adding physics for test
        Simulation simulation;
        Dictionary<BodyHandle, Matrix> bodyTransforms = new();
        Model cubeModel;
        Model bulletModel;
        PhysicsSandbox physicsSandbox;
        //BufferPool bufferPool;

        public ThirdPersonSandboxScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            camera = new FPSCamera(gd, new Vector3(0,0,0));
            camera.Position = Globals.CAMERAPOS;

            chair = new Entity("models/waterfall.xnb", "models/texture_1", true);
            //bufferPool = new();
            chair.SetPosition(new Vector3(0, 0, 50));


            // Create ground
      

            //var groundHandle = physics.Simulation.Bodies.Add(
            //    BodyDescription.CreateKinematic(
            //        pose: new RigidPose(new System.Numerics.Vector3(0, -0.5f, 0)),

            //        groundDesc));
            physicsSandbox = new();
        }

        public override void Update(GameTime gameTime)
        {
            physicsSandbox.Update(gameTime, camera, Keyboard.GetState());

            base.Update(gameTime);
        }

        public override void UpdateInput(GameTime gameTime)
        {
            if (!Globals.screenManager.IsActive)
                return; //If game window is not focused, then exit here.
            
            camera.Update(gameTime); //Update the camera.



            if (InputManager.GetKeyboardClick(Keys.Escape)) //Handle key input.
            {
                Globals.screenManager.AddScreen(new PauseMenu());
            }

            // Add F1 key to switch to scene selection
            if (InputManager.GetKeyboardClick(Keys.F1))
            {
                Globals.screenManager.AddScreen(new SceneSelectionMenu());
            }
        }      
        

        public override void Draw2D(GameTime gameTime)
        {
            // Draw third person sandbox UI
            string message = "Third Person Sandbox Scene\n\nWASD = move\nESC = menu\nF1 = scene selection";
            Vector2 messageSize = Globals.fontNTR.MeasureString(message);
            
            // Position message in top-left corner
            Vector2 position = new Vector2(20, 20);
            
            // Draw text with better visibility
            getSpriteBatch.DrawString(Globals.fontNTR, message, position + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, message, position, Color.White);
        }

        public override void Draw3D(GameTime gameTime)
        {
            //Render the chair model.
            physicsSandbox.Draw(gameTime, this.camera);
            //chair.Draw3D(gameTime, this.camera);           
        }
    }
}