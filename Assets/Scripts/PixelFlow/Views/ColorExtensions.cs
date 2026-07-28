using UnityEngine;

namespace PixelFlow.Views
{
    /// <summary>
    /// Color extension methods for Zero-Hardcode §2.2 compliance.
    /// 
    /// <see cref="WithAlpha"/> enables alpha-manipulation expressions like
    /// <c>new Color(r, g, b, 0.55f)</c> to be written as <c>color.WithAlpha(0.55f)</c>,
    /// which is a documented transformation on a config-sourced Color value rather
    /// than a hardcoded RGB literal.
    /// 
    /// Per game_plan.md §2.2: the RGB channels always come from a ScriptableObject
    /// (ThemePaletteAsset, ColorBlindPalette, etc.), and only the alpha override
    /// is specified inline — this is an acceptable pattern for rendering opacity.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Returns a new Color with the same RGB channels as <paramref name="color"/>
        /// and the specified <paramref name="alpha"/> value.
        /// </summary>
        /// <param name="color">Source color whose RGB channels are preserved.</param>
        /// <param name="alpha">Alpha value (0 = transparent, 1 = opaque).</param>
        /// <returns>A new Color instance with (r, g, b, alpha).</returns>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
