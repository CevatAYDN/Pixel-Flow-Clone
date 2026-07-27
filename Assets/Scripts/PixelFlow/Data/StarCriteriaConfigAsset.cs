using UnityEngine;

namespace PixelFlow.Data
{
    /// <summary>
    /// Yıldız kriterleri konfigürasyonu - game_plan.md §3.5'e göre.
    /// LevelData.stars artık bu asset'i referans alacak.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StarCriteriaConfig",
        menuName = "PixelFlow/Star Criteria Config")]
    public class StarCriteriaConfigAsset : ScriptableObject
    {
        [Header("=== Yıldız Eşikleri (Viyadük Kullanımına Göre) ===")]
        [Tooltip("3 yıldız için maksimum viyadük kullanımı. Varsayılan: 0")]
        [Range(0, 10)]
        public int ThreeStarsMaxViaducts = 0;

        [Tooltip("2 yıldız için maksimum viyadük kullanımı. Varsayılan: 2")]
        [Range(0, 10)]
        public int TwoStarsMaxViaducts = 2;

        [Header("=== Görünen Metinler (Editörde görüntüleme için) ===")]
        [Tooltip("1 yıldız açıklaması")]
        public string OneStar = "complete";

        [Tooltip("2 yıldız açıklaması")]
        public string TwoStars = "viaducts_used <= 2";

        [Tooltip("3 yıldız açıklaması")]
        public string ThreeStars = "viaducts_used == 0";
    }
}