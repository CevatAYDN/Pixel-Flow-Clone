using PixelFlow.Models;

namespace PixelFlow.Services
{
    public static class ScoreCalculator
    {
        public static (int finalScore, int stars) Calculate(
            int gridWidth, int gridHeight,
            double elapsedTime, int hintsUsed, int totalHintsAvailable, int viaductsUsed)
        {
            double cellCount = gridWidth * gridHeight;
            double baseScore = cellCount * 100.0;

            double idealTime = cellCount * 0.5;
            double timeMultiplier = elapsedTime <= idealTime
                ? 1.0
                : System.Math.Max(0.25, idealTime / elapsedTime);

            double hintMultiplier = 1.0 - (hintsUsed * 0.10);
            if (hintMultiplier < 0.0) hintMultiplier = 0.0;

            double viaductPenalty = viaductsUsed * 0.08;
            if (viaductPenalty > 1.0) viaductPenalty = 1.0;

            double finalScore = baseScore * timeMultiplier * hintMultiplier * (1.0 - viaductPenalty);
            int roundedScore = (int)(finalScore + 0.5);

            int stars;
            if (viaductsUsed == 0)
                stars = 3;
            else if (viaductsUsed <= 2)
                stars = 2;
            else
                stars = 1;

            return (roundedScore, stars);
        }
    }
}
