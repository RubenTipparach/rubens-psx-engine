using System;
using System.IO;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using rubens_psx_engine.system.config;

namespace rubens_psx_engine.tests
{
    [TestFixture]
    public class RenderingConfigTests
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
        public void LoadConfig_WithValidYaml_LoadsCorrectly()
        {
            // Arrange
            var testYaml = @"
dither:
  renderWidth: 480
  renderHeight: 270
  strength: 0.5
  colorLevels: 8.0
  usePointSampling: false

bloom:
  preset: 2

tint:
  enabled: true
  color: [0.8, 0.6, 0.4, 1.0]
  intensity: 0.7

rendering:
  enablePostProcessing: true
  scaleMode: linear
  maintainAspectRatio: false
";
            
            File.WriteAllText(testConfigPath, testYaml);
            
            // Act
            var config = RenderingConfigManager.LoadConfig();
            
            // Assert
            Assert.That(config.Dither.RenderWidth, Is.EqualTo(480));
            Assert.That(config.Dither.RenderHeight, Is.EqualTo(270));
            Assert.That(config.Dither.Strength, Is.EqualTo(0.5f));
            Assert.That(config.Dither.ColorLevels, Is.EqualTo(8.0f));
            Assert.That(config.Dither.UsePointSampling, Is.False);
            
            Assert.That(config.Bloom.Preset, Is.EqualTo(2));
            
            Assert.That(config.Tint.Enabled, Is.True);
            Assert.That(config.Tint.Color[0], Is.EqualTo(0.8f));
            Assert.That(config.Tint.Intensity, Is.EqualTo(0.7f));
            
            Assert.That(config.Rendering.EnablePostProcessing, Is.True);
            Assert.That(config.Rendering.ScaleMode, Is.EqualTo("linear"));
            Assert.That(config.Rendering.MaintainAspectRatio, Is.False);
        }

        [Test]
        public void LoadConfig_WithMissingFile_CreatesDefaultConfig()
        {
            // Arrange
            if (File.Exists(testConfigPath))
            {
                File.Delete(testConfigPath);
            }
            
            // Act
            var config = RenderingConfigManager.LoadConfig();
            
            // Assert - should have default values
            Assert.That(config.Dither.RenderWidth, Is.EqualTo(320));
            Assert.That(config.Dither.RenderHeight, Is.EqualTo(180));
            Assert.That(config.Dither.Strength, Is.EqualTo(0.8f));
            Assert.That(config.Dither.ColorLevels, Is.EqualTo(6.0f));
            Assert.That(config.Dither.UsePointSampling, Is.True);
            
            // Should create the file
            Assert.That(File.Exists(testConfigPath), Is.True);
        }

        [Test]
        public void LoadConfig_WithInvalidYaml_FallsBackToDefaults()
        {
            // Arrange
            var invalidYaml = @"
invalid: yaml: content
  - broken
    syntax
";
            File.WriteAllText(testConfigPath, invalidYaml);
            
            // Act
            var config = RenderingConfigManager.LoadConfig();
            
            // Assert - should fall back to defaults
            Assert.That(config.Dither.RenderWidth, Is.EqualTo(320));
            Assert.That(config.Dither.RenderHeight, Is.EqualTo(180));
        }

        [Test]
        public void GetRenderResolution_ReturnsConfiguredValues()
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
        public void GetSamplerState_WithPointSampling_ReturnsPointClamp()
        {
            // Arrange
            var testYaml = @"
dither:
  usePointSampling: true
";
            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            var samplerState = RenderingConfigManager.GetSamplerState();
            
            // Assert
            Assert.That(samplerState, Is.EqualTo(Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp));
        }

        [Test]
        public void GetSamplerState_WithLinearSampling_ReturnsLinearClamp()
        {
            // Arrange
            var testYaml = @"
dither:
  usePointSampling: false
";
            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            var samplerState = RenderingConfigManager.GetSamplerState();
            
            // Assert
            Assert.That(samplerState, Is.EqualTo(Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp));
        }

        [Test]
        public void TintConfig_GetColor_ReturnsCorrectColor()
        {
            // Arrange
            var testYaml = @"
tint:
  color: [0.9, 0.7, 0.5, 0.3]
";
            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            var color = RenderingConfigManager.Config.Tint.GetColor();
            
            // Assert
            Assert.That(color.R, Is.EqualTo((byte)(0.9f * 255)));
            Assert.That(color.G, Is.EqualTo((byte)(0.7f * 255)));
            Assert.That(color.B, Is.EqualTo((byte)(0.5f * 255)));
            Assert.That(color.A, Is.EqualTo((byte)(0.3f * 255)));
        }

        [Test]
        public void ConfigValidation_ClampsRenderResolution()
        {
            // Arrange - try to set invalid resolution
            var testYaml = @"
dither:
  renderWidth: 50      # Too low
  renderHeight: 2000   # Too high
";
            File.WriteAllText(testConfigPath, testYaml);
            
            // Act
            var config = RenderingConfigManager.LoadConfig();
            
            // Assert - should be clamped to valid range
            Assert.That(config.Dither.RenderWidth, Is.GreaterThanOrEqualTo(160));
            Assert.That(config.Dither.RenderHeight, Is.LessThanOrEqualTo(1080));
        }

        [Test]
        public void ConfigValidation_ClampsDitherStrength()
        {
            // Arrange
            var testYaml = @"
dither:
  strength: 2.5  # Above max
";
            File.WriteAllText(testConfigPath, testYaml);
            
            // Act
            var config = RenderingConfigManager.LoadConfig();
            
            // Assert
            Assert.That(config.Dither.Strength, Is.LessThanOrEqualTo(1.0f));
        }

        [Test]
        public void ConfigValidation_ClampsColorLevels()
        {
            // Arrange
            var testYaml = @"
dither:
  colorLevels: 1.0  # Too low
";
            File.WriteAllText(testConfigPath, testYaml);
            
            // Act
            var config = RenderingConfigManager.LoadConfig();
            
            // Assert
            Assert.That(config.Dither.ColorLevels, Is.GreaterThanOrEqualTo(2.0f));
        }

        [Test]
        public void ConfigValidation_ClampsBloomPreset()
        {
            // Arrange
            var testYaml = @"
bloom:
  preset: 10  # Out of range
";
            File.WriteAllText(testConfigPath, testYaml);
            
            // Act
            var config = RenderingConfigManager.LoadConfig();
            
            // Assert
            Assert.That(config.Bloom.Preset, Is.LessThanOrEqualTo(7));
        }

        [Test]
        public void ReloadConfig_UpdatesConfigInstance()
        {
            // Arrange
            var initialYaml = @"
dither:
  strength: 0.3
";
            File.WriteAllText(testConfigPath, initialYaml);
            var initialConfig = RenderingConfigManager.LoadConfig();
            var initialStrength = initialConfig.Dither.Strength;
            
            // Update config file
            var updatedYaml = @"
dither:
  strength: 0.9
";
            File.WriteAllText(testConfigPath, updatedYaml);
            
            // Act
            RenderingConfigManager.ReloadConfig();
            var updatedConfig = RenderingConfigManager.Config;
            
            // Assert
            Assert.That(initialStrength, Is.EqualTo(0.3f));
            Assert.That(updatedConfig.Dither.Strength, Is.EqualTo(0.9f));
        }
    }
}