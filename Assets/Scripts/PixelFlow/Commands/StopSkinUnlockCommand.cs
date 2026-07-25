using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using PixelFlow.Services;
using UnityEngine;

namespace PixelFlow.Commands
{
    public class StopSkinUnlockCommand : ICommand<StopSkinUnlockedSignal>, IResettable
    {
        [Inject] public IInventoryModel InventoryModel { get; set; }
        [Inject] public IEconomyService EconomyService { get; set; }
        [Inject] public ILoggerService Logger { get; set; }
        [Inject] public IFeedbackService FeedbackService { get; set; }
        [Inject, OptionalInject] public GameConfig Config { get; set; }
        [Inject, OptionalInject] public ISkinCatalogService SkinCatalog { get; set; }
        [Inject, OptionalInject] public PixelFlow.Data.StorageKeysConfigAsset Keys { get; set; }

        public void Execute(StopSkinUnlockedSignal signal)
        {
            if (string.IsNullOrEmpty(signal.SkinId))
            {
                Logger?.LogWarning("[PixelFlow.StopSkinUnlockCommand] SkinId is null or empty.");
                return;
            }

            var skinConfig = GetSkinConfig(signal.SkinId);
            if (skinConfig == null)
            {
                Logger?.LogWarning($"[PixelFlow.StopSkinUnlockCommand] StopSkinConfig not found for SkinId: {signal.SkinId}");
                return;
            }

            if (InventoryModel.IsStopSkinUnlocked(signal.SkinId))
            {
                Logger?.Log($"[PixelFlow.StopSkinUnlockCommand] Stop skin {signal.SkinId} already unlocked. Auto-equipping.");
                InventoryModel.EquipStopSkin((ColorType)skinConfig.ThemePalette, signal.SkinId);
                FeedbackService?.Play(FeedbackPreset.LightClick);
                return;
            }

            if (string.IsNullOrEmpty(Keys?.CurrencyIdCoin))
            {
                Logger?.LogError("[PixelFlow.StopSkinUnlockCommand] CurrencyIdCoin is not configured.");
                FeedbackService?.Play(FeedbackPreset.ErrorFailure);
                return;
            }
            string coinCurrencyId = Keys.CurrencyIdCoin;

            if (EconomyService != null)
            {
                if (!EconomyService.CanAfford(coinCurrencyId, skinConfig.UnlockCoinCost))
                {
                    Logger?.LogWarning($"[PixelFlow.StopSkinUnlockCommand] Insufficient coins to unlock {signal.SkinId}. Cost: {skinConfig.UnlockCoinCost}, Available: {EconomyService.GetBalance(coinCurrencyId)}");
                    FeedbackService?.Play(FeedbackPreset.ErrorFailure);
                    return;
                }
                
                if (!EconomyService.Spend(coinCurrencyId, skinConfig.UnlockCoinCost, $"stop_skin_unlock:{signal.SkinId}"))
                {
                    Logger?.LogWarning($"[PixelFlow.StopSkinUnlockCommand] Failed to spend coins for {signal.SkinId}");
                    FeedbackService?.Play(FeedbackPreset.ErrorFailure);
                    return;
                }
            }
            else if (Config != null)
            {
                if (!InventoryModel.TrySpendCoins(skinConfig.UnlockCoinCost))
                {
                    Logger?.LogWarning($"[PixelFlow.StopSkinUnlockCommand] Insufficient coins to unlock {signal.SkinId}. Cost: {skinConfig.UnlockCoinCost}");
                    FeedbackService?.Play(FeedbackPreset.ErrorFailure);
                    return;
                }
            }
            else
            {
                Logger?.LogError("[PixelFlow.StopSkinUnlockCommand] Neither EconomyService nor Config available for coin validation!");
                FeedbackService?.Play(FeedbackPreset.ErrorFailure);
                return;
            }

            InventoryModel.UnlockStopSkin(signal.SkinId);
            InventoryModel.EquipStopSkin((ColorType)skinConfig.ThemePalette, signal.SkinId);
            Logger?.Log($"[PixelFlow.StopSkinUnlockCommand] ✔ Successfully unlocked and equipped stop skin: {signal.SkinId} for {skinConfig.DisplayName}");
            FeedbackService?.Play(FeedbackPreset.SuccessFanfare);
        }

        private StopSkinConfig GetSkinConfig(string skinId)
        {
            if (SkinCatalog == null)
            {
                Logger?.LogError("[StopSkinUnlockCommand] ISkinCatalogService not injected — skin lookup failed.");
                return null;
            }
            return SkinCatalog.GetStopSkinById(skinId);
        }

        public void Reset()
        {
            // No mutable state to reset
        }
    }
}
