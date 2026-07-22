using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;

namespace PixelFlow.Commands
{
    // Kayıt: GameContextLifecycle.OnConfigure'da fluent API ile yapılıyor.
    public class LoadLevelCommand : ICommand<LoadLevelSignal>, IResettable
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public IHintModel HintModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }
        [Inject] public IObstacleService ObstacleService { get; set; }
        [Inject] public ISaveThrottler SaveThrottler { get; set; }
        [Inject] public ITutorialDriver TutorialDriver { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }
        [Inject] public ILevelLoaderService LevelLoaderService { get; set; }

        public void Execute(LoadLevelSignal signal)
        {
            // GDD §8: Level yükleme sorumluluğunu dedicated servise delegate et.
            LevelLoaderService.LoadLevel(signal, GridModel, LevelModel,
                GameSessionModel, HintModel, HistoryService, ObstacleService,
                TutorialDriver, SignalBus, GameStateModel, SaveThrottler,
                PlayerPrefsService, LoggerService);
        }

        public void Reset()
        {
            // Injected dependencies are automatically cleared by the framework's CommandPool
        }
    }
}
