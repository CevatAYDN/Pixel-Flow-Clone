using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
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
    /// Minimal no-op stub for Nexus.Core.Services.IAudioService.
    /// Satisfies DI injection in FeedbackService without requiring
    /// the full Nexus AudioService setup.
    /// </summary>
    public sealed class StubAudioService : Nexus.Core.Services.IAudioService
    {
        public float MasterVolume { get; set; }
        public float BgmVolume { get; set; }
        public float SfxVolume { get; set; }
        public bool IsMuted { get; set; }
        public float BgmStateMultiplier { get; set; }

        public void PlayBgm(AudioClip clip, bool loop = true, float fadeDuration = 0.5f) { }
        public void StopBgm(float fadeDuration = 0.5f) { }
        public void PlaySfx(AudioClip clip, float volume = 1f, float pitchMin = 1f, float pitchMax = 1f) { }
        public void PlaySfxAtPosition(AudioClip clip, Vector3 position, float volume = 1f) { }
    }

    /// <summary>
    /// Minimal no-op stub for ICameraProvider.
    /// Satisfies DI injection in VehicleSimulator.
    /// </summary>
    public sealed class StubCameraProvider : ICameraProvider
    {
        private Camera _stubCam;
        public Camera MainCamera
        {
            get
            {
                if (_stubCam == null)
                {
                    var go = new GameObject("StubCamera");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    _stubCam = go.AddComponent<Camera>();
                }
                return _stubCam;
            }
        }
    }

    /// <summary>
    /// Minimal no-op stub for IGridViewProvider.
    /// Satisfies DI injection in VehicleSimulator.
    /// </summary>
    public sealed class StubGridViewProvider : IGridViewProvider
    {
        public Transform GridTransform => null;
    }

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

        private readonly Dictionary<string, float> _floats = new Dictionary<string, float>();

        public float GetFloat(string key, float defaultValue = 0f)
            => _floats.TryGetValue(key, out var val) ? val : defaultValue;

        public void SetFloat(string key, float value) => _floats[key] = value;

        private readonly Dictionary<string, long> _longs = new Dictionary<string, long>();

        public long GetLong(string key, long defaultValue = 0L)
            => _longs.TryGetValue(key, out var val) ? val : defaultValue;

        public void SetLong(string key, long value) => _longs[key] = value;

        public bool HasKey(string key) => _store.ContainsKey(key) || _strings.ContainsKey(key) || _floats.ContainsKey(key) || _longs.ContainsKey(key);

        public void DeleteKey(string key)
        {
            _store.Remove(key);
            _strings.Remove(key);
            _floats.Remove(key);
            _longs.Remove(key);
        }

        public void Save() { }
    }

    [TestFixture]
    public class PixelFlowPlayModeTests
    {
        // ──────────────────────────────────────────────
        // Context factory (same pattern as EditMode tests)
        // ──────────────────────────────────────────────

        private static StorageKeysConfigAsset CreateTestStorageKeysConfig()
        {
            var storageKeys = ScriptableObject.CreateInstance<StorageKeysConfigAsset>();
            storageKeys.KeyTheme = "AppTheme";
            storageKeys.KeyColorBlind = "ColorBlindMode";
            storageKeys.KeyVehicleStyle = "VehicleStyle";
            storageKeys.KeyMasterVol = "MasterVolume";
            storageKeys.KeySfxVol = "SfxVolume";
            storageKeys.KeyMusicVol = "MusicVolume";
            storageKeys.KeyHaptics = "HapticsDisabled";
            storageKeys.KeyUnlockedLevels = "UnlockedLevels";
            storageKeys.KeyHintCount = "HintCount";
            storageKeys.KeySoundMuted = "SoundMuted";
            storageKeys.KeyTutorialStep = "TutorialStep";
            storageKeys.KeyCloudPlayerId = "PF_CloudPlayerId";
            storageKeys.KeyCloudRecord = "PF_CloudRecord";
            storageKeys.KeyDailyLogin_LastLogin = "NT_DailyLogin_LastLogin";
            storageKeys.KeyDailyLogin_Streak = "NT_DailyLogin_Streak";
            storageKeys.KeyDailyLogin_VipSkinGranted = "NT_DailyLogin_VipSkinGranted";
            storageKeys.DailyLoginVipSkinId = "skin_vip_golden";
            storageKeys.KeyRushHour_Active = "NT_RushHour_Active";
            storageKeys.KeyRushHour_EndTime = "NT_RushHour_EndTime";
            storageKeys.KeyRushHour_Cooldown = "NT_RushHour_Cooldown";
            storageKeys.CurrencyIdCoin = "coins";
            storageKeys.CurrencyIdGem = "gems";
            storageKeys.CurrencyIdTicket = "tickets";
            storageKeys.KeyPuzzleSavePrefix = "NT_PuzzleSave_";
            storageKeys.EditorKeyUnlockedLevelsAllOverride = "UnlockedLevels";
            return storageKeys;
        }

        private static DefaultSkinIdsConfigAsset CreateTestDefaultSkinIdsConfig()
        {
            var config = ScriptableObject.CreateInstance<DefaultSkinIdsConfigAsset>();
            config.DefaultVehicleSkinId = "skin_car_red";
            config.DefaultStopSkinId = "skin_stop_classic";
            return config;
        }

        private static BouncyPhysicsConfigAsset CreateTestBouncyPhysicsConfig()
        {
            var config = ScriptableObject.CreateInstance<BouncyPhysicsConfigAsset>();
            config.BounceForce = 4.5f;
            config.BounceDamping = 0.75f;
            config.SquishFactor = 0.35f;
            return config;
        }

        private static StarCriteriaConfigAsset CreateTestStarCriteriaConfig()
        {
            var config = ScriptableObject.CreateInstance<StarCriteriaConfigAsset>();
            config.ThreeStarsMaxViaducts = 0;
            config.TwoStarsMaxViaducts = 2;
            config.OneStar = "complete";
            config.TwoStars = "viaducts_used <= 2";
            config.ThreeStars = "viaducts_used == 0";
            return config;
        }

        private static RushHourConfigAsset CreateTestRushHourConfig()
        {
            var config = ScriptableObject.CreateInstance<RushHourConfigAsset>();
            config.DurationSeconds = 3600;
            config.CoinMultiplier = 2.0f;
            config.CooldownHours = 48;
            config.MinLevel = 10;
            config.TriggerAfterHours = 24;
            return config;
        }

        private static DifficultyFormulaConfigAsset CreateTestDifficultyFormulaConfig()
        {
            var config = ScriptableObject.CreateInstance<DifficultyFormulaConfigAsset>();
            config.ColorWeight = 10;
            config.IntersectionWeight = 5;
            config.ObstacleWeight = 3;
            config.ViaductWeight = 4;
            return config;
        }

        private static PhaseConfigAsset CreateTestPhaseConfig()
        {
            var phaseConfig = ScriptableObject.CreateInstance<PhaseConfigAsset>();
            var p1 = ScriptableObject.CreateInstance<PhaseDefinitionAsset>();
            p1.name = "Phase1_Test"; p1.Phase = GamePhase.Phase1; p1.StartLevelIndex = 0; p1.EndLevelIndex = 11;
            p1.GridSizeMin = 5; p1.GridSizeMax = 6; p1.ColorCountMin = 1; p1.ColorCountMax = 2;
            phaseConfig.Phase1 = p1;
            phaseConfig.Phase2 = p1;
            phaseConfig.Phase3 = p1;
            phaseConfig.Phase4 = p1;
            return phaseConfig;
        }

        private static NexusTestContext CreateGameContext()
        {
            return NexusTestHarness.CreateContext(builder =>
            {
                builder.Bind<IPlayerPrefsService, InMemoryPlayerPrefsService>();

                var testConfig = ScriptableObject.CreateInstance<GameConfig>();
                testConfig.name = "GameConfig (Test)";
                testConfig.PathSolverMaxIterations = 500;
                testConfig.PathSolverMaxIterationsCap = 2000;
                testConfig.FixedTimeStep = 1f / 60f;
                testConfig.VehicleSpeed = 2f;
                testConfig.SpawnInterval = 0.5f;
                builder.BindInstance(testConfig);

                builder.BindInstance(CreateTestStorageKeysConfig());
                builder.BindInstance(ScriptableObject.CreateInstance<EconomyConfigAsset>());
                builder.BindInstance(CreateTestPhaseConfig());
                builder.BindInstance(ScriptableObject.CreateInstance<LevelCatalogAsset>());
                builder.BindInstance(CreateTestDefaultSkinIdsConfig());
                builder.BindInstance(CreateTestBouncyPhysicsConfig());
                builder.BindInstance(CreateTestStarCriteriaConfig());
                builder.BindInstance(CreateTestRushHourConfig());
                builder.BindInstance(CreateTestDifficultyFormulaConfig());
                builder.BindInstance(ScriptableObject.CreateInstance<ThemePaletteAsset>());
                builder.BindInstance(ScriptableObject.CreateInstance<ColorBlindPaletteAsset>());
                builder.BindInstance(ScriptableObject.CreateInstance<VehicleMaterialConfigAsset>());
                builder.BindInstance(ScriptableObject.CreateInstance<VehicleVisualConfigAsset>());

                builder.Bind<IPathService, PathService>();
                builder.Bind<IGameHistoryService, GameHistoryService>();
                builder.Bind<IPathSolver, RuntimePathSolver>();
                builder.Bind<IHintService, HintService>();
                builder.Bind<IVehicleSimulator, VehicleSimulator>();
                builder.Bind<ISaveThrottler, SaveThrottler>();
                builder.BindService<INexusService, HapticService>();
                builder.Bind<IHapticService, HapticService>();
                builder.BindService<INexusService, LoggerService>();
                builder.Bind<ILoggerService, LoggerService>();
                builder.Bind<ICrisisAdService, CrisisAdService>();
                builder.Bind<IObstacleService, ObstacleService>();
                builder.Bind<ITutorialDriver, TutorialDriver>();
                builder.Bind<IPowerUpService, PowerUpService>();
                builder.Bind<IFeedbackService, FeedbackService>();
                builder.Bind<Nexus.Core.Services.IAudioService, StubAudioService>();
                builder.Bind<Nexus.Core.Services.INetworkEconomyValidator, LocalEconomyValidator>();
                builder.BindService<IEconomyService, Nexus.Core.Services.EconomyService>();
                builder.Bind<ITimeProvider, UnityTimeProvider>();
                builder.BindService<INexusService, TickService>();
                builder.Bind<ITickService, TickService>();
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
                builder.BindService<ILevelLoaderService, LevelLoaderService>();

                builder.BindInstance<IRecoveryStrategy>(new DefaultRecoveryStrategy(maxRetries: 3));
                builder.BindService<IScoreCalculator, ScoreCalculator>();
                builder.BindService<GridStateSerializer>();

                builder.Bind<ICameraProvider, StubCameraProvider>();
                builder.Bind<IGridViewProvider, StubGridViewProvider>();

                builder.BindSignal<InputInteractionSignal>().To<ProcessInputCommand>();
                builder.BindSignal<CheckWinConditionSignal>().To<CheckWinConditionCommand>();
                builder.BindSignal<LoadLevelSignal>().To<LoadLevelCommand>();
                builder.BindSignal<RequestHintSignal>().To<UseHintCommand>();
                builder.BindSignal<ChangeThemeSignal>().To<ChangeThemeCommand>();
                builder.BindSignal<StartSimulationSignal>().To<StartSimulationCommand>();
                builder.BindSignal<PauseSimulationSignal>().To<PauseSimulationCommand>();
                builder.BindCommand<LevelCompletedSignal, SaveProgressCommand>(ExecutionMode.Exclusive, priority: 0);
                builder.BindSignal<UndoSignal>().To<UndoCommand>();
                builder.BindSignal<RedoSignal>().To<RedoCommand>();
                builder.BindSignal<PlaceViaductSignal>().To<PlaceViaductCommand>();
                builder.BindSignal<RequestInterstitialAdSignal>().To<InterstitialAdCommand>();
            });
        }

        // ──────────────────────────────────────────────
        // Test 1: Basic Grid initialization
        // ──────────────────────────────────────────────

        [Test]
        public void Grid_InitializesWithCorrectDimensions()
        {
            using var ctx = CreateGameContext();
            var grid = ctx.GetModel<IGridModel>();

            grid.Initialize(5, 5);

            Assert.AreEqual(5, grid.Width);
            Assert.AreEqual(5, grid.Height);
            Assert.AreEqual(25, grid.Grid.Length);
        }

        // ──────────────────────────────────────────────
        // Test 2: Path drawing basic mechanics
        // ──────────────────────────────────────────────

        [Test]
        public void PathDrawing_ConnectsNodes()
        {
            using var ctx = CreateGameContext();
            var grid = ctx.GetModel<IGridModel>();

            grid.Initialize(5, 5);

            // Place nodes manually
            grid.Grid[0, 0].State = CellState.Node;
            grid.Grid[0, 0].Color = ColorType.Red;
            grid.Grid[0, 0].AddPathColor(ColorType.Red);

            grid.Grid[4, 4].State = CellState.Node;
            grid.Grid[4, 4].Color = ColorType.Red;
            grid.Grid[4, 4].AddPathColor(ColorType.Red);

            // Simulate path drawing
            grid.ActiveColor.Value = ColorType.Red;
            grid.Paths[ColorType.Red] = new List<Vector2Int> { new Vector2Int(0, 0) };

            // Add intermediate cells
            grid.Grid[1, 0].State = CellState.Path;
            grid.Grid[1, 0].Color = ColorType.Red;
            grid.Grid[1, 0].AddPathColor(ColorType.Red);
            grid.Paths[ColorType.Red].Add(new Vector2Int(1, 0));

            grid.Grid[2, 0].State = CellState.Path;
            grid.Grid[2, 0].Color = ColorType.Red;
            grid.Grid[2, 0].AddPathColor(ColorType.Red);
            grid.Paths[ColorType.Red].Add(new Vector2Int(2, 0));

            grid.Grid[3, 0].State = CellState.Path;
            grid.Grid[3, 0].Color = ColorType.Red;
            grid.Grid[3, 0].AddPathColor(ColorType.Red);
            grid.Paths[ColorType.Red].Add(new Vector2Int(3, 0));

            grid.Grid[4, 0].State = CellState.Path;
            grid.Grid[4, 0].Color = ColorType.Red;
            grid.Grid[4, 0].AddPathColor(ColorType.Red);
            grid.Paths[ColorType.Red].Add(new Vector2Int(4, 0));

            grid.Grid[4, 4].AddPathColor(ColorType.Red);
            grid.Paths[ColorType.Red].Add(new Vector2Int(4, 4));

            Assert.AreEqual(6, grid.Paths[ColorType.Red].Count);
        }

        // ──────────────────────────────────────────────
        // Test 3: Undo/Redo with GameHistoryService
        // ──────────────────────────────────────────────

        [Test]
        public void GameHistoryService_SupportsUndo()
        {
            using var ctx = CreateGameContext();
            var grid = ctx.GetModel<IGridModel>();
            var history = ctx.Context.Container.Resolve<IGameHistoryService>();

            grid.Initialize(5, 5);

            // Record initial state
            history.Record(grid);

            // Modify grid once
            grid.Grid[0, 0].State = CellState.Path;
            grid.Grid[0, 0].Color = ColorType.Blue;

            // Record after first modification
            history.Record(grid);

            // Modify again (no record — last change is current live state)
            grid.Grid[0, 0].State = CellState.Node;
            grid.Grid[0, 0].Color = ColorType.Red;

            // Undo: should restore {Path, Blue} (was the last recorded snapshot)
            history.Undo(grid);
            Assert.AreEqual(CellState.Path, grid.Grid[0, 0].State);
            Assert.AreEqual(ColorType.Blue, grid.Grid[0, 0].Color);

            // Undo again: should restore {Empty, None}
            history.Undo(grid);
            Assert.AreEqual(CellState.Empty, grid.Grid[0, 0].State);
        }

        // ──────────────────────────────────────────────
        // Test 4: Hint system basic functionality
        // ──────────────────────────────────────────────

        [Test]
        public void HintModel_DecrementsOnUse()
        {
            using var ctx = CreateGameContext();
            var hintModel = ctx.GetModel<IHintModel>();

            int initial = hintModel.HintsRemaining;
            hintModel.UseHint();

            Assert.AreEqual(initial - 1, hintModel.HintsRemaining);
        }

        // ──────────────────────────────────────────────
        // Test 5: Viaduct placement (bridge crossing)
        // ──────────────────────────────────────────────

        [Test]
        public void ViaductPlacement_AllowsPathCrossing()
        {
            using var ctx = CreateGameContext();
            var grid = ctx.GetModel<IGridModel>();
            var session = ctx.GetModel<IGameSessionModel>();

            grid.Initialize(5, 5);

            // Set up session with viaducts
            session.StartSession(3, 5);

            // Create crossing point
            grid.Grid[2, 2].State = CellState.Bridge;
            grid.Grid[2, 2].HasViaduct = true;

            // Blue path crossing horizontally
            grid.Grid[2, 2].AddPathColor(ColorType.Blue);
            grid.Grid[2, 2].UnderColor = ColorType.Blue;

            // Red path crossing vertically
            grid.Grid[2, 2].AddPathColor(ColorType.Red);
            grid.Grid[2, 2].OverColor = ColorType.Red;

            Assert.IsTrue(grid.Grid[2, 2].HasPathColor(ColorType.Blue));
            Assert.IsTrue(grid.Grid[2, 2].HasPathColor(ColorType.Red));
            Assert.AreEqual(2, grid.Grid[2, 2].PathColorCount);
        }

        // ──────────────────────────────────────────────
        // Test 6: Level progression unlocks levels
        // ──────────────────────────────────────────────

        [Test]
        public void ProgressModel_UnlocksLevels()
        {
            using var ctx = CreateGameContext();
            var progress = ctx.GetModel<IProgressModel>();

            // Default unlocked levels = 1 (level 0 is always available)
            Assert.AreEqual(1, progress.UnlockedLevels);

            progress.UnlockLevel(5);
            Assert.AreEqual(7, progress.UnlockedLevels); // 5 + 2

            progress.UnlockLevel(10);
            Assert.AreEqual(12, progress.UnlockedLevels); // 10 + 2
        }

        // ──────────────────────────────────────────────
        // Test 7: Game state transitions
        // ──────────────────────────────────────────────

        [Test]
        public void GameStateModel_TransitionsCorrectly()
        {
            using var ctx = CreateGameContext();
            var state = ctx.GetModel<IGameStateModel>();

            Assert.AreEqual(GameState.Boot, state.CurrentState);

            state.SetState(GameState.MainMenu);
            Assert.AreEqual(GameState.MainMenu, state.CurrentState);

            state.SetState(GameState.Playing);
            Assert.AreEqual(GameState.Playing, state.CurrentState);

            state.SetState(GameState.Paused);
            Assert.AreEqual(GameState.Paused, state.CurrentState);

            // Paused → Playing (not Simulating — Boot → MainMenu → Playing → Paused → Playing)
            state.SetState(GameState.Playing);
            Assert.AreEqual(GameState.Playing, state.CurrentState);
        }

        // ──────────────────────────────────────────────
        // Test 8: Viaduct bonus from level progression
        // ──────────────────────────────────────────────

        [Test]
        public void LoadLevelCommand_AppliesViaductBonus()
        {
            using var ctx = CreateGameContext();
            var grid = ctx.GetModel<IGridModel>();
            var session = ctx.GetModel<IGameSessionModel>();
            var signalBus = ctx.Context.Container.Resolve<ISignalBus>();

            // Create test level
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 15; // Should give +1 viaduct bonus (15 / 10 = 1)
            level.width = 5;
            level.height = 5;
            level.viaductLimit = 2;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 4), color = ColorType.Red }
            };

            signalBus.Fire(new LoadLevelSignal { LevelToLoad = level });

            // Total viaducts = level limit (2) + bonus (1) = 3
            Assert.AreEqual(3, session.MaxViaducts);
            Assert.AreEqual(3, session.AvailableViaducts); // All viaducts available at start
        }

        // ──────────────────────────────────────────────
        // Test 9: Hint award for star performance
        // ──────────────────────────────────────────────

        [Test]
        public void HintModel_AwardsHintForThreeStars()
        {
            using var ctx = CreateGameContext();
            var hintModel = ctx.GetModel<IHintModel>();

            int initialHints = hintModel.HintsRemaining;

            // 3 stars should award a hint
            hintModel.AwardHintForStar(3);
            Assert.AreEqual(initialHints + 1, hintModel.HintsRemaining);

            // 2 stars has 50% chance - test multiple times
            int hintCount2Star = 0;
            for (int i = 0; i < 100; i++)
            {
                int before = hintModel.HintsRemaining;
                hintModel.AwardHintForStar(2);
                if (hintModel.HintsRemaining > before)
                    hintCount2Star++;
            }
            // Should be around 50% (between 30-70 for statistical tolerance)
            Assert.Greater(hintCount2Star, 30);
            Assert.Less(hintCount2Star, 70);

            // 1 star should not award hints
            int before1Star = hintModel.HintsRemaining;
            hintModel.AwardHintForStar(1);
            Assert.AreEqual(before1Star, hintModel.HintsRemaining);
        }

        // ──────────────────────────────────────────────
        // Test 11: InventoryModel PlayMode Integration
        // ──────────────────────────────────────────────

        [Test]
        public void InventoryModel_PlayModeIntegration_AddsCoinsAndUnlocksSkins()
        {
            using var ctx = CreateGameContext();
            var inventory = ctx.GetModel<IInventoryModel>();

            Assert.AreEqual(0, inventory.Coins);
            inventory.AddCoins(250);
            Assert.AreEqual(250, inventory.Coins);

            Assert.IsTrue(inventory.TrySpendCoins(100));
            Assert.AreEqual(150, inventory.Coins);

            inventory.UnlockSkin("skin_ice_cream");
            Assert.IsTrue(inventory.IsSkinUnlocked("skin_ice_cream"));

            inventory.EquipSkin(ColorType.Yellow, "skin_ice_cream");
            Assert.AreEqual("skin_ice_cream", inventory.GetEquippedSkin(ColorType.Yellow));
        }
    }
}
