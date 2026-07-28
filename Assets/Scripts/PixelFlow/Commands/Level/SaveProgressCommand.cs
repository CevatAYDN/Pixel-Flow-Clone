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
        [Inject, OptionalInject] public IRushHourEventService RushHourEventService { get; set; }
        [Inject, OptionalInject] public GameConfig Config { get; set; }
        [Inject, OptionalInject] public StorageKeysConfigAsset Keys { get; set; }
        [Inject] public GridStateSerializer GridStateSerializer { get; set; }

        private string CoinCurrencyId => Keys?.CurrencyIdCoin;

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
                int completedCount = currentLevel.levelIndex + 1;
                if (completedCount > completed)
                {
                    PlayerPrefsService?.SetInt("PF_CompletedLevelsCount", completedCount);
                    PlayerPrefsService?.Save();
                }
            }
            LoggerService?.Log($"[SaveProgressCommand] Level completed! Unlocked levels: {previousUnlocked} -> {ProgressModel.UnlockedLevels}");

            // Star bazlı hint ödülü
            int stars = GameSessionModel.StarsEarned;
            HintModel?.AwardHintForStar(stars);
            LoggerService?.Log($"[SaveProgressCommand] Awarded hint for {stars} stars.");

            // Coin ödülü: flow score başına coin + seviye tamamlama bonusu (Rush Hour 2x çarpanı destekli)
            var cfg = ResolveConfig();
            if (string.IsNullOrEmpty(CoinCurrencyId)) throw new DataValidationException("SaveProgressCommand requires configured currency identifiers.");
            int coinPerFlow = cfg.CoinPerFlowScore;
            int levelBonus = cfg.LevelCompleteCoinBonus;
            float rushMultiplier = RushHourEventService != null && RushHourEventService.IsEventActive ? RushHourEventService.CoinMultiplier : 1.0f;
            int totalCoins = Mathf.RoundToInt(((GameSessionModel.CurrentFlowScore * coinPerFlow) + levelBonus) * rushMultiplier);
            EconomyService?.Earn(CoinCurrencyId, totalCoins, "level_complete");
            LoggerService?.Log($"[SaveProgressCommand] Awarded {totalCoins} coins (base: {(GameSessionModel.CurrentFlowScore * coinPerFlow) + levelBonus}, RushHour 2x={RushHourEventService?.IsEventActive}).");

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
            throw new DataValidationException("GameConfig erişilemedi! SaveProgressCommand coin ödülü hesaplanamıyor. GameContextLifecycle'da GameConfig yüklü olmalı.");
        }

        public void Reset()
        {
            // Do not nullify injected properties to prevent null-ref risks on framework reuse
        }
    }
}
