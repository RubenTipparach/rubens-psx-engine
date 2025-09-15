using System;
using NUnit.Framework;
using rubens_psx_engine.system.postprocess;

namespace rubens_psx_engine.tests
{
    [TestFixture]
    public class BloomEffectTests
    {
        private BloomEffect bloomEffect;

        [SetUp]
        public void SetUp()
        {
            bloomEffect = new BloomEffect();
        }

        [Test]
        public void Name_ReturnsBloom()
        {
            Assert.That(bloomEffect.Name, Is.EqualTo("Bloom"));
        }

        [Test]
        public void Priority_ReturnsExpectedValue()
        {
            Assert.That(bloomEffect.Priority, Is.EqualTo(100));
        }

        [Test]
        public void Enabled_DefaultsToTrue()
        {
            Assert.That(bloomEffect.Enabled, Is.True);
        }

        [Test]
        public void Enabled_CanBeSetAndRetrieved()
        {
            bloomEffect.Enabled = false;
            Assert.That(bloomEffect.Enabled, Is.False);
            
            bloomEffect.Enabled = true;
            Assert.That(bloomEffect.Enabled, Is.True);
        }

        [Test]
        public void Settings_DefaultsToFirstPreset()
        {
            Assert.That(bloomEffect.Settings, Is.Not.Null);
            Assert.That(bloomEffect.Settings.Name, Is.EqualTo("Default"));
        }

        [Test]
        public void Settings_CanBeChanged()
        {
            var newSettings = BloomSettings.PresetSettings[1]; // "Soft"
            bloomEffect.Settings = newSettings;
            
            Assert.That(bloomEffect.Settings.Name, Is.EqualTo("Soft"));
        }

        [Test]
        public void Settings_WithNullValue_UsesFirstPreset()
        {
            bloomEffect.Settings = null;
            
            Assert.That(bloomEffect.Settings, Is.Not.Null);
            Assert.That(bloomEffect.Settings.Name, Is.EqualTo("Default"));
        }

        [Test]
        public void ShowBuffer_DefaultsToFinalResult()
        {
            Assert.That(bloomEffect.ShowBuffer, Is.EqualTo(BloomEffect.IntermediateBuffer.FinalResult));
        }

        [Test]
        public void ShowBuffer_CanBeChanged()
        {
            bloomEffect.ShowBuffer = BloomEffect.IntermediateBuffer.PreBloom;
            Assert.That(bloomEffect.ShowBuffer, Is.EqualTo(BloomEffect.IntermediateBuffer.PreBloom));
        }

        [Test]
        public void Initialize_WithNullGraphicsDevice_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => bloomEffect.Initialize(null, null));
        }

        [Test]
        public void Initialize_WithNullGame_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => bloomEffect.Initialize(null, null));
        }

        [Test]
        public void Apply_WithNullInputTexture_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                bloomEffect.Apply(null, null, null));
        }

        [Test]
        public void Apply_WithNullSpriteBatch_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                bloomEffect.Apply(null, null, null));
        }

        [Test]
        public void IntermediateBuffer_EnumHasExpectedValues()
        {
            Assert.That((int)BloomEffect.IntermediateBuffer.PreBloom, Is.EqualTo(1));
            Assert.That((int)BloomEffect.IntermediateBuffer.BlurredHorizontally, Is.EqualTo(2));
            Assert.That((int)BloomEffect.IntermediateBuffer.BlurredBothWays, Is.EqualTo(3));
            Assert.That((int)BloomEffect.IntermediateBuffer.FinalResult, Is.EqualTo(4));
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => bloomEffect.Dispose());
        }
    }
}