using UnityEngine;
using PixelFlow.Data;

namespace PixelFlow.Models
{
    public enum ColorBlindMode
    {
        None = 0,
        Protanopia = 1,
        Deuteranopia = 2,
        Tritanopia = 3,
    }

    /// <summary>
    /// GDD §11.1: Renk körlüğü paleti.
    ///   Protanopia  : Kırmızı algılanmaz → Koyu sarıya kaydır.
    ///   Deuteranopia: Yeşil algılanmaz → Maviye kaydır.
    ///   Tritanopia  : Mavi/sarı ayrımı zor → Turuncu/sarıya kaydır.
    /// IBM Color Blind Safety Palette baz alınmıştır.
    ///
    /// Statik wrapper — tüm renk değerleri ColorBlindPaletteAsset ScriptableObject'inden gelir.
    /// Initialize() ile bootstrap'ta atanır; atanmazsa hardcoded fallback kullanılır.
    /// </summary>
    public static class ColorBlindPalette
    {
        private static ColorBlindPaletteAsset _asset;

        /// <summary>
        /// Bootstrap'ta GameContextLifecycle tarafından çağrılır.
        /// Asset atandıktan sonra tüm renk değerleri SO'dan alınır.
        /// </summary>
        public static void Initialize(ColorBlindPaletteAsset asset)
        {
            _asset = asset;
        }

        public static Color Remap(ColorType type, ColorBlindMode mode)
        {
            var asset = _asset;
            if (asset != null)
                return asset.Remap(type, mode);
            return RemapFallback(type, mode);
        }

        public static Color GetStandard(ColorType type)
        {
            var asset = _asset;
            if (asset != null)
                return asset.GetStandard(type);
            return GetStandardFallback(type);
        }

        // Hardcoded fallback (asset yokken test/editor için)
        private static Color GetStandardFallback(ColorType type)
        {
            switch (type)
            {
                case ColorType.Red:    return new Color(1f, 0.239f, 0.498f);
                case ColorType.Green:  return new Color(0.420f, 0.796f, 0.467f);
                case ColorType.Blue:   return new Color(0f, 0.831f, 1f);
                case ColorType.Yellow: return new Color(1f, 0.851f, 0.239f);
                case ColorType.Purple: return new Color(0.702f, 0.420f, 1f);
                default:               return Color.gray;
            }
        }

        // Full remap fallback (asset yokken test/editor için — tüm 15 renk korunur)
        private static Color RemapFallback(ColorType type, ColorBlindMode mode)
        {
            if (mode == ColorBlindMode.None) return GetStandardFallback(type);
            switch (type)
            {
                case ColorType.Red:
                    if (mode == ColorBlindMode.Protanopia)  return new Color(0.95f, 0.90f, 0.25f);
                    if (mode == ColorBlindMode.Deuteranopia) return new Color(0.85f, 0.60f, 0.10f);
                    return new Color(0.85f, 0.30f, 0.10f);
                case ColorType.Green:
                    if (mode == ColorBlindMode.Protanopia)  return new Color(0.00f, 0.65f, 0.75f);
                    if (mode == ColorBlindMode.Deuteranopia) return new Color(0.10f, 0.50f, 0.95f);
                    return new Color(0.20f, 0.65f, 0.40f);
                case ColorType.Blue:
                    if (mode == ColorBlindMode.Protanopia)  return new Color(0.00f, 0.45f, 0.85f);
                    if (mode == ColorBlindMode.Deuteranopia) return new Color(0.10f, 0.50f, 0.95f);
                    return new Color(0.55f, 0.30f, 0.85f);
                case ColorType.Yellow:
                    if (mode == ColorBlindMode.Protanopia)  return new Color(0.95f, 0.85f, 0.20f);
                    if (mode == ColorBlindMode.Deuteranopia) return new Color(0.95f, 0.90f, 0.30f);
                    return new Color(0.95f, 0.50f, 0.20f);
                case ColorType.Purple:
                    if (mode == ColorBlindMode.Protanopia)  return new Color(0.55f, 0.30f, 0.85f);
                    if (mode == ColorBlindMode.Deuteranopia) return new Color(0.65f, 0.30f, 0.90f);
                    return new Color(0.75f, 0.20f, 0.65f);
                default:
                    return GetStandardFallback(type);
            }
        }

        public static string GetColorBlindLabel(ColorBlindMode mode)
        {
            switch (mode)
            {
                case ColorBlindMode.None:         return "Renk Körlüğü: Kapalı";
                case ColorBlindMode.Protanopia:   return "Renk Körlüğü: Protanopia";
                case ColorBlindMode.Deuteranopia: return "Renk Körlüğü: Deuteranopia";
                case ColorBlindMode.Tritanopia:   return "Renk Körlüğü: Tritanopia";
                default: return "Renk Körlüğü: Bilinmiyor";
            }
        }
    }
}
