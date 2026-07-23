using UnityEngine;

namespace PixelFlow.Data
{
    /// <summary>
    /// Color Jam 3D - Araç Skin Konfigürasyonu (ScriptableObject).
    /// Oyundaki araçların 3D görsellerini, ses efektlerini ve kilit açma bedellerini tanımlar.
    /// </summary>
    [CreateAssetMenu(fileName = "VehicleSkin_", menuName = "PixelFlow/Vehicle Skin Config", order = 20)]
    public class VehicleSkinConfig : ScriptableObject
    {
        [Header("=== Skin Identifiers ===")]
        [Tooltip("Benzersiz skin kimliği (örn: skin_bus_gold)")]
        public string SkinId = "skin_default";

        [Tooltip("Ekranda görünecek isim")]
        public string DisplayName = "Varsayılan Araç";

        [Tooltip("Skin'in ait olduğu renk ailesi")]
        public ColorType ColorFamily = ColorType.Blue;

        [Header("=== 3D Assets & Visuals ===")]
        [Tooltip("Araç 3D Prefab modeli")]
        public GameObject Prefab3D;

        [Tooltip("Garaj UI ikonu")]
        public Sprite Icon;

        [Header("=== Economy & Unlock ===")]
        [Tooltip("Kilit açma altın bedeli (0 ise ücretsiz)")]
        public int UnlockCoinCost = 100;

        [Tooltip("Altın yerine Ödüllü Reklam izlenerek mi açılıyor?")]
        public bool RequiresRewardedAd = false;

        [Header("=== Audio Juice ===")]
        [Tooltip("Araç motoru/hareket ses efekti")]
        public AudioClip EngineSound;

        [Tooltip("Araç korna ses efekti")]
        public AudioClip HornSound;
    }
}
