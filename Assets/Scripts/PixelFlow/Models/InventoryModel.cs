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
        event Action<int> OnCoinsChanged;
        event Action<string> OnSkinUnlocked;
        event Action<ColorType, string> OnSkinEquipped;

        void AddCoins(int amount);
        bool TrySpendCoins(int amount);
        bool IsSkinUnlocked(string skinId);
        void UnlockSkin(string skinId);
        void EquipSkin(ColorType colorFamily, string skinId);
        string GetEquippedSkin(ColorType colorFamily);
    }

    public class InventoryModel : IInventoryModel, IReactiveModel
    {
        [Inject, OptionalInject] public ILoggerService LoggerService { get; set; }

        private int _coins = 0;
        private readonly HashSet<string> _unlockedSkins = new HashSet<string> { "skin_default" };
        private readonly Dictionary<ColorType, string> _equippedSkins = new Dictionary<ColorType, string>();

        public int Coins => _coins;

        public event Action<int> OnCoinsChanged;
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

        public ValueTask OnBind(CancellationToken ct) => default;

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
