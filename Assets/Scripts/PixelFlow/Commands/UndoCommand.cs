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
        [Inject] public ILoggerService LoggerService { get; set; }

        public void Execute(UndoSignal signal)
        {
            var state = GameStateModel.CurrentState;
            LoggerService?.Log($"[PixelFlow.UndoCommand] Executing Undo... Current state: {state}");

            if (state != GameState.Playing && state != GameState.Paused)
            {
                LoggerService?.LogWarning($"[PixelFlow.UndoCommand] Aborted: Cannot Undo while in state {state}");
                return;
            }

            if (HistoryService.Undo(GridModel, GameSessionModel))
            {
                LoggerService?.Log($"[PixelFlow.UndoCommand] Undo successful. History Undo count remaining: {HistoryService.UndoCount}");
                if (state == GameState.Paused)
                {
                    LoggerService?.Log("[PixelFlow.UndoCommand] Recovering from Paused/Crisis state. Reverting to Playing state.");
                    GameSessionModel.MarkCrisisUndoUsed();
                    GameStateModel.SetState(GameState.Playing);
                    CrisisAdService.RecordCrisisAttempt();
                }
                SignalBus.Fire(new GridUpdatedSignal());
                SaveHelper.TrySave(SaveThrottler, GridModel, GameSessionModel, LevelModel, PlayerPrefsService);
                HapticService.Vibrate(HapticType.Light);
            }
            else
            {
                LoggerService?.LogWarning("[PixelFlow.UndoCommand] Undo failed: History undo stack is empty.");
            }
        }

        public void Reset() { }
    }
}
