using System.Collections.Generic;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    public class StandardDFSPathSolverStrategy : IPathSolverStrategy
    {
        private readonly RuntimePathSolver _internalSolver = new RuntimePathSolver();

        public SolverStrategyType StrategyType => SolverStrategyType.StandardDFS;

        public bool Solve(LevelData level, out Dictionary<ColorType, List<Vector2Int>> solutions)
        {
            return _internalSolver.Solve(level, out solutions);
        }

        public bool SolvePartial(LevelData level, ColorType color, int steps, out List<Vector2Int> partialPath)
        {
            return _internalSolver.SolvePartial(level, color, steps, out partialPath);
        }
    }
}
