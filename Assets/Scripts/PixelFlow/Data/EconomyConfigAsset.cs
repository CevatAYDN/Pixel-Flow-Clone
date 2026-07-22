using UnityEngine;

namespace PixelFlow.Data
{
    /// <summary>
    /// Merkezi ekonomi konfigürasyonu ScriptableObject'i.
    /// Tüm hardcoded formula ve sabitler bu asset'te toplanır:
    /// - Viyadük bonus formülü (LevelLoaderService)
    /// - Skor hesaplama sabitleri (ScoreCalculator)
    /// - Yıldız eşik değerleri (ScoreCalculator)
    /// 
    /// GameContextLifecycle içinde Resources'tan yüklenir
    /// ve [Inject] ile servislere enjekte edilir.
    /// </summary>
    [CreateAssetMenu(
        fileName = "EconomyConfig",
        menuName = "PixelFlow/Economy Config")]
    public class EconomyConfigAsset : ScriptableObject
    {
        [Header("=== Viyadük Bonus (LevelLoaderService) ===")]
        [Tooltip("Her N level'da bir bonus viyadük: levelIndex / BonusDivisor")]
        public int ViaductBonusDivisor = 10;
        [Tooltip("Maksimum bonus viyadük sayısı")]
        public int ViaductBonusMax = 3;

        [Header("=== Skor Hesaplama (ScoreCalculator) ===")]
        [Tooltip("Her hücre için taban skor")]
        public double BaseScorePerCell = 100.0;
        [Tooltip("İdeal süre = cellCount * IdealTimeFactor saniye")]
        public double IdealTimeFactor = 0.5;
        [Tooltip("Minimum zaman çarpanı (sınır)")]
        public double MinTimeMultiplier = 0.25;
        [Tooltip("Her kullanılan hint başına ceza çarpanı")]
        public double HintPenaltyPerUse = 0.10;
        [Tooltip("Her kullanılan viyadük başına ceza çarpanı")]
        public double ViaductPenaltyPerUse = 0.08;

        [Header("=== Yıldız Eşikleri (ScoreCalculator) ===")]
        [Tooltip("3 yıldız için maksimum viyadük kullanımı")]
        public int ThreeStarsMaxViaducts = 0;
        [Tooltip("2 yıldız için maksimum viyadük kullanımı")]
        public int TwoStarsMaxViaducts = 2;

        // ─── Helper Methods ───

        /// <summary>
        /// Level index'e göre bonus viyadük sayısını hesaplar.
        /// Örn: divisor=10, max=3 → level 15 → 1 bonus; level 35 → 3 bonus.
        /// </summary>
        public int CalculateViaductBonus(int levelIndex)
        {
            int bonus = levelIndex / ViaductBonusDivisor;
            return Mathf.Min(bonus, ViaductBonusMax);
        }

        /// <summary>
        /// Viyadük kullanımına göre yıldız sayısını belirler.
        /// </summary>
        public int CalculateStars(int viaductsUsed)
        {
            if (viaductsUsed <= ThreeStarsMaxViaducts)
                return 3;
            if (viaductsUsed <= TwoStarsMaxViaducts)
                return 2;
            return 1;
        }

        /// <summary>
        /// Skor hesaplaması için ideal süreyi döndürür.
        /// </summary>
        public double GetIdealTime(int cellCount)
        {
            return cellCount * IdealTimeFactor;
        }
    }
}
