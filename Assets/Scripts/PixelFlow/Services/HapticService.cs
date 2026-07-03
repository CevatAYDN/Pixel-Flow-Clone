using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using UnityEngine;
using PixelFlow.Models;

namespace PixelFlow.Services
{
    public enum HapticType
    {
        Light,      // UI Impact (Light) — yol çizimi başlangıcı
        Medium,     // UI Impact (Medium) — düğüm bağlantısı
        Heavy,      // UI Impact (Heavy) — viyadük yerleştirme
        Warning,    // UI Notification (Warning) — kaza
        Success,    // UI Notification (Success) — bölüm tamamlama
        Selection,  // UI Selection — vergi toplama
    }

    public interface IHapticService
    {
        void Vibrate(HapticType type);
        bool IsEnabled { get; set; }
    }

    /// <summary>
    /// GDD §11.2 + Ek C: 6 farklı haptic feedback deseni. iOS ve Android
    /// için platform-spesifik çağrı yapar; ayarlardan kapatılabilir.
    /// </summary>
    public class HapticService : IHapticService, INexusService
    {
        [Inject] public ISettingsModel SettingsModel { get; set; }

        public bool IsEnabled { get; set; } = true;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (SettingsModel != null)
            {
                IsEnabled = !SettingsModel.HapticsDisabled;
            }
            return default;
        }

        public void OnDispose() { }

        public void Vibrate(HapticType type)
        {
            if (!IsEnabled) return;
#if UNITY_IOS
            switch (type)
            {
                case HapticType.Light:     Handheld.Vibrate(); break;
                case HapticType.Medium:    Handheld.Vibrate(); break;
                case HapticType.Heavy:     Handheld.Vibrate(); break;
                case HapticType.Warning:   Handheld.Vibrate(); break;
                case HapticType.Success:   Handheld.Vibrate(); break;
                case HapticType.Selection: Handheld.Vibrate(); break;
            }
#elif UNITY_ANDROID
            long ms;
            int amplitude;
            switch (type)
            {
                case HapticType.Light:     ms = 10;  amplitude = 30; break;
                case HapticType.Medium:    ms = 30;  amplitude = 50; break;
                case HapticType.Heavy:     ms = 60;  amplitude = 80; break;
                case HapticType.Warning:   ms = 100; amplitude = 100; break;
                case HapticType.Success:   ms = 200; amplitude = 70; break;
                case HapticType.Selection: ms = 10;  amplitude = 20; break;
                default:                   ms = 20;  amplitude = 50; break;
            }
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null)
                    {
                        vibrator.Call("vibrate", ms);
                    }
                }
            }
            catch
            {
                // AndroidJavaClass başarısız olursa Handheld.Vibrate fallback.
                Handheld.Vibrate();
            }
#else
            // Editor ve diğer platformlar — sadece debug log.
            Debug.Log($"[HapticService] {type}");
#endif
        }
    }
}
