using System.Collections.Generic;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Services
{
    public class PathSolverFactory
    {
        private readonly Dictionary<SolverStrategyType, IPathSolverStrategy> _strategies;
        private readonly GameConfig _config;

        public PathSolverFactory(GameConfig config)
        {
            _config = config ?? throw new DataValidationException("GameConfig is required for PathSolverFactory!");
            
            _strategies = new Dictionary<SolverStrategyType, IPathSolverStrategy>
            {
                { SolverStrategyType.StandardDFS, new StandardDFSPathSolverStrategy(config) },
                { SolverStrategyType.PhaseBased, new PhaseBasedSolverStrategy(config) },
                { SolverStrategyType.DynamicDifficulty, new DynamicDifficultySolverStrategy(config) }
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

            int threshold = _config.HighDifficultySolverThreshold;

            if (level.difficultyScore > threshold)
            {
                return GetSolver(SolverStrategyType.DynamicDifficulty);
            }

            return GetSolver(SolverStrategyType.StandardDFS);
        }
    }
}
