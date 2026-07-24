using PixelFlow.Data;
using PixelFlow.Models;
using UnityEngine;

namespace PixelFlow.Services
{
    public static class ScoreCalculator
    {
        /// <summary>
        /// Skor hesaplar. game_plan.md §2.2 (Zero-Hardcode): tüm sabitler EconomyConfigAsset'ten gelir.
        /// config null ise build'de DataValidationException fırlatılır; editor/testte varsayılan
        /// EconomyConfigAsset instance'ı kullanılır (SO default değerleri).
        /// </summary>
        public static (int finalScore, int stars) Calculate(
            int gridWidth, int gridHeight,
            double elapsedTime, int hintsUsed, int totalHintsAvailable, int viaductsUsed,
            EconomyConfigAsset config = null)
        {
            if (config == null)
            {
#if !UNITY_EDITOR
                throw new DataValidationException("EconomyConfigAsset erişilemedi! ScoreCalculator sabitleri yüklenemiyor.");
#else
                config = ScriptableObject.CreateInstance<EconomyConfigAsset>();
#endif
            }

            double cellCount = gridWidth * gridHeight;

            double baseScore = cellCount * config.BaseScorePerCell;
            double idealTime = cellCount * config.IdealTimeFactor;
            double timeMultiplier = elapsedTime <= idealTime
                ? 1.0
                : System.Math.Max(config.MinTimeMultiplier, idealTime / elapsedTime);

            double hintMultiplier = 1.0 - (hintsUsed * config.HintPenaltyPerUse);
            if (hintMultiplier < 0.0) hintMultiplier = 0.0;

            double viaductPenalty = viaductsUsed * config.ViaductPenaltyPerUse;
            if (viaductPenalty > 1.0) viaductPenalty = 1.0;

            double finalScore = baseScore * timeMultiplier * hintMultiplier * (1.0 - viaductPenalty);
            int roundedScore = (int)(finalScore + 0.5);

            int stars = config.CalculateStars(viaductsUsed);

            return (roundedScore, stars);
        }
    }
}
