using System;
using System.IO;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using rubens_psx_engine.system.config;
using rubens_psx_engine.system.postprocess;

namespace rubens_psx_engine.tests
{
    [TestFixture]
    public class VisualRenderingIntegrationTests
    {
        private string testConfigPath;
        private string originalConfigContent;

        [SetUp]
        public void SetUp()
        {
            testConfigPath = "config.yml";
            
            // Backup original config if it exists
            if (File.Exists(testConfigPath))
            {
                originalConfigContent = File.ReadAllText(testConfigPath);
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Restore original config
            if (originalConfigContent != null)
            {
                File.WriteAllText(testConfigPath, originalConfigContent);
            }
            else if (File.Exists(testConfigPath))
            {
                File.Delete(testConfigPath);
            }
            
            // Reset the config manager
            RenderingConfigManager.ReloadConfig();
        }

        [Test]
        public void PostProcessingEnabled_ConfiguresRetroRendererCorrectly()
        {
            // Arrange - Create config with post-processing enabled
            var testYaml = @"
dither:
  renderWidth: 480
  renderHeight: 270
  strength: 0.9
  colorLevels: 6.0
  usePointSampling: true

bloom:
  preset: 3

tint:
  enabled: true
  color: [0.9, 0.8, 0.7, 1.0]
  intensity: 0.8

rendering:
  enablePostProcessing: true
  scaleMode: ""nearest""
  maintainAspectRatio: true

input:
  lockMouse: false

development:
  enableScreenshots: true
  screenshotDirectory: ""screenshots""
";

            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act - Load configuration
            var config = RenderingConfigManager.Config;
            
            // Assert - Verify all configurations are applied correctly
            Assert.That(config.Rendering.EnablePostProcessing, Is.True, 
                "Post-processing should be enabled when set to true in config");
            
            Assert.That(config.Dither.RenderWidth, Is.EqualTo(480), 
                "Dither render width should match config value");
            
            Assert.That(config.Dither.Strength, Is.EqualTo(0.9f), 
                "Dither strength should match config value");
            
            Assert.That(config.Bloom.Preset, Is.EqualTo(3), 
                "Bloom preset should match config value");
            
            Assert.That(config.Tint.Enabled, Is.True, 
                "Tint should be enabled when set to true in config");
            
            Assert.That(config.Input.LockMouse, Is.False, 
                "Mouse lock should be disabled when set to false in config");
            
            Assert.That(config.Development.EnableScreenshots, Is.True, 
                "Screenshots should be enabled when set to true in config");
            
            // Test helper methods
            var renderResolution = RenderingConfigManager.GetRenderResolution();
            Assert.That(renderResolution.X, Is.EqualTo(480), 
                "Helper method should return correct render width");
            Assert.That(renderResolution.Y, Is.EqualTo(270), 
                "Helper method should return correct render height");
            
            var samplerState = RenderingConfigManager.GetSamplerState();
            Assert.That(samplerState, Is.EqualTo(Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp), 
                "Point sampling should be used when usePointSampling is true");
        }

        [Test]
        public void PostProcessingDisabled_SkipsEffectsCorrectly()
        {
            // Arrange - Create config with post-processing disabled
            var testYaml = @"
rendering:
  enablePostProcessing: false
  
dither:
  renderWidth: 320
  renderHeight: 180
  
input:
  lockMouse: true
  
development:
  enableScreenshots: false
";

            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act - Load configuration
            var config = RenderingConfigManager.Config;
            
            // Assert - Verify post-processing is disabled but other settings work
            Assert.That(config.Rendering.EnablePostProcessing, Is.False, 
                "Post-processing should be disabled when set to false in config");
            
            Assert.That(config.Input.LockMouse, Is.True, 
                "Mouse lock should be enabled when set to true in config");
                
            Assert.That(config.Development.EnableScreenshots, Is.False, 
                "Screenshots should be disabled when set to false in config");
            
            // Even when post-processing is disabled, config values should still be readable
            Assert.That(config.Dither.RenderWidth, Is.EqualTo(320), 
                "Config values should still be accessible even when post-processing is disabled");
        }

        [Test]
        public void DitherEffectConfigIntegration_WorksEndToEnd()
        {
            // Arrange - Create config with specific dither settings
            var testYaml = @"
dither:
  renderWidth: 640
  renderHeight: 360
  strength: 0.7
  colorLevels: 8.0
  usePointSampling: false

rendering:
  enablePostProcessing: true
";

            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act - Create dither effect and load from config
            var ditherEffect = new DitherEffect();
            ditherEffect.LoadFromConfig();
            
            // Assert - Verify effect properties match config
            Assert.That(ditherEffect.ScreenResolution.X, Is.EqualTo(640), 
                "DitherEffect screen width should match config render width");
                
            Assert.That(ditherEffect.ScreenResolution.Y, Is.EqualTo(360), 
                "DitherEffect screen height should match config render height");
                
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(0.7f), 
                "DitherEffect strength should match config strength");
                
            Assert.That(ditherEffect.ColorLevels, Is.EqualTo(8.0f), 
                "DitherEffect color levels should match config color levels");
            
            // Test sampler state when point sampling is disabled
            var samplerState = RenderingConfigManager.GetSamplerState();
            Assert.That(samplerState, Is.EqualTo(Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp), 
                "Linear sampling should be used when usePointSampling is false");
        }

        [Test]
        public void BloomComponentDeprecation_ShowsWarningsCorrectly()
        {
            // This test documents that BloomComponent is deprecated
            // The warnings should appear during compilation but not break functionality
            
            // We can't easily test compilation warnings in unit tests,
            // but we can verify the obsolete attribute is present
            var bloomComponentType = typeof(BloomComponent);
            var obsoleteAttribute = Attribute.GetCustomAttribute(bloomComponentType, typeof(ObsoleteAttribute)) as ObsoleteAttribute;
            
            Assert.That(obsoleteAttribute, Is.Not.Null, 
                "BloomComponent should have ObsoleteAttribute marking it as deprecated");
                
            Assert.That(obsoleteAttribute.Message, Contains.Substring("RetroRenderer"), 
                "Deprecation message should mention RetroRenderer as the replacement");
        }

        [Test]
        public void ScreenshotManager_ConfigurationIntegration()
        {
            // Arrange - Test screenshot configuration
            var testYaml = @"
development:
  enableScreenshots: true
  screenshotDirectory: ""test_screenshots""
";

            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act - Get config
            var config = RenderingConfigManager.Config.Development;
            
            // Assert - Verify screenshot settings
            Assert.That(config.EnableScreenshots, Is.True, 
                "Screenshots should be enabled when set to true in config");
                
            Assert.That(config.ScreenshotDirectory, Is.EqualTo("test_screenshots"), 
                "Screenshot directory should match config value");
        }

        [Test]
        public void MouseLockConfiguration_IntegrationTest()
        {
            // Arrange - Test mouse lock configuration
            var testYaml = @"
input:
  lockMouse: true

rendering:
  enablePostProcessing: false
";

            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act - Get config
            var config = RenderingConfigManager.Config.Input;
            
            // Assert - Verify mouse lock setting
            Assert.That(config.LockMouse, Is.True, 
                "Mouse should be locked when set to true in config");
        }

        [Test]
        public void CompleteConfigurationChain_VisualRenderingPipeline()
        {
            // This is the main integration test that verifies the complete visual rendering pipeline
            
            // Arrange - Create comprehensive config
            var testYaml = @"
# Complete configuration for visual testing
dither:
  renderWidth: 512
  renderHeight: 288
  strength: 0.8
  colorLevels: 4.0
  usePointSampling: true

bloom:
  preset: 6  # Blendo preset

tint:
  enabled: false
  color: [1.0, 1.0, 1.0, 1.0]
  intensity: 1.0

rendering:
  enablePostProcessing: true
  scaleMode: ""nearest""
  maintainAspectRatio: true

input:
  lockMouse: false

development:
  enableScreenshots: true
  screenshotDirectory: ""screenshots""
";

            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act - Test complete configuration chain
            var config = RenderingConfigManager.Config;
            var ditherEffect = new DitherEffect();
            ditherEffect.LoadFromConfig();
            var tintEffect = new TintEffect();
            var bloomEffect = new BloomEffect();
            
            // Apply tint config
            tintEffect.TintColor = config.Tint.GetColor();
            tintEffect.Intensity = config.Tint.Intensity;
            tintEffect.Enabled = config.Tint.Enabled;
            
            // Apply bloom config
            if (config.Bloom.Preset >= 0 && config.Bloom.Preset < BloomSettings.PresetSettings.Length)
            {
                bloomEffect.Settings = BloomSettings.PresetSettings[config.Bloom.Preset];
            }
            
            // Assert - Verify complete chain works
            
            // 1. Configuration loading
            Assert.That(config.Rendering.EnablePostProcessing, Is.True, 
                "Post-processing should be enabled for visual rendering");
                
            // 2. Dither effect configuration
            Assert.That(ditherEffect.ScreenResolution.X, Is.EqualTo(512), 
                "Dither effect should use configured render width");
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(0.8f), 
                "Dither effect should use configured strength");
                
            // 3. Tint effect configuration
            Assert.That(tintEffect.Enabled, Is.False, 
                "Tint effect should be disabled when config is false");
            Assert.That(tintEffect.TintColor, Is.EqualTo(Color.White), 
                "Tint color should match config values");
                
            // 4. Bloom effect configuration
            Assert.That(bloomEffect.Settings.Name, Is.EqualTo("Blendo"), 
                "Bloom effect should use the configured preset");
                
            // 5. Input configuration
            Assert.That(config.Input.LockMouse, Is.False, 
                "Mouse should be unlocked for testing");
                
            // 6. Development configuration
            Assert.That(config.Development.EnableScreenshots, Is.True, 
                "Screenshots should be enabled for visual verification");
                
            Console.WriteLine("✅ Complete visual rendering pipeline configuration verified!");
            Console.WriteLine($"   • Render Resolution: {ditherEffect.ScreenResolution.X}x{ditherEffect.ScreenResolution.Y}");
            Console.WriteLine($"   • Dither Strength: {ditherEffect.DitherStrength}");
            Console.WriteLine($"   • Color Levels: {ditherEffect.ColorLevels}");
            Console.WriteLine($"   • Bloom Preset: {bloomEffect.Settings.Name}");
            Console.WriteLine($"   • Post-processing: {(config.Rendering.EnablePostProcessing ? "Enabled" : "Disabled")}");
            Console.WriteLine($"   • Mouse Lock: {(config.Input.LockMouse ? "Enabled" : "Disabled")}");
            Console.WriteLine($"   • Screenshots: {(config.Development.EnableScreenshots ? "Enabled" : "Disabled")}");
        }
    }
}