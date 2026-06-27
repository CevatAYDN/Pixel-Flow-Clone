using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;
using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Data;

namespace PixelFlow.Commands
{
    // Kayıt: GameContextLifecycle.OnConfigure'da fluent API ile yapılıyor.
    public class UseHintCommand : ICommand<RequestHintSignal>, IResettable
    {
        [Inject] public IHintModel HintModel { get; set; }
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public IPathService PathService { get; set; }

        public void Execute(RequestHintSignal signal)
        {
            if (HintModel.HintsRemaining <= 0)
            {
                UnityEngine.Debug.Log("[UseHintCommand] No hints remaining!");
                return;
            }

            var level = LevelModel.CurrentLevel;
            if (level == null || level.solutions == null || level.solutions.Count == 0) return;

            foreach (var solution in level.solutions)
            {
                bool isSolved = IsColorSolved(solution.color, solution.pathPositions);
                if (!isSolved)
                {
                    ApplySolution(solution);
                    HintModel.UseHint();
                    GridModel.LockedColors.Add(solution.color);
                    UnityEngine.Debug.Log($"[UseHintCommand] Applied hint for color: {solution.color} (LOCKED)");
                    SignalBus.Fire(new GridUpdatedSignal());
                    SignalBus.Fire(new CheckWinConditionSignal());
                    break;
                }
            }
        }

        private bool IsColorSolved(ColorType color, List<Vector2Int> solutionPath)
        {
            if (!GridModel.Paths.ContainsKey(color)) return false;
            var currentPath = GridModel.Paths[color];
            
            if (currentPath.Count != solutionPath.Count) return false;

            for (int i = 0; i < currentPath.Count; i++)
            {
                // Accept path in either direction
                if (currentPath[i] != solutionPath[i] && currentPath[i] != solutionPath[solutionPath.Count - 1 - i])
                {
                    return false;
                }
            }
            return true;
        }

        private void ApplySolution(PathSolution solution)
        {
            if (!GridModel.Paths.ContainsKey(solution.color))
            {
                GridModel.Paths[solution.color] = new List<Vector2Int>();
            }
            else
            {
                PathService.ClearPath(solution.color);
            }

            foreach (var pos in solution.pathPositions)
            {
                if (pos.x < 0 || pos.x >= GridModel.Width || pos.y < 0 || pos.y >= GridModel.Height)
                    continue;

                GridModel.Paths[solution.color].Add(pos);
                var cell = GridModel.Grid[pos.x, pos.y];
                if (cell.State == CellState.Empty)
                {
                    cell.State = CellState.Path;
                    cell.Color = solution.color;
                }
                else if (cell.Color != solution.color && cell.State == CellState.Path)
                {
                    PathService.BreakPath(cell.Color, pos);
                    cell.State = CellState.Path;
                    cell.Color = solution.color;
                }
            }
        }

        public void Reset()
        {
            // Do not nullify injected properties to prevent null-ref risks on framework reuse
        }
    }
}
