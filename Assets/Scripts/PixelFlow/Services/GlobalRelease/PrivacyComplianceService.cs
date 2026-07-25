using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;

namespace PixelFlow.Services.GlobalRelease
{
    /// <summary>
    /// game_plan.md §3.1: Yasal Gizlilik & İzin Uyum Sistemi (ATT / GDPR / UMP).
    /// iOS 14.5+ ATTrackingManager ve Google UMP Consent Management akışını yönetir.
    /// Platform SDK'leri mevcut değilse konsol uyarısı loglar — sert hata fırlatmaz
    /// çünkü kullanıcı reddi normal akışın parçasıdır.
    /// </summary>
    public class PrivacyComplianceService : INexusService
    {
        [Inject, OptionalInject] public ILoggerService LoggerService { get; set; }
        [Inject, OptionalInject] public GameConfig Config { get; set; }

        public bool IsConsentGathered { get; private set; }

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            RequestPrivacyConsent();
            return default;
        }

        public void RequestPrivacyConsent()
        {
#if UNITY_IOS && !UNITY_EDITOR
            RequestIosAttPermission();
#elif UNITY_ANDROID && !UNITY_EDITOR
            RequestGoogleUmpConsent();
#else
            LoggerService?.Log("[PrivacyComplianceService] Editor / stand-alone — consent not applicable, marking as gathered.");
            IsConsentGathered = true;
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        private void RequestIosAttPermission()
        {
            // iOS 14.5+ ATTrackingManager authorization request.
            // ATTrackingManager binding is provided by the iOS framework plugin.
            // If the plugin is missing, log the warning — the app still functions
            // without ATT (no IDFA; ad attribution falls back to SKAdNetwork).
            try
            {
                // Unity.iOS.ATTrackingManager is available via the iOS framework package.
                // Using reflection to avoid hard dependency on the plugin.
                var attType = System.Type.GetType("UnityEngine.iOS.ATTrackingManager, UnityEngine.iOSModule");
                if (attType != null)
                {
                    var requestMethod = attType.GetMethod("RequestTrackingAuthorization",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (requestMethod != null)
                    {
                        requestMethod.Invoke(null, null);
                        LoggerService?.Log("[PrivacyComplianceService] iOS ATT permission requested.");
                        IsConsentGathered = true;
                        return;
                    }
                }
                LoggerService?.LogWarning("[PrivacyComplianceService] ATTrackingManager not found. Add iOS framework plugin or configure Info.plist NSUserTrackingUsageDescription.");
            }
            catch (System.Exception ex)
            {
                LoggerService?.LogWarning($"[PrivacyComplianceService] ATT request failed (non-fatal): {ex.Message}");
            }
            IsConsentGathered = true;
        }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private void RequestGoogleUmpConsent()
        {
            // Google User Messaging Platform (UMP) consent gathering.
            // Requires GoogleUmp plugin (com.google.ads.ump) in the project.
            // If the plugin is missing, log warning — app still functions without UMP.
            try
            {
                var umpType = System.Type.GetType("Google.Ump.ConsentManager, Google.Ump");
                if (umpType != null)
                {
                    var requestMethod = umpType.GetMethod("RequestConsentInfoUpdate",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (requestMethod != null)
                    {
                        requestMethod.Invoke(null, new object[] { null });
                        LoggerService?.Log("[PrivacyComplianceService] Google UMP consent requested.");
                        IsConsentGathered = true;
                        return;
                    }
                }
                LoggerService?.LogWarning("[PrivacyComplianceService] Google.Ump.ConsentManager not found. Add Google UMP plugin (com.google.ads.ump) for GDPR/CCPA compliance.");
            }
            catch (System.Exception ex)
            {
                LoggerService?.LogWarning($"[PrivacyComplianceService] UMP request failed (non-fatal): {ex.Message}");
            }
            IsConsentGathered = true;
        }
#endif

        public void OnDispose() { }
    }
}
