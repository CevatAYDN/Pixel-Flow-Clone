using Nexus.Core;
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

        public void Execute(RedoSignal signal)
        {
            if (GameStateModel.CurrentState != GameState.Playing && GameStateModel.CurrentState != GameState.Paused)
                return;

            if (HistoryService.Redo(GridModel, GameSessionModel))
            {
                SignalBus.Fire(new GridUpdatedSignal());
            }
        }

        public void Reset() { }
    }
}
