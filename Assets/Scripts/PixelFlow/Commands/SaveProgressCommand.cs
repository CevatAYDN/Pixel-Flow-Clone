using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;

namespace PixelFlow.Commands
{
    // Kayıt: GameContextLifecycle.OnConfigure'da fluent API ile yapılıyor.
    public class SaveProgressCommand : ICommand<LevelCompletedSignal>, IResettable
    {
        [Inject] public IProgressModel ProgressModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public IHintModel HintModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }

        public void Execute(LevelCompletedSignal signal)
        {
            int previousUnlocked = ProgressModel.UnlockedLevels;
            var currentLevel = LevelModel.CurrentLevel;
            if (currentLevel != null)
            {
                ProgressModel.UnlockLevel(currentLevel.levelIndex);

                // Tamamlanan seviye sayısını kaydet
                int completed = PlayerPrefsService != null ? PlayerPrefsService.GetInt("PF_CompletedLevelsCount", 0) : 0;
                if (currentLevel.levelIndex > completed)
                {
                    PlayerPrefsService?.SetInt("PF_CompletedLevelsCount", currentLevel.levelIndex);
                    PlayerPrefsService?.Save();
                }
            }
            LoggerService?.Log($"[SaveProgressCommand] Level completed! Unlocked levels: {previousUnlocked} -> {ProgressModel.UnlockedLevels}");

            // Star bazlı hint ödülü
            int stars = GameSessionModel.StarsEarned;
            HintModel?.AwardHintForStar(stars);
            LoggerService?.Log($"[SaveProgressCommand] Awarded hint for {stars} stars.");

            // Seviye tamamlandığı için yarım kalan bulmaca kaydını sil
            GridStateSerializer.ClearSave(PlayerPrefsService);

            SignalBus.Fire(new ProgressUpdatedSignal
            {
                UnlockedLevels = ProgressModel.UnlockedLevels
            });
        }

        public void Reset()
        {
            // Do not nullify injected properties to prevent null-ref risks on framework reuse
        }
    }
}
