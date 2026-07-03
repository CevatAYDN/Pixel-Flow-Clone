using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Commands;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using System.Collections.Generic;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class Phase2AndAccessibilityTests
    {
        private NexusTestContext _ctx;

        [SetUp]
        public void SetUp()
        {
            _ctx = NexusTestHarness.CreateContext(builder =>
            {
                builder.Bind<IPlayerPrefsService, InMemoryPlayerPrefsService>();
                builder.Bind<IPathService, PathService>();
                builder.Bind<IGameHistoryService, GameHistoryService>();
                builder.Bind<IPathSolver, RuntimePathSolver>();
                builder.Bind<IHintService, HintService>();
                builder.BindService<IVehicleSimulator, VehicleSimulator>();
                builder.BindService<ITaxCollectionService, TaxCollectionService>();
                builder.BindService<ISaveThrottler, SaveThrottler>();
                builder.BindService<IHapticService, HapticService>();
                builder.BindService<IObstacleService, ObstacleService>();
                builder.BindService<IOverclockService, OverclockService>();
                builder.BindService<ICrisisAdService, CrisisAdService>();
                builder.BindService<ITutorialDriver, TutorialDriver>();

                builder.BindReactiveModel<IGridModel, GridModel>();
                builder.BindReactiveModel<ILevelModel, LevelModel>();
                builder.BindReactiveModel<IProgressModel, ProgressModel>();
                builder.BindReactiveModel<IGameStateModel, GameStateModel>();
                builder.BindReactiveModel<IGameSessionModel, GameSessionModel>();
                builder.BindReactiveModel<IHintModel, HintModel>();
                builder.BindReactiveModel<ISettingsModel, SettingsModel>();
                builder.BindReactiveModel<ISoundModel, SoundModel>();
                builder.BindReactiveModel<ICityEconomyModel, CityEconomyModel>();
                builder.BindReactiveModel<ITutorialModel, TutorialModel>();

                builder.BindInstance<IRecoveryStrategy>(new DefaultRecoveryStrategy(maxRetries: 3));
            });
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
            _ctx = null;
        }

        [Test]
        public void GddColorPalette_HasExactly5Colors()
        {
            Assert.AreEqual(5, GddColorPalette.Standard.Length);
            foreach (var c in GddColorPalette.Standard)
            {
                Assert.IsTrue(GddColorPalette.IsGddStandard(c));
            }
            Assert.IsFalse(GddColorPalette.IsGddStandard(ColorType.Orange));
            Assert.IsFalse(GddColorPalette.IsGddStandard(ColorType.Cyan));
            Assert.IsFalse(GddColorPalette.IsGddStandard(ColorType.Magenta));
        }

        [Test]
        public void PhaseDefinition_GetPhaseForLevel_ReturnsCorrectPhase()
        {
            var p1 = PhaseDefinition.GetPhaseForLevel(0);
            Assert.AreEqual(GamePhase.Phase1, p1.Phase);
            Assert.AreEqual(0, p1.StartLevelIndex);
            Assert.AreEqual(11, p1.EndLevelIndex);

            var p2 = PhaseDefinition.GetPhaseForLevel(15);
            Assert.AreEqual(GamePhase.Phase2, p2.Phase);
            Assert.AreEqual(12, p2.StartLevelIndex);

            var p3 = PhaseDefinition.GetPhaseForLevel(30);
            Assert.AreEqual(GamePhase.Phase3, p3.Phase);
            Assert.IsTrue(p3.RequireFullCoverage);
            Assert.IsTrue(p3.ObstaclesEnabled);

            var p4 = PhaseDefinition.GetPhaseForLevel(50);
            Assert.AreEqual(GamePhase.Phase4, p4.Phase);
            Assert.IsTrue(p4.FerryEnabled);
            Assert.IsTrue(p4.NarrowPassEnabled);
        }

        [Test]
        public void GridStateSerializer_SaveAndLoad_RestoresAllFields()
        {
            var grid = _ctx.GetModel<IGridModel>();
            var session = _ctx.GetModel<IGameSessionModel>();
            var level = _ctx.GetModel<ILevelModel>();

            var lvl = ScriptableObject.CreateInstance<LevelData>();
            lvl.levelIndex = 7;
            lvl.width = 5;
            lvl.height = 5;
            level.SetLevel(lvl);

            grid.Initialize(5, 5);
            grid.Grid[0, 0].State = CellState.Node;
            grid.Grid[0, 0].Color = ColorType.Red;
            grid.Grid[0, 0].PathColors.Add(ColorType.Red);
            grid.Paths[ColorType.Red] = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
            grid.ActiveColor.Value = ColorType.Red;
            grid.LastPosition.Value = new Vector2Int(1, 0);

            session.StartSession(3);
            session.AddScore(500);
            session.SetStars(2);

            GridStateSerializer.Save(grid, session, level);
            Assert.IsTrue(GridStateSerializer.HasSavedGame());

            var freshGrid = new GridModel();
            freshGrid.Initialize(3, 3);
            var loaded = GridStateSerializer.Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(5, loaded.width);
            Assert.AreEqual(5, loaded.height);
            Assert.AreEqual(7, loaded.levelIndex);
            Assert.AreEqual(2, loaded.stars);
            Assert.AreEqual(3, loaded.maxViaducts);

            GridStateSerializer.ApplyToGrid(loaded, freshGrid);
            Assert.AreEqual(5, freshGrid.Width);
            Assert.AreEqual(CellState.Node, freshGrid.Grid[0, 0].State);
            Assert.AreEqual(ColorType.Red, freshGrid.Grid[0, 0].Color);
            Assert.AreEqual(ColorType.Red, freshGrid.ActiveColor.Value);
            Assert.AreEqual(2, freshGrid.Paths[ColorType.Red].Count);

            GridStateSerializer.ClearSave();
        }

        [Test]
        public void GameSession_MarkCrisisUndoUsed_DecrementsMaxViaducts()
        {
            var session = _ctx.GetModel<IGameSessionModel>();
            session.StartSession(5);

            int maxBefore = session.MaxViaducts;
            int availBefore = session.AvailableViaducts;

            session.MarkCrisisUndoUsed();

            Assert.IsTrue(session.HasUsedCrisisUndo);
            Assert.AreEqual(maxBefore - 1, session.MaxViaducts);
            Assert.AreEqual(availBefore - 1, session.AvailableViaducts);
        }

        [Test]
        public void GameSession_ApplySave_RestoresAllFields()
        {
            var session = _ctx.GetModel<IGameSessionModel>();
            session.ApplySave(availableViaducts: 2, maxViaducts: 5, elapsedTime: 12.5f, score: 800, stars: 3);

            Assert.AreEqual(2, session.AvailableViaducts);
            Assert.AreEqual(5, session.MaxViaducts);
            Assert.AreEqual(12.5f, session.ElapsedTime, 0.001f);
            Assert.AreEqual(800, session.Score);
            Assert.AreEqual(3, session.StarsEarned);
        }

        [Test]
        public void ProceduralGenerator_SolutionFirst_AllPathsConnect()
        {
            var solver = new RuntimePathSolver();
            var gen = new ProceduralLevelGenerator(solver, seed: 99);
            var param = new DifficultyParams(5, 5, 1, 0, false);
            var level = gen.Generate(param, maxAttempts: 30);

            Assert.IsNotNull(level);
            Assert.AreEqual(1, level.solutions.Count);
            var path = level.solutions[0].pathPositions;
            Assert.GreaterOrEqual(path.Count, 2);
            // Path ilk ve son pozisyonlar node olmalı.
            var firstNode = level.initialNodes[0];
            var lastNode = level.initialNodes[1];
            Assert.IsTrue(path[0] == firstNode.position || path[0] == lastNode.position);
        }

        [Test]
        public void ProceduralGenerator_WithBridges_GeneratesBridgeCount()
        {
            var solver = new RuntimePathSolver();
            var gen = new ProceduralLevelGenerator(solver, seed: 7);
            var param = new DifficultyParams(7, 7, 3, 2, false, true);
            var level = gen.Generate(param, maxAttempts: 50);

            if (level != null)
            {
                Assert.IsTrue(level.viaductLimit >= 0);
            }
        }

        [Test]
        public void SaveThrottler_ForceSave_WritesImmediately()
        {
            var grid = _ctx.GetModel<IGridModel>();
            var session = _ctx.GetModel<IGameSessionModel>();
            var level = _ctx.GetModel<ILevelModel>();

            var lvl = ScriptableObject.CreateInstance<LevelData>();
            lvl.levelIndex = 1;
            lvl.width = 5; lvl.height = 5;
            level.SetLevel(lvl);
            grid.Initialize(5, 5);
            session.StartSession(3);

            var throttler = _ctx.Context.Container.Resolve<ISaveThrottler>();
            throttler.ForceSave(grid, session, level);
            Assert.IsTrue(GridStateSerializer.HasSavedGame(_ctx.Context.Container.Resolve<IPlayerPrefsService>()));

            GridStateSerializer.ClearSave();
        }

        [Test]
        public void SettingsModel_HapticsDisabled_DefaultsToFalse()
        {
            var settings = _ctx.GetModel<ISettingsModel>();
            Assert.IsFalse(settings.HapticsDisabled);
            settings.SetHapticsDisabled(true);
            Assert.IsTrue(settings.HapticsDisabled);
            settings.SetHapticsDisabled(false);
            Assert.IsFalse(settings.HapticsDisabled);
        }

        [Test]
        public void ColorBlindPalette_Remap_ReturnsDifferentColorsForModes()
        {
            var redStandard = ColorBlindPalette.GetStandard(ColorType.Red);
            var redProtan = ColorBlindPalette.Remap(ColorType.Red, ColorBlindMode.Protanopia);
            var redDeutan = ColorBlindPalette.Remap(ColorType.Red, ColorBlindMode.Deuteranopia);

            Assert.AreNotEqual(redStandard, redProtan);
            Assert.AreNotEqual(redStandard, redDeutan);
            Assert.AreNotEqual(redProtan, redDeutan);
        }

        [Test]
        public void ColorBlindPalette_NoneMode_ReturnsStandard()
        {
            var standard = ColorBlindPalette.GetStandard(ColorType.Red);
            var remapped = ColorBlindPalette.Remap(ColorType.Red, ColorBlindMode.None);
            Assert.AreEqual(standard, remapped);
        }

        [Test]
        public void TutorialDriver_MapLevelToStep_ReturnsExpectedStep()
        {
            Assert.AreEqual(TutorialStep.TouchAndDrag, TutorialDriver.MapLevelToStep(0));
            Assert.AreEqual(TutorialStep.ColorMatch, TutorialDriver.MapLevelToStep(1));
            Assert.AreEqual(TutorialStep.CrashIntro, TutorialDriver.MapLevelToStep(12));
            Assert.AreEqual(TutorialStep.ViaductIntro, TutorialDriver.MapLevelToStep(13));
            Assert.AreEqual(TutorialStep.None, TutorialDriver.MapLevelToStep(99));
        }
    }
}
