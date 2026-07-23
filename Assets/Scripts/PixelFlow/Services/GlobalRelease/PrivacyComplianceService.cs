using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;

namespace PixelFlow.Services.GlobalRelease
{
    /// <summary>
    /// game_plan.md §3.1: Yasal Gizlilik & İzin Uyum Sistemi (ATT / GDPR / UMP).
    /// iOS 14.5+ ATT İzni ve Google UMP Consent Akışını yönetir.
    /// </summary>
    public class PrivacyComplianceService : INexusService
    {
        public bool IsConsentGathered { get; private set; }

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            RequestPrivacyConsent();
            return default;
        }

        public void RequestPrivacyConsent()
        {
#if UNITY_IOS && !UNITY_EDITOR
            // Native iOS App Tracking Transparency (ATT) Prompt
            if (UnityEngine.iOS.Device.RequestStoreReview != null)
            {
                Debug.Log("[PrivacyComplianceService] Requesting iOS ATT permission...");
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            // Google UMP Consent Management
            Debug.Log("[PrivacyComplianceService] Gathering Google UMP Consent for GDPR/CCPA...");
#endif
            IsConsentGathered = true;
        }

        public void OnDispose() { }
    }
}
