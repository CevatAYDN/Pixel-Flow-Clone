using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;

namespace PixelFlow.Commands
{
    /// <summary>
    /// Gökkuşağı Yolu power-up'ını aktive eder.
    /// Oyuncuya 3 segmentlik rainbow çizim hakkı verir.
    /// Rainbow segmentler her renk aracın geçebileceği evrensel yol oluşturur.
    /// </summary>
    public class RainbowRoadCommand : ICommand<ActivateRainbowRoadSignal>, IResettable
    {
        [Inject] public IPowerUpService PowerUpService { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public PixelFlow.Services.IAudioService AudioService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        public void Execute(ActivateRainbowRoadSignal signal)
        {
            if (PowerUpService == null)
            {
                LoggerService?.LogWarning("[RainbowRoadCommand] PowerUpService is null!");
                return;
            }

            // Sadece Playing veya Paused modunda çalışsın
            var state = GameStateModel?.CurrentState;
            if (state != GameState.Playing && state != GameState.Paused)
            {
                LoggerService?.Log($"[RainbowRoadCommand] Ignored: current state is {state} (requires Playing or Paused).");
                return;
            }

            if (PowerUpService.HasActiveRainbowRoad)
            {
                LoggerService?.Log("[RainbowRoadCommand] Rainbow Road zaten aktif. Kullanımlar sıfırlanıyor.");
            }

            PowerUpService.ActivateRainbowRoad();
            AudioService?.PlaySfx(SfxType.PowerUpActivate);
            LoggerService?.Log($"[RainbowRoadCommand] Rainbow Road aktive edildi! {PowerUpService.RainbowRoadUses} kullanım hakkı.");
        }

        public void Reset() { }
    }
}
