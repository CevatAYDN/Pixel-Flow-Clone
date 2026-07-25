using NUnit.Framework;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Services;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class AdManagerServiceTests
    {
        private AdManagerService _adManager;

        [SetUp]
        public void SetUp()
        {
            _adManager = new AdManagerService();
            _adManager.Keys = ScriptableObject.CreateInstance<StorageKeysConfigAsset>();
            _adManager.Keys.CurrencyIdCoin = "coins";
            _adManager.Keys.CurrencyIdGem = "gems";
        }

        [Test]
        public void IsRewardedAdReady_ReturnsTrue()
        {
            Assert.IsTrue(_adManager.IsRewardedAdReady());
        }

        [Test]
        public void ShowRewardedAd_TriggersCompletionCallback()
        {
            bool rewardGranted = false;
            _adManager.ShowRewardedAd("double_coins", success => rewardGranted = success);

            Assert.IsTrue(rewardGranted);
        }
    }
}
