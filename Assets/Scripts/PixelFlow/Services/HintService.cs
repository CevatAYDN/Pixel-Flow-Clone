using System.Collections.Generic;
using System.Linq;
using PixelFlow.Data;
using PixelFlow.Models;
using UnityEngine;

namespace PixelFlow.Services
{
    public sealed class HintService : IHintService
    {
        private readonly IPathSolver _solver;

        public HintService(IPathSolver solver)
        {
            _solver = solver;
        }

        public List<Vector2Int> GetHint(LevelData level, ColorType color, int steps = 1)
        {
            if (steps <= 0) return null;

            if (steps == 1)
            {
                _solver.SolvePartial(level, color, 2, out var partial); // start node + 1 step
                if (partial != null && partial.Count >= 2)
                    return new List<Vector2Int> { partial[1] };
                return null;
            }

            _solver.SolvePartial(level, color, steps + 1, out var result);
            if (result != null && result.Count > 1)
                return result.Skip(1).Take(steps).ToList();

            return null;
        }

        public List<Vector2Int> GetNextUnsolvedHint(LevelData level, IGridModel grid, int steps = 1)
        {
            if (level == null || level.initialNodes == null) return null;

            var colorGroups = level.initialNodes
                .GroupBy(n => n.color)
                .ToList();

            foreach (var group in colorGroups)
            {
                if (group.Key == ColorType.None) continue;
                if (!grid.Paths.ContainsKey(group.Key) || grid.Paths[group.Key].Count < 2)
                {
                    Debug.Log($"[HintService] Unsolved color '{group.Key}' found. Requesting hint (steps={steps})...");
                    var result = GetHint(level, group.Key, steps);
                    Debug.Log($"[HintService] GetHint for '{group.Key}' returned {(result != null ? result.Count + " positions" : "null")}");
                    if (result != null) return result;
                }
            }

            Debug.LogWarning($"[HintService] GetNextUnsolvedHint: no unsolved colors found. Grid has {grid.Paths.Count} paths.");
            return null;
        }


      
    }
}
