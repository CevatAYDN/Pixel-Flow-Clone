using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Commands
{
    /// <summary>
    /// Sıkışmayı Temizle (Clear Jam) power-up'ı.
    /// Tüm çizili yolları temizler, grid'i başlangıç durumuna döndürür.
    /// Node/obstacle/viaduct state'leri korunur.
    /// </summary>
    public class ClearJamCommand : ICommand<ClearJamSignal>, IResettable
    {
        [Inject] public IPowerUpService PowerUpService { get; set; }
        [Inject] public IPathService PathService { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }
        [Inject] public PixelFlow.Services.IAudioService AudioService { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject, OptionalInject] public GameConfig Config { get; set; }

        // game_plan.md §2.2: Clear Jam hak sayısı GameConfig'ten gelir. Build'de config yoksa fail-loud.
        private GameConfig ResolveConfig()
        {
            if (Config != null) return Config;
#if !UNITY_EDITOR
            throw new DataValidationException("GameConfig erişilemedi! ClearJamCommand hak sayısı belirleyemiyor.");
#else
            return ScriptableObject.CreateInstance<GameConfig>();
#endif
        }

        public void Execute(ClearJamSignal signal)
        {
            var state = GameStateModel?.CurrentState;
            if (state != GameState.Playing && state != GameState.Paused)
            {
                LoggerService?.Log($"[ClearJamCommand] Ignored: current state is {state} (requires Playing or Paused).");
                return;
            }

            if (PowerUpService != null && PowerUpService.ClearJamUsesRemaining <= 0)
            {
                PowerUpService.AddClearJamUse(ResolveConfig().ClearJamUsesPerLevel);
            }

            if (PowerUpService == null || !PowerUpService.TryUseClearJam())
            {
                LoggerService?.LogWarning("[ClearJamCommand] No Clear Jam uses remaining!");
                return;
            }

            LoggerService?.Log($"[ClearJamCommand] Clear Jam kullanıldı! Kalan: {PowerUpService.ClearJamUsesRemaining}");

            // Power-up ses efekti çal
            AudioService?.PlaySfx(SfxType.PowerUpClear);

            // PathService ile tüm yolları temizle
            PathService?.ClearAllPaths();

            // History'i temizle — undo ile geri alınamaz olsun (kullan-at power-up)
            HistoryService?.Clear();

            // Grid güncelleme sinyali gönder
            SignalBus?.Fire(new GridUpdatedSignal());

            LoggerService?.Log("[ClearJamCommand] All paths cleared successfully. Grid updated.");
        }

        public void Reset() { }
    }
}
