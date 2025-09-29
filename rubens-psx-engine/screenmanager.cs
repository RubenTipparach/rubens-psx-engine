using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine.system.config;
using rubens_psx_engine.system.utils;



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
            try
            {
                Logger.Info("ScreenManager: Starting initialization");
                Logger.LogSystemInfo();

                //Initialize the game.
                Logger.Info("ScreenManager: Creating GraphicsDeviceManager");
                graphics = new GraphicsDeviceManager(this);

                Logger.Info("ScreenManager: Creating SettingsManager");
                settingsManager = new SettingsManager();
                bool foundSettings = settingsManager.ReadSettingsFromFile();

                //If there are no settings found
                if (!foundSettings)
                {
                    Logger.Info("ScreenManager: No settings found, creating defaults");
                    //No settings found. Use default settings.
                    DisplayMode defaultMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                    settingsManager.GetSettings.screenwidth = defaultMode.Width;
                    settingsManager.GetSettings.screenheight = defaultMode.Height;
                    settingsManager.GetSettings.fullscreen = true;
                    settingsManager.GetSettings.soundvolume = 1.0f;
                    Logger.Info($"ScreenManager: Default resolution set to {defaultMode.Width}x{defaultMode.Height}");

                    //Write the settings file.
                    settingsManager.WriteSettingsToFile();
                }
                else
                {
                    Logger.Info($"ScreenManager: Settings loaded - Resolution: {settingsManager.GetSettings.screenwidth}x{settingsManager.GetSettings.screenheight}, Fullscreen: {settingsManager.GetSettings.fullscreen}");
                }

            
                Logger.Info("ScreenManager: Configuring graphics settings");
                //Settings are now loaded. Hook them into all the game systems.
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    Logger.Info("ScreenManager: Running on Windows platform");
                    //this.Window.IsBorderlessEXT = settingsManager.GetSettings.fullscreen;
                }
                else
                {
                    Logger.Info("ScreenManager: Running on non-Windows platform");
                    graphics.IsFullScreen = settingsManager.GetSettings.fullscreen;
                }

                graphics.PreferredBackBufferWidth = settingsManager.GetSettings.screenwidth;
                graphics.PreferredBackBufferHeight = settingsManager.GetSettings.screenheight;
                Logger.Info($"ScreenManager: Graphics buffer set to {settingsManager.GetSettings.screenwidth}x{settingsManager.GetSettings.screenheight}");

                SoundEffect.MasterVolume = settingsManager.GetSettings.soundvolume;
                Logger.Info($"ScreenManager: Sound volume set to {settingsManager.GetSettings.soundvolume}");

                graphics.SynchronizeWithVerticalRetrace = true; //vsync
                graphics.PreferMultiSampling = true;
                this.IsFixedTimeStep = false;

                Logger.Info("ScreenManager: Loading game configuration");
                this.Window.Title = RenderingConfigManager.Config.Game.Name;
                this.Window.AllowUserResizing = false;
                Logger.Info($"ScreenManager: Window title set to '{RenderingConfigManager.Config.Game.Name}'");

                Logger.Info("ScreenManager: Creating SoundManager");
                soundManager = new SoundManager();

                Content.RootDirectory = Globals.baseFolder;
                Logger.Info($"ScreenManager: Content root directory set to '{Globals.baseFolder}'");

                TargetElapsedTime = TimeSpan.FromSeconds(1 / 60.0f);
                Logger.Info("ScreenManager: Initialization completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Critical("ScreenManager: Fatal error during initialization", ex);
                throw; // Re-throw to prevent corrupted state
            }
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
            try
            {
                Logger.Info("ScreenManager: Starting LoadContent");
                Logger.Info($"ScreenManager: GraphicsDevice - {GraphicsDevice?.GetType().Name ?? "NULL"}");

                Logger.Info("ScreenManager: Creating SpriteBatch");
                spriteBatch = new SpriteBatch(GraphicsDevice);

                Logger.Info("ScreenManager: Loading white texture");
                try
                {
                    Globals.white = Content.Load<Texture2D>("textures\\white");
                    Logger.Info("ScreenManager: White texture loaded successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error("ScreenManager: Failed to load white texture", ex);
                    throw;
                }

                Logger.Info("ScreenManager: Initializing RetroRenderer");
                try
                {
                    retroRenderer = new rubens_psx_engine.system.postprocess.RetroRenderer(GraphicsDevice, this);
                    retroRenderer.Initialize();
                    Logger.Info("ScreenManager: RetroRenderer initialized successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error("ScreenManager: Failed to initialize RetroRenderer", ex);
                    throw;
                }

                Logger.Info("ScreenManager: Initializing ScreenshotManager");
                try
                {
                    screenshotManager = new rubens_psx_engine.system.utils.ScreenshotManager(GraphicsDevice, this);
                    Logger.Info("ScreenManager: ScreenshotManager initialized successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error("ScreenManager: Failed to initialize ScreenshotManager", ex);
                    throw;
                }

                Logger.Info("ScreenManager: Adding LoadScreen");
                AddScreen(new LoadScreen());
                Logger.Info("ScreenManager: LoadContent completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Critical("ScreenManager: Fatal error in LoadContent", ex);
                throw;
            }
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
                        Logger.Error("ScreenManager: Error disposing screen", ex);
#if !RELEASE
                        System.Console.WriteLine($"ScreenManager: Error disposing screen: {ex.Message}");
#endif
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
            // Post-processing can be enabled/disabled via config or screen override
            var config = rubens_psx_engine.system.config.RenderingConfigManager.Config;
            bool usePostProcessing = config.Rendering.EnablePostProcessing;

            // Check if the top screen overrides post-processing
            if (screens.Count > 0 && screens[screens.Count - 1] != null)
            {
                var screenPostProcessingOverride = screens[screens.Count - 1].OverridePostProcessing();
                if (screenPostProcessingOverride.HasValue)
                {
                    usePostProcessing = screenPostProcessingOverride.Value;
                }
            }

            // Apply post-processing setting to RetroRenderer
            retroRenderer.PostProcessStack.Enabled = usePostProcessing;
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
                    Logger.Error("ScreenManager: Error disposing screen during ExitAllScreens", ex);
#if !RELEASE
                    System.Console.WriteLine($"ScreenManager: Error disposing screen during ExitAllScreens: {ex.Message}");
#endif
                }
            }
            
            screens.Clear();
        }

    }
}
