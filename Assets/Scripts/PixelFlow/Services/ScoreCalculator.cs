using PixelFlow.Data;
using PixelFlow.Models;

namespace PixelFlow.Services
{
    public static class ScoreCalculator
    {
        /// <summary>
        /// Skor hesaplar. EconomyConfigAsset yoksa hardcoded fallback kullanır.
        /// </summary>
        public static (int finalScore, int stars) Calculate(
            int gridWidth, int gridHeight,
            double elapsedTime, int hintsUsed, int totalHintsAvailable, int viaductsUsed,
            EconomyConfigAsset config = null)
        {
            double cellCount = gridWidth * gridHeight;

            // EconomyConfigAsset'ten değerler (fallback'ler orijinal sabitler)
            double baseScorePerCell = config != null ? config.BaseScorePerCell : 100.0;
            double idealTimeFactor = config != null ? config.IdealTimeFactor : 0.5;
            double minTimeMultiplier = config != null ? config.MinTimeMultiplier : 0.25;
            double hintPenaltyPerUse = config != null ? config.HintPenaltyPerUse : 0.10;
            double viaductPenaltyPerUse = config != null ? config.ViaductPenaltyPerUse : 0.08;

            double baseScore = cellCount * baseScorePerCell;
            double idealTime = cellCount * idealTimeFactor;
            double timeMultiplier = elapsedTime <= idealTime
                ? 1.0
                : System.Math.Max(minTimeMultiplier, idealTime / elapsedTime);

            double hintMultiplier = 1.0 - (hintsUsed * hintPenaltyPerUse);
            if (hintMultiplier < 0.0) hintMultiplier = 0.0;

            double viaductPenalty = viaductsUsed * viaductPenaltyPerUse;
            if (viaductPenalty > 1.0) viaductPenalty = 1.0;

            double finalScore = baseScore * timeMultiplier * hintMultiplier * (1.0 - viaductPenalty);
            int roundedScore = (int)(finalScore + 0.5);

            int stars;
            if (config != null)
                stars = config.CalculateStars(viaductsUsed);
            else
            {
                // Fallback (orijinal sabitler)
                if (viaductsUsed == 0)
                    stars = 3;
                else if (viaductsUsed <= 2)
                    stars = 2;
                else
                    stars = 1;
            }

            return (roundedScore, stars);
        }
    }
}
