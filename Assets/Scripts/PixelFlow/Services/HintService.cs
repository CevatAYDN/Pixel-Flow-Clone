using System.Collections.Generic;
using System.Linq;
using PixelFlow.Data;
using PixelFlow.Models;
using Nexus.Core;
using Nexus.Core.Services;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PixelFlow.Services
{
    public sealed class HintService : IHintService, INexusService
    {
        private readonly IPathSolver _solver;
        [Inject] public ILoggerService LoggerService { get; set; }

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
            {
                int count = Mathf.Min(steps, result.Count - 1);
                var hint = new List<Vector2Int>(count);
                for (int i = 0; i < count; i++)
                    hint.Add(result[i + 1]);
                return hint;
            }

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
                    LoggerService?.Log($"[HintService] Unsolved color '{group.Key}' found. Requesting hint (steps={steps})...");
                    var result = GetHint(level, group.Key, steps);
                    LoggerService?.Log($"[HintService] GetHint for '{group.Key}' returned {(result != null ? result.Count + " positions" : "null")}");
                    if (result != null) return result;
                }
            }

            LoggerService?.LogWarning($"[HintService] GetNextUnsolvedHint: no unsolved colors found. Grid has {grid.Paths.Count} paths.");
            return null;
        }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
