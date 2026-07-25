using UnityEngine;

namespace PixelFlow.Data
{
    /// <summary>
    /// Stop (Durak) Skin Konfigürasyonu (ScriptableObject).
    /// Oyundaki durakların görsel temalarını ve kilit açma bedellerini tanımlar.
    /// </summary>
    [CreateAssetMenu(fileName = "StopSkin_", menuName = "PixelFlow/Stop Skin Config", order = 21)]
    public class StopSkinConfig : ScriptableObject
    {
        [Header("=== Skin Identifiers ===")]
        [Tooltip("Benzersiz skin kimliği (örn: stop_skin_pastelpark)")]
        public string SkinId = "stop_skin_default";

        [Tooltip("Ekranda görünecek isim")]
        public string DisplayName = "Varsayılan Durak";

        [Tooltip("Skin'in ait olduğu tema paleti")]
        public int ThemePalette = 1;

        [Header("=== 3D Assets & Visuals ===")]
        [Tooltip("Durak 3D Prefab modeli")]
        public GameObject Prefab3D;

        [Tooltip("Garaj UI ikonu")]
        public Sprite Icon;

        [Header("=== Economy & Unlock ===")]
        [Tooltip("Kilit açma altın bedeli (0 ise ücretsiz)")]
        public int UnlockCoinCost = 800;

        [Tooltip("Altın yerine Ödüllü Reklam izlenerek mi açılıyor?")]
        public bool RequiresRewardedAd = false;
    }
}