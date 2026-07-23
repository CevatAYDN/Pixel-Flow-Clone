using NUnit.Framework;
using PixelFlow.Data;
using PixelFlow.Models;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class InventoryModelTests
    {
        private InventoryModel _inventory;

        [SetUp]
        public void SetUp()
        {
            _inventory = new InventoryModel();
        }

        [Test]
        public void DefaultCoins_IsZero()
        {
            Assert.AreEqual(0, _inventory.Coins);
        }

        [Test]
        public void AddCoins_IncreasesCoinBalanceAndFiresEvent()
        {
            int eventFiredValue = -1;
            _inventory.OnCoinsChanged += coins => eventFiredValue = coins;

            _inventory.AddCoins(150);

            Assert.AreEqual(150, _inventory.Coins);
            Assert.AreEqual(150, eventFiredValue);
        }

        [Test]
        public void TrySpendCoins_SufficientFunds_DeductsCoinsAndReturnsTrue()
        {
            _inventory.AddCoins(200);

            bool success = _inventory.TrySpendCoins(120);

            Assert.IsTrue(success);
            Assert.AreEqual(80, _inventory.Coins);
        }

        [Test]
        public void TrySpendCoins_InsufficientFunds_ReturnsFalseAndCoinsUnchanged()
        {
            _inventory.AddCoins(50);

            bool success = _inventory.TrySpendCoins(100);

            Assert.IsFalse(success);
            Assert.AreEqual(50, _inventory.Coins);
        }

        [Test]
        public void DefaultSkin_IsUnlockedByDefault()
        {
            Assert.IsTrue(_inventory.IsSkinUnlocked("skin_default"));
        }

        [Test]
        public void UnlockSkin_NewSkin_UnlocksAndFiresEvent()
        {
            string unlockedId = null;
            _inventory.OnSkinUnlocked += id => unlockedId = id;

            _inventory.UnlockSkin("skin_ice_cream_truck");

            Assert.IsTrue(_inventory.IsSkinUnlocked("skin_ice_cream_truck"));
            Assert.AreEqual("skin_ice_cream_truck", unlockedId);
        }

        [Test]
        public void EquipSkin_UnlockedSkin_EquipsSuccessfully()
        {
            _inventory.UnlockSkin("skin_police");

            ColorType targetColor = ColorType.Red;
            _inventory.EquipSkin(targetColor, "skin_police");

            Assert.AreEqual("skin_police", _inventory.GetEquippedSkin(targetColor));
        }

        [Test]
        public void EquipSkin_LockedSkin_DoesNotEquip()
        {
            ColorType targetColor = ColorType.Blue;
            _inventory.EquipSkin(targetColor, "skin_locked_monster_truck");

            Assert.AreEqual("skin_default", _inventory.GetEquippedSkin(targetColor));
        }
    }
}
