using NUnit.Framework;
using PixelFlow.Data;
using PixelFlow.Services;
using UnityEngine;
using static PixelFlow.Editor.Tests.GameTestContext;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class ProceduralLevelGeneratorTests
    {
        [Test]
        public void ProceduralLevelGenerator_Generate_ProducesSolvableLevel()
        {
            var solver = CreateTestRuntimePathSolver();
            var generator = new ProceduralLevelGenerator(solver, seed: 12345);

            var diffParams = new DifficultyParams
            {
                gridWidth = 5,
                gridHeight = 5,
                colorCount = 2,
                bridgeCount = 0,
                requireFullGridCoverage = false
            };

            var level = generator.Generate(diffParams, maxAttempts: 20);

            Assert.IsNotNull(level);
            Assert.AreEqual(5, level.width);
            Assert.AreEqual(5, level.height);
            Assert.IsTrue(level.initialNodes.Count >= 4);

            // Solver check: zero softlock guarantee
            bool isSolvable = solver.Solve(level, out var solutions);
            Assert.IsTrue(isSolvable);
            Assert.IsNotNull(solutions);
        }

        [Test]
        public void ProceduralLevelGenerator_CalculateDifficultyScore_MatchesFormula()
        {
            var solver = CreateTestRuntimePathSolver();
            var generator = new ProceduralLevelGenerator(solver);
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 5;
            level.height = 5;

            var diffParams = new DifficultyParams
            {
                gridWidth = 5,
                gridHeight = 5,
                colorCount = 3,
                bridgeCount = 1
            };

            int score = generator.CalculateDifficultyScore(level, diffParams);

            // Formula: (colors 3 * 10) + (intersections 0 * 5) + (obstacles 0 * 3) - (viaductLimit 1 * 4) = 26
            Assert.AreEqual(26, score);
        }
    }
}
