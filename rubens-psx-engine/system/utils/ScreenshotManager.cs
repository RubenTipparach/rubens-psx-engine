using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace rubens_psx_engine.system.utils
{
    /// <summary>
    /// Utility class for taking and managing screenshots
    /// </summary>
    public class ScreenshotManager
    {
        private GraphicsDevice graphicsDevice;
        private Game game;
        private bool previousF12State = false;
        private string screenshotDirectory;
        
        public ScreenshotManager(GraphicsDevice graphicsDevice, Game game)
        {
            this.graphicsDevice = graphicsDevice;
            this.game = game;
            
            // Get screenshot directory from config
            var config = rubens_psx_engine.system.config.RenderingConfigManager.Config.Development;
            screenshotDirectory = config.ScreenshotDirectory;
            
            // Ensure directory exists
            if (!Directory.Exists(screenshotDirectory))
            {
                Directory.CreateDirectory(screenshotDirectory);
            }
        }
        
        /// <summary>
        /// Update screenshot manager - call this in your main Update loop
        /// </summary>
        public void Update()
        {
            var config = rubens_psx_engine.system.config.RenderingConfigManager.Config.Development;
            if (!config.EnableScreenshots) return;
            
            var keyboard = Keyboard.GetState();
            bool currentF12State = keyboard.IsKeyDown(Keys.F12);
            
            // Take screenshot on F12 key press (not hold)
            if (currentF12State && !previousF12State)
            {
                TakeScreenshot();
            }
            
            previousF12State = currentF12State;
        }
        
        /// <summary>
        /// Take a screenshot of the current frame buffer
        /// </summary>
        public void TakeScreenshot()
        {
            try
            {
                // Get the back buffer data
                int width = graphicsDevice.PresentationParameters.BackBufferWidth;
                int height = graphicsDevice.PresentationParameters.BackBufferHeight;
                Color[] backBuffer = new Color[width * height];
                
                graphicsDevice.GetBackBufferData(backBuffer);
                
                // Create texture from back buffer data
                var texture = new Texture2D(graphicsDevice, width, height);
                texture.SetData(backBuffer);
                
                // Generate unique filename with timestamp
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
                string filename = Path.Combine(screenshotDirectory, $"screenshot_{timestamp}.png");
                
                // Save as PNG
                using (var stream = File.Create(filename))
                {
                    texture.SaveAsPng(stream, width, height);
                }
                
                // Clean up
                texture.Dispose();
                
                Console.WriteLine($"Screenshot saved: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save screenshot: {ex.Message}");
            }
        }
    }
}