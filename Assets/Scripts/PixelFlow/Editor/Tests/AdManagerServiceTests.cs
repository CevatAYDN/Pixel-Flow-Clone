using NUnit.Framework;
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
            _adManager.ShowRewardedAd("2x_coins", success => rewardGranted = success);

            Assert.IsTrue(rewardGranted);
        }
    }
}
