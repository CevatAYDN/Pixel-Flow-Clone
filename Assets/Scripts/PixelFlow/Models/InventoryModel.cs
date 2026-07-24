using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;

namespace PixelFlow.Models
{
    public interface IInventoryModel : IReactiveModel
    {
        int Coins { get; }
        int Gems { get; }
        int Tickets { get; }
        bool IsStarPassActive { get; }
        event Action<int> OnCoinsChanged;
        event Action<int> OnGemsChanged;
        event Action<int> OnTicketsChanged;
        event Action<bool> OnStarPassChanged;
        event Action<string> OnSkinUnlocked;
        event Action<ColorType, string> OnSkinEquipped;

        void AddCoins(int amount);
        bool TrySpendCoins(int amount);
        void AddGems(int amount);
        bool TrySpendGems(int amount);
        void AddTickets(int amount);
        bool TrySpendTickets(int amount);
        void SetStarPassActive(bool active);
        bool IsSkinUnlocked(string skinId);
        void UnlockSkin(string skinId);
        void EquipSkin(ColorType colorFamily, string skinId);
        string GetEquippedSkin(ColorType colorFamily);
    }

    public class InventoryModel : IInventoryModel, IReactiveModel
    {
        [Inject, OptionalInject] public ILoggerService LoggerService { get; set; }
        [Inject, OptionalInject] public GameConfig Config { get; set; }

        private int _coins = 0;
        private int _gems = 0;
        private int _tickets = 0;
        private bool _starPassActive = false;
        private readonly HashSet<string> _unlockedSkins = new HashSet<string> { "skin_default" };
        private readonly Dictionary<ColorType, string> _equippedSkins = new Dictionary<ColorType, string>();

        public int Coins => _coins;
        public int Gems => _gems;
        public int Tickets => _tickets;
        public bool IsStarPassActive => _starPassActive;

        public event Action<int> OnCoinsChanged;
        public event Action<int> OnGemsChanged;
        public event Action<int> OnTicketsChanged;
        public event Action<bool> OnStarPassChanged;
        public event Action<string> OnSkinUnlocked;
        public event Action<ColorType, string> OnSkinEquipped;

        public InventoryModel()
        {
            // Initialize default skins for each color
            foreach (ColorType color in Enum.GetValues(typeof(ColorType)))
            {
                _equippedSkins[color] = "skin_default";
            }
        }

        public ValueTask OnBind(CancellationToken ct)
        {
            // game_plan.md §9.1: başlangıç sert para / etkinlik bilet bakiyeleri config'ten gelir.
            if (Config != null)
            {
                _gems = Config.DefaultGems;
                _tickets = Config.DefaultTickets;
            }
            return default;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            _coins += amount;
            LoggerService?.Log($"[PixelFlow.InventoryModel] Added {amount} coins. New balance: {_coins}");
            OnCoinsChanged?.Invoke(_coins);
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0 || _coins < amount)
            {
                LoggerService?.LogWarning($"[PixelFlow.InventoryModel] Spend coins failed! Requested: {amount}, Current balance: {_coins}");
                return false;
            }
            _coins -= amount;
            LoggerService?.Log($"[PixelFlow.InventoryModel] Spent {amount} coins. Remaining balance: {_coins}");
            OnCoinsChanged?.Invoke(_coins);
            return true;
        }

        // === Gem (sert para) — game_plan.md §9.1: 3★, achievement, IAP ===
        public void AddGems(int amount)
        {
            if (amount <= 0) return;
            _gems += amount;
            LoggerService?.Log($"[PixelFlow.InventoryModel] Added {amount} gems. New balance: {_gems}");
            OnGemsChanged?.Invoke(_gems);
        }

        public bool TrySpendGems(int amount)
        {
            if (amount <= 0 || _gems < amount)
            {
                LoggerService?.LogWarning($"[PixelFlow.InventoryModel] Spend gems failed! Requested: {amount}, Current balance: {_gems}");
                return false;
            }
            _gems -= amount;
            LoggerService?.Log($"[PixelFlow.InventoryModel] Spent {amount} gems. Remaining balance: {_gems}");
            OnGemsChanged?.Invoke(_gems);
            return true;
        }

        // === Ticket (etkinlik parası) — game_plan.md §9.1: etkinlik görevleri / mağaza ===
        public void AddTickets(int amount)
        {
            if (amount <= 0) return;
            _tickets += amount;
            LoggerService?.Log($"[PixelFlow.InventoryModel] Added {amount} tickets. New balance: {_tickets}");
            OnTicketsChanged?.Invoke(_tickets);
        }

        public bool TrySpendTickets(int amount)
        {
            if (amount <= 0 || _tickets < amount)
            {
                LoggerService?.LogWarning($"[PixelFlow.InventoryModel] Spend tickets failed! Requested: {amount}, Current balance: {_tickets}");
                return false;
            }
            _tickets -= amount;
            LoggerService?.Log($"[PixelFlow.InventoryModel] Spent {amount} tickets. Remaining balance: {_tickets}");
            OnTicketsChanged?.Invoke(_tickets);
            return true;
        }

        // === Star Pass (sezonluk premium track) — game_plan.md §9.3 ===
        public void SetStarPassActive(bool active)
        {
            if (_starPassActive == active) return;
            _starPassActive = active;
            LoggerService?.Log($"[PixelFlow.InventoryModel] Star Pass active = {_starPassActive}");
            OnStarPassChanged?.Invoke(_starPassActive);
        }

        public bool IsSkinUnlocked(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return false;
            return _unlockedSkins.Contains(skinId);
        }

        public void UnlockSkin(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return;
            if (_unlockedSkins.Add(skinId))
            {
                LoggerService?.Log($"[PixelFlow.InventoryModel] Unlocked new vehicle skin: {skinId}");
                OnSkinUnlocked?.Invoke(skinId);
            }
        }

        public void EquipSkin(ColorType colorFamily, string skinId)
        {
            if (!IsSkinUnlocked(skinId))
            {
                LoggerService?.LogWarning($"[PixelFlow.InventoryModel] Cannot equip locked skin: {skinId} for color: {colorFamily}");
                return;
            }
            _equippedSkins[colorFamily] = skinId;
            LoggerService?.Log($"[PixelFlow.InventoryModel] Equipped skin: {skinId} for color family: {colorFamily}");
            OnSkinEquipped?.Invoke(colorFamily, skinId);
        }

        public string GetEquippedSkin(ColorType colorFamily)
        {
            return _equippedSkins.TryGetValue(colorFamily, out var skinId) ? skinId : "skin_default";
        }
    }
}
