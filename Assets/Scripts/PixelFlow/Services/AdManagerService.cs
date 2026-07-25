using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    public interface IAdManagerService : ICrisisAdService
    {
        bool IsRewardedAdReady();
        void ShowRewardedAd(string placementId, Action<bool> onCompleted);
        void ShowInterstitialAd(string placementId);
    }

    /// <summary>
    /// Color Jam 3D - Reklam & Monetization Servisi.
    /// Rewarded Video (2x Coin, VIP Skin, Power-up) ve Seviye 5+ Geçiş Reklamlarını (Interstitial) yönetir.
    /// ICrisisAdService ile %100 geriye dönük uyumludur.
    /// </summary>
    public class AdManagerService : CrisisAdService, IAdManagerService, INexusService
    {
        [Inject] public IInventoryModel InventoryModel { get; set; }
        [Inject] public new IGameSessionModel GameSessionModel { get; set; }
        [Inject] public IEconomyService EconomyService { get; set; }
        [Inject, OptionalInject] public new GameConfig Config { get; set; }
        [Inject, OptionalInject] public ILoggerService LoggerService { get; set; }
        [Inject] public new ISignalBus SignalBus { get; set; }

        public bool IsRewardedAdReady()
        {
            return true;
        }

        public void ShowRewardedAd(string placementId, Action<bool> onCompleted)
        {
            LoggerService?.Log($"[PixelFlow.AdManagerService] Rewarded Video requested for placement: {placementId}");
            
            // Process reward based on placement
            bool rewardGranted = GrantRewardedAdReward(placementId);
            
            onCompleted?.Invoke(rewardGranted);
            
            if (rewardGranted)
            {
                LoggerService?.Log($"[PixelFlow.AdManagerService] ✔ Rewarded ad reward granted for placement: {placementId}");
            }
        }

        public void ShowInterstitialAd(string placementId)
        {
            int level = LevelModel?.CurrentLevel?.levelIndex ?? 0;
            // game_plan.md §2.2: minimum seviye eşiği GameConfig'ten gelir (base ConfigMinLevel).
            if (level + 1 < ConfigMinLevel)
            {
                LoggerService?.Log($"[PixelFlow.AdManagerService] Interstitial skipped (Level {level + 1} < Min Level {ConfigMinLevel} threshold).");
                return;
            }

            // Interstitial frequency check
            if (!ShouldShowInterstitial(level + 1))
            {
                LoggerService?.Log($"[PixelFlow.AdManagerService] Interstitial skipped - frequency check failed (Level {level + 1}).");
                return;
            }

            LoggerService?.Log($"[PixelFlow.AdManagerService] Showing Interstitial Ad for placement: {placementId} (Level {level + 1})");
            SignalBus?.Fire(new RequestInterstitialAdSignal());
        }

        private bool GrantRewardedAdReward(string placementId)
        {
            switch (placementId)
            {
                case "double_coins":
                    // 2x Coin reward
                    if (GameSessionModel != null && EconomyService != null)
                    {
                        int baseCoins = GameSessionModel.CoinsEarnedThisLevel > 0 ? GameSessionModel.CoinsEarnedThisLevel : Config?.RewardedAdCoinReward ?? 100;
                        EconomyService.Earn("coin", baseCoins, $"rewarded_ad:{placementId}");
                    }
                    else if (InventoryModel != null)
                    {
                        int baseCoins = GameSessionModel?.CoinsEarnedThisLevel > 0 ? GameSessionModel.CoinsEarnedThisLevel : Config?.RewardedAdCoinReward ?? 100;
                        InventoryModel.AddCoins(baseCoins);
                    }
                    return true;

                case "extra_undo":
                    // Extra Undo hint (add 2 undos)
                    if (GameSessionModel != null)
                    {
                        GameSessionModel.AddUndoHints(2);
                    }
                    return true;

                case "daily_chest_double":
                    // Daily Chest 2x
                    if (EconomyService != null)
                    {
                        int baseCoins = Config?.DailyChestCoins ?? 100;
                        EconomyService.Earn("coin", baseCoins, $"rewarded_ad:{placementId}");
                    }
                    else if (InventoryModel != null)
                    {
                        int baseCoins = Config?.DailyChestCoins ?? 100;
                        InventoryModel.AddCoins(baseCoins);
                    }
                    return true;

                case "revive":
                    // Revive - handled by VehicleSimulator
                    return true;

                case "lucky_wheel":
                    // Lucky Wheel - grants random reward
                    GrantLuckyWheelReward();
                    return true;

                default:
                    LoggerService?.LogWarning($"[PixelFlow.AdManagerService] Unknown rewarded placement: {placementId}");
                    return false;
            }
        }

        private bool ShouldShowInterstitial(int level)
        {
            if (Config == null) return true;

            // First N levels no ads (game_plan.md §9.4)
            if (level <= Config.MinLevelForInterstitial)
                return false;

            // Frequency check (every N levels)
            int frequency = Config.InterstitialLevelInterval;
            if (frequency <= 0) return false;

            return (level - Config.MinLevelForInterstitial) % frequency == 0;
        }

        private void GrantLuckyWheelReward()
        {
            // Lucky Wheel: random reward - coin, gem, or skin
            var rng = new System.Random();
            int rewardType = rng.Next(3);
            
            if (EconomyService != null)
            {
                switch (rewardType)
                {
                    case 0:
                        EconomyService.Earn("coin", rng.Next(50, 500), "lucky_wheel");
                        break;
                    case 1:
                        EconomyService.Earn("gem", rng.Next(5, 20), "lucky_wheel");
                        break;
                    case 2:
                        // Could grant a random common skin
                        break;
                }
            }
            else if (InventoryModel != null)
            {
                switch (rewardType)
                {
                    case 0:
                        InventoryModel.AddCoins(rng.Next(50, 500));
                        break;
                    case 1:
                        InventoryModel.AddGems(rng.Next(5, 20));
                        break;
                }
            }
        }

        public new ValueTask InitializeAsync(CancellationToken ct) => default;
        public new void OnDispose() { }
    }
}
