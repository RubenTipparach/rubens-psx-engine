using System;
using System.IO;
using Microsoft.Xna.Framework;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace rubens_psx_engine.system.config
{
    /// <summary>
    /// Configuration settings for dithering/pixelation effects
    /// </summary>
    public class DitherConfig
    {
        public int RenderWidth { get; set; } = 320;
        public int RenderHeight { get; set; } = 180;
        public float Strength { get; set; } = 0.8f;
        public float ColorLevels { get; set; } = 6.0f;
        public bool UsePointSampling { get; set; } = true;
    }

    /// <summary>
    /// Configuration settings for bloom effects
    /// </summary>
    public class BloomConfig
    {
        public int Preset { get; set; } = 6; // Blendo preset
        public float? Threshold { get; set; }
        public float? Intensity { get; set; }
        public float? BlurAmount { get; set; }
    }

    /// <summary>
    /// Configuration settings for tint effects
    /// </summary>
    public class TintConfig
    {
        public bool Enabled { get; set; } = false;
        public float[] Color { get; set; } = { 1.0f, 1.0f, 1.0f, 1.0f };
        public float Intensity { get; set; } = 1.0f;

        public Color GetColor()
        {
            if (Color == null || Color.Length != 4)
                return Microsoft.Xna.Framework.Color.White;
            
            return new Color(Color[0], Color[1], Color[2], Color[3]);
        }
    }

    /// <summary>
    /// Antialiasing configuration settings
    /// </summary>
    public class AntialiasingConfig
    {
        public bool Enabled { get; set; } = false;
        public int SampleCount { get; set; } = 4;
        
        /// <summary>
        /// Get validated sample count (ensures it's a power of 2 and within valid range)
        /// </summary>
        public int GetValidatedSampleCount()
        {
            // Valid MSAA sample counts: 1, 2, 4, 8, 16
            int[] validCounts = { 1, 2, 4, 8, 16 };
            
            // Find the closest valid count
            int closest = validCounts[0];
            int minDiff = Math.Abs(SampleCount - closest);
            
            foreach (int count in validCounts)
            {
                int diff = Math.Abs(SampleCount - count);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = count;
                }
            }
            
            return closest;
        }
    }

    /// <summary>
    /// UI rendering configuration settings
    /// </summary>
    public class UIRenderingConfig
    {
        public bool UseNativeResolution { get; set; } = true;
        public float ScaleFactor { get; set; } = 1.0f;
        
        /// <summary>
        /// Get validated scale factor (clamped to reasonable range)
        /// </summary>
        public float GetValidatedScaleFactor()
        {
            return Math.Max(0.5f, Math.Min(4.0f, ScaleFactor));
        }
    }

    /// <summary>
    /// General rendering pipeline configuration
    /// </summary>
    public class RenderingPipelineConfig
    {
        public bool EnablePostProcessing { get; set; } = true;
        public string ScaleMode { get; set; } = "nearest";
        public bool MaintainAspectRatio { get; set; } = true;
        public AntialiasingConfig Antialiasing { get; set; } = new AntialiasingConfig();
        public UIRenderingConfig UI { get; set; } = new UIRenderingConfig();
    }

    /// <summary>
    /// Input configuration settings
    /// </summary>
    public class InputConfig
    {
        public bool LockMouse { get; set; } = false;
    }

    /// <summary>
    /// Development and debugging configuration
    /// </summary>
    public class DevelopmentConfig
    {
        public bool EnableScreenshots { get; set; } = true;
        public string ScreenshotDirectory { get; set; } = "screenshots";
    }

    /// <summary>
    /// Main configuration class containing all rendering settings
    /// </summary>
    public class RenderingConfig
    {
        public DitherConfig Dither { get; set; } = new DitherConfig();
        public BloomConfig Bloom { get; set; } = new BloomConfig();
        public TintConfig Tint { get; set; } = new TintConfig();
        public RenderingPipelineConfig Rendering { get; set; } = new RenderingPipelineConfig();
        public InputConfig Input { get; set; } = new InputConfig();
        public DevelopmentConfig Development { get; set; } = new DevelopmentConfig();
    }

    /// <summary>
    /// Manager class for loading and managing rendering configuration
    /// </summary>
    public static class RenderingConfigManager
    {
        private static RenderingConfig _config;
        private static readonly string ConfigPath = "config.yml";

        public static RenderingConfig Config => _config ?? LoadConfig();

        /// <summary>
        /// Load configuration from YAML file
        /// </summary>
        public static RenderingConfig LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    // Create default config if file doesn't exist
                    _config = new RenderingConfig();
                    SaveConfig();
                    return _config;
                }

                var yaml = File.ReadAllText(ConfigPath);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                _config = deserializer.Deserialize<RenderingConfig>(yaml);
                
                // Validate configuration
                ValidateConfig(_config);
                
                return _config;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
                
                // Fall back to default configuration
                _config = new RenderingConfig();
                return _config;
            }
        }

        /// <summary>
        /// Save current configuration to YAML file
        /// </summary>
        public static void SaveConfig()
        {
            try
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var yaml = serializer.Serialize(_config ?? new RenderingConfig());
                File.WriteAllText(ConfigPath, yaml);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
            }
        }

        /// <summary>
        /// Reload configuration from file
        /// </summary>
        public static void ReloadConfig()
        {
            _config = null;
            LoadConfig();
        }

        /// <summary>
        /// Get the render resolution based on config
        /// </summary>
        public static Point GetRenderResolution()
        {
            return new Point(Config.Dither.RenderWidth, Config.Dither.RenderHeight);
        }

        /// <summary>
        /// Get the appropriate sampler state for the current config
        /// </summary>
        public static Microsoft.Xna.Framework.Graphics.SamplerState GetSamplerState()
        {
            return Config.Dither.UsePointSampling 
                ? Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp 
                : Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp;
        }

        /// <summary>
        /// Validate configuration values
        /// </summary>
        private static void ValidateConfig(RenderingConfig config)
        {
            // Clamp render resolution to reasonable bounds
            config.Dither.RenderWidth = Math.Max(160, Math.Min(1920, config.Dither.RenderWidth));
            config.Dither.RenderHeight = Math.Max(90, Math.Min(1080, config.Dither.RenderHeight));
            
            // Clamp dither strength
            config.Dither.Strength = Math.Max(0f, Math.Min(1f, config.Dither.Strength));
            
            // Clamp color levels
            config.Dither.ColorLevels = Math.Max(2f, Math.Min(256f, config.Dither.ColorLevels));
            
            // Validate bloom preset
            config.Bloom.Preset = Math.Max(0, Math.Min(7, config.Bloom.Preset));
            
            // Validate tint color
            if (config.Tint.Color == null || config.Tint.Color.Length != 4)
            {
                config.Tint.Color = new float[] { 1.0f, 1.0f, 1.0f, 1.0f };
            }
        }
    }
}