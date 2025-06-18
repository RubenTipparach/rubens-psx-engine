using anakinsoft.system.cameras;
using anakinsoft.system.physics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;


namespace rubens_psx_engine
{
    public class Worldscreen : Screen
    {
        Camera camera;
        public Camera GetCamera { get { return camera; } }

        Entity chair;

        //adding physics for test
        PhysicsSystem physics;
        Dictionary<BodyHandle, Matrix> bodyTransforms = new();
        Model cubeModel;
        Model bulletModel;

        public Worldscreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            camera = new FPSCamera(gd, new Vector3(0,0,0));
            camera.Position = Globals.CAMERAPOS;

            chair = new Entity("models/waterfall.xnb", "models/texture_1", true);
            chair.SetPosition(new Vector3(0, 0, 50));

            // Create ground
            var groundDesc = new CollidableDescription(
                physics.Simulation.Shapes.Add(new Box(100, 1, 100)), 0.1f);

            //var groundHandle = physics.Simulation.Bodies.Add(
            //    BodyDescription.CreateKinematic(
            //        pose: new RigidPose(new System.Numerics.Vector3(0, -0.5f, 0)),
                    
            //        groundDesc));

        }

        public override void Update(GameTime gameTime)
        {
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
        }      
        

        public override void Draw2D(GameTime gameTime)
        {
            //Draw a message in center of screen.
            string message = "FNA Starter Kit\n\nWASD = move chair\nESC = menu";
            Vector2 messageSize = Globals.fontNTR.MeasureString(message);
            //Globals.screenManager.getSpriteBatch.DrawString(Globals.fontNTR, message, new Vector2(Globals.screenManager.Window.ClientBounds.Width / 2 - messageSize.X / 2, 20 ), Color.White);
            
            //Draw image of an orange.
            //Globals.screenManager.getSpriteBatch.Draw(Globals.orange, new Rectangle(20, 20, 200, 200), Color.White);
        }

        public override void Draw3D(GameTime gameTime)
        {
            //Render the chair model.
            chair.Draw3D(gameTime, this.camera);           
        }
    }
}