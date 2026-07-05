using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;

namespace PixelFlow.Commands
{
    /// <summary>
    /// UndoSignal'i işler: GameHistoryService.Undo() çağırır.
    /// Başarılı olursa state geri yüklenir ve GridUpdatedSignal fırlatılır.
    /// </summary>
    public class UndoCommand : ICommand<UndoSignal>, IResettable
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public ISaveThrottler SaveThrottler { get; set; }
        [Inject] public ICrisisAdService CrisisAdService { get; set; }
        [Inject] public IHapticService HapticService { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }

        public void Execute(UndoSignal signal)
        {
            var state = GameStateModel.CurrentState;
            if (state != GameState.Playing && state != GameState.Paused)
                return;

            if (HistoryService.Undo(GridModel))
            {
                if (state == GameState.Paused)
                {
                    GameSessionModel.MarkCrisisUndoUsed();
                    GameStateModel.SetState(GameState.Playing);
                    CrisisAdService.RecordCrisisAttempt();
                }
                SignalBus.Fire(new GridUpdatedSignal());
                SaveThrottler?.TryRequestSave(() => GridStateSerializer.Save(GridModel, GameSessionModel, LevelModel, PlayerPrefsService));
                HapticService.Vibrate(HapticType.Light);
            }
        }

        public void Reset() { }
    }
}
