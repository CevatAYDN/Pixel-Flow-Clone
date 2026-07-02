using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using PixelFlow.Services;
using System.Linq;

namespace PixelFlow.Commands
{
    // Kayıt: GameContextLifecycle.OnConfigure'da fluent API ile yapılıyor.
    public class CheckWinConditionCommand : ICommand<CheckWinConditionSignal>, IResettable
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public IHintModel HintModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }

        public void Execute(CheckWinConditionSignal signal)
        {
            if (GameStateModel.CurrentState != GameState.Playing)
            {
                UnityEngine.Debug.Log($"[CheckWinConditionCommand] Aborting check: GameState is not Playing (current: {GameStateModel.CurrentState})");
                return;
            }

            // 1. Check no empty cells remain
            for (int x = 0; x < GridModel.Width; x++)
            {
                for (int y = 0; y < GridModel.Height; y++)
                {
                    if (GridModel.Grid[x, y].State == CellState.Empty)
                    {
                        UnityEngine.Debug.Log($"[CheckWinConditionCommand] Win check failed: empty cell remaining at ({x}, {y})");
                        return;
                    }
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
                    if (nodes.Count < 2) continue;

                    // The color must have a path registered
                    if (!GridModel.Paths.ContainsKey(group.Key) || GridModel.Paths[group.Key].Count == 0)
                    {
                        UnityEngine.Debug.Log($"[CheckWinConditionCommand] Win check failed: color {group.Key} has no path");
                        return;
                    }

                    var path = GridModel.Paths[group.Key];

                    // Verify that the path starts at one of the nodes and ends at the other node
                    var startPos = path[0];
                    var endPos = path[path.Count - 1];
                    var node1 = nodes[0].position;
                    var node2 = nodes[1].position;

                    bool validConnection = (startPos == node1 && endPos == node2) || (startPos == node2 && endPos == node1);
                    if (!validConnection)
                    {
                        UnityEngine.Debug.Log($"[CheckWinConditionCommand] Win check failed: color {group.Key} path does not connect nodes. Path endpoints: ({startPos}, {endPos}), Node positions: ({node1}, {node2})");
                        return;
                    }

                    // Additionally verify all path positions belong to the grid
                    foreach (var pos in path)
                    {
                        if (pos.x < 0 || pos.x >= GridModel.Width || pos.y < 0 || pos.y >= GridModel.Height)
                        {
                            UnityEngine.Debug.Log($"[CheckWinConditionCommand] Win check failed: path position {pos} out of bounds");
                            return;
                        }
                    }
                }
            }

            UnityEngine.Debug.Log("[CheckWinConditionCommand] WIN CONDITION MET! Level Completed!");

            int hintsUsed = HintModel != null
                ? HintModel.TotalHintsUsed
                : 0;
            int totalHints = HintModel != null
                ? HintModel.HintsRemaining + hintsUsed
                : 3;

            var (finalScore, stars) = ScoreCalculator.Calculate(
                GridModel.Width, GridModel.Height,
                GameSessionModel.ElapsedTime,
                hintsUsed, totalHints);

            GameSessionModel.AddScore(finalScore);
            GameSessionModel.SetStars(stars);

            UnityEngine.Debug.Log("[CheckWinConditionCommand] Paths completed! Transitioning to Simulation Phase...");
            GameStateModel.SetState(GameState.Simulating);
        }

        public void Reset()
        {
            // Do not nullify injected properties to prevent null-ref risks on framework reuse
        }
    }
}
