using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Signals;

namespace PixelFlow.Services.GlobalRelease
{
    /// <summary>
    /// game_plan.md §3.3: Mağaza İçi Değerlendirme Akışı (In-App Review API).
    /// Seviye 10 veya 15 tamamlandığında Apple StoreKit / Android In-App Review API'yi tetikler.
    /// </summary>
    public class InAppReviewService : INexusService
    {
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public IPlayerPrefsService Prefs { get; set; }

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            SignalBus?.Subscribe<LevelCompletedSignal>(OnLevelCompleted);
            return default;
        }

        private void OnLevelCompleted(LevelCompletedSignal signal)
        {
            if (Prefs == null) return;
            int completedCount = Prefs.GetInt("CompletedLevelsCount", 0) + 1;
            Prefs.SetInt("CompletedLevelsCount", completedCount);
            Prefs.Save();

            if (completedCount == 10 || completedCount == 15)
            {
                TriggerInAppReview();
            }
        }

        public void TriggerInAppReview()
        {
            Debug.Log("[InAppReviewService] Triggering Apple StoreKit / Android In-App Review API dialog...");
#if UNITY_IOS && !UNITY_EDITOR
            UnityEngine.iOS.Device.RequestStoreReview();
#endif
        }

        public void OnDispose() { }
    }
}
