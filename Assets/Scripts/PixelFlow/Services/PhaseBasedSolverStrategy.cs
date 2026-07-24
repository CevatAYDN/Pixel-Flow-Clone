using System.Collections.Generic;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    public class PhaseBasedSolverStrategy : IPathSolverStrategy
    {
        private readonly StandardDFSPathSolverStrategy _baseStrategy = new StandardDFSPathSolverStrategy();

        public SolverStrategyType StrategyType => SolverStrategyType.PhaseBased;

        public bool Solve(LevelData level, out Dictionary<ColorType, List<Vector2Int>> solutions)
        {
            if (!_baseStrategy.Solve(level, out solutions))
            {
                return false;
            }

            // Verify phase-specific constraints
            if (level.requireFullGridCoverage && solutions != null)
            {
                int totalCells = level.width * level.height;
                var coveredPositions = new HashSet<Vector2Int>();
                foreach (var kvp in solutions)
                {
                    foreach (var pos in kvp.Value)
                    {
                        coveredPositions.Add(pos);
                    }
                }

                // Add obstacles and bridge nodes to covered positions count
                if (level.obstacles != null)
                {
                    foreach (var obs in level.obstacles)
                    {
                        coveredPositions.Add(obs.position);
                    }
                }

                if (coveredPositions.Count < totalCells)
                {
                    // Solution exists, but does not meet FullGridCoverage objective constraint
                    return false;
                }
            }

            return true;
        }

        public bool SolvePartial(LevelData level, ColorType color, int steps, out List<Vector2Int> partialPath)
        {
            return _baseStrategy.SolvePartial(level, color, steps, out partialPath);
        }
    }
}
