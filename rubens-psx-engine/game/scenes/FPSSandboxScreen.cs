using anakinsoft.system.cameras;
using anakinsoft.system.physics;
using anakinsoft.utilities;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;
using System.Collections.Generic;
using System.IO;

using Vector3N = System.Numerics.Vector3;
namespace anakinsoft.game.scenes
{
    public class FPSSandboxScreen : Screen
    {
        FPSCamera fpsCamera;
        public Camera GetCamera { get { return fpsCamera; } }

        Entity chair;

        //adding physics for test
        Simulation simulation;
        Dictionary<BodyHandle, Matrix> bodyTransforms = new();
        Model cubeModel;
        Model bulletModel;
        FPSPhysicsSandbox physicsSandbox;

        // Camera offset configuration - adjustable variables for tweaking
        public Vector3 CameraOffset = new Vector3(0, 15, 0); // Y offset to mount camera above character center
        public Vector3 CameraLookOffset = new Vector3(0, -5, 0); // Additional offset for look direction

        public FPSSandboxScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            fpsCamera = new FPSCamera(gd, new Vector3(0, 0, 0));
            fpsCamera.Position = Globals.CAMERAPOS;

            chair = new Entity("models/waterfall.xnb", "models/texture_1", true);
            chair.SetPosition(new Vector3(0, 0, 50));

            physicsSandbox = new FPSPhysicsSandbox();
        }

        public override void Update(GameTime gameTime)
        {
            physicsSandbox.Update(gameTime, fpsCamera, Keyboard.GetState());

            // Mount FPS camera to character controller
            UpdateCameraMountedToCharacter();

            base.Update(gameTime);
        }

        private void UpdateCameraMountedToCharacter()
        {
            var character = physicsSandbox.GetCharacter();
            if (character.HasValue)
            {
                // Get character position and orientation
                var characterPos = character.Value.Body.Pose.Position.ToVector3();
                var characterOrientation = character.Value.Body.Pose.Orientation.ToQuaternion();
                
                // Apply camera offset relative to character center
                var offsetInWorldSpace = Vector3.Transform(CameraOffset, Matrix.CreateFromQuaternion(characterOrientation));
                
                // Set camera position to character center + offset
                fpsCamera.Position = characterPos + offsetInWorldSpace;
                
                // Optional: Add additional look offset for targeting
                var lookOffsetInWorldSpace = Vector3.Transform(CameraLookOffset, Matrix.CreateFromQuaternion(characterOrientation));
                // Note: FPS camera handles its own look direction via mouse input, so we don't override Target here
            }
        }

        public override void UpdateInput(GameTime gameTime)
        {
            if (!Globals.screenManager.IsActive)
                return; //If game window is not focused, then exit here.
            
            fpsCamera.Update(gameTime); //Update the camera.

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
                    System.Console.WriteLine("FPSSandboxScreen: L key pressed - toggling bounding boxes");
                    physicsSandbox.Scene.BoundingBoxRenderer.ToggleBoundingBoxes();
                    
                    if (physicsSandbox.Scene.Physics?.Simulation?.BroadPhase != null)
                    {
                        var activeCount = physicsSandbox.Scene.Physics.Simulation.BroadPhase.ActiveTree.LeafCount;
                        var staticCount = physicsSandbox.Scene.Physics.Simulation.BroadPhase.StaticTree.LeafCount;
                        System.Console.WriteLine($"FPSSandboxScreen: Physics bodies - Active: {activeCount}, Static: {staticCount}");
                    }
                }
                else
                {
                    System.Console.WriteLine("FPSSandboxScreen: No BoundingBoxRenderer available");
                }
            }
        }      
        

        public override void Draw2D(GameTime gameTime)
        {
            // Draw FPS sandbox UI
            string message = "FPS Sandbox Scene\n\nWASD = move\nMouse = look\nESC = menu\nF1 = scene selection\nLeft Click = shoot\nB = spawn box\nL = bounding boxes";
            Vector2 messageSize = Globals.fontNTR.MeasureString(message);
            
            // Position message in top-left corner
            Vector2 position = new Vector2(20, 20);
            
            // Draw text with better visibility
            getSpriteBatch.DrawString(Globals.fontNTR, message, position + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, message, position, Color.White);

            // Draw camera offset info for debugging
            string offsetInfo = $"Camera Offset: {CameraOffset}\nLook Offset: {CameraLookOffset}";
            Vector2 offsetPosition = new Vector2(20, Globals.screenManager.Window.ClientBounds.Height - 80);
            
            getSpriteBatch.DrawString(Globals.fontNTR, offsetInfo, offsetPosition + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, offsetInfo, offsetPosition, Color.Yellow);
        }

        public override void Draw3D(GameTime gameTime)
        {
            //Render the physics sandbox
            physicsSandbox.Draw(gameTime, fpsCamera);
        }
    }
}