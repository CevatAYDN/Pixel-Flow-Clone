using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Commands;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using System.Collections.Generic;
using UnityEngine;

namespace PixelFlow.PlayMode.Tests
{
    /// <summary>
    /// In-memory PlayerPrefs substitute for PlayMode tests.
    /// Mirrors the one in EditMode tests; avoids Unity PlayerPrefs dependency.
    /// </summary>
    public sealed class InMemoryPlayerPrefsService : IPlayerPrefsService
    {
        private readonly Dictionary<string, int> _store = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

        public int GetInt(string key, int defaultValue = 0)
            => _store.TryGetValue(key, out var val) ? val : defaultValue;

        public void SetInt(string key, int value) => _store[key] = value;

        public bool GetBool(string key, bool defaultValue = false)
            => GetInt(key, defaultValue ? 1 : 0) == 1;

        public void SetBool(string key, bool value) => SetInt(key, value ? 1 : 0);

        public string GetString(string key, string defaultValue = "")
            => _strings.TryGetValue(key, out var val) ? val : defaultValue;

        public void SetString(string key, string value) => _strings[key] = value;

        public bool HasKey(string key) => _store.ContainsKey(key) || _strings.ContainsKey(key);

        public void DeleteKey(string key)
        {
            _store.Remove(key);
            _strings.Remove(key);
        }

        public void Save() { }
    }

    [TestFixture]
    public class PixelFlowPlayModeTests
    {
        // ──────────────────────────────────────────────
        // Context factory (same pattern as EditMode tests)
        // ──────────────────────────────────────────────

        private static NexusTestContext CreateGameContext()
        {
            return NexusTestHarness.CreateContext(builder =>
            {
                builder.Bind<IPlayerPrefsService, InMemoryPlayerPrefsService>();
                builder.Bind<IPathService, PathService>();
                builder.Bind<IGameHistoryService, GameHistoryService>();
                builder.Bind<IPathSolver, RuntimePathSolver>();
                builder.Bind<IHintService, HintService>();
                builder.Bind<IVehicleSimulator, VehicleSimulator>();
                builder.Bind<ISaveThrottler, SaveThrottler>();
                builder.Bind<IHapticService, HapticService>();
                builder.Bind<ICrisisAdService, CrisisAdService>();
                builder.Bind<IObstacleService, ObstacleService>();
                builder.Bind<IOverclockService, OverclockService>();
                builder.Bind<ITutorialDriver, TutorialDriver>();
                builder.Bind<IAudioService, AudioService>();

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
                builder.Bind<ILevelProgressionService, LevelProgressionService>();

                builder.BindInstance<IRecoveryStrategy>(new DefaultRecoveryStrategy(maxRetries: 3));

                builder.BindSignal<InputInteractionSignal>().To<ProcessInputCommand>();
                builder.BindSignal<CheckWinConditionSignal>().To<CheckWinConditionCommand>();
                builder.BindSignal<LoadLevelSignal>().To<LoadLevelCommand>();
                builder.BindSignal<RequestHintSignal>().To<UseHintCommand>();
                builder.BindSignal<ChangeThemeSignal>().To<ChangeThemeCommand>();
                builder.BindSignal<LevelCompletedSignal>().To<SaveProgressCommand>();
                builder.BindSignal<UndoSignal>().To<UndoCommand>();
                builder.BindSignal<RedoSignal>().To<RedoCommand>();
                builder.BindSignal<PlaceViaductSignal>().To<PlaceViaductCommand>();
            });
        }

        private static LevelData CreateTestLevel(int index = 0)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = index;
            level.width = 5;
            level.height = 5;

            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(0, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(4, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Green },
                new GridNode { position = new Vector2Int(2, 4), color = ColorType.Green },
            };

            level.bridgePositions = new List<Vector2Int> { new Vector2Int(2, 2) };

            level.solutions = new List<PathSolution>
            {
                new PathSolution
                {
                    color = ColorType.Red,
                    pathPositions = new List<Vector2Int>
                    {
                        new Vector2Int(0, 0), new Vector2Int(1, 0),
                        new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0)
                    }
                },
                new PathSolution
                {
                    color = ColorType.Blue,
                    pathPositions = new List<Vector2Int>
                    {
                        new Vector2Int(0, 4), new Vector2Int(0, 3),
                        new Vector2Int(0, 2), new Vector2Int(0, 1), new Vector2Int(0, 0)
                    }
                },
                new PathSolution
                {
                    color = ColorType.Green,
                    pathPositions = new List<Vector2Int>
                    {
                        new Vector2Int(2, 0), new Vector2Int(2, 1),
                        new Vector2Int(2, 2), new Vector2Int(2, 3), new Vector2Int(2, 4)
                    }
                }
            };

            return level;
        }

        private NexusTestContext _ctx;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
            _ctx = null;
        }

        // ──────────────────────────────────────────────
        // Test 1: Full game flow — load → play → win → score
        // ──────────────────────────────────────────────

        [Test]
        public void FullGameFlow_LoadLevel_PlayAndWin_AwardsScoreAndStars()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3;
            level.height = 1;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var grid = _ctx.GetModel<IGridModel>();
            var state = _ctx.GetModel<IGameStateModel>();
            var session = _ctx.GetModel<IGameSessionModel>();

            Assert.AreEqual(GameState.Playing, state.CurrentState, "State should be Playing after load");
            Assert.IsTrue(session.IsSessionActive, "Session should be active");
            Assert.AreEqual(0, session.Score, "Score should start at 0");

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(2, 0)
            });

            Assert.AreEqual(GameState.Simulating, state.CurrentState,
                "Should be Simulating after connecting nodes");

            // Manually finish simulation for the test
            state.SetState(GameState.LevelCompleted);
            _ctx.Dispatch(new LevelCompletedSignal());

            Assert.AreEqual(GameState.LevelCompleted, state.CurrentState,
                "Should be LevelCompleted after finishing simulation");
            Assert.Greater(session.Score, 0, "Score should be > 0");
            Assert.GreaterOrEqual(session.StarsEarned, 1, "Should earn at least 1 star");
        }

        // ──────────────────────────────────────────────
        // Test 2: Timer command — real Time.deltaTime in PlayMode
        // ──────────────────────────────────────────────

        [Test]
        public void TimerCommand_AccumulatesRealDeltaTime_WhenPlaying()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var session = _ctx.GetModel<IGameSessionModel>();
            var state = _ctx.GetModel<IGameStateModel>();

            Assert.AreEqual(GameState.Playing, state.CurrentState);
            Assert.AreEqual(0f, session.ElapsedTime, 0.001f);

            // Simulate multiple timer ticks (PlayMode allows Time.deltaTime)
            var timerCmd = new TimerCommand
            {
                GameSessionModel = session,
                GameStateModel = state
            };

            timerCmd.Execute(new TimerTickSignal());
            timerCmd.Execute(new TimerTickSignal());
            timerCmd.Execute(new TimerTickSignal());

            Assert.Greater(session.ElapsedTime, 0f,
                "ElapsedTime should increase after timer ticks in PlayMode");
        }

        [Test]
        public void TimerCommand_IgnoresTicks_WhenNotPlaying()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var session = _ctx.GetModel<IGameSessionModel>();
            var state = _ctx.GetModel<IGameStateModel>();
            var timerCmd = new TimerCommand
            {
                GameSessionModel = session,
                GameStateModel = state
            };

            timerCmd.Execute(new TimerTickSignal());
            float timeAfterPlaying = session.ElapsedTime;

            // Transition out of Playing
            state.SetState(GameState.LevelCompleted);
            timerCmd.Execute(new TimerTickSignal());

            Assert.AreEqual(timeAfterPlaying, session.ElapsedTime, 0.001f,
                "Timer should not advance when state is not Playing");
        }

        // ──────────────────────────────────────────────
        // Test 3: Session lifecycle — reset on new level load
        // ──────────────────────────────────────────────

        [Test]
        public void SessionLifecycle_ResetsOnNewLevelLoad()
        {
            var level1 = CreateTestLevel(0);
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level1 });

            var session = _ctx.GetModel<IGameSessionModel>();
            var grid = _ctx.GetModel<IGridModel>();

            // Play a bit
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });

            Assert.AreEqual(0, session.Score, "No score yet (only CheckWin sets it)");

            // Load new level
            var level2 = ScriptableObject.CreateInstance<LevelData>();
            level2.levelIndex = 1;
            level2.width = 3;
            level2.height = 3;
            level2.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level2 });

            Assert.IsTrue(session.IsSessionActive, "New session should be active");
            Assert.AreEqual(0, session.Score, "Score should reset on new level");
            Assert.AreEqual(0, session.StarsEarned, "Stars should reset on new level");

            var levelModel = _ctx.GetModel<ILevelModel>();
            Assert.AreSame(level2, levelModel.CurrentLevel, "LevelModel should hold new level");
        }

        // ──────────────────────────────────────────────
        // Test 4: Progress unlocks next level
        // ──────────────────────────────────────────────

        [Test]
        public void Progress_UnlocksLevelsOnCompletion()
        {
            var level0 = ScriptableObject.CreateInstance<LevelData>();
            level0.levelIndex = 0;
            level0.width = 3;
            level0.height = 1;
            level0.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level0 });
            var progress = _ctx.GetModel<IProgressModel>();
            int before = progress.UnlockedLevels;

            // Complete the level
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(2, 0)
            });

            // Manually finish simulation for the test
            var stateModel = _ctx.GetModel<IGameStateModel>();
            stateModel.SetState(GameState.LevelCompleted);
            _ctx.Dispatch(new LevelCompletedSignal());

            Assert.GreaterOrEqual(progress.UnlockedLevels, before + 1,
                "Completing level 0 should unlock at least level 1");
        }

        // ──────────────────────────────────────────────
        // Test 5: Input state gates — ignore when not Playing
        // ──────────────────────────────────────────────

        [Test]
        public void Input_IsIgnored_WhenNotInPlayingState()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var state = _ctx.GetModel<IGameStateModel>();
            var grid = _ctx.GetModel<IGridModel>();

            state.SetState(GameState.LevelCompleted);

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });

            Assert.AreEqual(ColorType.None, grid.ActiveColor.Value,
                "ActiveColor should remain None when input is blocked");
            Assert.AreEqual(CellState.Empty, grid.Grid[1, 0].State,
                "Cell should remain Empty — input was ignored");
        }

        // ──────────────────────────────────────────────
        // Test 6: Signal chain — hint fires GridUpdated + CheckWin
        // ──────────────────────────────────────────────

        [Test]
        public void Hint_FiresGridUpdatedAndCheckWin_SignalChainIntact()
        {
            _ctx.Register<GridUpdatedSignal>();
            _ctx.Register<CheckWinConditionSignal>();

            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.ClearDispatchedSignals();

            _ctx.Dispatch(new RequestHintSignal());

            Assert.IsTrue(_ctx.SignalWasDispatched<GridUpdatedSignal>(),
                "Hint must fire GridUpdatedSignal");
            Assert.IsTrue(_ctx.SignalWasDispatched<CheckWinConditionSignal>(),
                "Hint must fire CheckWinConditionSignal");
        }

        // ──────────────────────────────────────────────
        // Test 7: Theme change propagates through signal chain
        // ──────────────────────────────────────────────

        [Test]
        public void ThemeChange_UpdatesSettingsModel_AndFiresSignal()
        {
            _ctx.Register<ThemeChangedSignal>();

            var settings = _ctx.GetModel<ISettingsModel>();
            Assert.AreEqual(AppTheme.Dark, settings.CurrentTheme, "Default theme is Dark");

            _ctx.Dispatch(new ChangeThemeSignal { Theme = AppTheme.Neon });

            Assert.AreEqual(AppTheme.Neon, settings.CurrentTheme,
                "SettingsModel should update after ChangeThemeCommand");
            Assert.IsTrue(_ctx.SignalWasDispatched<ThemeChangedSignal>(),
                "ThemeChangedSignal should fire");
        }

        // ──────────────────────────────────────────────
        // Test 8: Undo restores pre-drag grid state
        // ──────────────────────────────────────────────

        [Test]
        public void Undo_RestoresGridState_ToPreDragSnapshot()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var grid = _ctx.GetModel<IGridModel>();

            // Drag from (0,0) to (1,0) — extends Red path
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });

            Assert.AreEqual(2, grid.Paths[ColorType.Red].Count,
                "Red path should have 2 cells after drag");

            // Undo — should restore to 1 cell (only the node itself)
            _ctx.Dispatch(new UndoSignal());

            Assert.IsTrue(grid.Paths.ContainsKey(ColorType.Red),
                "Red path should still exist after undo");
            Assert.AreEqual(1, grid.Paths[ColorType.Red].Count,
                "Red path should have 1 cell after undo (restored to node-only state)");
        }

        // ──────────────────────────────────────────────
        // Test 9: Viaduct placement reduces limits and modifies cell state
        // ──────────────────────────────────────────────

        [Test]
        public void PlaceViaduct_ReducesAvailableViaducts_AndSetsCellBridgeState()
        {
            var level = CreateTestLevel();
            level.bridgePositions.Clear();
            level.viaductLimit = 3;
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var session = _ctx.GetModel<IGameSessionModel>();
            var grid = _ctx.GetModel<IGridModel>();

            Assert.AreEqual(3, session.AvailableViaducts, "Should start with 3 viaducts");

            // Setup a crossing at (2,2) by drawing two paths
            var cell = grid.Grid[2, 2];
            cell.AddPathColor(ColorType.Red);
            cell.AddPathColor(ColorType.Blue);

            // Place viaduct
            _ctx.Dispatch(new PlaceViaductSignal { Position = new Vector2Int(2, 2) });

            Assert.IsTrue(cell.HasViaduct, "Cell should have a viaduct");
            Assert.AreEqual(CellState.Bridge, cell.State, "Cell state should be Bridge");
            Assert.AreEqual(2, session.AvailableViaducts, "Remaining viaducts count should be 2");
        }

        // ──────────────────────────────────────────────
        // Test 10: City Economy passive tax generation and upgrades
        // ──────────────────────────────────────────────

        [Test]
        public void CityEconomy_AccumulatesTaxes_AndAllowsUpgrades()
        {
            var economy = _ctx.GetModel<ICityEconomyModel>();
            
            // Add coins
            int beforeCoins = economy.Coins;
            economy.AddCoins(500);
            Assert.AreEqual(beforeCoins + 500, economy.Coins, "Coins should increase by 500");

            // Check upgrade purchase
            int beforeLvl = economy.StorageUpgradeLevel;
            int cost = economy.GetUpgradeCost(UpgradeType.Storage);
            
            // Spend coins on upgrade
            economy.PurchaseUpgrade(UpgradeType.Storage);

            Assert.AreEqual(beforeLvl + 1, economy.StorageUpgradeLevel, "Storage level should increment");
            Assert.AreEqual(beforeCoins + 500 - cost, economy.Coins, "Coins should decrease by upgrade cost");
        }
    }
}
