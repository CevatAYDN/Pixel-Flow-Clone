using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
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
            // EncryptedStorageService: AES-256 + HMAC-SHA256 Anti-Cheat Güvenlikli Depolama
            builder.Bind<IPlayerPrefsService, EncryptedStorageService>();
            
            // Tüm Nexus Core Çekirdek Servisleri (13 Servis Tam Güç Entegrasyonu)
            builder.BindService<INexusService, FeedbackService>();
            builder.Bind<IFeedbackService, FeedbackService>();
            builder.BindService<INexusService, ObjectPoolService>();
            builder.Bind<IObjectPoolService, ObjectPoolService>();
            builder.BindService<INexusService, WindowManager>();
            builder.Bind<IWindowManager, WindowManager>();
            builder.BindService<INexusService, EconomyService>();
            builder.Bind<IEconomyService, EconomyService>();
            builder.BindService<INexusService, ProgressionService>();
            builder.Bind<IProgressionService, ProgressionService>();
            builder.BindService<INexusService, TickService>();
            builder.Bind<ITickService, TickService>();
            builder.BindService<INexusService, AdService>();
            builder.Bind<IAdService, AdService>();
            builder.BindService<INexusService, IapService>();
            builder.Bind<IIapService, IapService>();
            builder.BindService<INexusService, AnalyticsService>();
            builder.Bind<IAnalyticsService, AnalyticsService>();

            // PixelFlow Özel Servisleri
            builder.BindService<IPathService, PathService>();
            builder.BindService<IGameHistoryService, GameHistoryService>();
            builder.BindService<IVehicleSimulator, VehicleSimulator>();
            builder.BindService<PixelFlow.Services.IAudioService, PixelFlow.Services.AudioService>();
            builder.BindService<IGameplayTimerService, GameplayTimerService>();
            builder.Bind<ITimeProvider, UnityTimeProvider>();
            builder.BindService<ISaveThrottler, SaveThrottler>();
            builder.BindService<INexusService, HapticService>();
            builder.Bind<IHapticService, HapticService>();
            builder.BindService<INexusService, LoggerService>();
            builder.Bind<ILoggerService, LoggerService>();
            builder.BindService<ITutorialDriver, TutorialDriver>();
            builder.BindService<ICrisisAdService, CrisisAdService>();
            builder.BindService<IObstacleService, ObstacleService>();
            builder.BindService<INexusService, LocalizationService>();
            builder.BindService<ILocalizationService, LocalizationService>();
            builder.Bind<ILocalizationTableProvider, ResourceLocalizationTableProvider>();
            builder.BindService<IDailyCrisisService, DailyCrisisService>();
            builder.Bind<IPathSolver, RuntimePathSolver>();
            builder.Bind<IHintService, HintService>();
            builder.Bind<ILevelProgressionService, LevelProgressionService>();

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
            builder.BindSignal<PixelFlow.Signals.RequestReturnToHubSignal>().To<PixelFlow.Commands.ReturnToHubCommand>();
            builder.BindSignal<PixelFlow.Signals.RequestRewardedAdSignal>().To<PixelFlow.Commands.RewardedAdCommand>();
            builder.BindSignal<PixelFlow.Signals.RequestInterstitialAdSignal>().To<PixelFlow.Commands.InterstitialAdCommand>();
            builder.BindSignal<PixelFlow.Signals.EnterDistrictSignal>().To<PixelFlow.Commands.EnterDistrictCommand>();
        }

        public ValueTask OnInitializeAsync(CancellationToken ct) => default;
        public ValueTask OnStartAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
