using NUnit.Framework;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class DailyCrisisAndObstacleTests
    {
        [Test]
        public void ScoreCalculator_FollowsGddStarRules()
        {
            // 0 viaducts used -> 3 stars (Perfect Flow)
            var (_, stars0) = ScoreCalculator.Calculate(5, 5, 10f, 0, 3, 0);
            Assert.AreEqual(3, stars0, "0 viaducts must yield 3 stars per GDD §2.8.");

            // 1 or 2 viaducts used -> 2 stars
            var (_, stars1) = ScoreCalculator.Calculate(5, 5, 10f, 0, 3, 1);
            Assert.AreEqual(2, stars1, "1 viaduct must yield 2 stars per GDD §2.8.");

            var (_, stars2) = ScoreCalculator.Calculate(5, 5, 10f, 0, 3, 2);
            Assert.AreEqual(2, stars2, "2 viaducts must yield 2 stars per GDD §2.8.");

            // 3+ viaducts used -> 1 star
            var (_, stars3) = ScoreCalculator.Calculate(5, 5, 10f, 0, 3, 3);
            Assert.AreEqual(1, stars3, "3+ viaducts must yield 1 star per GDD §2.8.");
        }

        [Test]
        public void ProceduralLevelGenerator_GeneratesMultiCellObstacles()
        {
            var solver = new RuntimePathSolver();
            var generator = new ProceduralLevelGenerator(solver, 42);

            var param = DifficultyParams.Phase3_Default;
            param.gridWidth = 10;
            param.gridHeight = 10;
            param.colorCount = 3;
            param.requireFullGridCoverage = false; // Matematiksel tıkanıklığı önlemek için false yapıyoruz
            param.obstaclesEnabled = true;

            var level = generator.Generate(param);
            Assert.IsNotNull(level, "Procedural generator must produce a valid level.");
            Assert.IsTrue(level.obstacles.Count > 0, "Level must contain obstacles.");
        }

        [Test]
        public void DailyCrisisModel_TracksCompletionAndStreak()
        {
            var prefs = new InMemoryPlayerPrefsService();
            var model = new DailyCrisisModel { PlayerPrefsService = prefs };
            model.LoadState();

            Assert.IsFalse(model.IsCrisisCompleted(0));
            Assert.AreEqual(0, model.BadgesEarned);

            model.CompleteCrisis(0);
            Assert.IsTrue(model.IsCrisisCompleted(0));
            Assert.AreEqual(1, model.BadgesEarned);

            model.CompleteCrisis(1);
            model.CompleteCrisis(2);

            Assert.AreEqual(1, model.StreakCount, "Completing all 3 daily challenges increments streak.");
        }

        [Test]
        public void LocalizationService_RetrievesKeysAndFormatsRTL()
        {
            var prefs = new InMemoryPlayerPrefsService();
            var loc = new LocalizationService { PlayerPrefsService = prefs };
            loc.InitializeAsync(default);

            Assert.AreEqual("Neon Transit", loc.GetString("app_name"));
            Assert.AreEqual("Undo", loc.GetString("btn_undo"));

            loc.SetLanguage("tr");
            Assert.AreEqual("Geri Al", loc.GetString("btn_undo"));

            loc.SetLanguage("ar");
            string reversed = loc.FormatRTLIfNeeded("ABC");
            Assert.AreEqual("CBA", reversed, "RTL formatter must reverse character order for Arabic.");
        }

        [Test]
        public void PathIntersection_AllowsCrossingAndSetsUnderOverColors()
        {
            var grid = new GridModel();
            grid.Initialize(5, 5);

            var cell = grid.Grid[2, 2];
            cell.State = CellState.Path;
            cell.AddPathColor(ColorType.Blue);
            cell.UnderColor = ColorType.Blue;

            // Second color crossing same cell
            cell.AddPathColor(ColorType.Red);
            cell.OverColor = ColorType.Red;

            Assert.AreEqual(2, cell.PathColorCount);
            Assert.AreEqual(ColorType.Blue, cell.UnderColor);
            Assert.AreEqual(ColorType.Red, cell.OverColor);
            Assert.IsFalse(cell.HasViaduct, "Cell does not have viaduct yet, soft warning should trigger.");
        }
    }
}
