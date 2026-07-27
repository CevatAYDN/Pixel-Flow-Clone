using UnityEngine;

namespace PixelFlow.Data
{
    /// <summary>
    /// Rush Hour Event konfigürasyonu - game_plan.md §6 ve §15.6'e göre.
    /// 24 saatlik Çift Para etkinliği parametreleri.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RushHourConfig",
        menuName = "PixelFlow/Rush Hour Config")]
    public class RushHourConfigAsset : ScriptableObject
    {
        [Header("=== Event Timing ===")]
        [Tooltip("Etkinlik süresi (saniye). Varsayılan: 24 saat = 86400 saniye")]
        [Range(60, 604800)]
        public int DurationSeconds = 86400;

        [Header("=== Economy ===")]
        [Tooltip("Etkinlik sırasında coin çarpanı. Varsayılan: 2.0 (2x Para)")]
        [Range(1.0f, 10.0f)]
        public float CoinMultiplier = 2.0f;

        [Header("=== Cooldown ===")]
        [Tooltip("Etkinlikler arası bekleme süresi (saat). Varsayılan: 48 saat")]
        [Range(1, 168)]
        public int CooldownHours = 48;

        [Header("=== Trigger Conditions ===")]
        [Tooltip("Minimum seviye (bu seviyeden önce event tetiklenmez)")]
        [Range(1, 150)]
        public int MinLevel = 10;

        [Tooltip("Tetikleyici: son etkinlikten bu yana geçen saat")]
        [Range(1, 72)]
        public int TriggerAfterHours = 24;
    }
}