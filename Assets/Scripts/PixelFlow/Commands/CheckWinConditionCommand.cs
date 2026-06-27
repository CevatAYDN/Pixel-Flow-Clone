using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Commands
{
    public class CheckWinConditionCommand : ICommand<CheckWinConditionSignal>
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }

        public void Execute(CheckWinConditionSignal signal)
        {
            if (GameStateModel.CurrentState != GameState.Playing)
                return;

            for (int x = 0; x < GridModel.Width; x++)
            {
                for (int y = 0; y < GridModel.Height; y++)
                {
                    if (GridModel.Grid[x, y].State == CellState.Empty)
                        return;
                }
            }

            GameStateModel.SetState(GameState.LevelCompleted);
            SignalBus.Fire(new LevelCompletedSignal());
        }
    }
}
