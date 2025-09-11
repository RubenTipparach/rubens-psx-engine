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
using rubens_psx_engine.system;

using Vector3N = System.Numerics.Vector3;
namespace rubens_psx_engine
{
    public class ThirdPersonSandboxScreen : PhysicsScreen
    {
        Camera camera;
        public Camera GetCamera { get { return camera; } }

        Entity chair;

        //adding physics for test
        PhysicsSandbox physicsSandbox;

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
            SetScene(physicsSandbox.Scene); // Register scene with physics screen for automatic disposal
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

            // Handle L key to toggle bounding box visualization
            if (InputManager.GetKeyboardClick(Keys.L))
            {
                if (physicsSandbox?.Scene?.BoundingBoxRenderer != null)
                {
                    System.Console.WriteLine("ThirdPersonSandboxScreen: L key pressed - toggling bounding boxes");
                    physicsSandbox.Scene.BoundingBoxRenderer.ToggleBoundingBoxes();
                    
                    if (physicsSandbox.Scene.Physics?.Simulation?.BroadPhase != null)
                    {
                        var activeCount = physicsSandbox.Scene.Physics.Simulation.BroadPhase.ActiveTree.LeafCount;
                        var staticCount = physicsSandbox.Scene.Physics.Simulation.BroadPhase.StaticTree.LeafCount;
                        System.Console.WriteLine($"ThirdPersonSandboxScreen: Physics bodies - Active: {activeCount}, Static: {staticCount}");
                    }
                }
                else
                {
                    System.Console.WriteLine("ThirdPersonSandboxScreen: No BoundingBoxRenderer available");
                }
            }
        }      
        

        public override void Draw2D(GameTime gameTime)
        {
            // Draw third person sandbox UI
            string message = "Third Person Sandbox Scene\n\nWASD = move\nESC = menu\nF1 = scene selection\nL = bounding boxes";
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