using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Commands;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    /// <summary>
    /// Shared test factory for all PixelFlow EditMode tests.
    /// Every focused test class should reuse CreateGameContext() and CreateTestLevel()
    /// instead of duplicating setup logic.
    /// </summary>
    public static class GameTestContext
    {
        public static GameConfig CreateTestGameConfig()
        {
            var testConfig = ScriptableObject.CreateInstance<GameConfig>();
            testConfig.name = "GameConfig (Test)";
            testConfig.VehicleSpeed = 3f;
            testConfig.SpawnInterval = 1.2f;
            testConfig.MaxProgressPerFrame = 0.25f;
            testConfig.MaxSimulationSafetyDuration = 45f;
            testConfig.FerryPeriod = 10f;
            testConfig.HistoryMaxDepth = 200;
            testConfig.HubCameraSize = 7f;
            testConfig.MinZoom = 8f;
            testConfig.MaxZoom = 12f;
            testConfig.DefaultHintCount = 3;
            testConfig.TwoStarHintChance = 0.5f;
            testConfig.MaxRetriesBeforeInterstitial = 3;
            testConfig.MinLevelForInterstitial = 5;
            testConfig.InterstitialLevelInterval = 3;
            testConfig.FirstAdLevel = 5;
            testConfig.RewardedUndoLimit = 3;
            testConfig.InterstitialFrequency = 3;
            testConfig.RewardedAdCoinReward = 100;
            testConfig.RewardedAdHintReward = 2;
            testConfig.DoubleCoinMultiplier = 2f;
            testConfig.InterstitialPlacementId = "interstitial_level_end";
            testConfig.RewardedPlacementId = "rewarded_double_coins";
            testConfig.BannerPlacementId = "banner_bottom";
            testConfig.IdleReminderSeconds = 300f;
            testConfig.MaxGraceSkips = 3;
            testConfig.PathSolverMaxIterations = 200000;
            testConfig.AudioSampleRate = 44100;
            testConfig.DefaultUnlockedLevels = 1;
            testConfig.RainbowRoadSegmentsPerActivation = 3;
            testConfig.ClearJamUsesPerLevel = 1;
            testConfig.SaveFormatVersion = 2;
            testConfig.SaveVersionKey = "PF_SaveFormat_Version";
            testConfig.CoinPerFlowScore = 5;
            testConfig.LevelCompleteCoinBonus = 50;
            testConfig.DailyChestCoins = 100;
            testConfig.GemsPerThreeStarLevel = 5;
            testConfig.StarPassGemBonus = 3;
            testConfig.DefaultGems = 0;
            testConfig.DefaultTickets = 0;
            testConfig.FixedTimeStep = 1f / 60f;
            testConfig.SpawnCheckInterval = 10;
            testConfig.SpeedVariationRange = 0.3f;
            testConfig.CollisionDistance = 0.45f;
            testConfig.ViaductZDiffThreshold = 0.15f;
            testConfig.ViaductOverZOffset = -0.4f;
            testConfig.ViaductUnderZOffset = -0.1f;
            testConfig.NormalZOffset = -0.2f;
            testConfig.CameraTransitionDuration = 0.18f;
            testConfig.HubCameraPosition = new Vector3(8f, 12f, -8f);
            testConfig.HubCameraEuler = new Vector3(45f, 45f, 0f);
            testConfig.StateTransitionDuration = 0.8f;
            testConfig.PuzzleFallbackCameraSize = 5f;
            testConfig.CrashShakeIntensity = 0.35f;
            testConfig.CrashShakeDuration = 0.45f;
            testConfig.CrashFocusOffset = 0.4f;
            testConfig.AudioPoolSize = 3;
            testConfig.PathSolverMaxIterationsCap = 1000000;
            testConfig.VehiclePartPoolCubes = 512;
            testConfig.VehiclePartPoolCylinders = 256;
            testConfig.RejectionPulseFrequency = 15f;
            testConfig.MaxPathsPerBridge = 2;
            testConfig.DefaultTheme = AppTheme.Dark;
            testConfig.DefaultMasterVolume = 1f;
            testConfig.DefaultSfxVolume = 1f;
            testConfig.DefaultMusicVolume = 0.7f;
            testConfig.DefaultHapticsDisabled = false;
            return testConfig;
        }

        public static StorageKeysConfigAsset CreateTestStorageKeysConfig()
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
            storageKeys.EditorKeyUnlockedLevelsAllOverride = "UnlockedLevels";
            return storageKeys;
        }

        public static RuntimePathSolver CreateTestRuntimePathSolver()
        {
            return new RuntimePathSolver { Config = CreateTestGameConfig() };
        }

        /// <summary>
        /// Builds a Nexus test context with all PixelFlow game bindings registered.
        /// Uses InMemoryPlayerPrefsService so models can be constructed in EditMode.
        /// Every test must Dispose() the returned context in teardown.
        /// </summary>
        public static NexusTestContext CreateGameContext()
        {
            return NexusTestHarness.CreateContext(builder =>
            {
                builder.Bind<IPlayerPrefsService, InMemoryPlayerPrefsService>();

                builder.BindInstance(CreateTestGameConfig());

                var storageKeys = CreateTestStorageKeysConfig();
                builder.BindInstance(storageKeys);

                var economyConfig = ScriptableObject.CreateInstance<EconomyConfigAsset>();
                builder.BindInstance(economyConfig);

                var phaseConfig = ScriptableObject.CreateInstance<PhaseConfigAsset>();
                builder.BindInstance(phaseConfig);

                var themeConfig = ScriptableObject.CreateInstance<ThemePaletteAsset>();
                builder.BindInstance(themeConfig);

                var vehicleMatConfig = ScriptableObject.CreateInstance<VehicleMaterialConfigAsset>();
                builder.BindInstance(vehicleMatConfig);

                // Economy services (required by SaveProgressCommand)
                builder.BindService<IEconomyService, Nexus.Core.Services.EconomyService>();
                builder.Bind<Nexus.Core.Services.INetworkEconomyValidator, LocalEconomyValidator>();

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
                builder.BindSignal<ActivateRainbowRoadSignal>().To<RainbowRoadCommand>();
                builder.BindSignal<ClearJamSignal>().To<ClearJamCommand>();
                builder.BindSignal<ChangeThemeSignal>().To<ChangeThemeCommand>();
                builder.BindSignal<ChangeAudioVolumeSignal>().To<ChangeAudioVolumeCommand>();
                builder.BindSignal<ChangeColorBlindModeSignal>().To<ChangeColorBlindModeCommand>();
                builder.BindSignal<ToggleHapticsSignal>().To<ToggleHapticsCommand>();
                builder.BindCommand<LevelCompletedSignal, SaveProgressCommand>(ExecutionMode.Exclusive, priority: 0);
                builder.BindSignal<UndoSignal>().To<UndoCommand>();
                builder.BindSignal<RedoSignal>().To<RedoCommand>();
                builder.BindSignal<PlaceViaductSignal>().To<PlaceViaductCommand>();
                builder.BindSignal<RequestInterstitialAdSignal>().To<InterstitialAdCommand>();
                builder.BindSignal<StartSimulationSignal>().To<StartSimulationCommand>();
                builder.BindSignal<PauseSimulationSignal>().To<PauseSimulationCommand>();
            });
        }

        /// <summary>
        /// Creates a 5x5 test level with Red, Blue, Green nodes, one bridge at (2,2),
        /// and known solution paths.
        /// </summary>
        public static LevelData CreateTestLevel(int index = 0)
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

        /// <summary>
        /// Creates an empty level with no initial nodes, bridges, or solutions.
        /// Useful for testing edge cases and manual grid setup.
        /// </summary>
        public static LevelData CreateEmptyLevel(int width, int height, int index = 0)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = index;
            level.width = width;
            level.height = height;
            level.initialNodes = new List<GridNode>();
            level.bridgePositions = new List<Vector2Int>();
            level.solutions = new List<PathSolution>();
            return level;
        }
    }
}
