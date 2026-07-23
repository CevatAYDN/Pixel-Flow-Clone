using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;

using UnityEngine;

namespace PixelFlow
{
    public class GameContextLifecycle : MonoBehaviour, IContextLifecycle
    {
        public void OnConfigure(IContextBuilder builder)
        {
            NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] OnConfigure: Initializing framework dependency injection bindings...");
            // PlayerPrefs servisini singleton olarak bağla; kalıcı state kullanan tüm
            // modeller bunu constructor injection ile alır (test edilebilir).
            builder.Bind<IPlayerPrefsService, EncryptedStorageService>();
            
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
            builder.BindService<PixelFlow.Services.IAudioService, PixelFlow.Services.AudioService>();
            builder.BindService<IGameplayTimerService, GameplayTimerService>();
            builder.Bind<ITimeProvider, UnityTimeProvider>();
            builder.BindService<ISaveThrottler, SaveThrottler>();
            builder.BindService<IHapticService, HapticService>();
            builder.BindService<ILoggerService, LoggerService>();
            builder.BindService<ITutorialDriver, TutorialDriver>();
            builder.BindService<ICrisisAdService, CrisisAdService>();
            builder.BindService<IObstacleService, ObstacleService>();
            builder.BindService<ILocalizationService, LocalizationService>();
            builder.Bind<ILocalizationTableProvider, ResourceLocalizationTableProvider>();
            builder.BindService<IDailyCrisisService, DailyCrisisService>();
            builder.Bind<IPathSolver, RuntimePathSolver>();
            builder.BindService<IHintService, HintService>(); // Fixed: was Bind<> now BindService<> for auto-init
            builder.BindService<IPowerUpService, PowerUpService>();
            builder.Bind<ILevelProgressionService, LevelProgressionService>();
            builder.BindService<ILevelLoaderService, LevelLoaderService>(); // GDD §8: DI injection for level loading

            // Global Release Production Services (game_plan.md §3)
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

            builder.BindSignal<PixelFlow.Signals.InputInteractionSignal>().To<PixelFlow.Commands.ProcessInputCommand>();
            builder.BindSignal<PixelFlow.Signals.CheckWinConditionSignal>().To<PixelFlow.Commands.CheckWinConditionCommand>();
            builder.BindSignal<PixelFlow.Signals.LoadLevelSignal>().To<PixelFlow.Commands.LoadLevelCommand>();
            builder.BindSignal<PixelFlow.Signals.RequestHintSignal>().To<PixelFlow.Commands.UseHintCommand>();
            builder.BindSignal<PixelFlow.Signals.ActivateRainbowRoadSignal>().To<PixelFlow.Commands.RainbowRoadCommand>();
            builder.BindSignal<PixelFlow.Signals.ClearJamSignal>().To<PixelFlow.Commands.ClearJamCommand>();
            builder.BindSignal<PixelFlow.Signals.ChangeThemeSignal>().To<PixelFlow.Commands.ChangeThemeCommand>();
            builder.BindCommand<PixelFlow.Signals.LevelCompletedSignal, PixelFlow.Commands.SaveProgressCommand>(ExecutionMode.Exclusive, priority: 0);
            builder.BindSignal<PixelFlow.Signals.UndoSignal>().To<PixelFlow.Commands.UndoCommand>();
            builder.BindSignal<PixelFlow.Signals.RedoSignal>().To<PixelFlow.Commands.RedoCommand>();
            builder.BindSignal<PixelFlow.Signals.PlaceViaductSignal>().To<PixelFlow.Commands.PlaceViaductCommand>();
            builder.BindSignal<PixelFlow.Signals.RequestInterstitialAdSignal>().To<PixelFlow.Commands.InterstitialAdCommand>();
            // GDD §8: Yeni MVCS sinyalleri ve command'leri
            builder.BindSignal<PixelFlow.Signals.StartSimulationSignal>().To<PixelFlow.Commands.StartSimulationCommand>();
            builder.BindSignal<PixelFlow.Signals.PauseSimulationSignal>().To<PixelFlow.Commands.PauseSimulationCommand>();
            builder.BindSignal<PixelFlow.Signals.LoadedInitialLevelSignal>();
            builder.BindSignal<PixelFlow.Signals.FlowScoreUpdatedSignal>();
            builder.BindSignal<PixelFlow.Signals.ProgressUpdatedSignal>();

            // GameConfig ScriptableObject — Resources'tan yüklenir (game_plan.md §2.2: Zero Silent Fallback Policy)
            var config = UnityEngine.Resources.Load<GameConfig>("Configs/GameConfig");
            if (config == null)
            {
#if !UNITY_EDITOR
                throw new DataValidationException("Resources/Configs/GameConfig.asset bulunamadı!");
#else
                config = UnityEngine.ScriptableObject.CreateInstance<GameConfig>();
                config.name = "GameConfig (Runtime Default)";
                NexusRuntime.Logger?.LogWarning("[PixelFlow.GameContextLifecycle] GameConfig.asset not found in Resources. Using runtime defaults.");
#endif
            }
            else
            {
                NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] Configs/GameConfig asset loaded successfully.");
            }
            builder.BindInstance(config);

            // ThemePaletteAsset
            var palette = UnityEngine.Resources.Load<ThemePaletteAsset>("Configs/ThemePalette");
            if (palette == null)
            {
#if !UNITY_EDITOR
                throw new DataValidationException("Resources/Configs/ThemePalette.asset bulunamadı!");
#else
                palette = UnityEngine.ScriptableObject.CreateInstance<ThemePaletteAsset>();
                palette.name = "ThemePalette (Runtime Default)";
                NexusRuntime.Logger?.LogWarning("[PixelFlow.GameContextLifecycle] ThemePalette.asset not found in Resources. Using runtime defaults.");
#endif
            }
            else
            {
                NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] Configs/ThemePalette asset loaded successfully.");
            }
            builder.BindInstance(palette);

            // ColorBlindPaletteAsset — GDD §11.1: Renk körlüğü paleti
            var colorBlindPalette = UnityEngine.Resources.Load<ColorBlindPaletteAsset>("Configs/ColorBlindPalette");
            if (colorBlindPalette == null)
            {
#if !UNITY_EDITOR
                throw new DataValidationException("Resources/Configs/ColorBlindPalette.asset bulunamadı!");
#else
                colorBlindPalette = UnityEngine.ScriptableObject.CreateInstance<ColorBlindPaletteAsset>();
                colorBlindPalette.name = "ColorBlindPalette (Runtime Default)";
                NexusRuntime.Logger?.LogWarning("[PixelFlow.GameContextLifecycle] ColorBlindPalette.asset not found in Resources. Using runtime defaults.");
#endif
            }
            else
            {
                NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] Configs/ColorBlindPalette asset loaded successfully.");
            }
            builder.BindInstance(colorBlindPalette);
            Models.ColorBlindPalette.Initialize(colorBlindPalette);

            // VehicleMaterialConfigAsset — araç görsel malzeme renkleri
            var vehicleMatConfig = UnityEngine.Resources.Load<VehicleMaterialConfigAsset>("Configs/VehicleMaterialConfig");
            if (vehicleMatConfig == null)
            {
#if !UNITY_EDITOR
                throw new DataValidationException("Resources/Configs/VehicleMaterialConfig.asset bulunamadı!");
#else
                vehicleMatConfig = UnityEngine.ScriptableObject.CreateInstance<VehicleMaterialConfigAsset>();
                vehicleMatConfig.name = "VehicleMaterialConfig (Runtime Default)";
                NexusRuntime.Logger?.LogWarning("[PixelFlow.GameContextLifecycle] VehicleMaterialConfig.asset not found in Resources. Using runtime defaults.");
#endif
            }
            else
            {
                NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] Configs/VehicleMaterialConfig asset loaded successfully.");
            }
            builder.BindInstance(vehicleMatConfig);
            Views.VehicleVisualFactory.Initialize(vehicleMatConfig);

            // EconomyConfigAsset — GDD §9: Ekonomi/balance konfigürasyonu
            var economyConfig = UnityEngine.Resources.Load<EconomyConfigAsset>("Configs/EconomyConfig");
            if (economyConfig == null)
            {
#if !UNITY_EDITOR
                throw new DataValidationException("Resources/Configs/EconomyConfig.asset bulunamadı!");
#else
                economyConfig = UnityEngine.ScriptableObject.CreateInstance<EconomyConfigAsset>();
                economyConfig.name = "EconomyConfig (Runtime Default)";
                NexusRuntime.Logger?.LogWarning("[PixelFlow.GameContextLifecycle] EconomyConfig.asset not found in Resources. Using runtime defaults.");
#endif
            }
            else
            {
                NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] Configs/EconomyConfig asset loaded successfully.");
            }
            builder.BindInstance(economyConfig);

            // LevelCatalogAsset — GDD §3.6: Merkezi level kataloğu
            var levelCatalog = UnityEngine.Resources.Load<LevelCatalogAsset>("Configs/LevelCatalog");
            if (levelCatalog == null)
            {
#if !UNITY_EDITOR
                throw new DataValidationException("Resources/Configs/LevelCatalog.asset bulunamadı!");
#else
                levelCatalog = UnityEngine.ScriptableObject.CreateInstance<LevelCatalogAsset>();
                levelCatalog.name = "LevelCatalog (Runtime Default)";
                NexusRuntime.Logger?.LogWarning("[PixelFlow.GameContextLifecycle] Configs/LevelCatalog.asset not found in Resources. Using runtime defaults (fallback to Resources.Load chain).");
#endif
            }
            else
            {
                NexusRuntime.Logger?.Log("[PixelFlow.GameContextLifecycle] Configs/LevelCatalog asset loaded successfully.");
            }
            builder.BindInstance(levelCatalog);
        }

        public ValueTask OnInitializeAsync(CancellationToken ct) => default;
        public ValueTask OnStartAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
