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
            builder.Bind<ILevelProgressionService, LevelProgressionService>();
            builder.Bind<ILevelLoaderService, LevelLoaderService>(); // GDD §8

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

            builder.BindSignal<PixelFlow.Signals.InputInteractionSignal>().To<PixelFlow.Commands.ProcessInputCommand>();
            builder.BindSignal<PixelFlow.Signals.CheckWinConditionSignal>().To<PixelFlow.Commands.CheckWinConditionCommand>();
            builder.BindSignal<PixelFlow.Signals.LoadLevelSignal>().To<PixelFlow.Commands.LoadLevelCommand>();
            builder.BindSignal<PixelFlow.Signals.RequestHintSignal>().To<PixelFlow.Commands.UseHintCommand>();
            builder.BindSignal<PixelFlow.Signals.ChangeThemeSignal>().To<PixelFlow.Commands.ChangeThemeCommand>();
            builder.BindCommand<PixelFlow.Signals.LevelCompletedSignal, PixelFlow.Commands.SaveProgressCommand>(ExecutionMode.Exclusive, priority: 0);
            builder.BindSignal<PixelFlow.Signals.UndoSignal>().To<PixelFlow.Commands.UndoCommand>();
            builder.BindSignal<PixelFlow.Signals.RedoSignal>().To<PixelFlow.Commands.RedoCommand>();
            builder.BindSignal<PixelFlow.Signals.TimerTickSignal>().To<PixelFlow.Commands.TimerCommand>();
            builder.BindSignal<PixelFlow.Signals.PlaceViaductSignal>().To<PixelFlow.Commands.PlaceViaductCommand>();
            builder.BindSignal<PixelFlow.Signals.RequestInterstitialAdSignal>().To<PixelFlow.Commands.InterstitialAdCommand>();
            // GDD §8: Yeni MVCS sinyalleri ve command'leri
            builder.BindSignal<PixelFlow.Signals.StartSimulationSignal>().To<PixelFlow.Commands.StartSimulationCommand>();
            builder.BindSignal<PixelFlow.Signals.PauseSimulationSignal>().To<PixelFlow.Commands.PauseSimulationCommand>();
            builder.BindSignal<PixelFlow.Signals.LoadedInitialLevelSignal>();
            builder.BindSignal<PixelFlow.Signals.FlowScoreUpdatedSignal>();
            builder.BindSignal<PixelFlow.Signals.ProgressUpdatedSignal>();

            // GameConfig ScriptableObject — Resources'tan yüklenir, tüm servislere enjekte edilebilir.
            var config = UnityEngine.Resources.Load<GameConfig>("GameConfig");
            if (config != null)
            {
                builder.BindInstance(config);
            }
        }

        public ValueTask OnInitializeAsync(CancellationToken ct) => default;
        public ValueTask OnStartAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
