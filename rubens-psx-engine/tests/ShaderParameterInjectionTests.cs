using System;
using System.IO;
using Microsoft.Xna.Framework;
using Moq;
using NUnit.Framework;
using rubens_psx_engine.system.config;
using rubens_psx_engine.system.postprocess;

namespace rubens_psx_engine.tests
{
    [TestFixture]
    public class ShaderParameterInjectionTests
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
        public void ConfigToShaderParameters_DitherStrength_MapsCorrectly()
        {
            // Arrange
            var testYaml = @"
dither:
  strength: 0.75
  colorLevels: 8.0
  renderWidth: 480
  renderHeight: 270
";
            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            var ditherEffect = new DitherEffect();
            ditherEffect.LoadFromConfig();
            
            // Act & Assert - verify that config values are correctly loaded into effect properties
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(0.75f), 
                "DitherStrength should match config value");
            Assert.That(ditherEffect.ColorLevels, Is.EqualTo(8.0f), 
                "ColorLevels should match config value");
            Assert.That(ditherEffect.ScreenResolution.X, Is.EqualTo(480f), 
                "ScreenResolution X should match renderWidth");
            Assert.That(ditherEffect.ScreenResolution.Y, Is.EqualTo(270f), 
                "ScreenResolution Y should match renderHeight");
        }

        [Test]
        public void ConfigChanges_PropagateToShaderParameters()
        {
            // Arrange - initial config
            var initialYaml = @"
dither:
  strength: 0.3
  colorLevels: 4.0
  renderWidth: 320
  renderHeight: 180
";
            File.WriteAllText(testConfigPath, initialYaml);
            RenderingConfigManager.ReloadConfig();
            
            var ditherEffect = new DitherEffect();
            ditherEffect.LoadFromConfig();
            
            // Verify initial values
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(0.3f));
            Assert.That(ditherEffect.ColorLevels, Is.EqualTo(4.0f));
            
            // Act - update config
            var updatedYaml = @"
dither:
  strength: 0.9
  colorLevels: 12.0
  renderWidth: 640
  renderHeight: 360
";
            File.WriteAllText(testConfigPath, updatedYaml);
            RenderingConfigManager.ReloadConfig();
            ditherEffect.LoadFromConfig();
            
            // Assert - verify updated values
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(0.9f), 
                "Updated strength should be applied");
            Assert.That(ditherEffect.ColorLevels, Is.EqualTo(12.0f), 
                "Updated color levels should be applied");
            Assert.That(ditherEffect.ScreenResolution.X, Is.EqualTo(640f), 
                "Updated render width should be applied");
            Assert.That(ditherEffect.ScreenResolution.Y, Is.EqualTo(360f), 
                "Updated render height should be applied");
        }

        [Test]
        public void TintEffect_ConfigValues_LoadCorrectly()
        {
            // Arrange
            var testYaml = @"
tint:
  enabled: true
  color: [0.8, 0.6, 0.4, 0.9]
  intensity: 0.75
";
            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            var config = RenderingConfigManager.Config.Tint;
            
            // Assert
            Assert.That(config.Enabled, Is.True);
            Assert.That(config.Intensity, Is.EqualTo(0.75f));
            
            var color = config.GetColor();
            Assert.That(color.R, Is.EqualTo((byte)(0.8f * 255)));
            Assert.That(color.G, Is.EqualTo((byte)(0.6f * 255)));
            Assert.That(color.B, Is.EqualTo((byte)(0.4f * 255)));
            Assert.That(color.A, Is.EqualTo((byte)(0.9f * 255)));
        }

        [Test]
        public void BloomEffect_ConfigPreset_MapsCorrectly()
        {
            // Arrange
            var testYaml = @"
bloom:
  preset: 3  # Saturated preset
";
            File.WriteAllText(testConfigPath, testYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act
            var config = RenderingConfigManager.Config.Bloom;
            
            // Assert
            Assert.That(config.Preset, Is.EqualTo(3));
            
            // Verify the preset maps to the correct BloomSettings
            Assert.That(config.Preset, Is.LessThan(BloomSettings.PresetSettings.Length));
            var bloomSettings = BloomSettings.PresetSettings[config.Preset];
            Assert.That(bloomSettings.Name, Is.EqualTo("Saturated"));
        }

        [Test]
        public void RenderResolution_PropagatesFromConfigToEffect()
        {
            // Test the full chain: YAML -> Config -> Effect -> Shader Parameters
            
            // Arrange
            var testYaml = @"
dither:
  renderWidth: 800
  renderHeight: 450
  strength: 0.5
  colorLevels: 10.0
";
            File.WriteAllText(testConfigPath, testYaml);
            
            // Act - simulate the full initialization chain
            RenderingConfigManager.ReloadConfig();
            var renderResolution = RenderingConfigManager.GetRenderResolution();
            
            var ditherEffect = new DitherEffect();
            ditherEffect.LoadFromConfig();
            
            // Assert - verify the chain works
            Assert.That(renderResolution.X, Is.EqualTo(800), 
                "Config manager should return correct render width");
            Assert.That(renderResolution.Y, Is.EqualTo(450), 
                "Config manager should return correct render height");
                
            Assert.That(ditherEffect.ScreenResolution.X, Is.EqualTo(800f), 
                "Effect should receive correct render width");
            Assert.That(ditherEffect.ScreenResolution.Y, Is.EqualTo(450f), 
                "Effect should receive correct render height");
        }

        [Test]
        public void SamplerState_BasedOnConfig_ReturnsCorrectState()
        {
            // Arrange - point sampling
            var pointSamplingYaml = @"
dither:
  usePointSampling: true
";
            File.WriteAllText(testConfigPath, pointSamplingYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act & Assert
            var pointState = RenderingConfigManager.GetSamplerState();
            Assert.That(pointState, Is.EqualTo(Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp));
            
            // Arrange - linear sampling
            var linearSamplingYaml = @"
dither:
  usePointSampling: false
";
            File.WriteAllText(testConfigPath, linearSamplingYaml);
            RenderingConfigManager.ReloadConfig();
            
            // Act & Assert
            var linearState = RenderingConfigManager.GetSamplerState();
            Assert.That(linearState, Is.EqualTo(Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp));
        }

        [Test]
        public void ConfigValidation_EnsuresValidShaderParameters()
        {
            // Arrange - try to set invalid values that could break shaders
            var invalidYaml = @"
dither:
  renderWidth: -100      # Negative values
  renderHeight: 0        # Zero height
  strength: 5.0          # Above 1.0
  colorLevels: 0.5       # Too low for meaningful quantization
";
            File.WriteAllText(testConfigPath, invalidYaml);
            
            // Act
            var config = RenderingConfigManager.LoadConfig();
            
            // Assert - values should be clamped to safe ranges
            Assert.That(config.Dither.RenderWidth, Is.GreaterThan(0), 
                "Width should be positive");
            Assert.That(config.Dither.RenderHeight, Is.GreaterThan(0), 
                "Height should be positive");
            Assert.That(config.Dither.Strength, Is.InRange(0f, 1f), 
                "Strength should be normalized");
            Assert.That(config.Dither.ColorLevels, Is.GreaterThanOrEqualTo(2f), 
                "Color levels should allow meaningful quantization");
        }

        [Test]
        public void RuntimeConfigReload_UpdatesShaderParametersCorrectly()
        {
            // This test simulates a runtime config reload scenario
            
            // Arrange - initial config
            var initialYaml = @"
dither:
  renderWidth: 320
  renderHeight: 180
  strength: 0.8
  colorLevels: 6.0
rendering:
  enablePostProcessing: true
";
            File.WriteAllText(testConfigPath, initialYaml);
            RenderingConfigManager.ReloadConfig();
            
            var ditherEffect = new DitherEffect();
            ditherEffect.LoadFromConfig();
            
            var initialWidth = ditherEffect.ScreenResolution.X;
            var initialStrength = ditherEffect.DitherStrength;
            
            // Act - simulate user editing config file at runtime
            var updatedYaml = @"
dither:
  renderWidth: 640
  renderHeight: 360
  strength: 0.4
  colorLevels: 8.0
rendering:
  enablePostProcessing: false
";
            File.WriteAllText(testConfigPath, updatedYaml);
            
            // Simulate runtime reload
            RenderingConfigManager.ReloadConfig();
            ditherEffect.LoadFromConfig();
            
            // Assert - verify hot-reload worked
            Assert.That(initialWidth, Is.EqualTo(320f), "Initial width should be 320");
            Assert.That(initialStrength, Is.EqualTo(0.8f), "Initial strength should be 0.8");
            
            Assert.That(ditherEffect.ScreenResolution.X, Is.EqualTo(640f), 
                "Width should update after reload");
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(0.4f), 
                "Strength should update after reload");
            Assert.That(RenderingConfigManager.Config.Rendering.EnablePostProcessing, Is.False, 
                "Post-processing flag should update after reload");
        }
    }
}