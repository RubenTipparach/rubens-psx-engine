using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine.system;
using rubens_psx_engine.system.config;

namespace rubens_psx_engine
{

    public class PauseMenu : rubens_psx_engine.system.MenuScreen
    {
        const int MARGIN_LEFT = 50;

        Button[] buttons;

        public PauseMenu()
        {
            this.transitionOffTime = 200;
            this.transitionOnTime = 200;

            Reinitialize(); //Set up all the buttons.
        }

        public override void Reinitialize()
        {
            var buttonList = new List<Button>
            {
                new Button("Resume", HitButton_Resume)
            };

            // Only add Scene Selection button if scene menu is enabled in config
            if (SceneManager.IsSceneMenuEnabled())
            {
                buttonList.Add(new Button("Scene Selection", HitButton_SceneSelection));
            }

            buttonList.Add(new Button("Options", HitButton_Settings));
            buttonList.Add(new Button("Exit to desktop", HitButton_Quit));

            buttons = buttonList.ToArray();

            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].SetPosition(new Vector2(100, 200 + i * 80));
            }
        }

    
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void UpdateInput(GameTime gameTime)
        {
            if (!Globals.screenManager.IsActive)
                return;

            if (InputManager.GetKeyboardClick(Keys.Escape))
            {
                HitButton_Resume(null, null);
            }
            
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].Update(gameTime);
            }            

            base.UpdateInput(gameTime);
        }
        
        private void HitButton_Resume(object sender, ButtonArgs data)
        {
            ExitScreen();
        }

        private void HitButton_SceneSelection(object sender, ButtonArgs data)
        {
            Globals.screenManager.AddScreen(new SceneSelectionMenu());
        }

        private void HitButton_Settings(object sender, ButtonArgs data)
        {
            Globals.screenManager.AddScreen(new OptionsPage());
        }      

        private void HitButton_Quit(object sender, ButtonArgs data)
        {
            Globals.screenManager.Exit();
        }

        public override void Draw2D(GameTime gameTime)
        {
            //Dark red background.
            Globals.screenManager.getSpriteBatch.Draw(Globals.white, new Rectangle(0, 0, Globals.screenManager.Window.ClientBounds.Width, Globals.screenManager.Window.ClientBounds.Height), (Color.DarkRed * .8f) * this.getTransition);

            //header title.
            var gameName = RenderingConfigManager.Config.Game.Name;
            Globals.screenManager.getSpriteBatch.DrawString(Globals.fontNTR, gameName, new Vector2(100,100), Color.White * this.getTransition);

            //Buttons.            
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].Draw2D(gameTime);
            }
                        
        }
    }
}