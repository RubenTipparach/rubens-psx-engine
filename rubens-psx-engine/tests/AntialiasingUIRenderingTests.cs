using System;
using System.IO;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using rubens_psx_engine.system.config;
using rubens_psx_engine.system.postprocess;

namespace rubens_psx_engine.tests
{
    [TestFixture]
    public class AntialiasingUIRenderingTests
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
        public void AntialiasingConfiguration_LoadsCorrectly()
        {
            // Arrange
            var testYaml = @"
rendering:
  antialiasing:
    enabled: true
    sampleCount: 8
";

            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            var config = RenderingConfigManager.Config.Rendering.Antialiasing;
            
            // Assert
            Assert.That(config.Enabled, Is.True, 
                "Antialiasing should be enabled when set to true in config");
            Assert.That(config.SampleCount, Is.EqualTo(8), 
                "Sample count should match config value");
        }

        [Test]
        public void AntialiasingConfiguration_ValidatesSampleCount()
        {
            // Arrange - Test invalid sample count
            var testYaml = @"
rendering:
  antialiasing:
    enabled: true
    sampleCount: 7  # Invalid - not power of 2
";

            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            var config = RenderingConfigManager.Config.Rendering.Antialiasing;
            var validatedCount = config.GetValidatedSampleCount();
            
            // Assert
            Assert.That(validatedCount, Is.EqualTo(8), 
                "Invalid sample count should be rounded to nearest valid value (8)");
            
            // Test edge cases
            config.SampleCount = 0;
            Assert.That(config.GetValidatedSampleCount(), Is.EqualTo(1), 
                "Sample count 0 should default to 1");
                
            config.SampleCount = 20;
            Assert.That(config.GetValidatedSampleCount(), Is.EqualTo(16), 
                "Sample count 20 should round to 16");
        }

        [Test]
        public void UIRenderingConfiguration_LoadsCorrectly()
        {
            // Arrange
            var testYaml = @"
rendering:
  ui:
    useNativeResolution: false
    scaleFactor: 2.0
";

            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            var config = RenderingConfigManager.Config.Rendering.UI;
            
            // Assert
            Assert.That(config.UseNativeResolution, Is.False, 
                "Native resolution should be disabled when set to false");
            Assert.That(config.ScaleFactor, Is.EqualTo(2.0f), 
                "Scale factor should match config value");
        }

        [Test]
        public void UIRenderingConfiguration_ValidatesScaleFactor()
        {
            // Arrange - Test invalid scale factors
            var config = new UIRenderingConfig();
            
            // Test too small scale factor
            config.ScaleFactor = 0.1f;
            Assert.That(config.GetValidatedScaleFactor(), Is.EqualTo(0.5f), 
                "Scale factor 0.1 should be clamped to minimum 0.5");
                
            // Test too large scale factor
            config.ScaleFactor = 10.0f;
            Assert.That(config.GetValidatedScaleFactor(), Is.EqualTo(4.0f), 
                "Scale factor 10.0 should be clamped to maximum 4.0");
                
            // Test valid scale factor
            config.ScaleFactor = 1.5f;
            Assert.That(config.GetValidatedScaleFactor(), Is.EqualTo(1.5f), 
                "Valid scale factor should remain unchanged");
        }

        [Test]
        public void CompleteAntialiasingUIConfig_IntegrationTest()
        {
            // Arrange - Test complete configuration
            var testYaml = @"
# Complete antialiasing and UI configuration
dither:
  renderWidth: 640
  renderHeight: 360
  strength: 0.5
  colorLevels: 8.0
  usePointSampling: true

rendering:
  enablePostProcessing: true
  scaleMode: ""nearest""
  maintainAspectRatio: true
  
  antialiasing:
    enabled: true
    sampleCount: 4
    
  ui:
    useNativeResolution: true
    scaleFactor: 1.0

input:
  lockMouse: false

development:
  enableScreenshots: true
  screenshotDirectory: ""test_screenshots""
";

            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            var config = RenderingConfigManager.Config;
            
            // Assert - Complete configuration validation
            
            // 1. Rendering pipeline
            Assert.That(config.Rendering.EnablePostProcessing, Is.True, 
                "Post-processing should be enabled");
            Assert.That(config.Rendering.ScaleMode, Is.EqualTo("nearest"), 
                "Scale mode should be nearest");
            Assert.That(config.Rendering.MaintainAspectRatio, Is.True, 
                "Aspect ratio should be maintained");
                
            // 2. Antialiasing configuration
            Assert.That(config.Rendering.Antialiasing.Enabled, Is.True, 
                "Antialiasing should be enabled");
            Assert.That(config.Rendering.Antialiasing.SampleCount, Is.EqualTo(4), 
                "Antialiasing sample count should be 4");
            Assert.That(config.Rendering.Antialiasing.GetValidatedSampleCount(), Is.EqualTo(4), 
                "Validated sample count should be 4");
                
            // 3. UI rendering configuration
            Assert.That(config.Rendering.UI.UseNativeResolution, Is.True, 
                "UI should use native resolution");
            Assert.That(config.Rendering.UI.ScaleFactor, Is.EqualTo(1.0f), 
                "UI scale factor should be 1.0");
            Assert.That(config.Rendering.UI.GetValidatedScaleFactor(), Is.EqualTo(1.0f), 
                "Validated UI scale factor should be 1.0");
                
            // 4. Dither configuration (unchanged)
            Assert.That(config.Dither.RenderWidth, Is.EqualTo(640), 
                "Dither render width should be 640");
            Assert.That(config.Dither.Strength, Is.EqualTo(0.5f), 
                "Dither strength should be 0.5");
                
            // 5. Input/Development configuration
            Assert.That(config.Input.LockMouse, Is.False, 
                "Mouse should not be locked");
            Assert.That(config.Development.EnableScreenshots, Is.True, 
                "Screenshots should be enabled");
                
            Console.WriteLine("✅ Complete antialiasing and UI configuration verified!");
            Console.WriteLine($"   • Post-processing: {(config.Rendering.EnablePostProcessing ? "Enabled" : "Disabled")}");
            Console.WriteLine($"   • Antialiasing: {(config.Rendering.Antialiasing.Enabled ? "Enabled" : "Disabled")} ({config.Rendering.Antialiasing.SampleCount}x)");
            Console.WriteLine($"   • Native UI: {(config.Rendering.UI.UseNativeResolution ? "Enabled" : "Disabled")}");
            Console.WriteLine($"   • UI Scale: {config.Rendering.UI.ScaleFactor}x");
            Console.WriteLine($"   • Dither Resolution: {config.Dither.RenderWidth}x{config.Dither.RenderHeight}");
            Console.WriteLine($"   • Dither Strength: {config.Dither.Strength}");
        }

        [Test]
        public void AntialiasingConfiguration_DefaultValues()
        {
            // Test default values when no config is provided
            var config = new RenderingPipelineConfig();
            
            Assert.That(config.Antialiasing.Enabled, Is.False, 
                "Antialiasing should be disabled by default");
            Assert.That(config.Antialiasing.SampleCount, Is.EqualTo(4), 
                "Default sample count should be 4");
            Assert.That(config.UI.UseNativeResolution, Is.True, 
                "Native UI resolution should be enabled by default");
            Assert.That(config.UI.ScaleFactor, Is.EqualTo(1.0f), 
                "Default UI scale factor should be 1.0");
        }

        [Test]
        public void SeparateRenderingPhases_ConfigurationTest()
        {
            // This test documents the separate rendering phases for 3D world vs UI
            
            var testYaml = @"
dither:
  renderWidth: 320
  renderHeight: 180
  strength: 0.8

rendering:
  enablePostProcessing: true
  antialiasing:
    enabled: false
  ui:
    useNativeResolution: true
    scaleFactor: 1.0
";

            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            var config = RenderingConfigManager.Config;
            
            // Verify the configuration supports separate phases
            Assert.That(config.Rendering.EnablePostProcessing, Is.True, 
                "3D world should use post-processing (low-res with effects)");
            Assert.That(config.Rendering.UI.UseNativeResolution, Is.True, 
                "UI should render at native resolution (crisp and clean)");
            Assert.That(config.Dither.RenderWidth, Is.EqualTo(320), 
                "3D world should render at low resolution (320x180)");
            Assert.That(config.Rendering.UI.ScaleFactor, Is.EqualTo(1.0f), 
                "UI should render at full scale for maximum sharpness");
                
            Console.WriteLine("✅ Separate rendering phases configuration verified!");
            Console.WriteLine("   PHASE 1: 3D World Rendering");
            Console.WriteLine($"     • Resolution: {config.Dither.RenderWidth}x{config.Dither.RenderHeight}");
            Console.WriteLine($"     • Post-processing: {(config.Rendering.EnablePostProcessing ? "Yes" : "No")}");
            Console.WriteLine($"     • Antialiasing: {(config.Rendering.Antialiasing.Enabled ? "Yes" : "No")}");
            Console.WriteLine("   PHASE 2: UI Rendering");
            Console.WriteLine($"     • Native resolution: {(config.Rendering.UI.UseNativeResolution ? "Yes" : "No")}");
            Console.WriteLine($"     • Scale factor: {config.Rendering.UI.ScaleFactor}x");
            Console.WriteLine("   RESULT: Pixelated 3D world + Crisp UI overlay");
        }

        [Test]
        public void RenderingPipelineConfig_YamlSerialization()
        {
            // Test that complex nested configuration serializes/deserializes correctly
            var testYaml = @"
rendering:
  enablePostProcessing: false
  scaleMode: ""linear""
  maintainAspectRatio: false
  antialiasing:
    enabled: true
    sampleCount: 16
  ui:
    useNativeResolution: false
    scaleFactor: 0.8
";

            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            var config = RenderingConfigManager.Config.Rendering;
            
            // Test all nested properties
            Assert.That(config.EnablePostProcessing, Is.False);
            Assert.That(config.ScaleMode, Is.EqualTo("linear"));
            Assert.That(config.MaintainAspectRatio, Is.False);
            Assert.That(config.Antialiasing.Enabled, Is.True);
            Assert.That(config.Antialiasing.SampleCount, Is.EqualTo(16));
            Assert.That(config.UI.UseNativeResolution, Is.False);
            Assert.That(config.UI.ScaleFactor, Is.EqualTo(0.8f));
            
            // Test validation methods
            Assert.That(config.Antialiasing.GetValidatedSampleCount(), Is.EqualTo(16));
            Assert.That(config.UI.GetValidatedScaleFactor(), Is.EqualTo(0.8f));
        }
    }
}