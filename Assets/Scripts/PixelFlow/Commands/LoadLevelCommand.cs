using Nexus.Core;
using PixelFlow.Signals;
using PixelFlow.Services;

namespace PixelFlow.Commands
{
    // Kayıt: GameContextLifecycle.OnConfigure'da fluent API ile yapılıyor.
    public class LoadLevelCommand : ICommand<LoadLevelSignal>, IResettable
    {
        [Inject] public ILevelLoaderService LevelLoaderService { get; set; }

        public void Execute(LoadLevelSignal signal)
        {
            // GDD §8: Level yükleme sorumluluğu LevelLoaderService'e devredildi.
            // Tüm bağımlılıklar (GridModel, LevelModel, Session, SignalBus, vb.)
            // LevelLoaderService'e [Inject] ile enjekte edilir.
            LevelLoaderService.LoadLevel(signal);
        }

        public void Reset() { }
    }
}
