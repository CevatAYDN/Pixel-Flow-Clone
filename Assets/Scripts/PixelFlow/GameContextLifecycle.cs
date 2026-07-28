using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Commands;

using UnityEngine;

namespace PixelFlow
{
    public class GameContextLifecycle : MonoBehaviour, IContextLifecycle
    {
        [Header("Bootstrap Config Assets")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private StorageKeysConfigAsset storageKeysConfig;
        [SerializeField] private ThemePaletteAsset themePalette;
        [SerializeField] private ColorBlindPaletteAsset colorBlindPalette;
        [SerializeField] private VehicleMaterialConfigAsset vehicleMaterialConfig;
        [SerializeField] private VehicleVisualConfigAsset vehicleVisualConfig;
        [SerializeField] private EconomyConfigAsset economyConfig;
        [SerializeField] private LevelCatalogAsset levelCatalog;
        [SerializeField] private PhaseConfigAsset phaseConfig;
        [SerializeField] private DifficultyFormulaConfigAsset difficultyFormulaConfig;
        [SerializeField] private DefaultSkinIdsConfigAsset defaultSkinConfig;
        [SerializeField] private BouncyPhysicsConfigAsset bouncyPhysicsConfig;
        [SerializeField] private StarCriteriaConfigAsset starCriteriaConfig;
        [SerializeField] private RushHourConfigAsset rushHourConfig;

        public void OnConfigure(IContextBuilder builder)
        {
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] OnConfigure: Initializing framework dependency injection bindings...");
            var storageService = new StrictEncryptedStorageService();
            builder.BindInstance<IPlayerPrefsService>(storageService);
            
            // Tüm Nexus Core Çekirdek Servisleri — BindService ile tek instance
            // (BindService<> hem INexusService arayüzünü bağlar hem de auto-init kuyruğuna ekler.
            // Ayrıca Bind<> ile ikinci instance yaratılmasını önlemek için sadece iş arayüzü kullanılır.)
            builder.BindService<IFeedbackService, FeedbackService>();
            builder.BindService<IObjectPoolService, ObjectPoolService>();
            builder.BindService<IWindowManager, WindowManager>();
            builder.BindService<IEconomyService, EconomyService>();
            builder.BindService<IProgressionService, ProgressionService>();
            builder.BindService<ITickService, TickService>();
            builder.BindService<IAdService, AdService>();
            builder.BindService<IIapService, IapService>();
            builder.BindService<IAnalyticsService, AnalyticsService>();

            // Nexus Altyapı Bağımlılıkları (FeedbackService, WindowManager, EconomyService için)
            builder.Bind<Nexus.Core.Services.IAudioService, Nexus.Core.Services.AudioService>();
            builder.Bind<Nexus.Core.Services.IAudioRootProvider, Nexus.Core.Services.DefaultAudioRootProvider>();
            builder.Bind<Nexus.Core.Services.IUIAssetProvider, Nexus.Core.Services.ResourcesUIAssetProvider>();
            builder.Bind<Nexus.Core.Services.INetworkEconomyValidator, LocalEconomyValidator>();

            // PixelFlow Özel Servisleri
            builder.BindService<IPathService, PathService>();
            builder.BindService<IGameHistoryService, GameHistoryService>();
            builder.BindService<IVehicleSimulator, VehicleSimulator>();
            builder.BindService<ICameraProvider, CameraProvider>();
            builder.BindService<IGridViewProvider, GridViewProvider>();

            // Bind IGridInputService → GridInputService (input state machine)
            builder.Bind<IGridInputService, GridInputService>();
            builder.BindService<PixelFlow.Services.IAudioService, PixelFlow.Services.AudioService>();
            builder.BindService<IGameplayTimerService, GameplayTimerService>();
            builder.Bind<LevelVictoryCompositeHandler, LevelVictoryCompositeHandler>();
            builder.Bind<RewardedAdCommand, RewardedAdCommand>();
            builder.Bind<ITimeProvider, UnityTimeProvider>();
            builder.BindService<ISaveThrottler, SaveThrottler>();
            builder.BindService<IHapticService, HapticService>();
            builder.BindService<ILoggerService, LoggerService>();
            builder.BindService<ITutorialDriver, TutorialDriver>();
            builder.BindService<ICrisisAdService, CrisisAdService>();
            builder.BindService<IObstacleService, ObstacleService>();
            builder.BindService<Nexus.Core.Services.ILocalizationService, PixelFlow.Services.LocalizationService>();
            builder.Bind<ILocalizationTableProvider, ResourceLocalizationTableProvider>();
            builder.BindService<IDailyCrisisService, DailyCrisisService>();
            builder.BindService<IDailyLoginStreakService, DailyLoginStreakService>(); // LiveOps: Daily Login Streak
            builder.BindService<IRushHourEventService, RushHourEventService>(); // LiveOps: Rush Hour Event
            builder.Bind<IPathSolver, RuntimePathSolver>();
            builder.Bind<ILevelValidator, LevelValidator>();
            builder.Bind<PathSolverFactory>();
            builder.Bind<IPathSolverStrategy, StandardDFSPathSolverStrategy>();
            builder.Bind<IPathSolverStrategy, PhaseBasedSolverStrategy>();
            builder.Bind<IPathSolverStrategy, DynamicDifficultySolverStrategy>();
            builder.BindService<IHintService, HintService>(); // Fixed: was Bind<> now BindService<> for auto-init
            builder.BindService<IPowerUpService, PowerUpService>();
            builder.Bind<ILevelProgressionService, LevelProgressionService>();
            builder.BindService<ILevelLoaderService, LevelLoaderService>(); // GDD §8: DI injection for level loading
            builder.BindService<IapIntegrationService, IapIntegrationService>(); // IAP integration with EconomyConfig

            // Score Calculator
            builder.BindService<IScoreCalculator, ScoreCalculator>();

            // Skin Catalog Service (LiveOps: Garage & Collection)
            builder.BindService<ISkinCatalogService, SkinCatalogService>();

            // Grid State Serializer
            builder.BindService<GridStateSerializer>();

            // Global Release Production Services (game_plan.md §3)
            builder.BindInstance<ICloudSaveAdapter>(new PixelFlow.Services.GlobalRelease.EncryptedCloudSaveAdapter(storageService));
            builder.BindService<CloudSaveManager>();
            builder.BindService<PixelFlowAnalyticsTracker>();
            builder.BindService<PixelFlow.Services.GlobalRelease.PrivacyComplianceService>();
            builder.BindService<PixelFlow.Services.GlobalRelease.SilentCrashDiagnosticsService>();
            builder.BindService<PixelFlow.Services.GlobalRelease.InAppReviewService>();
            builder.BindService<PixelFlow.Services.GlobalRelease.LocalNotificationService>();

            // Default recovery: 3 retry → skip on failure
            builder.BindInstance<IRecoveryStrategy>(new DefaultRecoveryStrategy(maxRetries: 3));

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

            // ─────────────────────────────────────────────────────────────
            // Signal & Command Bindings — Domain Scoped Architecture
            // ─────────────────────────────────────────────────────────────

            // 1. Gameplay Core (Signals/Gameplay/ -> Commands/Gameplay/)
            builder.BindSignal<PixelFlow.Signals.InputInteractionSignal>().To<PixelFlow.Commands.ProcessInputCommand>();
            builder.BindSignal<PixelFlow.Signals.CheckWinConditionSignal>().To<PixelFlow.Commands.CheckWinConditionCommand>();
            builder.BindSignal<PixelFlow.Signals.PlaceViaductSignal>().To<PixelFlow.Commands.PlaceViaductCommand>();
            builder.BindSignal<PixelFlow.Signals.StartSimulationSignal>().To<PixelFlow.Commands.StartSimulationCommand>();
            builder.BindSignal<PixelFlow.Signals.PauseSimulationSignal>().To<PixelFlow.Commands.PauseSimulationCommand>();
            builder.BindSignal<PixelFlow.Signals.ClearJamSignal>().To<PixelFlow.Commands.ClearJamCommand>();
            builder.BindSignal<PixelFlow.Signals.ActivateRainbowRoadSignal>().To<PixelFlow.Commands.RainbowRoadCommand>();
            builder.BindSignal<PixelFlow.Signals.FlowScoreUpdatedSignal>();

            // 2. Level Progression & Save (Signals/Level/ -> Commands/Level/)
            builder.BindSignal<PixelFlow.Signals.LoadLevelSignal>().To<PixelFlow.Commands.LoadLevelCommand>();
            builder.BindCommand<PixelFlow.Signals.LevelCompletedSignal, PixelFlow.Commands.SaveProgressCommand>(ExecutionMode.Exclusive, priority: 0);
            builder.BindSignal<PixelFlow.Signals.LoadedInitialLevelSignal>();
            builder.BindSignal<PixelFlow.Signals.ProgressUpdatedSignal>();

            // 3. Hints & History (Signals/Hints/ & Signals/History/)
            builder.BindSignal<PixelFlow.Signals.RequestHintSignal>().To<PixelFlow.Commands.UseHintCommand>();
            builder.BindSignal<PixelFlow.Signals.UndoSignal>().To<PixelFlow.Commands.UndoCommand>();
            builder.BindSignal<PixelFlow.Signals.RedoSignal>().To<PixelFlow.Commands.RedoCommand>();

            // 4. Settings & Audio (Signals/Settings/ -> Commands/Settings/)
            builder.BindSignal<PixelFlow.Signals.ChangeThemeSignal>().To<PixelFlow.Commands.ChangeThemeCommand>();
            builder.BindSignal<PixelFlow.Signals.ChangeAudioVolumeSignal>().To<PixelFlow.Commands.ChangeAudioVolumeCommand>();
            builder.BindSignal<PixelFlow.Signals.ChangeColorBlindModeSignal>().To<PixelFlow.Commands.ChangeColorBlindModeCommand>();
            builder.BindSignal<PixelFlow.Signals.ToggleHapticsSignal>().To<PixelFlow.Commands.ToggleHapticsCommand>();

            // 5. Meta & Ads & Shop (Signals/Meta/ -> Commands/Meta/)
            builder.BindSignal<PixelFlow.Signals.ShowGarageSignal>();
            builder.BindSignal<PixelFlow.Signals.SkinUnlockedSignal>().To<PixelFlow.Commands.SkinUnlockCommand>();
            builder.BindSignal<PixelFlow.Signals.StopSkinUnlockedSignal>().To<PixelFlow.Commands.StopSkinUnlockCommand>();
            builder.BindSignal<PixelFlow.Signals.RequestInterstitialAdSignal>().To<PixelFlow.Commands.InterstitialAdCommand>();
            builder.BindSignal<PixelFlow.Signals.RushHourStartedSignal>();
            builder.BindSignal<PixelFlow.Signals.RushHourEndedSignal>();
            builder.BindSignal<PixelFlow.Signals.ProgressUpdatedSignal>();

            if (gameConfig == null)
            {
                throw new DataValidationException("GameConfig referansı GameContextLifecycle üzerinde atanmadı. Lütfen Bootstrap Config Assets alanlarını doldurun.");
            }
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] GameConfig reference bound successfully.");
            builder.BindInstance(gameConfig);

            // VehiclePartPool'u config ile başlat (pool boyutları data-driven)
            Views.VehiclePartPool.SetConfig(gameConfig);

            // StorageKeysConfigAsset — zero-hardcode PlayerPrefs anahtarları
            if (storageKeysConfig == null)
            {
                throw new DataValidationException("StorageKeysConfig referansı GameContextLifecycle üzerinde atanmadı. Lütfen Bootstrap Config Assets alanlarını doldurun.");
            }
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] StorageKeysConfig reference bound successfully.");
            builder.BindInstance(storageKeysConfig);

            // ThemePaletteAsset
            if (themePalette == null)
            {
                throw new DataValidationException("ThemePalette referansı GameContextLifecycle üzerinde atanmadı. Lütfen Bootstrap Config Assets alanlarını doldurun.");
            }
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] ThemePalette reference bound successfully.");
            builder.BindInstance(themePalette);

            // ColorBlindPaletteAsset — GDD §11.1: Renk körlüğü paleti
            if (colorBlindPalette == null)
            {
                throw new DataValidationException("ColorBlindPalette referansı GameContextLifecycle üzerinde atanmadı. Lütfen Bootstrap Config Assets alanlarını doldurun.");
            }
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] ColorBlindPalette reference bound successfully.");
            builder.BindInstance(colorBlindPalette);
            Models.ColorBlindPalette.Initialize(colorBlindPalette);

            // VehicleMaterialConfigAsset — araç görsel malzeme renkleri
            if (vehicleMaterialConfig == null)
            {
                throw new DataValidationException("VehicleMaterialConfig referansı GameContextLifecycle üzerinde atanmadı. Lütfen Bootstrap Config Assets alanlarını doldurun.");
            }
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] VehicleMaterialConfig reference bound successfully.");
            builder.BindInstance(vehicleMaterialConfig);
            // VehicleVisualConfigAsset — araç görsel parametreleri (config-driven)
            if (vehicleVisualConfig == null)
            {
                throw new DataValidationException("VehicleVisualConfig referansı GameContextLifecycle üzerinde atanmadı. Lütfen Bootstrap Config Assets alanlarını doldurun.");
            }
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] VehicleVisualConfig reference bound successfully.");
            builder.BindInstance(vehicleVisualConfig);
            Views.VehicleVisualFactory.Initialize(vehicleMaterialConfig, vehicleVisualConfig);

            // EconomyConfigAsset — GDD §9: Ekonomi/balance konfigürasyonu
            if (economyConfig == null)
            {
                throw new DataValidationException("EconomyConfig referansı GameContextLifecycle üzerinde atanmadı. Lütfen Bootstrap Config Assets alanlarını doldurun.");
            }
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] EconomyConfig reference bound successfully.");
            builder.BindInstance(economyConfig);

            // LevelCatalogAsset — GDD §3.6: Merkezi level kataloğu
            if (levelCatalog == null)
            {
                throw new DataValidationException("LevelCatalog referansı GameContextLifecycle üzerinde atanmadı. Lütfen Bootstrap Config Assets alanlarını doldurun.");
            }
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] LevelCatalog reference bound successfully.");
            builder.BindInstance(levelCatalog);

            // PhaseConfigAsset — phase configuration
            if (phaseConfig == null)
            {
                throw new DataValidationException("PhaseConfig referansı GameContextLifecycle üzerinde atanmadı. Lütfen Bootstrap Config Assets alanlarını doldurun.");
            }
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] PhaseConfig reference bound successfully.");
            builder.BindInstance(phaseConfig);

            // DifficultyFormulaConfigAsset — Zorluk formülü
            if (difficultyFormulaConfig == null)
            {
                throw new DataValidationException("DifficultyFormulaConfig referansı GameContextLifecycle üzerinde atanmadı. Lütfen Bootstrap Config Assets alanlarını doldurun.");
            }
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] DifficultyFormulaConfig reference bound successfully.");
            builder.BindInstance(difficultyFormulaConfig);

            // DefaultSkinIdsConfigAsset — Varsayılan skin ID'leri
            if (defaultSkinConfig == null)
            {
                throw new DataValidationException("DefaultSkinIdsConfig referansı GameContextLifecycle üzerinde atanmadı. Lütfen Bootstrap Config Assets alanlarını doldurun.");
            }
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] DefaultSkinIdsConfig reference bound successfully.");
            builder.BindInstance(defaultSkinConfig);

            // RushHourConfigAsset — LiveOps Rush Hour Event
            if (rushHourConfig == null)
            {
                throw new DataValidationException("RushHourConfig referansı GameContextLifecycle üzerinde atanmadı. Lütfen Bootstrap Config Assets alanlarını doldurun.");
            }
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] RushHourConfig reference bound successfully.");
            builder.BindInstance(rushHourConfig);

            // BouncyPhysicsConfigAsset — Global bouncy physics defaults
            if (bouncyPhysicsConfig == null)
            {
                throw new DataValidationException("BouncyPhysicsConfig referansı GameContextLifecycle üzerinde atanmadı. Lütfen Bootstrap Config Assets alanlarını doldurun.");
            }
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] BouncyPhysicsConfig reference bound successfully.");
            builder.BindInstance(bouncyPhysicsConfig);

            // StarCriteriaConfigAsset — Global star criteria defaults
            if (starCriteriaConfig == null)
            {
                throw new DataValidationException("StarCriteriaConfig referansı GameContextLifecycle üzerinde atanmadı. Lütfen Bootstrap Config Assets alanlarını doldurun.");
            }
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] StarCriteriaConfig reference bound successfully.");
            builder.BindInstance(starCriteriaConfig);
            
            // Register skins from Resources into SkinCatalogService
            var vehicleSkins = Resources.LoadAll<VehicleSkinConfig>("Configs/Skins");
            var stopSkins = Resources.LoadAll<StopSkinConfig>("Configs/Skins");
            
            // Fallback to "Skins" folder if not found in "Configs/Skins"
            if ((vehicleSkins == null || vehicleSkins.Length == 0) && (stopSkins == null || stopSkins.Length == 0))
            {
                vehicleSkins = Resources.LoadAll<VehicleSkinConfig>("Skins");
                stopSkins = Resources.LoadAll<StopSkinConfig>("Skins");
            }

            // We'll initialize the catalog after all services are bound by using a callback
            // The SkinCatalogService will be initialized in OnInitializeAsync
            
            }

        public ValueTask OnInitializeAsync(CancellationToken ct)
        {
            // Register skins from Resources into SkinCatalogService
            var root = GetComponentInParent<Root>();
            if (root != null && root.Context != null)
            {
                var skinCatalog = root.Context.Container.Resolve<ISkinCatalogService>();
                if (skinCatalog != null)
                {
                    var vehicleSkins = Resources.LoadAll<VehicleSkinConfig>("Configs/Skins");
                    var stopSkins = Resources.LoadAll<StopSkinConfig>("Configs/Skins");
                    
                    if ((vehicleSkins == null || vehicleSkins.Length == 0) && (stopSkins == null || stopSkins.Length == 0))
                    {
                        vehicleSkins = Resources.LoadAll<VehicleSkinConfig>("Skins");
                        stopSkins = Resources.LoadAll<StopSkinConfig>("Skins");
                    }

                    if (vehicleSkins != null && vehicleSkins.Length > 0)
                        skinCatalog.RegisterVehicleSkins(vehicleSkins);
                    
                    if (stopSkins != null && stopSkins.Length > 0)
                        skinCatalog.RegisterStopSkins(stopSkins);
                }
            }
            
            return default;
        }
        public ValueTask OnStartAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
