using UnityEngine;

namespace PixelFlow.Data
{
    /// <summary>
    /// Araç görsellerinde kullanılan shared material renk konfigürasyonu.
    /// Tüm hardcoded Color değerleri bu asset'te toplanır.
    /// 
    /// GameContextLifecycle içinde Resources'tan yüklenir
    /// ve VehicleVisualFactory.Initialize() ile atanır.
    /// </summary>
    [CreateAssetMenu(
        fileName = "VehicleMaterialConfig",
        menuName = "PixelFlow/Vehicle Material Config")]
    public class VehicleMaterialConfigAsset : ScriptableObject
    {
        [Header("=== Vehicle Body Materials ===")]
        [Tooltip("Varsayılan gövde/sprit rengi (beyaz — runtimeda ColorType ile çarpılır)")]
        public Color SpriteColor = Color.white;

        [Tooltip("Tekerlek ve aksamlarda kullanılan koyu metal rengi")]
        public Color MetalColor = new Color(0.15f, 0.15f, 0.18f, 1f);

        [Tooltip("Cam / ön cam rengi (yarı saydam camgöbeği)")]
        public Color WindowColor = new Color(0.2f, 0.9f, 1f, 0.9f);

        [Header("=== Lighting ===")]
        [Tooltip("Far / lokomotif ön ışık rengi (sıcak sarı)")]
        public Color HeadlightColor = new Color(1f, 0.95f, 0.5f, 1f);

        [Tooltip("Arka stop lambası rengi (kırmızı)")]
        public Color TaillightColor = new Color(1f, 0.15f, 0.15f, 1f);

        [Header("=== Accents ===")]
        [Tooltip("Beyaz aksan (çatı şeridi vb.)")]
        public Color WhiteAccentColor = Color.white;
    }
}
