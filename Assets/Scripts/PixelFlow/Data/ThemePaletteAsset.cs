using UnityEngine;
using PixelFlow.Models;

namespace PixelFlow.Data
{
    /// <summary>
    /// Merkezi tema paleti ScriptableObject'i. Tüm hardcoded renk sabitleri
    /// (camera background, ambient light, cell background, crash/feedback colors,
    /// obstacle palettes, fallback sprite border) bu asset'te toplanır.
    /// 
    /// GameContextLifecycle içinde GameConfig gibi Resources'tan yüklenir
    /// ve [Inject] ile servislere/mediator'lara enjekte edilir.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ThemePalette",
        menuName = "PixelFlow/Theme Palette")]
    public class ThemePaletteAsset : ScriptableObject
    {
        [System.Serializable]
        public struct ThemeColors
        {
            [Tooltip("Camera background color")]
            public Color CameraBackground;
            [Tooltip("RenderSettings.ambientLight")]
            public Color AmbientLight;
        }

        [System.Serializable]
        public struct ObstaclePalette
        {
            public Color Background;
            public Color Icon;
        }

        [Header("=== Camera & Ambient (per theme) ===")]
        public ThemeColors Dark = new ThemeColors
        {
            CameraBackground = new Color(0.043f, 0.059f, 0.098f),
            AmbientLight = new Color(0.3f, 0.3f, 0.4f)
        };
        public ThemeColors Light = new ThemeColors
        {
            CameraBackground = new Color(0.92f, 0.92f, 0.94f),
            AmbientLight = new Color(0.8f, 0.8f, 0.85f)
        };
        public ThemeColors Neon = new ThemeColors
        {
            CameraBackground = new Color(0.03f, 0.01f, 0.06f),
            AmbientLight = new Color(0.6f, 0.2f, 0.8f)
        };

        [Header("=== Cell Background (per theme) ===")]
        [Tooltip("Arka plan hücre rengi — Dark tema")]
        public Color CellBgDark = new Color(0.043f, 0.059f, 0.098f, 1f);
        [Tooltip("Arka plan hücre rengi — Light tema")]
        public Color CellBgLight = new Color(0.92f, 0.92f, 0.94f, 1f);
        [Tooltip("Arka plan hücre rengi — Neon tema")]
        public Color CellBgNeon = new Color(0.078f, 0.055f, 0.157f, 1f);

        [Header("=== Crash & Feedback ===")]
        [Tooltip("Kaza pulse animasyonu — parlak kırmızı")]
        public Color CrashPulseBright = new Color(0.937f, 0.267f, 0.267f);
        [Tooltip("Kaza pulse animasyonu — koyu kırmızı")]
        public Color CrashPulseDark = new Color(0.6f, 0.1f, 0.1f);
        [Tooltip("3. renk reddedildiğinde pulse rengi")]
        public Color RejectionPulse = new Color(0.937f, 0.267f, 0.267f, 1f);

        [Header("=== Fallback Sprite Border ===")]
        [Tooltip("Procedural fallback sprite border rengi")]
        public Color FallbackBorderColor = new Color(0.18f, 0.22f, 0.32f, 0.85f);

        [Header("=== Obstacle Colors ===")]
        public ObstaclePalette Lake = new ObstaclePalette
        {
            Background = new Color(0.10f, 0.28f, 0.55f, 1f),
            Icon = new Color(0.20f, 0.55f, 0.85f, 1f)
        };
        public ObstaclePalette Park = new ObstaclePalette
        {
            Background = new Color(0.15f, 0.40f, 0.20f, 1f),
            Icon = new Color(0.25f, 0.65f, 0.30f, 1f)
        };
        public ObstaclePalette Construction = new ObstaclePalette
        {
            Background = new Color(0.55f, 0.40f, 0.10f, 1f),
            Icon = new Color(0.85f, 0.65f, 0.15f, 1f)
        };
        public ObstaclePalette OneWay = new ObstaclePalette
        {
            Background = Color.clear,
            Icon = new Color(0.8f, 0.8f, 0.85f, 1f)
        };
        public ObstaclePalette Ferry = new ObstaclePalette
        {
            Background = new Color(0.15f, 0.35f, 0.50f, 1f),
            Icon = new Color(0.30f, 0.65f, 0.85f, 1f)
        };
        public ObstaclePalette NarrowPass = new ObstaclePalette
        {
            Background = new Color(0.45f, 0.45f, 0.50f, 1f),
            Icon = new Color(0.85f, 0.85f, 0.90f, 1f)
        };

        // ─── Helper Methods ───

        public Color GetCellBackground(AppTheme theme)
        {
            switch (theme)
            {
                case AppTheme.Dark: return CellBgDark;
                case AppTheme.Light: return CellBgLight;
                case AppTheme.Neon: return CellBgNeon;
                default: return CellBgDark;
            }
        }

        public ThemeColors GetThemeColors(AppTheme theme)
        {
            switch (theme)
            {
                case AppTheme.Dark: return Dark;
                case AppTheme.Light: return Light;
                case AppTheme.Neon: return Neon;
                default: return Dark;
            }
        }

        public ObstaclePalette GetObstaclePalette(ObstacleType type)
        {
            switch (type)
            {
                case ObstacleType.Lake: return Lake;
                case ObstacleType.Park: return Park;
                case ObstacleType.Construction: return Construction;
                case ObstacleType.OneWay: return OneWay;
                case ObstacleType.Ferry: return Ferry;
                case ObstacleType.NarrowPass: return NarrowPass;
                default: return default;
            }
        }
    }
}
