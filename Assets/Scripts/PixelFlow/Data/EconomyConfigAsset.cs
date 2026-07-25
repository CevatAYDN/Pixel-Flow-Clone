using UnityEngine;
using System.Collections.Generic;

namespace PixelFlow.Data
{
    /// <summary>
    /// Merkezi ekonomi konfigürasyonu ScriptableObject'i.
    /// Tüm hardcoded formula ve sabitler bu asset'te toplanır:
    /// - Viyadük bonus formülü (LevelLoaderService)
    /// - Skor hesaplama sabitleri (ScoreCalculator)
    /// - Yıldız eşik değerleri (ScoreCalculator)
    /// - IAP Ürün Kataloğu (game_plan.md §9.3)
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

        [Header("=== IAP Ürün Kataloğu (game_plan.md §9.3) ===")]
        [Tooltip("IAP ürün tanımları - Product ID, fiyat, içerik ve tip")]
        public List<IapProductDefinition> IapProducts = new List<IapProductDefinition>();

        /// <summary>
        /// game_plan.md §9.3 kataloğundaki 9 varsayılan ürünü liste boşsa otomatik oluşturur/döndürür.
        /// </summary>
        public List<IapProductDefinition> EnsureCanonicalIapProducts()
        {
            if (IapProducts == null) IapProducts = new List<IapProductDefinition>();
            if (IapProducts.Count == 0)
            {
                IapProducts = GetCanonicalIapProducts();
            }
            return IapProducts;
        }

        public static List<IapProductDefinition> GetCanonicalIapProducts()
        {
            return new List<IapProductDefinition>
            {
                new IapProductDefinition { ProductId = "no_ads", DisplayName = "No Ads", PriceUsd = 2.99f, Type = IapProductType.NonConsumable, RemovesAds = true, Description = "Removes all interstitial ads" },
                new IapProductDefinition { ProductId = "starter_pack", DisplayName = "Starter Pack", PriceUsd = 0.99f, Type = IapProductType.NonConsumable, CoinAmount = 1000, GemAmount = 50, UnlockSkinId = "skin_car_rare_01", Description = "1000 Coins + 50 Gems + Rare Car Skin" },
                new IapProductDefinition { ProductId = "coin_pack_s", DisplayName = "Coin Pack S", PriceUsd = 1.99f, Type = IapProductType.Consumable, CoinAmount = 2500, Description = "2,500 Gold Coins" },
                new IapProductDefinition { ProductId = "coin_pack_m", DisplayName = "Coin Pack M", PriceUsd = 4.99f, Type = IapProductType.Consumable, CoinAmount = 7500, Description = "7,500 Gold Coins" },
                new IapProductDefinition { ProductId = "coin_pack_l", DisplayName = "Coin Pack L", PriceUsd = 9.99f, Type = IapProductType.Consumable, CoinAmount = 20000, Description = "20,000 Gold Coins" },
                new IapProductDefinition { ProductId = "gem_pack_s", DisplayName = "Gem Pack S", PriceUsd = 2.99f, Type = IapProductType.Consumable, GemAmount = 100, Description = "100 Gems" },
                new IapProductDefinition { ProductId = "gem_pack_m", DisplayName = "Gem Pack M", PriceUsd = 7.99f, Type = IapProductType.Consumable, GemAmount = 350, Description = "350 Gems" },
                new IapProductDefinition { ProductId = "star_pass", DisplayName = "Star Pass", PriceUsd = 4.99f, Type = IapProductType.Subscription, Description = "30 day seasonal reward track" },
                new IapProductDefinition { ProductId = "vip_bundle", DisplayName = "VIP Bundle", PriceUsd = 14.99f, Type = IapProductType.NonConsumable, RemovesAds = true, CoinAmount = 5000, GemAmount = 200, UnlockSkinId = "skin_vip_golden", Description = "No Ads + 5000 Coins + 200 Gems + Legendary Skin" },
            };
        }

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

    /// <summary>
    /// IAP ürün tanımı - game_plan.md §9.3'e göre.
    /// </summary>
    [System.Serializable]
    public class IapProductDefinition
    {
        [Tooltip("Unity IAP Product ID (örn: no_ads, starter_pack, coin_pack_s)")]
        public string ProductId;

        [Tooltip("Görünen isim")]
        public string DisplayName;

        [Tooltip("Fiyat (USD)")]
        public float PriceUsd;

        [Tooltip("Para birimi kodu (USD, EUR, TRY, etc.)")]
        public string CurrencyCode = "USD";

        [Tooltip("Ürün tipi")]
        public IapProductType Type = IapProductType.Consumable;

        [Header("İçerik (Consumable/Non-consumable için)")]
        [Tooltip("Coin miktarı")]
        public int CoinAmount;

        [Tooltip("Gem miktarı")]
        public int GemAmount;

        [Tooltip("Açılacak Skin ID (Non-consumable için)")]
        public string UnlockSkinId;

        [Tooltip("No Ads özelliği (Non-consumable için)")]
        public bool RemovesAds;

        [Tooltip("Açıklama")]
        [TextArea(2, 4)]
        public string Description;
    }

    /// <summary>
    /// IAP ürün tipleri - game_plan.md §9.3'e göre.
    /// </summary>
    public enum IapProductType
    {
        Consumable,          // Coin/Gem paketleri, tekrar satın alınabilir
        NonConsumable,       // No Ads, Starter Pack, VIP Bundle - tek seferlik
        Subscription         // Star Pass - abonelik benzeri
    }
}
