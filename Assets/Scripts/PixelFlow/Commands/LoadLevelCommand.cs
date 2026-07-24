using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Signals;
using PixelFlow.Services;

namespace PixelFlow.Commands
{
    // Kayıt: GameContextLifecycle.OnConfigure'da fluent API ile yapılıyor.
    public class LoadLevelCommand : ICommand<LoadLevelSignal>, IResettable
    {
        [Inject] public ILevelLoaderService LevelLoaderService { get; set; }
        [Inject] public IPowerUpService PowerUpService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        public void Execute(LoadLevelSignal signal)
        {
            var level = signal.LevelToLoad;
            LoggerService?.Log($"[PixelFlow.LoadLevelCommand] LoadLevelSignal received for Level {(level != null ? level.levelIndex + 1 : 0)} ({level?.name}).");

            // GDD §8: Level yükleme sorumluluğu LevelLoaderService'e devredildi.
            LevelLoaderService.LoadLevel(signal);

            // Her yeni level'da power-up'ları sıfırla (1 Clear Jam + Rainbow Road reset)
            PowerUpService?.ResetForNewLevel();
            LoggerService?.Log("[PixelFlow.LoadLevelCommand] Level loaded successfully & PowerUps reset for new level.");
        }

        public void Reset() { }
    }
}
