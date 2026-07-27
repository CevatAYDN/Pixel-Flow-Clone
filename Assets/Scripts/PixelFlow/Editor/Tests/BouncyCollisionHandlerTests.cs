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
            var physicsAsset = ScriptableObject.CreateInstance<BouncyPhysicsConfigAsset>();
            physicsAsset.BounceForce = 4.5f;
            physicsAsset.BounceDamping = 0.75f;
            physicsAsset.SquishFactor = 0.35f;

            BouncyCollisionHandler.ApplyBouncyBounce(_testVehicle, Vector3.up, physicsAsset);

            var bouncyComp = _testVehicle.GetComponent<BouncyVisualEffect>();
            Assert.IsNotNull(bouncyComp);
        }
    }
}
