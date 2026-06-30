using PixelFlow.Models;

namespace PixelFlow.Services
{
    /// <summary>
    /// Level tamamlama anında skor ve yıldız hesaplar.
    /// Ham puan = grid hücre sayısı × 100
    /// Time bonus: kalan süre × çarpan (süre sınırı grid boyutuna bağlı)
    /// Hint penalty: her kullanılan hint başına %10 kesinti
    /// </summary>
    public static class ScoreCalculator
    {
        /// <summary>
        /// 3 yıldız: ham puanın %100'ü (hiç hint kullanılmamış, hızlı bitmiş)
        /// 2 yıldız: ham puanın %50'si
        /// 1 yıldız: ham puanın %25'i
        /// 0 yıldız: herhangi bir puan (tamamlama başarısız)
        /// </summary>
        public static (int finalScore, int stars) Calculate(
            int gridWidth, int gridHeight,
            float elapsedTime, int hintsUsed, int totalHintsAvailable)
        {
            // Ham puan: grid büyüklüğüne bağlı temel puan
            int cellCount = gridWidth * gridHeight;
            float baseScore = cellCount * 100f;

            // Zaman bonusu: grid büyüklüğüne göre ideal süre
            float idealTime = cellCount * 0.5f; // saniye cinsinden
            float timeMultiplier = elapsedTime <= idealTime
                ? 1.0f
                : System.Math.Max(0.25f, idealTime / elapsedTime);

            // Hint penalty: her hint %10 kesinti
            float hintMultiplier = 1f - (hintsUsed * 0.10f);
            if (hintMultiplier < 0f) hintMultiplier = 0f;

            float finalScore = baseScore * timeMultiplier * hintMultiplier;
            int roundedScore = (int)(finalScore + 0.5f);

            // Yıldız hesaplama
            float percentage = hintMultiplier * timeMultiplier;
            int stars;
            if (percentage >= 0.90f && hintsUsed == 0)
                stars = 3;
            else if (percentage >= 0.50f)
                stars = 2;
            else
                stars = 1;

            return (roundedScore, stars);
        }
    }
}
