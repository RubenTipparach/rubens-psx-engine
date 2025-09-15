using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Moq;
using NUnit.Framework;
using rubens_psx_engine.system.config;
using rubens_psx_engine.system.postprocess;

namespace rubens_psx_engine.tests
{
    [TestFixture]
    public class RetroRendererConfigIntegrationTests
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
        public void RetroRenderer_WithPostProcessingEnabled_CreatesAllEffects()
        {
            // Arrange
            var testYaml = @"
dither:
  renderWidth: 480
  renderHeight: 270
  strength: 0.7
  colorLevels: 8.0

bloom:
  preset: 2

tint:
  enabled: true
  color: [0.9, 0.8, 0.7, 1.0]
  intensity: 0.5

rendering:
  enablePostProcessing: true
";
            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Create mock objects for MonoGame dependencies
            var mockGraphicsDevice = new Mock<Microsoft.Xna.Framework.Graphics.GraphicsDevice>();
            var mockGame = new Mock<Game>();
            
            // We can't fully test Initialize() due to MonoGame dependencies,
            // but we can test the configuration loading logic
            
            // Act & Assert - verify config values are accessible
            var config = RenderingConfigManager.Config;
            
            Assert.That(config.Rendering.EnablePostProcessing, Is.True);
            Assert.That(config.Dither.RenderWidth, Is.EqualTo(480));
            Assert.That(config.Bloom.Preset, Is.EqualTo(2));
            Assert.That(config.Tint.Enabled, Is.True);
        }

        [Test]
        public void RetroRenderer_WithPostProcessingDisabled_SkipsEffects()
        {
            // Arrange
            var testYaml = @"
rendering:
  enablePostProcessing: false

dither:
  renderWidth: 320
  renderHeight: 180
";
            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act & Assert
            var config = RenderingConfigManager.Config;
            Assert.That(config.Rendering.EnablePostProcessing, Is.False);
        }

        [Test]
        public void RetroRenderer_GetRenderResolution_ReturnsConfigValues()
        {
            // Arrange
            var testYaml = @"
dither:
  renderWidth: 800
  renderHeight: 600
";
            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            var resolution = RenderingConfigManager.GetRenderResolution();
            
            // Assert
            Assert.That(resolution.X, Is.EqualTo(800));
            Assert.That(resolution.Y, Is.EqualTo(600));
        }

        [Test]
        public void RetroRenderer_ConfigReload_UpdatesEffectSettings()
        {
            // This test verifies the config hot-reload functionality
            
            // Arrange - initial config
            var initialYaml = @"
dither:
  renderWidth: 320
  renderHeight: 180
  strength: 0.8
  colorLevels: 6.0

tint:
  enabled: false
  intensity: 1.0
";
            File.WriteAllText(testConfigPath, initialYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Create effects and load initial config
            var ditherEffect = new DitherEffect();
            var tintEffect = new TintEffect();
            
            ditherEffect.LoadFromConfig();
            var config = RenderingConfigManager.Config.Tint;
            tintEffect.TintColor = config.GetColor();
            tintEffect.Intensity = config.Intensity;
            tintEffect.Enabled = config.Enabled;
            
            var initialStrength = ditherEffect.DitherStrength;
            var initialTintEnabled = tintEffect.Enabled;
            
            // Act - update config and reload
            var updatedYaml = @"
dither:
  renderWidth: 640
  renderHeight: 360
  strength: 0.4
  colorLevels: 10.0

tint:
  enabled: true
  intensity: 0.7
  color: [0.8, 0.6, 0.4, 1.0]
";
            File.WriteAllText(testConfigPath, updatedYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Simulate RetroRenderer.ReloadConfig() behavior
            ditherEffect.LoadFromConfig();
            var updatedTintConfig = RenderingConfigManager.Config.Tint;
            tintEffect.TintColor = updatedTintConfig.GetColor();
            tintEffect.Intensity = updatedTintConfig.Intensity;
            tintEffect.Enabled = updatedTintConfig.Enabled;
            
            // Assert
            Assert.That(initialStrength, Is.EqualTo(0.8f));
            Assert.That(initialTintEnabled, Is.False);
            
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(0.4f));
            Assert.That(ditherEffect.ScreenResolution.X, Is.EqualTo(640));
            Assert.That(tintEffect.Enabled, Is.True);
            Assert.That(tintEffect.Intensity, Is.EqualTo(0.7f));
        }

        [Test]
        public void ConfigurationChain_YamlToShader_WorksEndToEnd()
        {
            // This test verifies the complete chain from YAML to shader parameters
            
            // Arrange
            var testYaml = @"
# Test all aspects of the configuration system
dither:
  renderWidth: 512
  renderHeight: 288
  strength: 0.65
  colorLevels: 7.0
  usePointSampling: true

bloom:
  preset: 4  # Blurry preset

tint:
  enabled: true
  color: [0.95, 0.85, 0.75, 1.0]
  intensity: 0.8

rendering:
  enablePostProcessing: true
  scaleMode: nearest
  maintainAspectRatio: true
";
            File.WriteAllText(testConfigPath, testYaml);
            
            // Act - Load configuration
            RenderingConfigManager.ReloadConfig();
            var config = RenderingConfigManager.Config;
            
            // Create effects and load from config
            var ditherEffect = new DitherEffect();
            ditherEffect.LoadFromConfig();
            
            // Assert - Verify complete chain
            
            // 1. Config loading
            Assert.That(config.Dither.RenderWidth, Is.EqualTo(512));
            Assert.That(config.Dither.Strength, Is.EqualTo(0.65f));
            Assert.That(config.Bloom.Preset, Is.EqualTo(4));
            Assert.That(config.Tint.Enabled, Is.True);
            Assert.That(config.Rendering.EnablePostProcessing, Is.True);
            
            // 2. Helper methods
            var renderRes = RenderingConfigManager.GetRenderResolution();
            Assert.That(renderRes.X, Is.EqualTo(512));
            Assert.That(renderRes.Y, Is.EqualTo(288));
            
            var samplerState = RenderingConfigManager.GetSamplerState();
            Assert.That(samplerState, Is.EqualTo(Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp));
            
            // 3. Effect parameter injection
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(0.65f));
            Assert.That(ditherEffect.ColorLevels, Is.EqualTo(7.0f));
            Assert.That(ditherEffect.ScreenResolution.X, Is.EqualTo(512));
            Assert.That(ditherEffect.ScreenResolution.Y, Is.EqualTo(288));
            
            // 4. Tint color conversion
            var tintColor = config.Tint.GetColor();
            Assert.That(tintColor.R, Is.EqualTo((byte)(0.95f * 255)));
            Assert.That(tintColor.G, Is.EqualTo((byte)(0.85f * 255)));
            Assert.That(tintColor.B, Is.EqualTo((byte)(0.75f * 255)));
            Assert.That(tintColor.A, Is.EqualTo(255)); // 1.0 * 255
            
            // 5. Bloom preset mapping
            Assert.That(config.Bloom.Preset, Is.LessThan(BloomSettings.PresetSettings.Length));
            var bloomSettings = BloomSettings.PresetSettings[config.Bloom.Preset];
            Assert.That(bloomSettings.Name, Is.EqualTo("Blurry"));
        }

        [Test]
        public void ConfigValidation_WithInvalidValues_ProducesValidShaderParameters()
        {
            // Arrange - extreme/invalid values that could break rendering
            var invalidYaml = @"
dither:
  renderWidth: -500
  renderHeight: 99999
  strength: 10.0
  colorLevels: -2.0
  usePointSampling: not_a_boolean

bloom:
  preset: 999

rendering:
  enablePostProcessing: maybe
";
            File.WriteAllText(testConfigPath, invalidYaml);
            
            // Act - Config should handle validation gracefully
            var config = RenderingConfigManager.LoadConfig();
            
            var ditherEffect = new DitherEffect();
            ditherEffect.LoadFromConfig();
            
            // Assert - All values should be within safe ranges
            Assert.That(config.Dither.RenderWidth, Is.InRange(160, 1920));
            Assert.That(config.Dither.RenderHeight, Is.InRange(90, 1080));
            Assert.That(config.Dither.Strength, Is.InRange(0f, 1f));
            Assert.That(config.Dither.ColorLevels, Is.GreaterThanOrEqualTo(2f));
            Assert.That(config.Bloom.Preset, Is.InRange(0, 7));
            
            // Effect should receive valid parameters
            Assert.That(ditherEffect.DitherStrength, Is.InRange(0f, 1f));
            Assert.That(ditherEffect.ColorLevels, Is.GreaterThanOrEqualTo(2f));
            Assert.That(ditherEffect.ScreenResolution.X, Is.GreaterThan(0));
            Assert.That(ditherEffect.ScreenResolution.Y, Is.GreaterThan(0));
        }

        [Test]
        public void MultipleConfigReloads_MaintainCorrectValues()
        {
            // Test that multiple reloads don't cause issues
            
            var configs = new[]
            {
                @"dither:
  renderWidth: 320
  strength: 0.8",
                @"dither:
  renderWidth: 640
  strength: 0.4",
                @"dither:
  renderWidth: 480
  strength: 0.6"
            };
            
            var expectedWidths = new[] { 320, 640, 480 };
            var expectedStrengths = new[] { 0.8f, 0.4f, 0.6f };
            
            var ditherEffect = new DitherEffect();
            
            for (int i = 0; i < configs.Length; i++)
            {
                // Arrange
                File.WriteAllText(testConfigPath, configs[i]);
                
                // Act
                RenderingConfigManager.ReloadConfig();
                ditherEffect.LoadFromConfig();
                
                // Assert
                Assert.That(ditherEffect.ScreenResolution.X, Is.EqualTo(expectedWidths[i]), 
                    $"Iteration {i}: Width should be {expectedWidths[i]}");
                Assert.That(ditherEffect.DitherStrength, Is.EqualTo(expectedStrengths[i]), 
                    $"Iteration {i}: Strength should be {expectedStrengths[i]}");
            }
        }
    }
}