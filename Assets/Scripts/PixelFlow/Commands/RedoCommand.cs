using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;

namespace PixelFlow.Commands
{
    /// <summary>
    /// RedoSignal'i işler: GameHistoryService.Redo() çağırır.
    /// Başarılı olursa state geri yüklenir ve GridUpdatedSignal fırlatılır.
    /// </summary>
    public class RedoCommand : ICommand<RedoSignal>, IResettable
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public IGameHistoryService HistoryService { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        public void Execute(RedoSignal signal)
        {
            var state = GameStateModel.CurrentState;
            LoggerService?.Log($"[PixelFlow.RedoCommand] Executing Redo... Current state: {state}");

            if (state != GameState.Playing && state != GameState.Paused)
            {
                LoggerService?.LogWarning($"[PixelFlow.RedoCommand] Aborted: Cannot Redo while in state {state}");
                return;
            }

            if (HistoryService.Redo(GridModel, GameSessionModel))
            {
                LoggerService?.Log($"[PixelFlow.RedoCommand] Redo successful. History Redo count remaining: {HistoryService.RedoCount}");
                SignalBus.Fire(new GridUpdatedSignal());
            }
            else
            {
                LoggerService?.LogWarning("[PixelFlow.RedoCommand] Redo failed: History redo stack is empty.");
            }
        }

        public void Reset() { }
    }
}
