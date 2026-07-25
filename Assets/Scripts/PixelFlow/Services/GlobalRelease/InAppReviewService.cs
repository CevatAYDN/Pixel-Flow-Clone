using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Signals;

namespace PixelFlow.Services.GlobalRelease
{
    /// <summary>
    /// game_plan.md §3.3: Mağaza İçi Değerlendirme Akışı (In-App Review API).
    /// Seviye 10 ve 15 tamamlandığında Apple StoreKit (SKStoreReviewController)
    /// ve Android Play In-App Review API'yi tetikler.
    /// Platform plugin'i yoksa sessizce geçer — hata fırlatmaz çünkü review
    /// isteği kritik bir işlem değildir.
    /// </summary>
    public class InAppReviewService : INexusService
    {
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public IPlayerPrefsService Prefs { get; set; }
        [Inject, OptionalInject] public ILoggerService LoggerService { get; set; }

        private ISignalSubscription _levelCompletedSub;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            _levelCompletedSub = SignalBus?.Subscribe<LevelCompletedSignal>(OnLevelCompleted);
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
#if UNITY_IOS && !UNITY_EDITOR
            RequestIosReview();
#elif UNITY_ANDROID && !UNITY_EDITOR
            RequestAndroidReview();
#else
            LoggerService?.Log("[InAppReviewService] Review requested (editor — no native dialog).");
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        private void RequestIosReview()
        {
            try
            {
                // Use the built-in Unity iOS review API (available in Unity 6000+).
                var iosDeviceType = System.Type.GetType("UnityEngine.iOS.Device, UnityEngine.iOSModule");
                if (iosDeviceType != null)
                {
                    var reqMethod = iosDeviceType.GetMethod("RequestStoreReview",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (reqMethod != null)
                    {
                        reqMethod.Invoke(null, null);
                        LoggerService?.Log("[InAppReviewService] iOS StoreKit review requested.");
                        return;
                    }
                }
                LoggerService?.LogWarning("[InAppReviewService] UnityEngine.iOS.Device.RequestStoreReview not found. Add iOS module.");
            }
            catch (System.Exception ex)
            {
                LoggerService?.LogWarning($"[InAppReviewService] iOS review request failed (non-fatal): {ex.Message}");
            }
        }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private void RequestAndroidReview()
        {
            // Android Play In-App Review API via the Google Play Review plugin.
            try
            {
                var reviewManagerType = System.Type.GetType("Google.Play.Review.ReviewManager, Google.Play.Review");
                if (reviewManagerType != null)
                {
                    // Create a ReviewManager instance and launch the review flow.
                    var reviewManager = System.Activator.CreateInstance(reviewManagerType);
                    var requestMethod = reviewManagerType.GetMethod("RequestReviewFlow");
                    if (requestMethod != null)
                    {
                        var requestTask = requestMethod.Invoke(reviewManager, null) as Task;
                        if (requestTask != null)
                        {
                            requestTask.ContinueWith(_ =>
                            {
                                LoggerService?.Log("[InAppReviewService] Android Play Review requested.");
                            });
                            return;
                        }
                    }
                }
                LoggerService?.LogWarning("[InAppReviewService] Google.Play.Review not found. Add Play Review plugin (com.google.play.review).");
            }
            catch (System.Exception ex)
            {
                LoggerService?.LogWarning($"[InAppReviewService] Android review request failed (non-fatal): {ex.Message}");
            }
        }
#endif

        public void OnDispose()
        {
            _levelCompletedSub?.Dispose();
            _levelCompletedSub = null;
        }
    }
}
