using System;

namespace PixelFlow.Services
{
    /// <summary>
    /// Tüm power-up'ları merkezi olarak yöneten servis arayüzü.
    /// Power-up state'leri, kullanım hakları ve UI bildirimleri burada toplanır.
    /// </summary>
    public interface IPowerUpService
    {
        // ── Rainbow Road ─────────────────────────────────────
        int RainbowRoadUses { get; }
        bool HasActiveRainbowRoad { get; }
        event Action<int> OnRainbowRoadUsesChanged;

        /// <summary>Rainbow Road'u 3 kullanımlık aktive eder.</summary>
        void ActivateRainbowRoad();
        /// <summary>Bir rainbow segment hakkı tüketir. Başarılıysa true döner.</summary>
        bool TryConsumeRainbowRoadSegment();
        /// <summary>Rainbow Road'u devre dışı bırakır.</summary>
        void DeactivateRainbowRoad();

        // ── Clear Jam ────────────────────────────────────────
        int ClearJamUsesRemaining { get; }
        event Action<int> OnClearJamUsesChanged;

        /// <summary>Clear Jam kullanılabilir mi?</summary>
        bool CanUseClearJam { get; }
        /// <summary>Clear Jam kullanım hakkını tüketir. Başarılıysa true döner.</summary>
        bool TryUseClearJam();
        /// <summary>Clear Jam hakkı ekler (ör: seviye başına 1).</summary>
        void AddClearJamUse(int amount = 1);

        /// <summary>Yeni level başlangıcında tüm power-up state'lerini sıfırlar.</summary>
        void ResetForNewLevel();
    }
}
