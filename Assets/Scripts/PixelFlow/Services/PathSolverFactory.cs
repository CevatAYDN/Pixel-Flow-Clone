using System.Collections.Generic;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    public class PathSolverFactory
    {
        private readonly Dictionary<SolverStrategyType, IPathSolverStrategy> _strategies;

        public PathSolverFactory()
        {
            _strategies = new Dictionary<SolverStrategyType, IPathSolverStrategy>
            {
                { SolverStrategyType.StandardDFS, new StandardDFSPathSolverStrategy() },
                { SolverStrategyType.PhaseBased, new PhaseBasedSolverStrategy() },
                { SolverStrategyType.DynamicDifficulty, new DynamicDifficultySolverStrategy() }
            };
        }

        public IPathSolverStrategy GetSolver(SolverStrategyType strategyType)
        {
            if (_strategies.TryGetValue(strategyType, out var strategy))
            {
                return strategy;
            }
            return _strategies[SolverStrategyType.StandardDFS];
        }

        public IPathSolverStrategy GetBestSolverForLevel(LevelData level)
        {
            if (level == null) return GetSolver(SolverStrategyType.StandardDFS);

            if (level.requireFullGridCoverage)
            {
                return GetSolver(SolverStrategyType.PhaseBased);
            }

            if (level.difficultyScore > 40)
            {
                return GetSolver(SolverStrategyType.DynamicDifficulty);
            }

            return GetSolver(SolverStrategyType.StandardDFS);
        }
    }
}
