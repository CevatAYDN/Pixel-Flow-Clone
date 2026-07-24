using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class LevelLoaderAndProgressionTests
    {
        [Test]
        public void LevelLoaderService_LoadLevel_InitializesGridAndSession()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var loader = ctx.GetModel<ILevelLoaderService>();
            var grid = ctx.GetModel<IGridModel>();
            var levelModel = ctx.GetModel<ILevelModel>();
            var stateModel = ctx.GetModel<IGameStateModel>();

            var testLevel = GameTestContext.CreateTestLevel(5);

            loader.LoadLevel(new LoadLevelSignal { LevelToLoad = testLevel });

            Assert.IsNotNull(levelModel.CurrentLevel);
            Assert.AreEqual(5, levelModel.CurrentLevel.levelIndex);
            Assert.AreEqual(5, grid.Width);
            Assert.AreEqual(5, grid.Height);
            Assert.AreEqual(GameState.Playing, stateModel.CurrentState);
        }

        [Test]
        public void LevelProgressionService_GetDifficultyForLevel_ReturnsValidParams()
        {
            var solver = new RuntimePathSolver();
            var service = new LevelProgressionService(solver);

            var diff1 = service.GetDifficultyForLevel(0);
            Assert.IsTrue(diff1.gridWidth >= 3);
            Assert.IsTrue(diff1.colorCount >= 1);

            var diff20 = service.GetDifficultyForLevel(20);
            Assert.IsTrue(diff20.gridWidth >= diff1.gridWidth || diff20.colorCount >= diff1.colorCount);
        }

        [Test]
        public void LevelProgressionService_GetOrGenerateLevel_ReturnsLevelData()
        {
            var solver = new RuntimePathSolver();
            var service = new LevelProgressionService(solver);

            var level = service.GetOrGenerateLevel(0);
            Assert.IsNotNull(level);
            Assert.IsTrue(level.width >= 3);
            Assert.IsTrue(level.height >= 3);
        }
    }
}
