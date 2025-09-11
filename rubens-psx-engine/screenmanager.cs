using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;



namespace rubens_psx_engine
{
    //ScreenManager is the backbone of the game. It handles the stack of all the different screens.
    //Screens can be added and removed from the stack. The top screen in the stack is the one the player interacts with.
    //We also handle the game initialization here.
    public class ScreenManager : Game
    {
        List<Screen> screens = new List<Screen>();

        GraphicsDeviceManager graphics;
        public GraphicsDeviceManager getGraphicsDevice { get { return graphics; } }

        SpriteBatch spriteBatch;
        public SpriteBatch getSpriteBatch 
        { 
            get 
            { 
                // TODO: Currently using legacy mode due to native UI resolution issues
                // Always return the standard spriteBatch for now
                return spriteBatch; 
            } 
        }

        BloomComponent bloom;
        public BloomComponent getBloom { get { return bloom; } }

        rubens_psx_engine.system.postprocess.RetroRenderer retroRenderer;
        rubens_psx_engine.system.utils.ScreenshotManager screenshotManager;

        SoundManager soundManager;
        public SoundManager GetSoundManager { get { return soundManager; } }

        SettingsManager settingsManager;
        public SettingsManager GetSettingsManager { get { return settingsManager; } }

        public ScreenManager()
        {
            //Initialize the game.
            graphics = new GraphicsDeviceManager(this);

            settingsManager = new SettingsManager();
            bool foundSettings = settingsManager.ReadSettingsFromFile();            

            //If there are no settings found
            if (!foundSettings)
            {
                //No settings found. Use default settings.
                DisplayMode defaultMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                settingsManager.GetSettings.screenwidth = defaultMode.Width;
                settingsManager.GetSettings.screenheight = defaultMode.Height;
                settingsManager.GetSettings.fullscreen = true;
                settingsManager.GetSettings.soundvolume = 1.0f;

                //Write the settings file.
                settingsManager.WriteSettingsToFile();
            }

            
            //Settings are now loaded. Hook them into all the game systems.
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                //this.Window.IsBorderlessEXT = settingsManager.GetSettings.fullscreen;
            }
            else
            {
                graphics.IsFullScreen = settingsManager.GetSettings.fullscreen;
            }

            graphics.PreferredBackBufferWidth = settingsManager.GetSettings.screenwidth;
            graphics.PreferredBackBufferHeight = settingsManager.GetSettings.screenheight;   
            

            SoundEffect.MasterVolume = settingsManager.GetSettings.soundvolume;

            graphics.SynchronizeWithVerticalRetrace = true; //vsync
            graphics.PreferMultiSampling = true;
            this.IsFixedTimeStep = false;
            this.Window.Title = Globals.WINDOWNAME;
            this.Window.AllowUserResizing = false;
            // Mouse visibility will be set based on config
            //this.IsMouseVisible = true;

            soundManager = new SoundManager();

            Content.RootDirectory = Globals.baseFolder;
            TargetElapsedTime = TimeSpan.FromSeconds(1 / 60.0f);
        }

        //Call this when player changes their screen resolution while in-game.
        public void InitializeAllScreens()
        {
            for (int i = screens.Count - 1; i >= 0; i--)
            {
                if (screens[i] == null)
                    continue;

                screens[i].Reinitialize();
            }

            // Handle resolution change for retro renderer
            retroRenderer?.OnResolutionChanged();
        }

        //Add a new screen to the stack.
        public void AddScreen(Screen screen)
        {
            screens.Add(screen);
        }

        //Check whether the screen is at the top of the stack.
        public bool GetIsTopScreen(Screen screen)
        {
            return (screens[screens.Count - 1] == screen);
        }

        //Get the list of screens (for menu management)
        public List<Screen> GetScreens()
        {
            return screens;
        }

        
        protected override void Initialize()
        {
            // DEPRECATED: BloomComponent disabled in favor of RetroRenderer
            // bloom = new BloomComponent(this);
            // bloom.Settings = BloomSettings.PresetSettings[6];
            // Components.Add(bloom);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Globals.white = Content.Load<Texture2D>("textures\\white");

            // Initialize the retro renderer after GraphicsDevice is ready
            retroRenderer = new rubens_psx_engine.system.postprocess.RetroRenderer(GraphicsDevice, this);
            retroRenderer.Initialize();

            // Initialize screenshot manager
            screenshotManager = new rubens_psx_engine.system.utils.ScreenshotManager(GraphicsDevice, this);

            AddScreen(new LoadScreen()); //Go to the loading screen.
        }

        protected override void UnloadContent()
        {
            // Dispose all screens first (this will clean up physics resources)
            ExitAllScreens();
            
            // Then dispose other manager resources
            retroRenderer?.Dispose();
        }

        //This gets called every frame. This is the main update loop.
        protected override void Update(GameTime gameTime)
        {
            InputManager.Update(gameTime);
            
            // Update mouse lock based on config
            var config = rubens_psx_engine.system.config.RenderingConfigManager.Config;
            this.IsMouseVisible = !config.Input.LockMouse;

            for (int i = screens.Count - 1; i >= 0; i--)
            {
                if (screens[i] == null)  //Just in case....
                    continue;

                //remove any screens waiting to be removed.
                if (screens[i].getState == ScreenState.Deactivated)
                {
                    var screenToRemove = screens[i];
                    screens.RemoveAt(i);
                    
                    // Dispose the screen to clean up its resources (including physics)
                    try
                    {
                        screenToRemove.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"ScreenManager: Error disposing screen: {ex.Message}");
                    }
                    
                    continue;
                }

                //update screen.
                screens[i].Update(gameTime);
            }

            //update input only on screen on top of the stack.
            if (screens.Count > 0)
            {
                screens[screens.Count - 1].UpdateInput(gameTime);
            }

            soundManager.Update(gameTime);
            
            // Update screenshot manager
            screenshotManager?.Update();

            base.Update(gameTime);
        }

        //Draw 2D and 3D stuff with separated rendering pipeline.
        protected override void Draw(GameTime gameTime)
        {
            // Always use RetroRenderer system now (BloomComponent is deprecated)
            // Post-processing can be enabled/disabled via config
            var config = rubens_psx_engine.system.config.RenderingConfigManager.Config;
            bool usePostProcessing = config.Rendering.EnablePostProcessing;
            bool useNativeUI = config.Rendering.UI.UseNativeResolution;

            // PHASE 1: Render 3D world (always use RetroRenderer for consistent behavior)
            // The RetroRenderer handles both post-processing enabled and disabled cases
            retroRenderer.BeginScene();
            
            // Check if any active screen has a custom background color
            Color clearColor = Globals.COLOR_BACKGROUND; // Default
            for (int i = 0; i < screens.Count; i++)
            {
                if (screens[i] != null)
                {
                    var screenBgColor = screens[i].GetBackgroundColor();
                    if (screenBgColor.HasValue)
                    {
                        clearColor = screenBgColor.Value;
                        break; // Use the first screen with a custom background color
                    }
                }
            }
            
            GraphicsDevice.Clear(clearColor);
            
            // Render 3D world and game content
            for (int i = 0; i < screens.Count; i++)
            {
                if (screens[i] == null)
                    continue;

                //Draw 3D things at low resolution for retro effect
                screens[i].Draw3D(gameTime);
            }
            
            graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            retroRenderer.EndScene();

            // PHASE 2: Render UI (temporarily forcing legacy mode to debug)
            // TODO: Fix native UI resolution rendering
            {
                // Legacy: Render UI at same resolution as 3D world
                spriteBatch.Begin();
                for (int i = 0; i < screens.Count; i++)
                {
                    if (screens[i] == null)
                        continue;

                    screens[i].Draw2D(gameTime);
                }
                spriteBatch.End();
            }
            
            graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            base.Draw(gameTime);
        }

        public void ResetBloom()
        {
            if (bloom != null)
            {
                Components.Remove(bloom);
                bloom.Dispose();
                bloom = new BloomComponent(this);
                bloom.Settings = BloomSettings.PresetSettings[6];
                Components.Add(bloom);
            }
        }

        /// <summary>
        /// Reload configuration and update retro renderer settings
        /// </summary>
        public void ReloadRenderingConfig()
        {
            rubens_psx_engine.system.config.RenderingConfigManager.ReloadConfig();
            retroRenderer?.ReloadConfig();
        }

        //Delete all screens from the stack.
        public void ExitAllScreens()
        {
            // Dispose all screens before clearing
            foreach (var screen in screens)
            {
                try
                {
                    screen?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"ScreenManager: Error disposing screen during ExitAllScreens: {ex.Message}");
                }
            }
            
            screens.Clear();
        }

    }
}
