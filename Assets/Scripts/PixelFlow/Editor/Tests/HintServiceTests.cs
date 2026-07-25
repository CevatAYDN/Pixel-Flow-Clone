using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Commands;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using UnityEngine;
using static PixelFlow.Editor.Tests.GameTestContext;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class HintServiceTests
    {
        private NexusTestContext _ctx;
        private IHintModel _hintModel;
        private IGridModel _grid;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
            _hintModel = _ctx.GetModel<IHintModel>();
            _grid = _ctx.GetModel<IGridModel>();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        [Test]
        public void UseHint_DecrementsHintsRemaining()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            int hintsBefore = _hintModel.HintsRemaining;
            Assert.GreaterOrEqual(hintsBefore, 1);

            _ctx.Dispatch(new RequestHintSignal());
            Assert.AreEqual(hintsBefore - 1, _hintModel.HintsRemaining);
        }

        [Test]
        public void UseHint_IncrementsTotalHintsUsed()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            Assert.AreEqual(0, _hintModel.TotalHintsUsed);

            _ctx.Dispatch(new RequestHintSignal());
            Assert.AreEqual(1, _hintModel.TotalHintsUsed);
        }

        [Test]
        public void UseHint_AppliesPath()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.Dispatch(new RequestHintSignal());
            Assert.Greater(_grid.Paths.Count, 0, "At least one path should exist after hint");
        }

        [Test]
        public void UseHint_RespectsSolvedColors()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            // Draw a partial Red path (2 cells)
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(2, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(3, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(4, 0) });

            int redPathCount = _grid.Paths[ColorType.Red].Count;

            _ctx.Dispatch(new RequestHintSignal());
            Assert.AreEqual(redPathCount, _grid.Paths[ColorType.Red].Count,
                "Solved red path should not be modified by hint");
        }

        [Test]
        public void UseHint_WithNoHintsLeft_DoesNothing()
        {
            var level = CreateTestLevel();
            var testConfig = GameTestContext.CreateTestGameConfig();
            testConfig.DefaultHintCount = 0;
            var ctx = NexusTestHarness.CreateContext(builder =>
            {
                builder.Bind<IPlayerPrefsService, InMemoryPlayerPrefsService>();
                builder.BindInstance(testConfig);
                builder.BindInstance(GameTestContext.CreateTestStorageKeysConfig());
                builder.BindService<IPathService, PathService>();
                builder.BindService<IGameHistoryService, GameHistoryService>();
                builder.Bind<IPathSolver, RuntimePathSolver>();
                builder.Bind<IHintService, HintService>();
                builder.BindService<IVehicleSimulator, VehicleSimulator>();
                builder.BindService<ISaveThrottler, SaveThrottler>();
                builder.BindService<IHapticService, HapticService>();
                var quietLogger = new LoggerService { IsEnabled = false };
                builder.BindInstance<ILoggerService>(quietLogger);
                builder.BindService<ICrisisAdService, CrisisAdService>();
                builder.BindService<IObstacleService, ObstacleService>();
                builder.BindService<ITutorialDriver, TutorialDriver>();
                builder.BindService<IPowerUpService, PowerUpService>();
                builder.BindService<PixelFlow.Services.IAudioService, PixelFlow.Services.AudioService>();
                builder.Bind<IFeedbackService, FeedbackService>();
                builder.Bind<Nexus.Core.Services.IAudioService, StubAudioService>();
                builder.Bind<Nexus.Core.Services.INetworkEconomyValidator, LocalEconomyValidator>();
                builder.Bind<ITimeProvider, UnityTimeProvider>();
                builder.BindService<INexusService, TickService>();
                builder.Bind<ITickService, TickService>();
                builder.BindService<Nexus.Core.Services.IEconomyService, Nexus.Core.Services.EconomyService>();
                builder.BindReactiveModel<IDailyCrisisModel, DailyCrisisModel>();
                builder.BindReactiveModel<IGridModel, GridModel>();
                builder.BindReactiveModel<ILevelModel, LevelModel>();
                builder.BindReactiveModel<IProgressModel, ProgressModel>();
                builder.BindReactiveModel<IGameStateModel, GameStateModel>();
                builder.BindReactiveModel<IGameSessionModel, GameSessionModel>();
                builder.BindReactiveModel<IHintModel, HintModel>();
                builder.BindReactiveModel<ISettingsModel, SettingsModel>();
                builder.BindReactiveModel<ISoundModel, SoundModel>();
                builder.BindReactiveModel<ITutorialModel, TutorialModel>();
                builder.BindReactiveModel<IInventoryModel, InventoryModel>();
                builder.Bind<ILevelProgressionService, LevelProgressionService>();
                builder.BindInstance<IRecoveryStrategy>(new DefaultRecoveryStrategy(maxRetries: 3));
                builder.Bind<ICameraProvider, StubCameraProvider>();
                builder.Bind<IGridViewProvider, StubGridViewProvider>();
                builder.BindService<ILevelLoaderService, LevelLoaderService>();
                builder.BindSignal<InputInteractionSignal>().To<ProcessInputCommand>();
                builder.BindSignal<CheckWinConditionSignal>().To<CheckWinConditionCommand>();
                builder.BindSignal<LoadLevelSignal>().To<LoadLevelCommand>();
                builder.BindSignal<RequestHintSignal>().To<UseHintCommand>();
                builder.BindSignal<ChangeThemeSignal>().To<ChangeThemeCommand>();
                builder.BindSignal<LevelCompletedSignal>().To<SaveProgressCommand>();
                builder.BindSignal<StartSimulationSignal>().To<StartSimulationCommand>();
                builder.BindSignal<PauseSimulationSignal>().To<PauseSimulationCommand>();
                builder.BindSignal<UndoSignal>().To<UndoCommand>();
                builder.BindSignal<RedoSignal>().To<RedoCommand>();
            });
            
            var hintModel = ctx.GetModel<IHintModel>();
            var grid = ctx.GetModel<IGridModel>();
            
            ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            
            // Hints should be 0 from config
            Assert.AreEqual(0, hintModel.HintsRemaining);
            
            // Requesting hint when at 0 should not auto-grant and should do nothing
            ctx.Dispatch(new RequestHintSignal());
            Assert.AreEqual(0, hintModel.HintsRemaining, "Hint count should stay 0 when DefaultHintCount=0");
            
            ctx.Dispose();
        }
    }
}
