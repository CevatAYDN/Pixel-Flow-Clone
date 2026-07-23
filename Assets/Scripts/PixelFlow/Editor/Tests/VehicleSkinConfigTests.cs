using NUnit.Framework;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class VehicleSkinConfigTests
    {
        private VehicleSkinConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<VehicleSkinConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
            {
                Object.DestroyImmediate(_config);
            }
        }

        [Test]
        public void VehicleSkinConfig_InitializesWithValidDefaults()
        {
            Assert.IsNotNull(_config.SkinId);
            Assert.IsNotNull(_config.DisplayName);
            Assert.GreaterOrEqual(_config.UnlockCoinCost, 0);
        }

        [Test]
        public void VehicleSkinConfig_CanModifyProperties()
        {
            _config.SkinId = "skin_monster";
            _config.DisplayName = "Canavar Kamyon";
            _config.ColorFamily = ColorType.Purple;
            _config.UnlockCoinCost = 500;
            _config.RequiresRewardedAd = true;

            Assert.AreEqual("skin_monster", _config.SkinId);
            Assert.AreEqual("Canavar Kamyon", _config.DisplayName);
            Assert.AreEqual(ColorType.Purple, _config.ColorFamily);
            Assert.AreEqual(500, _config.UnlockCoinCost);
            Assert.IsTrue(_config.RequiresRewardedAd);
        }
    }
}
