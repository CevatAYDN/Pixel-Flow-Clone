using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Commands
{
    public class LoadLevelCommand : ICommand<LoadLevelSignal>, IResettable
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }

        public void Execute(LoadLevelSignal signal)
        {
            UnityEngine.Debug.Log($"[LoadLevelCommand] Loading level: {signal.LevelToLoad.name} ({signal.LevelToLoad.width}x{signal.LevelToLoad.height})");
            LevelModel.SetLevel(signal.LevelToLoad);
            GridModel.Initialize(signal.LevelToLoad.width, signal.LevelToLoad.height);

            if (signal.LevelToLoad.initialNodes != null)
            {
                foreach (var node in signal.LevelToLoad.initialNodes)
                {
                    if (node.position.x >= 0 && node.position.x < GridModel.Width &&
                        node.position.y >= 0 && node.position.y < GridModel.Height)
                    {
                        var cell = GridModel.Grid[node.position.x, node.position.y];
                        cell.State = CellState.Node;
                        cell.Color = node.color;
                    }
                }
            }

            if (signal.LevelToLoad.bridgePositions != null)
            {
                foreach (var bridgePos in signal.LevelToLoad.bridgePositions)
                {
                    if (bridgePos.x >= 0 && bridgePos.x < GridModel.Width &&
                        bridgePos.y >= 0 && bridgePos.y < GridModel.Height)
                    {
                        GridModel.Grid[bridgePos.x, bridgePos.y].State = CellState.Bridge;
                    }
                }
            }

            GridModel.UpdateGrid();
        }

        public void Reset()
        {
            GridModel = null;
            LevelModel = null;
        }
    }
}
