using System;
using System.IO;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using rubens_psx_engine.system.config;
using rubens_psx_engine.system.postprocess;

namespace rubens_psx_engine.tests
{
    [TestFixture]
    public class DitherEffectConfigTests
    {
        private string testConfigPath;
        private string originalConfigContent;
        private DitherEffect ditherEffect;

        [SetUp]
        public void SetUp()
        {
            testConfigPath = "config.yml";
            
            // Backup original config if it exists
            if (File.Exists(testConfigPath))
            {
                originalConfigContent = File.ReadAllText(testConfigPath);
            }
            
            ditherEffect = new DitherEffect();
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
            ditherEffect?.Dispose();
        }

        [Test]
        public void LoadFromConfig_WithCustomDitherSettings_UpdatesProperties()
        {
            // Arrange
            var testYaml = @"
dither:
  renderWidth: 640
  renderHeight: 360
  strength: 0.6
  colorLevels: 8.0
  usePointSampling: false
";
            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            ditherEffect.LoadFromConfig();
            
            // Assert
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(0.6f));
            Assert.That(ditherEffect.ColorLevels, Is.EqualTo(8.0f));
            Assert.That(ditherEffect.ScreenResolution.X, Is.EqualTo(640));
            Assert.That(ditherEffect.ScreenResolution.Y, Is.EqualTo(360));
        }

        [Test]
        public void LoadFromConfig_WithDefaultSettings_SetsCorrectValues()
        {
            // Arrange - use default config values
            var testYaml = @"
dither:
  renderWidth: 320
  renderHeight: 180
  strength: 0.8
  colorLevels: 6.0
";
            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            ditherEffect.LoadFromConfig();
            
            // Assert
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(0.8f));
            Assert.That(ditherEffect.ColorLevels, Is.EqualTo(6.0f));
            Assert.That(ditherEffect.ScreenResolution.X, Is.EqualTo(320));
            Assert.That(ditherEffect.ScreenResolution.Y, Is.EqualTo(180));
        }

        [Test]
        public void LoadFromConfig_WithExtremeValues_LoadsProperly()
        {
            // Arrange
            var testYaml = @"
dither:
  renderWidth: 160
  renderHeight: 90
  strength: 1.0
  colorLevels: 2.0
";
            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            ditherEffect.LoadFromConfig();
            
            // Assert
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(1.0f));
            Assert.That(ditherEffect.ColorLevels, Is.EqualTo(2.0f));
            Assert.That(ditherEffect.ScreenResolution.X, Is.EqualTo(160));
            Assert.That(ditherEffect.ScreenResolution.Y, Is.EqualTo(90));
        }

        [Test]
        public void LoadFromConfig_CalledMultipleTimes_UpdatesValues()
        {
            // Arrange - initial config
            var initialYaml = @"
dither:
  strength: 0.2
  colorLevels: 4.0
";
            File.WriteAllText(testConfigPath, initialYaml);
            RenderingConfigManager.ReloadConfig();
            ditherEffect.LoadFromConfig();
            
            var initialStrength = ditherEffect.DitherStrength;
            var initialLevels = ditherEffect.ColorLevels;
            
            // Update config
            var updatedYaml = @"
dither:
  strength: 0.9
  colorLevels: 12.0
";
            File.WriteAllText(testConfigPath, updatedYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            ditherEffect.LoadFromConfig();
            
            // Assert
            Assert.That(initialStrength, Is.EqualTo(0.2f));
            Assert.That(initialLevels, Is.EqualTo(4.0f));
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(0.9f));
            Assert.That(ditherEffect.ColorLevels, Is.EqualTo(12.0f));
        }

        [Test]
        public void DitherEffect_PropertiesMatchConfigAfterLoad()
        {
            // Arrange
            var testYaml = @"
dither:
  renderWidth: 400
  renderHeight: 225
  strength: 0.75
  colorLevels: 5.0
";
            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            ditherEffect.LoadFromConfig();
            
            // Get config for comparison
            var config = RenderingConfigManager.Config.Dither;
            
            // Assert that effect properties match config
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(config.Strength));
            Assert.That(ditherEffect.ColorLevels, Is.EqualTo(config.ColorLevels));
            Assert.That(ditherEffect.ScreenResolution.X, Is.EqualTo(config.RenderWidth));
            Assert.That(ditherEffect.ScreenResolution.Y, Is.EqualTo(config.RenderHeight));
        }

        [Test]
        public void DitherEffect_Name_IsCorrect()
        {
            Assert.That(ditherEffect.Name, Is.EqualTo("Dither"));
        }

        [Test]
        public void DitherEffect_Priority_IsCorrect()
        {
            Assert.That(ditherEffect.Priority, Is.EqualTo(200));
        }

        [Test]
        public void DitherEffect_EnabledByDefault()
        {
            Assert.That(ditherEffect.Enabled, Is.True);
        }

        [Test]
        public void DitherEffect_DefaultValues_AreReasonable()
        {
            // Test that default values before loading config are reasonable
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(1.0f));
            Assert.That(ditherEffect.ColorLevels, Is.EqualTo(6.0f));
            Assert.That(ditherEffect.ScreenResolution, Is.EqualTo(Vector2.One));
        }

        [Test]
        public void LoadFromConfig_WithMissingConfigFile_DoesNotThrow()
        {
            // Arrange - delete config file
            if (File.Exists(testConfigPath))
            {
                File.Delete(testConfigPath);
            }
            
            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => ditherEffect.LoadFromConfig());
        }

        [Test]
        public void LoadFromConfig_WithPartialConfig_LoadsAvailableValues()
        {
            // Arrange - config with only some dither values
            var testYaml = @"
dither:
  strength: 0.4
  # Missing other values - should use defaults
bloom:
  preset: 3
";
            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            ditherEffect.LoadFromConfig();
            
            // Assert - should load available value and use defaults for others
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(0.4f));
            Assert.That(ditherEffect.ColorLevels, Is.EqualTo(6.0f)); // Default value
        }
    }
}