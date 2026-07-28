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
            testConfig.PathSolverMaxIterations = 500;
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
            testConfig.PathSolverMaxIterations = 500;
            testConfig.PathSolverMaxIterationsCap = 2000;
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

        public static PhaseConfigAsset CreateTestPhaseConfig()
        {
            var phaseConfig = ScriptableObject.CreateInstance<PhaseConfigAsset>();
            phaseConfig.Phase1 = CreatePhase("Phase1_Test", GamePhase.Phase1, 0, 11, 5, 6, 1, 2, 0, 0, false, false, false, false, false);
            phaseConfig.Phase2 = CreatePhase("Phase2_Test", GamePhase.Phase2, 12, 27, 7, 7, 2, 3, 1, 2, false, false, false, false, false);
            phaseConfig.Phase3 = CreatePhase("Phase3_Test", GamePhase.Phase3, 28, 44, 8, 9, 3, 4, 2, 3, true, true, true, false, false);
            phaseConfig.Phase4 = CreatePhase("Phase4_Test", GamePhase.Phase4, 45, 59, 10, 10, 4, 5, 3, 4, true, true, true, true, true);
            return phaseConfig;
        }

        public static LevelCatalogAsset CreateTestLevelCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<LevelCatalogAsset>();
            catalog.Levels.Add(new LevelCatalogAsset.LevelCatalogEntry
            {
                LevelIndex = 0,
                UseProceduralFallback = true,
                ProceduralDifficulty = DifficultyParams.Easy
            });
            return catalog;
        }

        public static DefaultSkinIdsConfigAsset CreateTestDefaultSkinIdsConfig()
        {
            var config = ScriptableObject.CreateInstance<DefaultSkinIdsConfigAsset>();
            config.DefaultVehicleSkinId = "skin_default";
            return config;
        }

        public static BouncyPhysicsConfigAsset CreateTestBouncyPhysicsConfig()
        {
            var config = ScriptableObject.CreateInstance<BouncyPhysicsConfigAsset>();
            config.BounceForce = 4.5f;
            config.BounceDamping = 0.75f;
            config.SquishFactor = 0.35f;
            return config;
        }

        public static StarCriteriaConfigAsset CreateTestStarCriteriaConfig()
        {
            var config = ScriptableObject.CreateInstance<StarCriteriaConfigAsset>();
            config.ThreeStarsMaxViaducts = 0;
            config.TwoStarsMaxViaducts = 2;
            config.OneStar = "complete";
            config.TwoStars = "viaducts_used <= 2";
            config.ThreeStars = "viaducts_used == 0";
            return config;
        }

        public static RushHourConfigAsset CreateTestRushHourConfig()
        {
            var config = ScriptableObject.CreateInstance<RushHourConfigAsset>();
            config.DurationSeconds = 3600;
            config.CoinMultiplier = 2.0f;
            config.CooldownHours = 48;
            config.MinLevel = 10;
            config.TriggerAfterHours = 24;
            return config;
        }

        public static DifficultyFormulaConfigAsset CreateTestDifficultyFormulaConfig()
        {
            var config = ScriptableObject.CreateInstance<DifficultyFormulaConfigAsset>();
            config.ColorWeight = 10;
            config.IntersectionWeight = 5;
            config.ObstacleWeight = 3;
            config.ViaductWeight = 4;
            return config;
        }

        public static ThemePaletteAsset CreateTestThemePalette()
        {
            var palette = ScriptableObject.CreateInstance<ThemePaletteAsset>();
            return palette;
        }

        public static ColorBlindPaletteAsset CreateTestColorBlindPalette()
        {
            var palette = ScriptableObject.CreateInstance<ColorBlindPaletteAsset>();
            return palette;
        }

        public static VehicleMaterialConfigAsset CreateTestVehicleMaterialConfig()
        {
            var config = ScriptableObject.CreateInstance<VehicleMaterialConfigAsset>();
            return config;
        }

        public static VehicleVisualConfigAsset CreateTestVehicleVisualConfig()
        {
            var config = ScriptableObject.CreateInstance<VehicleVisualConfigAsset>();
            config.TrainWheelYOffset = 0.09f;
            config.TrainWheelZOffset = 0.05f;
            config.TrainTrailTime = 0.55f;
            config.TrainTrailStartWidth = 0.22f;
            config.CarWheelZOffset = 0.06f;
            config.CarTrailTime = 0.45f;
            config.CarTrailStartWidth = 0.18f;
            return config;
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
                economyConfig.IapProducts = new List<IapProductDefinition>
                {
                    new IapProductDefinition
                    {
                        ProductId = "test_iap_product",
                        DisplayName = "Test Product",
                        PriceUsd = 0.99f,
                        CurrencyCode = "USD",
                        Type = IapProductType.Consumable,
                        CoinAmount = 50,
                        GemAmount = 0,
                        UnlockSkinId = string.Empty,
                        RemovesAds = false,
                        Description = "Test IAP product"
                    }
                };
                builder.BindInstance(economyConfig);

                var phaseConfig = CreateTestPhaseConfig();
                builder.BindInstance(phaseConfig);

                var levelCatalog = CreateTestLevelCatalog();
                builder.BindInstance(levelCatalog);

                var themeConfig = ScriptableObject.CreateInstance<ThemePaletteAsset>();
                builder.BindInstance(themeConfig);

                var vehicleMatConfig = ScriptableObject.CreateInstance<VehicleMaterialConfigAsset>();
                builder.BindInstance(vehicleMatConfig);

                var defaultSkinConfig = CreateTestDefaultSkinIdsConfig();
                builder.BindInstance(defaultSkinConfig);

                var bouncyPhysicsConfig = CreateTestBouncyPhysicsConfig();
                builder.BindInstance(bouncyPhysicsConfig);

                var starCriteriaConfig = CreateTestStarCriteriaConfig();
                builder.BindInstance(starCriteriaConfig);

                var rushHourConfig = CreateTestRushHourConfig();
                builder.BindInstance(rushHourConfig);

                var difficultyFormulaConfig = CreateTestDifficultyFormulaConfig();
                builder.BindInstance(difficultyFormulaConfig);

                var themePalette = CreateTestThemePalette();
                builder.BindInstance(themePalette);

                var colorBlindPalette = CreateTestColorBlindPalette();
                builder.BindInstance(colorBlindPalette);

                var vehicleMaterialConfig = CreateTestVehicleMaterialConfig();
                builder.BindInstance(vehicleMaterialConfig);

                var vehicleVisualConfig = CreateTestVehicleVisualConfig();
                builder.BindInstance(vehicleVisualConfig);

                // Economy services (required by SaveProgressCommand)
                builder.BindService<IEconomyService, Nexus.Core.Services.EconomyService>();
                builder.Bind<Nexus.Core.Services.INetworkEconomyValidator, LocalEconomyValidator>();

                builder.BindService<IPathService, PathService>();
                builder.BindService<IGameHistoryService, GameHistoryService>();
                builder.Bind<IPathSolver, RuntimePathSolver>();
                builder.BindService<IHintService, HintService>();
                builder.BindService<IVehicleSimulator, VehicleSimulator>();
                builder.BindService<ISaveThrottler, SaveThrottler>();
                builder.BindService<IScoreCalculator, ScoreCalculator>();
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
                builder.BindReactiveModel<IDailyCrisisModel, DailyCrisisModel>();
                builder.BindReactiveModel<IInventoryModel, InventoryModel>();
                builder.Bind<ILevelProgressionService, LevelProgressionService>();

                builder.Bind<ICameraProvider, StubCameraProvider>();
                builder.Bind<IGridViewProvider, StubGridViewProvider>();
                builder.BindService<ILevelLoaderService, LevelLoaderService>();
                builder.Bind<ICloudSaveAdapter, PixelFlow.Services.GlobalRelease.EncryptedCloudSaveAdapter>();
                builder.BindService<ILocalizationService, PixelFlow.Services.LocalizationService>();
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

        private static PhaseDefinitionAsset CreatePhase(string name, GamePhase phase, int startIdx, int endIdx,
            int gridMin, int gridMax, int colorMin, int colorMax, int bridgeMin, int bridgeMax,
            bool fullCoverage, bool obstacles, bool oneWay, bool ferry, bool narrowPass)
        {
            var asset = ScriptableObject.CreateInstance<PhaseDefinitionAsset>();
            asset.name = name;
            asset.Phase = phase;
            asset.StartLevelIndex = startIdx;
            asset.EndLevelIndex = endIdx;
            asset.GridSizeMin = gridMin;
            asset.GridSizeMax = gridMax;
            asset.ColorCountMin = colorMin;
            asset.ColorCountMax = colorMax;
            asset.BridgeCountMin = bridgeMin;
            asset.BridgeCountMax = bridgeMax;
            asset.RequireFullCoverage = fullCoverage;
            asset.ObstaclesEnabled = obstacles;
            asset.OneWayEnabled = oneWay;
            asset.FerryEnabled = ferry;
            asset.NarrowPassEnabled = narrowPass;
            return asset;
        }
    }
}