using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using static PixelFlow.Editor.Tests.GameTestContext;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class CityEconomyModelTests
    {
        private NexusTestContext _ctx;
        private ICityEconomyModel _economy;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
            _economy = _ctx.GetModel<ICityEconomyModel>();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        [Test]
        public void Coins_StartAtZero()
        {
            Assert.AreEqual(0, _economy.Coins);
        }

        [Test]
        public void AddCoins_IncreasesBalance()
        {
            _economy.AddCoins(100);
            Assert.AreEqual(100, _economy.Coins);
        }

        [Test]
        public void TrySpendCoins_Sufficient_DecreasesBalance()
        {
            _economy.AddCoins(100);
            bool result = _economy.TrySpendCoins(30);
            Assert.IsTrue(result);
            Assert.AreEqual(70, _economy.Coins);
        }

        [Test]
        public void TrySpendCoins_Insufficient_ReturnsFalse()
        {
            _economy.AddCoins(30);
            bool result = _economy.TrySpendCoins(100);
            Assert.IsFalse(result);
            Assert.AreEqual(30, _economy.Coins);
        }

        [Test]
        public void GetUpgradeCost_ReturnsPositiveValue()
        {
            int cost = _economy.GetUpgradeCost(UpgradeType.Storage);
            Assert.Greater(cost, 0);
        }

        [Test]
        public void GetUpgradeCost_IncreasesWithLevel()
        {
            int costLevel0 = _economy.GetUpgradeCost(UpgradeType.Storage);
            _economy.AddCoins(999999);
            _economy.PurchaseUpgrade(UpgradeType.Storage);
            int costLevel1 = _economy.GetUpgradeCost(UpgradeType.Storage);
            Assert.GreaterOrEqual(costLevel1, costLevel0,
                "Upgrade cost should not decrease as level increases");
        }

        [Test]
        public void GetUpgradeCost_DifferentCategories_DifferentCosts()
        {
            int storageCost = _economy.GetUpgradeCost(UpgradeType.Storage);
            int viaductCost = _economy.GetUpgradeCost(UpgradeType.Viaduct);
            Assert.AreNotEqual(storageCost, viaductCost,
                "Different upgrade types should have different base costs");
        }

        [Test]
        public void StorageLevel_DefaultsToZero()
        {
            Assert.AreEqual(0, _economy.StorageUpgradeLevel);
        }

        [Test]
        public void PurchaseUpgrade_IncreasesLevel()
        {
            _economy.AddCoins(999999);
            _economy.PurchaseUpgrade(UpgradeType.Storage);
            Assert.AreEqual(1, _economy.StorageUpgradeLevel);
        }

        [Test]
        public void PurchaseUpgrade_MaxLevel_NoCoinsSpent()
        {
            _economy.AddCoins(999999);
            for (int i = 0; i < 100; i++)
            {
                int coinsBefore = _economy.Coins;
                _economy.PurchaseUpgrade(UpgradeType.Storage);
                if (_economy.Coins == coinsBefore) break; // Can't afford further
            }
            int beforeCoins = _economy.Coins;
            _economy.PurchaseUpgrade(UpgradeType.Storage);
            Assert.AreEqual(beforeCoins, _economy.Coins,
                "Coins should not be spent when upgrade can't advance further");
        }

        [Test]
        public void PurchaseUpgrade_InsufficientCoins_NoChange()
        {
            int levelBefore = _economy.StorageUpgradeLevel;
            int coinsBefore = _economy.Coins;
            _economy.PurchaseUpgrade(UpgradeType.Storage);
            Assert.AreEqual(levelBefore, _economy.StorageUpgradeLevel);
            Assert.AreEqual(coinsBefore, _economy.Coins);
        }

        [Test]
        public void GetUpgradeCost_StorageBaseIs150()
        {
            int cost = _economy.GetUpgradeCost(UpgradeType.Storage);
            Assert.AreEqual(150, cost, "Storage base cost should be 150 at level 0");
        }

        [Test]
        public void GetUpgradeCost_RateBaseIs250()
        {
            int cost = _economy.GetUpgradeCost(UpgradeType.Rate);
            Assert.AreEqual(250, cost, "Rate base cost should be 250 at level 0");
        }
    }
}
