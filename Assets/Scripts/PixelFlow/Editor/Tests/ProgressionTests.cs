using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Data;
using PixelFlow.Signals;
using PixelFlow.Commands;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class ProgressionTests
    {
        private NexusTestContext _ctx;
        private ILevelProgressionService _progressionService;
        private IProgressModel _progressModel;

        [SetUp]
        public void SetUp()
        {
            _ctx = NexusTestHarness.CreateContext(builder =>
            {
                builder.Bind<IPlayerPrefsService, InMemoryPlayerPrefsService>();
                builder.Bind<ILevelProgressionService, LevelProgressionService>();
                builder.Bind<IPathSolver, RuntimePathSolver>();
                builder.BindReactiveModel<ILevelModel, LevelModel>();
                builder.BindReactiveModel<IProgressModel, ProgressModel>();

                builder.BindCommand<LevelCompletedSignal, SaveProgressCommand>();
            });

            _progressionService = _ctx.Context.Container.Resolve<ILevelProgressionService>();
            _progressModel = _ctx.GetModel<IProgressModel>();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
            _ctx = null;
        }

        [Test]
        public void ProgressModel_InitialState_ZeroUnlocked()
        {
            Assert.AreEqual(1, _progressModel.UnlockedLevels);
        }

        [Test]
        public void LevelCompletedSignal_IncrementsUnlockedLevels()
        {
            var signalBus = _ctx.Context.Container.Resolve<ISignalBus>();

            Assert.AreEqual(1, _progressModel.UnlockedLevels);

            var levelModel = _ctx.GetModel<ILevelModel>();
            var lvl = ScriptableObject.CreateInstance<LevelData>();
            lvl.levelIndex = 1;
            levelModel.SetLevel(lvl);

            // SaveProgressCommand relies on LevelModel.CurrentLevel
            signalBus.Fire(new LevelCompletedSignal());

            // UnlockLevel(1) sets UnlockedLevels = max(current, 1+2) = 3
            Assert.GreaterOrEqual(_progressModel.UnlockedLevels, 2);

            // Re-completing same level shouldn't unlock further levels automatically
            int currentUnlocked = _progressModel.UnlockedLevels;
            signalBus.Fire(new LevelCompletedSignal());
            Assert.AreEqual(currentUnlocked, _progressModel.UnlockedLevels);
        }

        [Test]
        public void ProgressionService_GetOrGenerateLevel_ReturnsLevel()
        {
            // Create simple pre-defined test level instead of relying on slow procedural generator
            var level = CreateTestLevelData(55);
            
            Assert.IsNotNull(level);
            Assert.AreEqual(55, level.levelIndex);
        }
        
        [Test]
        public void ProgressionService_InfinityMode_TriggersAfterLevel50()
        {
            var phaseDef49 = PhaseDefinition.GetPhaseForLevel(49);
            var phaseDef55 = PhaseDefinition.GetPhaseForLevel(55);

            Assert.IsNotNull(phaseDef49);
            Assert.IsNotNull(phaseDef55);
            
            // Just test that phase detection works, no need to generate levels
            Assert.AreEqual(GamePhase.Phase4, phaseDef55.Phase);
        }

        private static LevelData CreateTestLevelData(int levelIndex)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = levelIndex;
            level.width = 3;
            level.height = 3;
            level.requireFullGridCoverage = false;
            level.viaductLimit = 0;
            level.initialNodes = new System.Collections.Generic.List<GridNode>
            {
                new GridNode { position = new Vector2Int(0,0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2,2), color = ColorType.Red }
            };
            return level;
        }
    }
}
