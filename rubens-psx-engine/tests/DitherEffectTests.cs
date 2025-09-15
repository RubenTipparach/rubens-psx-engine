using System;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using rubens_psx_engine.system.postprocess;

namespace rubens_psx_engine.tests
{
    [TestFixture]
    public class DitherEffectTests
    {
        private DitherEffect ditherEffect;

        [SetUp]
        public void SetUp()
        {
            ditherEffect = new DitherEffect();
        }

        [Test]
        public void Name_ReturnsDither()
        {
            Assert.That(ditherEffect.Name, Is.EqualTo("Dither"));
        }

        [Test]
        public void Priority_ReturnsExpectedValue()
        {
            Assert.That(ditherEffect.Priority, Is.EqualTo(200));
        }

        [Test]
        public void Enabled_DefaultsToTrue()
        {
            Assert.That(ditherEffect.Enabled, Is.True);
        }

        [Test]
        public void DitherStrength_DefaultsToOne()
        {
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(1.0f));
        }

        [Test]
        public void ScreenResolution_DefaultsToOne()
        {
            Assert.That(ditherEffect.ScreenResolution, Is.EqualTo(Vector2.One));
        }

        [Test]
        public void DitherStrength_CanBeChanged()
        {
            var newStrength = 0.75f;
            ditherEffect.DitherStrength = newStrength;
            
            Assert.That(ditherEffect.DitherStrength, Is.EqualTo(newStrength));
        }

        [Test]
        public void ScreenResolution_CanBeChanged()
        {
            var newResolution = new Vector2(1280, 720);
            ditherEffect.ScreenResolution = newResolution;
            
            Assert.That(ditherEffect.ScreenResolution, Is.EqualTo(newResolution));
        }

        [Test]
        public void Initialize_WithNullGraphicsDevice_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                ditherEffect.Initialize(null, null));
        }

        [Test]
        public void Initialize_WithNullGame_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                ditherEffect.Initialize(null, null));
        }

        [Test]
        public void Apply_WithNullInputTexture_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                ditherEffect.Apply(null, null, null));
        }

        [Test]
        public void Apply_WithNullSpriteBatch_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                ditherEffect.Apply(null, null, null));
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => ditherEffect.Dispose());
        }
    }
}