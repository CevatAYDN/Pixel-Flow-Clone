using Nexus.Core;
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

        public void Execute(UndoSignal signal)
        {
            var state = GameStateModel.CurrentState;
            if (state != GameState.Playing && state != GameState.Paused)
                return;

            if (HistoryService.Undo(GridModel))
            {
                if (state == GameState.Paused)
                {
                    GameStateModel.SetState(GameState.Playing);
                }
                SignalBus.Fire(new GridUpdatedSignal());
            }
        }

        public void Reset() { }
    }
}
