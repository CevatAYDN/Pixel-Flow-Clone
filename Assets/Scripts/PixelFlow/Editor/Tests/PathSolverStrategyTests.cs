using System.Collections.Generic;
using NUnit.Framework;
using PixelFlow.Data;
using PixelFlow.Services;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class PathSolverStrategyTests
    {
        private PathSolverFactory _factory;
        private GameConfig _testConfig;

        [SetUp]
        public void SetUp()
        {
            _testConfig = ScriptableObject.CreateInstance<GameConfig>();
            _testConfig.HighDifficultySolverThreshold = 100;
            _factory = new PathSolverFactory(_testConfig);
        }

        [Test]
        public void Factory_ReturnsDefaultStrategy_WhenStandardRequested()
        {
            var strategy = _factory.GetSolver(SolverStrategyType.StandardDFS);
            Assert.IsNotNull(strategy);
            Assert.AreEqual(SolverStrategyType.StandardDFS, strategy.StrategyType);
        }

        [Test]
        public void Factory_SelectsPhaseBasedSolver_WhenCoverageRequired()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.requireFullGridCoverage = true;

            var strategy = _factory.GetBestSolverForLevel(level);
            Assert.IsNotNull(strategy);
            Assert.AreEqual(SolverStrategyType.PhaseBased, strategy.StrategyType);
        }

        [Test]
        public void StandardDFS_SolvesSimpleLevelSuccessfully()
        {
            var strategy = _factory.GetSolver(SolverStrategyType.StandardDFS);
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 4;
            level.height = 4;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red, isSource = true, pairIndex = 0 },
                new GridNode { position = new Vector2Int(0, 3), color = ColorType.Red, isSource = false, pairIndex = 1 }
            };

            bool solved = strategy.Solve(level, out var solutions);
            Assert.IsTrue(solved);
            Assert.IsNotNull(solutions);
            Assert.IsTrue(solutions.ContainsKey(ColorType.Red));
            Assert.IsTrue(solutions[ColorType.Red].Count > 0);
        }
    }
}
