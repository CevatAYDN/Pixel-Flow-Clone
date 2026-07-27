using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using PixelFlow.Services;
using UnityEngine;

namespace PixelFlow.Commands
{
    public class SkinUnlockCommand : ICommand<SkinUnlockedSignal>, IResettable
    {
        [Inject] public IInventoryModel InventoryModel { get; set; }
        [Inject] public IEconomyService EconomyService { get; set; }
        [Inject] public ILoggerService Logger { get; set; }
        [Inject] public IFeedbackService FeedbackService { get; set; }
        [Inject, OptionalInject] public GameConfig Config { get; set; }
        [Inject, OptionalInject] public ISkinCatalogService SkinCatalog { get; set; }
        [Inject, OptionalInject] public PixelFlow.Data.StorageKeysConfigAsset Keys { get; set; }

        public void Execute(SkinUnlockedSignal signal)
        {
            if (string.IsNullOrEmpty(signal.SkinId))
            {
                Logger?.LogWarning("[PixelFlow.SkinUnlockCommand] SkinId is null or empty.");
                return;
            }

            var skinConfig = GetSkinConfig(signal.SkinId);
            if (skinConfig == null)
            {
                Logger?.LogWarning($"[PixelFlow.SkinUnlockCommand] VehicleSkinConfig not found for SkinId: {signal.SkinId}");
                return;
            }

            if (InventoryModel.IsSkinUnlocked(signal.SkinId))
            {
                Logger?.Log($"[PixelFlow.SkinUnlockCommand] Skin {signal.SkinId} already unlocked. Auto-equipping.");
                InventoryModel.EquipSkin(skinConfig.ColorFamily, signal.SkinId);
                FeedbackService?.Play(FeedbackPreset.LightClick);
                return;
            }

            if (string.IsNullOrEmpty(Keys?.CurrencyIdCoin))
            {
                Logger?.LogError("[PixelFlow.SkinUnlockCommand] CurrencyIdCoin is not configured.");
                FeedbackService?.Play(FeedbackPreset.ErrorFailure);
                return;
            }
            string coinCurrencyId = Keys.CurrencyIdCoin;

            if (EconomyService != null)
            {
                if (!EconomyService.CanAfford(coinCurrencyId, skinConfig.UnlockCoinCost))
                {
                    Logger?.LogWarning($"[PixelFlow.SkinUnlockCommand] Insufficient coins to unlock {signal.SkinId}. Cost: {skinConfig.UnlockCoinCost}, Available: {EconomyService.GetBalance(coinCurrencyId)}");
                    FeedbackService?.Play(FeedbackPreset.ErrorFailure);
                    return;
                }
                
                if (!EconomyService.Spend(coinCurrencyId, skinConfig.UnlockCoinCost, $"skin_unlock:{signal.SkinId}"))
                {
                    Logger?.LogWarning($"[PixelFlow.SkinUnlockCommand] Failed to spend coins for {signal.SkinId}");
                    FeedbackService?.Play(FeedbackPreset.ErrorFailure);
                    return;
                }
            }
            else if (Config != null)
            {
                // Fallback: direct coin spend via InventoryModel if EconomyService not available
                if (!InventoryModel.TrySpendCoins(skinConfig.UnlockCoinCost))
                {
                    Logger?.LogWarning($"[PixelFlow.SkinUnlockCommand] Insufficient coins to unlock {signal.SkinId}. Cost: {skinConfig.UnlockCoinCost}");
                    FeedbackService?.Play(FeedbackPreset.ErrorFailure);
                    return;
                }
            }
            else
            {
                Logger?.LogError("[PixelFlow.SkinUnlockCommand] Neither EconomyService nor Config available for coin validation!");
                FeedbackService?.Play(FeedbackPreset.ErrorFailure);
                return;
            }

            InventoryModel.UnlockSkin(signal.SkinId);
            InventoryModel.EquipSkin(skinConfig.ColorFamily, signal.SkinId);
            Logger?.Log($"[PixelFlow.SkinUnlockCommand] ✔ Successfully unlocked and equipped skin: {signal.SkinId} for {skinConfig.DisplayName}");
            FeedbackService?.Play(FeedbackPreset.SuccessFanfare);
        }

        private VehicleSkinConfig GetSkinConfig(string skinId)
        {
            if (SkinCatalog == null)
            {
                Logger?.LogError("[SkinUnlockCommand] ISkinCatalogService not injected — skin lookup failed.");
                return null;
            }
            return SkinCatalog.GetVehicleSkinById(skinId);
        }

        public void Reset()
        {
            // No mutable state to reset
        }
    }
}
