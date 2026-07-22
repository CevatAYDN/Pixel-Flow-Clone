using UnityEngine;
using PixelFlow.Models;

namespace PixelFlow.Data
{
    /// <summary>
    /// GDD §11.1: Renk körlüğü paleti ScriptableObject'i.
    /// Tüm hardcoded Color değerleri bu asset'te toplanır.
    /// 
    /// Protanopia  : Kırmızı algılanmaz → Koyu sarıya kaydır.
    /// Deuteranopia: Yeşil algılanmaz → Maviye kaydır.
    /// Tritanopia  : Mavi/sarı ayrımı zor → Turuncu/sarıya kaydır.
    /// IBM Color Blind Safety Palette baz alınmıştır.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ColorBlindPalette",
        menuName = "PixelFlow/Color Blind Palette")]
    public class ColorBlindPaletteAsset : ScriptableObject
    {
        [System.Serializable]
        public struct ColorEntry
        {
            public Color Standard;
            public Color Protanopia;
            public Color Deuteranopia;
            public Color Tritanopia;
        }

        [Header("=== Pembe (Red) ===")]
        public ColorEntry Red = new ColorEntry
        {
            Standard    = new Color(1f, 0.239f, 0.498f),
            Protanopia  = new Color(0.95f, 0.90f, 0.25f),
            Deuteranopia = new Color(0.85f, 0.60f, 0.10f),
            Tritanopia  = new Color(0.85f, 0.30f, 0.10f)
        };

        [Header("=== Yeşil (Green) ===")]
        public ColorEntry Green = new ColorEntry
        {
            Standard    = new Color(0.420f, 0.796f, 0.467f),
            Protanopia  = new Color(0.00f, 0.65f, 0.75f),
            Deuteranopia = new Color(0.10f, 0.50f, 0.95f),
            Tritanopia  = new Color(0.20f, 0.65f, 0.40f)
        };

        [Header("=== Mavi (Blue) ===")]
        public ColorEntry Blue = new ColorEntry
        {
            Standard    = new Color(0f, 0.831f, 1f),
            Protanopia  = new Color(0.00f, 0.45f, 0.85f),
            Deuteranopia = new Color(0.10f, 0.50f, 0.95f),
            Tritanopia  = new Color(0.55f, 0.30f, 0.85f)
        };

        [Header("=== Sarı (Yellow) ===")]
        public ColorEntry Yellow = new ColorEntry
        {
            Standard    = new Color(1f, 0.851f, 0.239f),
            Protanopia  = new Color(0.95f, 0.85f, 0.20f),
            Deuteranopia = new Color(0.95f, 0.90f, 0.30f),
            Tritanopia  = new Color(0.95f, 0.50f, 0.20f)
        };

        [Header("=== Mor (Purple) ===")]
        public ColorEntry Purple = new ColorEntry
        {
            Standard    = new Color(0.702f, 0.420f, 1f),
            Protanopia  = new Color(0.55f, 0.30f, 0.85f),
            Deuteranopia = new Color(0.65f, 0.30f, 0.90f),
            Tritanopia  = new Color(0.75f, 0.20f, 0.65f)
        };

        // ─── Helper Methods ───

        public Color GetStandard(ColorType type)
        {
            return GetEntry(type).Standard;
        }

        public Color Remap(ColorType type, ColorBlindMode mode)
        {
            if (mode == ColorBlindMode.None)
                return GetStandard(type);

            var entry = GetEntry(type);
            switch (mode)
            {
                case ColorBlindMode.Protanopia:   return entry.Protanopia;
                case ColorBlindMode.Deuteranopia: return entry.Deuteranopia;
                case ColorBlindMode.Tritanopia:   return entry.Tritanopia;
                default:                          return entry.Standard;
            }
        }

        private ColorEntry GetEntry(ColorType type)
        {
            switch (type)
            {
                case ColorType.Red:     return Red;
                case ColorType.Green:   return Green;
                case ColorType.Blue:    return Blue;
                case ColorType.Yellow:  return Yellow;
                case ColorType.Purple:  return Purple;
                default:                return Red;
            }
        }

        /// <summary>
        /// Return all defined ColorType values (excluding None).
        /// Useful for iteration.
        /// </summary>
        public ColorType[] GetStandardColorTypes()
        {
            return new[] { ColorType.Red, ColorType.Green, ColorType.Blue, ColorType.Yellow, ColorType.Purple };
        }
    }
}
