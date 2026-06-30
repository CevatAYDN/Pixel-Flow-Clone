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
                    return GetHint(level, group.Key, steps);
                }

                var nodes = group.ToList();
                if (nodes.Count < 2) continue;
                var path = grid.Paths[group.Key];
                var startPos = path[0];
                var endPos = path[path.Count - 1];
                var node1 = nodes[0].position;
                var node2 = nodes[1].position;
                bool solved = (startPos == node1 && endPos == node2) || (startPos == node2 && endPos == node1);
                if (!solved)
                {
                    return GetHint(level, group.Key, steps);
                }
            }

            return null;
        }


      
    }
}
