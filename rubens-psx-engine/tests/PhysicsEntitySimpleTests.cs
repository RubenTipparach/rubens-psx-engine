using System;
using Microsoft.Xna.Framework;
using BepuPhysics.Collidables;
using NUnit.Framework;

namespace rubens_psx_engine.tests
{
    [TestFixture]
    public class PhysicsEntitySimpleTests
    {
        [Test]
        public void Box_CanBeCreatedWithValidDimensions()
        {
            var box = new Box(1f, 2f, 3f);
            
            Assert.That(box.Width, Is.EqualTo(1f));
            Assert.That(box.Height, Is.EqualTo(2f));
            Assert.That(box.Length, Is.EqualTo(3f));
        }

        [Test]
        public void Sphere_CanBeCreatedWithValidRadius()
        {
            var sphere = new Sphere(2.5f);
            
            Assert.That(sphere.Radius, Is.EqualTo(2.5f));
        }

        [Test]
        public void Capsule_CanBeCreatedWithValidParameters()
        {
            var capsule = new Capsule(1.5f, 4f);
            
            Assert.That(capsule.Radius, Is.EqualTo(1.5f));
            Assert.That(capsule.Length, Is.EqualTo(4f));
        }

        [Test]
        public void Vector3_CanBeUsedForPositions()
        {
            var position = new Vector3(10, 20, 30);
            
            Assert.That(position.X, Is.EqualTo(10));
            Assert.That(position.Y, Is.EqualTo(20));
            Assert.That(position.Z, Is.EqualTo(30));
        }

        [Test]
        public void Quaternion_CanBeUsedForRotations()
        {
            var rotation = Quaternion.CreateFromYawPitchRoll(1f, 0.5f, 0.25f);
            
            Assert.That(rotation, Is.Not.EqualTo(Quaternion.Identity));
        }

        [Test] 
        public void SystemNumericsVector3_CanBeUsedForForces()
        {
            var force = new System.Numerics.Vector3(100, 0, 0);
            
            Assert.That(force.X, Is.EqualTo(100));
            Assert.That(force.Y, Is.EqualTo(0));
            Assert.That(force.Z, Is.EqualTo(0));
        }
    }
}