using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using PixelFlow.Services;
using System.Collections.Generic;

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

            var currentLevel = LevelModel.CurrentLevel;

            // 1. Check no empty cells remain (only if level explicitly requires full grid coverage)
            if (currentLevel != null && currentLevel.requireFullGridCoverage)
            {
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
            }

            // 2. Check every color with nodes has a connected path
            if (currentLevel?.initialNodes != null)
            {
                var colorNodes = new Dictionary<ColorType, List<GridNode>>();
                for (int i = 0; i < currentLevel.initialNodes.Count; i++)
                {
                    var n = currentLevel.initialNodes[i];
                    if (n.color == ColorType.None) continue;
                    if (!colorNodes.TryGetValue(n.color, out var list))
                        colorNodes[n.color] = list = new List<GridNode>(2);
                    list.Add(n);
                }

                foreach (var kvp in colorNodes)
                {
                    if (kvp.Value.Count < 2) continue;

                    if (!GridModel.Paths.TryGetValue(kvp.Key, out var path) || path.Count == 0)
                    {
                        UnityEngine.Debug.Log($"[CheckWinConditionCommand] Win check failed: color {kvp.Key} has no path");
                        return;
                    }

                    var startPos = path[0];
                    var endPos = path[path.Count - 1];
                    var node1 = kvp.Value[0].position;
                    var node2 = kvp.Value[1].position;

                    bool validConnection = (startPos == node1 && endPos == node2) || (startPos == node2 && endPos == node1);
                    if (!validConnection)
                    {
                        UnityEngine.Debug.Log($"[CheckWinConditionCommand] Win check failed: color {kvp.Key} path does not connect nodes. Path endpoints: ({startPos}, {endPos}), Node positions: ({node1}, {node2})");
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

            int viaductsUsed = GameSessionModel.MaxViaducts - GameSessionModel.AvailableViaducts;

            var (finalScore, stars) = ScoreCalculator.Calculate(
                GridModel.Width, GridModel.Height,
                GameSessionModel.ElapsedTime,
                hintsUsed, totalHints,
                viaductsUsed);

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
