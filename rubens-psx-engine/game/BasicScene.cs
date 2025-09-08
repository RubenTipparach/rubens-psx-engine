using anakinsoft.system.cameras;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace rubens_psx_engine
{
    /// <summary>
    /// Basic test scene with simple geometry
    /// </summary>
    public class BasicScene : Screen
    {
        Camera camera;
        public Camera GetCamera { get { return camera; } }

        // Simple entities
        List<Entity> entities;

        public BasicScene()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            
            // Create camera
            camera = new FPSCamera(gd, new Vector3(0, 5, -10));
            camera.Position = new Vector3(0, 5, -10);

            // Create some basic entities
            CreateBasicGeometry();
        }

        private void CreateBasicGeometry()
        {
            entities = new List<Entity>();

            // Available textures
            string[] textures = {
                "textures/prototype/brick",
                "textures/prototype/concrete", 
                "textures/prototype/prototype_512x512_blue1",
                "textures/prototype/prototype_512x512_green1",
                "textures/prototype/prototype_512x512_orange",
            };

            //// Create a simple scene with a few objects
            //for (int i = 0; i < 5; i++)
            //{
            //    var entity = new Entity("models/waterfall.xnb", textures[i % textures.Length], true);
            //    entity.SetPosition(new Vector3(i * 3 - 6, 0, 0));
            //    entity.SetScale(1.0f);
            //    entities.Add(entity);
            //}

            // Add one object higher up
            var highEntity = new Entity("models/waterfall.xnb", "models/texture_1", true);
            highEntity.SetPosition(new Vector3(0, 3, 5));
            highEntity.SetScale(1.0f);
            entities.Add(highEntity);
        }

        public override void Update(GameTime gameTime)
        {
            // Update camera
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
            string message = "Basic Scene\n\nESC = menu\nF1 = scene selection";
            Vector2 position = new Vector2(20, 20);
            
            getSpriteBatch.DrawString(Globals.fontNTR, message, position + Vector2.One, Color.Black);
            getSpriteBatch.DrawString(Globals.fontNTR, message, position, Color.White);
        }

        public override void Draw3D(GameTime gameTime)
        {
            foreach (var entity in entities)
            {
                entity.Draw3D(gameTime, camera);
            }
        }
    }
}