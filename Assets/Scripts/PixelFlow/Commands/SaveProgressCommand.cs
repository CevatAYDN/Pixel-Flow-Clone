using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;
using PixelFlow.Data;
using UnityEngine;

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
        [Inject] public IEconomyService EconomyService { get; set; }
        [Inject] public IInventoryModel InventoryModel { get; set; }
        [Inject, OptionalInject] public GameConfig Config { get; set; }

        public void Execute(LevelCompletedSignal signal)
        {
            int previousUnlocked = ProgressModel.UnlockedLevels;
            var currentLevel = LevelModel.CurrentLevel;
            if (currentLevel != null)
            {
                ProgressModel.UnlockLevel(currentLevel.levelIndex);

                // Seviye başına en yüksek yıldız sayısını kalıcı sakla (LevelSelect ⭐ göstergesi).
                ProgressModel.RecordStars(currentLevel.levelIndex, GameSessionModel.StarsEarned);

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

            // Coin ödülü: flow score başına coin + seviye tamamlama bonusu
            // game_plan.md §2.2 (Zero-Silent-Fallback): sabitler GameConfig'ten gelir.
            var cfg = ResolveConfig();
            int coinPerFlow = cfg.CoinPerFlowScore;
            int levelBonus = cfg.LevelCompleteCoinBonus;
            int totalCoins = (GameSessionModel.CurrentFlowScore * coinPerFlow) + levelBonus;
            EconomyService?.Earn("coins", totalCoins, "level_complete");
            LoggerService?.Log($"[SaveProgressCommand] Awarded {totalCoins} coins (flow: {GameSessionModel.CurrentFlowScore}x{coinPerFlow} + bonus: {levelBonus}).");

            // Gem ödülü: game_plan.md §9.1 — 3 yıldızlı seviye tamamlamada sert para kazanılır.
            // Star Pass aktifse premium track ek bonusu eklenir (§9.3).
            if (stars >= 3 && InventoryModel != null)
            {
                int gemReward = cfg.GemsPerThreeStarLevel
                    + (InventoryModel.IsStarPassActive ? cfg.StarPassGemBonus : 0);
                if (gemReward > 0)
                {
                    InventoryModel.AddGems(gemReward);
                    LoggerService?.Log($"[SaveProgressCommand] Awarded {gemReward} gems for 3-star completion (StarPass={InventoryModel.IsStarPassActive}).");
                }
            }

            // Seviye tamamlandığı için yarım kalan bulmaca kaydını sil
            GridStateSerializer.ClearSave(PlayerPrefsService);

            SignalBus.Fire(new ProgressUpdatedSignal
            {
                UnlockedLevels = ProgressModel.UnlockedLevels
            });
        }

        // game_plan.md §2.2: config zorunludur. Build'de erişilemezse sessizce hardcode
        // değere düşmek yerine DataValidationException fırlatılır; editor/testte SO
        // varsayılanlarını taşıyan bir instance kullanılır (ScoreCalculator ile tutarlı desen).
        private GameConfig ResolveConfig()
        {
            if (Config != null) return Config;
#if !UNITY_EDITOR
            throw new DataValidationException("GameConfig erişilemedi! SaveProgressCommand coin ödülü hesaplanamıyor.");
#else
            return ScriptableObject.CreateInstance<GameConfig>();
#endif
        }

        public void Reset()
        {
            // Do not nullify injected properties to prevent null-ref risks on framework reuse
        }
    }
}
