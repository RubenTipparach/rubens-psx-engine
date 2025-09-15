using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using rubens_psx_engine.system.postprocess;

namespace rubens_psx_engine.tests
{
    [TestFixture]
    public class PostProcessStackSimpleTests
    {
        private Mock<IPostProcessEffect> mockEffect1;
        private Mock<IPostProcessEffect> mockEffect2;

        [SetUp]
        public void SetUp()
        {
            mockEffect1 = new Mock<IPostProcessEffect>();
            mockEffect1.SetupGet(e => e.Name).Returns("Effect1");
            mockEffect1.SetupGet(e => e.Priority).Returns(10);
            mockEffect1.SetupGet(e => e.Enabled).Returns(true);
            
            mockEffect2 = new Mock<IPostProcessEffect>();
            mockEffect2.SetupGet(e => e.Name).Returns("Effect2");
            mockEffect2.SetupGet(e => e.Priority).Returns(20);
            mockEffect2.SetupGet(e => e.Enabled).Returns(true);
        }

        [Test]
        public void MockEffect_CanBeCreatedAndConfigured()
        {
            Assert.That(mockEffect1.Object.Name, Is.EqualTo("Effect1"));
            Assert.That(mockEffect1.Object.Priority, Is.EqualTo(10));
            Assert.That(mockEffect1.Object.Enabled, Is.True);
        }

        [Test]
        public void BloomEffect_CanBeInstantiated()
        {
            var bloomEffect = new BloomEffect();
            Assert.That(bloomEffect.Name, Is.EqualTo("Bloom"));
            Assert.That(bloomEffect.Priority, Is.EqualTo(100));
            Assert.That(bloomEffect.Enabled, Is.True);
        }

        [Test]
        public void TintEffect_CanBeInstantiated()
        {
            var tintEffect = new TintEffect();
            Assert.That(tintEffect.Name, Is.EqualTo("Tint"));
            Assert.That(tintEffect.Priority, Is.EqualTo(50));
            Assert.That(tintEffect.Enabled, Is.True);
        }

        [Test]
        public void DitherEffect_CanBeInstantiated()
        {
            var ditherEffect = new DitherEffect();
            Assert.That(ditherEffect.Name, Is.EqualTo("Dither"));
            Assert.That(ditherEffect.Priority, Is.EqualTo(200));
            Assert.That(ditherEffect.Enabled, Is.True);
        }
    }
}