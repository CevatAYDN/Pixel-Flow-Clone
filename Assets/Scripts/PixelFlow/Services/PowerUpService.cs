using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using PixelFlow.Data;

namespace PixelFlow.Services
{
    /// <summary>
    /// Tüm power-up'ları merkezi olarak yöneten servis.
    /// Power-up state'leri, kullanım hakları ve UI bildirimleri burada toplanır.
    /// INexusService — GameContextLifecycle'da BindService ile kaydedilir.
    /// </summary>
    public class PowerUpService : IPowerUpService, INexusService
    {
        [Inject, OptionalInject] public GameConfig Config { get; set; }

        // ── Rainbow Road ─────────────────────────────────────
        private int _rainbowRoadUses;

        public int RainbowRoadUses => _rainbowRoadUses;
        public bool HasActiveRainbowRoad => _rainbowRoadUses > 0;
        public event Action<int> OnRainbowRoadUsesChanged;

        public void ActivateRainbowRoad()
        {
            int segments = Config != null ? Config.RainbowRoadSegmentsPerActivation : throw new Data.DataValidationException("GameConfig.RainbowRoadSegmentsPerActivation erişilemedi!");
            _rainbowRoadUses = System.Math.Max(1, segments);
            OnRainbowRoadUsesChanged?.Invoke(_rainbowRoadUses);
        }

        public bool TryConsumeRainbowRoadSegment()
        {
            if (_rainbowRoadUses <= 0) return false;
            _rainbowRoadUses--;
            OnRainbowRoadUsesChanged?.Invoke(_rainbowRoadUses);
            return true;
        }

        public void DeactivateRainbowRoad()
        {
            _rainbowRoadUses = 0;
            OnRainbowRoadUsesChanged?.Invoke(_rainbowRoadUses);
        }

        // ── Clear Jam ────────────────────────────────────────
        private int _clearJamUses;

        public int ClearJamUsesRemaining => _clearJamUses;
        public bool CanUseClearJam => _clearJamUses > 0;
        public event Action<int> OnClearJamUsesChanged;

        public bool TryUseClearJam()
        {
            if (_clearJamUses <= 0) return false;
            _clearJamUses--;
            OnClearJamUsesChanged?.Invoke(_clearJamUses);
            return true;
        }

        public void AddClearJamUse(int amount = 1)
        {
            _clearJamUses += amount;
            OnClearJamUsesChanged?.Invoke(_clearJamUses);
        }

        // ── INexusService ────────────────────────────────────
        public void ResetForNewLevel()
        {
            int clearJamPerLevel = Config != null ? Config.ClearJamUsesPerLevel : throw new Data.DataValidationException("GameConfig.ClearJamUsesPerLevel erişilemedi!");
            _rainbowRoadUses = 0;
            _clearJamUses = System.Math.Max(0, clearJamPerLevel);
            OnRainbowRoadUsesChanged?.Invoke(0);
            OnClearJamUsesChanged?.Invoke(_clearJamUses);
        }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
