using System;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using rubens_psx_engine.system.postprocess;

namespace rubens_psx_engine.tests
{
    [TestFixture]
    public class TintEffectTests
    {
        private TintEffect tintEffect;

        [SetUp]
        public void SetUp()
        {
            tintEffect = new TintEffect();
        }

        [Test]
        public void Name_ReturnsTint()
        {
            Assert.That(tintEffect.Name, Is.EqualTo("Tint"));
        }

        [Test]
        public void Priority_ReturnsExpectedValue()
        {
            Assert.That(tintEffect.Priority, Is.EqualTo(50));
        }

        [Test]
        public void Enabled_DefaultsToTrue()
        {
            Assert.That(tintEffect.Enabled, Is.True);
        }

        [Test]
        public void TintColor_DefaultsToWhite()
        {
            Assert.That(tintEffect.TintColor, Is.EqualTo(Color.White));
        }

        [Test]
        public void Intensity_DefaultsToOne()
        {
            Assert.That(tintEffect.Intensity, Is.EqualTo(1.0f));
        }

        [Test]
        public void TintColor_CanBeChanged()
        {
            var newColor = Color.Red;
            tintEffect.TintColor = newColor;
            
            Assert.That(tintEffect.TintColor, Is.EqualTo(newColor));
        }

        [Test]
        public void Intensity_CanBeChanged()
        {
            var newIntensity = 0.5f;
            tintEffect.Intensity = newIntensity;
            
            Assert.That(tintEffect.Intensity, Is.EqualTo(newIntensity));
        }

        [Test]
        public void Initialize_WithNullGame_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                tintEffect.Initialize(null, null));
        }

        [Test]
        public void Apply_WithNullInputTexture_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                tintEffect.Apply(null, null, null));
        }

        [Test]
        public void Apply_WithNullSpriteBatch_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                tintEffect.Apply(null, null, null));
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => tintEffect.Dispose());
        }
    }
}