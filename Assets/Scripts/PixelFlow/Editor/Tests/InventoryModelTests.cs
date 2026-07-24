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

        // === Gem (sert para) — game_plan.md §9.1 ===

        [Test]
        public void DefaultGems_IsZero()
        {
            Assert.AreEqual(0, _inventory.Gems);
        }

        [Test]
        public void AddGems_IncreasesBalanceAndFiresEvent()
        {
            int eventFiredValue = -1;
            _inventory.OnGemsChanged += gems => eventFiredValue = gems;

            _inventory.AddGems(5);

            Assert.AreEqual(5, _inventory.Gems);
            Assert.AreEqual(5, eventFiredValue);
        }

        [Test]
        public void AddGems_NonPositive_NoChange()
        {
            _inventory.AddGems(3);
            _inventory.AddGems(0);
            _inventory.AddGems(-10);
            Assert.AreEqual(3, _inventory.Gems);
        }

        [Test]
        public void TrySpendGems_SufficientFunds_DeductsAndReturnsTrue()
        {
            _inventory.AddGems(10);

            bool success = _inventory.TrySpendGems(4);

            Assert.IsTrue(success);
            Assert.AreEqual(6, _inventory.Gems);
        }

        [Test]
        public void TrySpendGems_InsufficientFunds_ReturnsFalseAndUnchanged()
        {
            _inventory.AddGems(2);

            bool success = _inventory.TrySpendGems(5);

            Assert.IsFalse(success);
            Assert.AreEqual(2, _inventory.Gems);
        }

        // === Etkinlik Bileti (Ticket) — game_plan.md §9.1 ===

        [Test]
        public void DefaultTickets_IsZero()
        {
            Assert.AreEqual(0, _inventory.Tickets);
        }

        [Test]
        public void AddTickets_IncreasesBalanceAndFiresEvent()
        {
            int eventFiredValue = -1;
            _inventory.OnTicketsChanged += t => eventFiredValue = t;

            _inventory.AddTickets(3);

            Assert.AreEqual(3, _inventory.Tickets);
            Assert.AreEqual(3, eventFiredValue);
        }

        [Test]
        public void TrySpendTickets_InsufficientFunds_ReturnsFalseAndUnchanged()
        {
            _inventory.AddTickets(1);

            bool success = _inventory.TrySpendTickets(2);

            Assert.IsFalse(success);
            Assert.AreEqual(1, _inventory.Tickets);
        }

        // === Star Pass (premium track) — game_plan.md §9.3 ===

        [Test]
        public void StarPass_DefaultInactive()
        {
            Assert.IsFalse(_inventory.IsStarPassActive);
        }

        [Test]
        public void SetStarPassActive_TogglesAndFiresEvent()
        {
            bool? eventValue = null;
            _inventory.OnStarPassChanged += active => eventValue = active;

            _inventory.SetStarPassActive(true);

            Assert.IsTrue(_inventory.IsStarPassActive);
            Assert.AreEqual(true, eventValue);
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
