using PixelFlow.Models;

namespace PixelFlow.Services
{
    public static class ScoreCalculator
    {
        public static (int finalScore, int stars) Calculate(
            int gridWidth, int gridHeight,
            float elapsedTime, int hintsUsed, int totalHintsAvailable, int viaductsUsed)
        {
            int cellCount = gridWidth * gridHeight;
            float baseScore = cellCount * 100f;

            float idealTime = cellCount * 0.5f;
            float timeMultiplier = elapsedTime <= idealTime
                ? 1.0f
                : System.Math.Max(0.25f, idealTime / elapsedTime);

            float hintMultiplier = 1f - (hintsUsed * 0.10f);
            if (hintMultiplier < 0f) hintMultiplier = 0f;

            float viaductPenalty = viaductsUsed * 0.08f;
            if (viaductPenalty > 1f) viaductPenalty = 1f;

            float finalScore = baseScore * timeMultiplier * hintMultiplier * (1f - viaductPenalty);
            int roundedScore = (int)(finalScore + 0.5f);

            float scoreRatio = cellCount > 0 ? finalScore / baseScore : 0f;
            int stars;
            if (scoreRatio >= 0.8f)
                stars = 3;
            else if (roundedScore > 0)
                stars = 2;
            else
                stars = 1;

            return (roundedScore, stars);
        }
    }
}
