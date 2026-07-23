using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
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
        [Inject, OptionalInject] public ILoggerService LoggerService { get; set; }

        public bool IsRewardedAdReady()
        {
            return true;
        }

        public void ShowRewardedAd(string placementId, Action<bool> onCompleted)
        {
            LoggerService?.Log($"[PixelFlow.AdManagerService] Rewarded Video requested for placement: {placementId}");
            onCompleted?.Invoke(true);
        }

        public void ShowInterstitialAd(string placementId)
        {
            int level = LevelModel?.CurrentLevel?.levelIndex ?? 0;
            if (level + 1 < 5)
            {
                LoggerService?.Log($"[PixelFlow.AdManagerService] Interstitial skipped (Level {level + 1} < Min Level 5 threshold).");
                return;
            }

            LoggerService?.Log($"[PixelFlow.AdManagerService] Showing Interstitial Ad for placement: {placementId} (Level {level + 1})");
            SignalBus?.Fire(new RequestInterstitialAdSignal());
        }
    }
}
