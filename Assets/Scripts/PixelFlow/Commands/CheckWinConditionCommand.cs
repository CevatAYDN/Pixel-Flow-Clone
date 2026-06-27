using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using System.Collections.Generic;
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

                    // Each color must have at least 2 nodes (start + end)
                    if (group.Count() < 2) continue;

                    // The color must have a path registered
                    if (!GridModel.Paths.ContainsKey(group.Key) || GridModel.Paths[group.Key].Count == 0)
                        return;

                    var path = GridModel.Paths[group.Key];

                    // All nodes of this color must be in the path (ensures they're connected)
                    foreach (var node in group)
                    {
                        if (!path.Contains(node.position))
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
            GridModel = null;
            LevelModel = null;
            GameStateModel = null;
            SignalBus = null;
        }
    }
}
