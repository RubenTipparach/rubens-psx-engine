using NUnit.Framework;
using System.Linq;

namespace rubens_psx_engine.tests
{
    [TestFixture]
    public class BloomSettingsTests
    {
        [Test]
        public void Constructor_SetsAllProperties()
        {
            var settings = new BloomSettings("Test", 0.5f, 2.0f, 1.5f, 0.8f, 1.2f, 0.9f);
            
            Assert.That(settings.Name, Is.EqualTo("Test"));
            Assert.That(settings.BloomThreshold, Is.EqualTo(0.5f));
            Assert.That(settings.BlurAmount, Is.EqualTo(2.0f));
            Assert.That(settings.BloomIntensity, Is.EqualTo(1.5f));
            Assert.That(settings.BaseIntensity, Is.EqualTo(0.8f));
            Assert.That(settings.BloomSaturation, Is.EqualTo(1.2f));
            Assert.That(settings.BaseSaturation, Is.EqualTo(0.9f));
        }

        [Test]
        public void PresetSettings_ContainsExpectedPresets()
        {
            var presets = BloomSettings.PresetSettings;
            
            Assert.That(presets, Is.Not.Null);
            Assert.That(presets.Length, Is.GreaterThan(0));
            
            // Check for known presets
            var defaultPreset = presets.FirstOrDefault(p => p.Name == "Default");
            Assert.That(defaultPreset, Is.Not.Null);
            
            var softPreset = presets.FirstOrDefault(p => p.Name == "Soft");
            Assert.That(softPreset, Is.Not.Null);
            
            var blendoPreset = presets.FirstOrDefault(p => p.Name == "Blendo");
            Assert.That(blendoPreset, Is.Not.Null);
        }

        [Test]
        public void PresetSettings_DefaultPreset_HasExpectedValues()
        {
            var defaultPreset = BloomSettings.PresetSettings.First(p => p.Name == "Default");
            
            Assert.That(defaultPreset.BloomThreshold, Is.EqualTo(0.25f));
            Assert.That(defaultPreset.BlurAmount, Is.EqualTo(4));
            Assert.That(defaultPreset.BloomIntensity, Is.EqualTo(1.25f));
            Assert.That(defaultPreset.BaseIntensity, Is.EqualTo(1));
            Assert.That(defaultPreset.BloomSaturation, Is.EqualTo(1));
            Assert.That(defaultPreset.BaseSaturation, Is.EqualTo(1));
        }

        [Test]
        public void PresetSettings_SoftPreset_HasExpectedValues()
        {
            var softPreset = BloomSettings.PresetSettings.First(p => p.Name == "Soft");
            
            Assert.That(softPreset.BloomThreshold, Is.EqualTo(0));
            Assert.That(softPreset.BlurAmount, Is.EqualTo(3));
            Assert.That(softPreset.BloomIntensity, Is.EqualTo(1));
            Assert.That(softPreset.BaseIntensity, Is.EqualTo(1));
        }

        [Test]
        public void PresetSettings_BlendoPreset_HasExpectedValues()
        {
            var blendoPreset = BloomSettings.PresetSettings.First(p => p.Name == "Blendo");
            
            Assert.That(blendoPreset.BloomThreshold, Is.EqualTo(0.3f));
            Assert.That(blendoPreset.BlurAmount, Is.EqualTo(6));
            Assert.That(blendoPreset.BloomIntensity, Is.EqualTo(1));
            Assert.That(blendoPreset.BaseIntensity, Is.EqualTo(1));
        }

        [Test]
        public void PresetSettings_OffPreset_HasLowIntensity()
        {
            var offPreset = BloomSettings.PresetSettings.First(p => p.Name == "off");
            
            Assert.That(offPreset.BloomIntensity, Is.EqualTo(0.4f));
        }

        [TestCase("Default")]
        [TestCase("Soft")]
        [TestCase("Desaturated")]
        [TestCase("Saturated")]
        [TestCase("Blurry")]
        [TestCase("Subtle")]
        [TestCase("Blendo")]
        [TestCase("off")]
        public void PresetSettings_ContainsExpectedPreset(string presetName)
        {
            var preset = BloomSettings.PresetSettings.FirstOrDefault(p => p.Name == presetName);
            Assert.That(preset, Is.Not.Null, $"Preset '{presetName}' not found");
        }

        [Test]
        public void Name_IsReadOnlyField()
        {
            var settings = new BloomSettings("Test", 0.5f, 2.0f, 1.5f, 0.8f, 1.2f, 0.9f);
            
            // Verify Name field is readonly
            var nameField = typeof(BloomSettings).GetField("Name");
            Assert.That(nameField, Is.Not.Null);
            Assert.That(nameField.IsInitOnly, Is.True); // readonly fields are InitOnly
            Assert.That(settings.Name, Is.EqualTo("Test"));
        }

        [Test]
        public void AllParameters_AreReadOnlyFields()
        {
            var fields = typeof(BloomSettings).GetFields()
                .Where(f => f.Name != "PresetSettings" && !f.IsStatic);
            
            foreach (var field in fields)
            {
                Assert.That(field.IsInitOnly, Is.True, $"Field '{field.Name}' should be readonly");
            }
        }
    }
}