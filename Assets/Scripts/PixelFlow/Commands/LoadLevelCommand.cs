using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;

namespace PixelFlow.Commands
{
    public class LoadLevelCommand : ICommand<LoadLevelSignal>
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }

        public void Execute(LoadLevelSignal signal)
        {
            UnityEngine.Debug.Log($"[LoadLevelCommand] Loading level: {signal.LevelToLoad.name} ({signal.LevelToLoad.width}x{signal.LevelToLoad.height})");
            LevelModel.SetLevel(signal.LevelToLoad);
            GridModel.Initialize(signal.LevelToLoad.width, signal.LevelToLoad.height);

            foreach (var node in signal.LevelToLoad.initialNodes)
            {
                if (node.position.x >= 0 && node.position.x < GridModel.Width &&
                    node.position.y >= 0 && node.position.y < GridModel.Height)
                {
                    GridModel.Grid[node.position.x, node.position.y].State = CellState.Node;
                    GridModel.Grid[node.position.x, node.position.y].Color = node.color;
                }
            }

            GridModel.UpdateGrid();
        }
    }
}
