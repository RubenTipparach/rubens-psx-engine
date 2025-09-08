using anakinsoft.system.cameras;
using anakinsoft.system.physics;
using anakinsoft.system.character;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine.system.controllers;
using System;
using System.Collections.Generic;
using Vector3N = System.Numerics.Vector3;

namespace rubens_psx_engine
{
    /// <summary>
    /// Third person scene using the same hallway geometry as the FPS scene
    /// Demonstrates third person controller in the FPS environment
    /// </summary>
    public class ThirdPersonHallwayScene : Screen
    {
        Camera camera;
        public Camera GetCamera { get { return camera; } }

        // Controller
        ThirdPersonController controller;
        
        // Hallway entities (same as FPS scene)
        List<Entity> hallwayBlocks;

        public ThirdPersonHallwayScene()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            
            // Create camera
            camera = new FPSCamera(gd, new Vector3(0, 2, 0));
            camera.Position = new Vector3(0, 2, 0);

            // Create third person controller
            controller = new ThirdPersonController();
            
            // Create the same hallway as FPS scene
            CreateHallway();
        }

        private void CreateHallway()
        {
            hallwayBlocks = new List<Entity>();

            // Available textures for variation (same as FPS scene)
            string[] textures = {
                "textures/prototype/brick",
                "textures/prototype/concrete", 
                "textures/prototype/dark",
                "textures/prototype/prototype_512x512_blue1",
                "textures/prototype/prototype_512x512_green1",
                "textures/prototype/prototype_512x512_orange",
                "textures/prototype/wood"
            };

            Random random = new Random(42); // Same seed for consistent generation

            // Create hallway floor, walls, and ceiling (same dimensions as FPS scene)
            // Hallway dimensions: 4 units wide, 4 units high, 50 units long
            
            // Floor
            for (int z = -10; z < 40; z += 2)
            {
                for (int x = -4; x <= 4; x += 2)
                {
                    var floor = new Entity("models/waterfall.xnb", textures[random.Next(textures.Length)], true);
                    floor.SetPosition(new Vector3(x, 0, z));
                    floor.SetScale(1.0f);
                    hallwayBlocks.Add(floor);
                }
            }

            // Left wall
            for (int z = -10; z < 40; z += 2)
            {
                for (int y = 2; y <= 6; y += 2)
                {
                    var wall = new Entity("models/waterfall.xnb", textures[random.Next(textures.Length)], true);
                    wall.SetPosition(new Vector3(-6, y, z));
                    wall.SetScale(1.0f);
                    hallwayBlocks.Add(wall);
                }
            }

            // Right wall  
            for (int z = -10; z < 40; z += 2)
            {
                for (int y = 2; y <= 6; y += 2)
                {
                    var wall = new Entity("models/waterfall.xnb", textures[random.Next(textures.Length)], true);
                    wall.SetPosition(new Vector3(6, y, z));
                    wall.SetScale(1.0f);
                    hallwayBlocks.Add(wall);
                }
            }

            // Ceiling
            for (int z = -10; z < 40; z += 2)
            {
                for (int x = -4; x <= 4; x += 2)
                {
                    var ceiling = new Entity("models/waterfall.xnb", textures[random.Next(textures.Length)], true);
                    ceiling.SetPosition(new Vector3(x, 8, z));
                    ceiling.SetScale(1.0f);
                    hallwayBlocks.Add(ceiling);
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            // Update controller
            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();
            controller.Update(gameTime, keyboard, mouse);

            // Update camera based on controller
            controller.UpdateCamera(camera);
            
            // Update camera matrices
            camera.Update(gameTime);

            base.Update(gameTime);
        }

        public override void UpdateInput(GameTime gameTime)
        {
            if (!Globals.screenManager.IsActive)
                return;

            if (InputManager.GetKeyboardClick(Keys.Escape))
            {
                Globals.screenManager.AddScreen(new PauseMenu());
            }

            if (InputManager.GetKeyboardClick(Keys.F1))
            {
                Globals.screenManager.AddScreen(new SceneSelectionMenu());
            }
        }

        public override void Draw2D(GameTime gameTime)
        {
            // Draw third person hallway UI
            string message = "Third Person Hallway Scene\n\nWASD = move\nThird person camera follows behind\nESC = menu\nF1 = scene selection";
            
            Vector2 position = new Vector2(20, 20);
            
            // Draw text with outline for visibility
            getSpriteBatch.DrawString(Globals.fontNTR, message, position + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, message, position, Color.White);
        }

        public override void Draw3D(GameTime gameTime)
        {
            // Draw all hallway blocks (same geometry as FPS scene)
            foreach (var block in hallwayBlocks)
            {
                block.Draw3D(gameTime, camera);
            }
        }

        public override void ExitScreen()
        {
            // Clean up controller
            controller?.Dispose();
            base.ExitScreen();
        }
    }
}