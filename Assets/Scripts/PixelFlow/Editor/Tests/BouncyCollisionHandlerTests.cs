using NUnit.Framework;
using PixelFlow.Data;
using PixelFlow.Services;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class BouncyCollisionHandlerTests
    {
        private GameObject _testVehicle;

        [SetUp]
        public void SetUp()
        {
            _testVehicle = new GameObject("TestVehicle");
        }

        [TearDown]
        public void TearDown()
        {
            if (_testVehicle != null)
            {
                Object.DestroyImmediate(_testVehicle);
            }
        }

        [Test]
        public void ApplyBouncyBounce_AttachesBouncyVisualEffectComponent()
        {
            BouncyCollisionHandler.ApplyBouncyBounce(_testVehicle, Vector3.up, BouncyPhysicsConfig.Default);

            var bouncyComp = _testVehicle.GetComponent<BouncyVisualEffect>();
            Assert.IsNotNull(bouncyComp);
        }
    }
}
