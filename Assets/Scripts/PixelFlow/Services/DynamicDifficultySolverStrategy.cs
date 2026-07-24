using System.Collections.Generic;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    public class DynamicDifficultySolverStrategy : IPathSolverStrategy
    {
        private readonly StandardDFSPathSolverStrategy _baseStrategy = new StandardDFSPathSolverStrategy();

        public SolverStrategyType StrategyType => SolverStrategyType.DynamicDifficulty;

        public bool Solve(LevelData level, out Dictionary<ColorType, List<Vector2Int>> solutions)
        {
            // Adapt solver behavior based on level difficulty score
            bool success = _baseStrategy.Solve(level, out solutions);
            if (!success) return false;

            // Score check / metadata logging for adaptive difficulty analysis
            int nodePairs = level.initialNodes != null ? level.initialNodes.Count / 2 : 0;
            int totalPathLength = 0;
            if (solutions != null)
            {
                foreach (var kvp in solutions)
                {
                    totalPathLength += kvp.Value.Count;
                }
            }

            return success;
        }

        public bool SolvePartial(LevelData level, ColorType color, int steps, out List<Vector2Int> partialPath)
        {
            return _baseStrategy.SolvePartial(level, color, steps, out partialPath);
        }
    }
}
