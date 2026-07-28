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
            public Sprite Sprite;
            public float IconScale;
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
        public ThemeColors Candy = new ThemeColors
        {
            CameraBackground = new Color(0.98f, 0.90f, 0.95f),
            AmbientLight = new Color(0.95f, 0.85f, 0.90f)
        };
        public ThemeColors Forest = new ThemeColors
        {
            CameraBackground = new Color(0.12f, 0.22f, 0.15f),
            AmbientLight = new Color(0.30f, 0.50f, 0.35f)
        };

        [Header("=== Cell Background (per theme) ===")]
        [Tooltip("Arka plan hücre rengi — Dark tema")]
        public Color CellBgDark = new Color(0.043f, 0.059f, 0.098f, 1f);
        [Tooltip("Arka plan hücre rengi — Light tema")]
        public Color CellBgLight = new Color(0.92f, 0.92f, 0.94f, 1f);
        [Tooltip("Arka plan hücre rengi — Neon tema")]
        public Color CellBgNeon = new Color(0.078f, 0.055f, 0.157f, 1f);
        [Tooltip("Arka plan hücre rengi — Candy tema")]
        public Color CellBgCandy = new Color(0.98f, 0.90f, 0.95f, 1f);
        [Tooltip("Arka plan hücre rengi — Forest tema")]
        public Color CellBgForest = new Color(0.12f, 0.22f, 0.15f, 1f);

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

        [Header("=== LevelSelect Box Colors ===")]
        [Tooltip("Tamamlanan seviye kutusu arka planı (#ECFDF5 açık mint)")]
        public Color LevelSelectCompletedBox = new Color(0.925f, 0.992f, 0.957f);
        [Tooltip("Kilitli olmayan seviye kutusu arka planı (#FFFFFF beyaz)")]
        public Color LevelSelectUnlockedBox = new Color(1f, 1f, 1f);
        [Tooltip("Kilitli seviye kutusu arka planı (#F1F5F9 açık gri)")]
        public Color LevelSelectLockedBox = new Color(0.945f, 0.961f, 0.976f);
        [Tooltip("Tamamlanan seviye metin rengi (#059669 yeşil)")]
        public Color LevelSelectCompletedText = new Color(0.02f, 0.59f, 0.41f);
        [Tooltip("Kilitli olmayan seviye metin rengi (#334155 slate)")]
        public Color LevelSelectUnlockedText = new Color(0.20f, 0.25f, 0.33f);
        [Tooltip("Kilitli seviye metin rengi (#94A3B8 muted slate)")]
        public Color LevelSelectLockedText = new Color(0.58f, 0.64f, 0.72f);
        [Tooltip("Yıldız rengi (#F59E0B amber)")]
        public Color LevelSelectStarColor = new Color(0.96f, 0.62f, 0.04f);

        [Header("=== Garage UI Colors ===")]
        [Tooltip("Kapat butonu arka planı (#EF4444 kırmızı)")]
        public Color GarageCloseBtnBg = new Color(0.94f, 0.27f, 0.27f);
        [Tooltip("Skin kart isim metni (#0F172A dark slate)")]
        public Color GarageSkinNameText = new Color(0.06f, 0.09f, 0.16f);
        [Tooltip("Equipped durum badge arka planı")]
        public Color GarageBadgeBgEquipped = new Color(0.92f, 0.99f, 0.95f);
        [Tooltip("Unlocked durum badge arka planı")]
        public Color GarageBadgeBgUnlocked = new Color(0.94f, 0.96f, 1f);
        [Tooltip("Locked durum badge arka planı (altın/amber)")]
        public Color GarageBadgeBgLocked = new Color(0.99f, 0.95f, 0.78f);
        [Tooltip("Equipped durum badge metin rengi (yeşil)")]
        public Color GarageBadgeTextEquipped = new Color(0.02f, 0.59f, 0.41f);
        [Tooltip("Unlocked durum badge metin rengi (mavi)")]
        public Color GarageBadgeTextUnlocked = new Color(0.14f, 0.38f, 0.92f);
        [Tooltip("Locked durum badge metin rengi (turuncu/altın)")]
        public Color GarageBadgeTextLocked = new Color(0.7f, 0.35f, 0.05f);

        [Header("=== Garage ColorFamily Background Colors ===")]
        [Tooltip("Red renk ailesi kart arka planı")]
        public Color GarageColorFamilyRed = new Color(0.99f, 0.88f, 0.88f);
        [Tooltip("Blue renk ailesi kart arka planı")]
        public Color GarageColorFamilyBlue = new Color(0.88f, 0.94f, 0.99f);
        [Tooltip("Green renk ailesi kart arka planı")]
        public Color GarageColorFamilyGreen = new Color(0.88f, 0.99f, 0.92f);
        [Tooltip("Yellow renk ailesi kart arka planı")]
        public Color GarageColorFamilyYellow = new Color(0.99f, 0.97f, 0.82f);
        [Tooltip("Purple renk ailesi kart arka planı")]
        public Color GarageColorFamilyPurple = new Color(0.94f, 0.88f, 0.99f);
        [Tooltip("Default/diğer renk ailesi kart arka planı")]
        public Color GarageColorFamilyDefault = new Color(0.92f, 0.95f, 0.98f);

        [Header("=== HUD Colors ===")]
        [Tooltip("Coin pill arka plan rengi (altın sarısı)")]
        public Color HudGoldPillBg = new Color(0.99f, 0.95f, 0.78f, 1f);
        [Tooltip("Düşük süre timer rengi — Color.Lerp başlangıcı (kırmızı)")]
        public Color HudTimerLowStart = new Color(1f, 0.18f, 0.18f, 1f);
        [Tooltip("Düşük süre timer rengi — Color.Lerp bitişi (sarı)")]
        public Color HudTimerLowEnd = new Color(1f, 0.92f, 0.02f, 1f);

        [Header("=== Procedural View Colors ===")]
        [Tooltip("GridView rainbow gradientinin mor (purple) renk ucu — varsayılan #7F00FF")]
        public Color ProceduralRainbowGradientPurple = new Color(0.5f, 0f, 1f);
        [Tooltip("BloomFlashView seviye tamamlama flaş rengi — sıcak sarı")]
        public Color BloomFlashColor = new Color(1f, 0.95f, 0.6f);
        [Tooltip("Construction engeli hazard stripe amber (turuncu) rengi")]
        public Color ProceduralConstructionAmber = new Color(0.95f, 0.65f, 0.1f, 1f);
        [Tooltip("Construction engeli hazard stripe koyu rengi")]
        public Color ProceduralConstructionDark = new Color(0.2f, 0.15f, 0.05f, 1f);
        [Tooltip("Lake su ripple koyu mavi rengi")]
        public Color ProceduralLakeWaterDeep = new Color(0.12f, 0.38f, 0.75f, 1f);
        [Tooltip("Lake su ripple açık mavi rengi")]
        public Color ProceduralLakeWaterLight = new Color(0.40f, 0.75f, 0.95f, 1f);
        [Tooltip("Park çim taban rengi (koyu yeşil)")]
        public Color ProceduralParkBase = new Color(0.18f, 0.52f, 0.24f, 1f);
        [Tooltip("Park yaprak rengi (açık yeşil)")]
        public Color ProceduralParkLeaf = new Color(0.35f, 0.75f, 0.38f, 1f);
        [Tooltip("Viyadük/bridge deck (güverte) rengi — açık gri")]
        public Color ProceduralBridgeDeck = new Color(0.82f, 0.84f, 0.90f, 1f);
        [Tooltip("Viyadük/bridge rail (korkuluk) rengi — altın sarısı")]
        public Color ProceduralBridgeRail = new Color(0.95f, 0.75f, 0.20f, 1f);
        [Tooltip("Procedural 3D bridge material rengi — açık gri-mavi")]
        public Color ProceduralBridgeMaterial = new Color(0.78f, 0.80f, 0.88f, 1f);
        [Tooltip("GridView crash glow rengi — kırmızı")]
        public Color PathGlowCrashRed = new Color(1f, 0f, 0f);

        [Header("=== Settings UI Colors ===")]
        [Tooltip("Aktif/seçili buton rengi (mavi)")]
        public Color SettingsButtonActive = new Color(0.2f, 0.6f, 1f);
        [Tooltip("Pasif/seçilmemiş buton rengi (koyu gri)")]
        public Color SettingsButtonInactive = new Color(0.2f, 0.2f, 0.25f);
        [Tooltip("Settings kapat butonu arka planı (kırmızı)")]
        public Color SettingsCloseBtnBg = new Color(0.94f, 0.27f, 0.27f);

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

        public Color GetGarageColorFamilyBg(ColorType color)
        {
            switch (color)
            {
                case ColorType.Red:    return GarageColorFamilyRed;
                case ColorType.Blue:   return GarageColorFamilyBlue;
                case ColorType.Green:  return GarageColorFamilyGreen;
                case ColorType.Yellow: return GarageColorFamilyYellow;
                case ColorType.Purple: return GarageColorFamilyPurple;
                default:               return GarageColorFamilyDefault;
            }
        }
    }
}
