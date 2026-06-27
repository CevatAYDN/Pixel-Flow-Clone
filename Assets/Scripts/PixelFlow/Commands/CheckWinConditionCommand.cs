using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using System.Linq;

namespace PixelFlow.Commands
{
    public class CheckWinConditionCommand : ICommand<CheckWinConditionSignal>, IResettable
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }

        public void Execute(CheckWinConditionSignal signal)
        {
            if (GameStateModel.CurrentState != GameState.Playing)
                return;

            // 1. Check no empty cells remain
            for (int x = 0; x < GridModel.Width; x++)
            {
                for (int y = 0; y < GridModel.Height; y++)
                {
                    if (GridModel.Grid[x, y].State == CellState.Empty)
                        return;
                }
            }

            // 2. Check every color with nodes has a connected path
            var currentLevel = LevelModel.CurrentLevel;
            if (currentLevel != null && currentLevel.initialNodes != null)
            {
                var colorGroups = currentLevel.initialNodes
                    .GroupBy(n => n.color)
                    .ToList();

                foreach (var group in colorGroups)
                {
                    if (group.Key == ColorType.None) continue;

                    var nodes = group.ToList();
                    // Each color must have exactly 2 nodes (start + end) in a standard level
                    if (nodes.Count < 2) continue;

                    // The color must have a path registered
                    if (!GridModel.Paths.ContainsKey(group.Key) || GridModel.Paths[group.Key].Count == 0)
                        return;

                    var path = GridModel.Paths[group.Key];

                    // Verify that the path starts at one of the nodes and ends at the other node
                    var startPos = path[0];
                    var endPos = path[path.Count - 1];
                    var node1 = nodes[0].position;
                    var node2 = nodes[1].position;

                    bool validConnection = (startPos == node1 && endPos == node2) || (startPos == node2 && endPos == node1);
                    if (!validConnection)
                        return;

                    // Additionally verify all path positions belong to the grid
                    foreach (var pos in path)
                    {
                        if (pos.x < 0 || pos.x >= GridModel.Width || pos.y < 0 || pos.y >= GridModel.Height)
                            return;
                    }
                }
            }

            UnityEngine.Debug.Log("[CheckWinConditionCommand] WIN CONDITION MET! Level Completed!");
            GameStateModel.SetState(GameState.LevelCompleted);
            SignalBus.Fire(new LevelCompletedSignal());
        }

        public void Reset()
        {
            // Do not nullify injected properties to prevent null-ref risks on framework reuse
        }
    }
}
