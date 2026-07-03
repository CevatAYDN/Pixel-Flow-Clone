using System.Collections.Generic;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Data
{
    /// <summary>
    /// GDD §2.1'deki 5 renk paleti. Renkler + şekiller eşleştirmesi:
    ///   Mavi (#00D4FF)    = Daire   (Blue)
    ///   Kırmızı (#FF3D7F) = Üçgen   (Red)
    ///   Sarı (#FFD93D)    = Kare    (Yellow)
    ///   Yeşil (#6BCB77)   = Elmas   (Green)
    ///   Mor (#B36BFF)     = Yıldız  (Purple)
    /// Tüm prosedürel seviye üretimi bu paletten seçim yapar — renk körlüğü
    /// erişilebilirliği için her rengin ayırt edici bir şekil eşleştirmesi vardır.
    /// </summary>
    public static class GddColorPalette
    {
        /// <summary>
        /// GDD standart paleti. Orange/Cyan/Magenta renkleri listede YOK —
        /// bunlar yalnızca geriye uyumluluk (eski .asset'ler) için enum'da kalır.
        /// </summary>
        public static readonly ColorType[] Standard =
        {
            ColorType.Red, ColorType.Green, ColorType.Blue, ColorType.Yellow, ColorType.Purple,
        };

        public static bool IsGddStandard(ColorType c)
        {
            for (int i = 0; i < Standard.Length; i++)
                if (Standard[i] == c) return true;
            return false;
        }

        public static ColorType PickRandom(System.Random rng)
        {
            return Standard[rng.Next(Standard.Length)];
        }
    }

    /// <summary>
    /// GDD §3.5'teki 4 fazlık progresyon eğrisi. Level index 0-based.
    /// Faz 1: Seviye 0-11 (12 seviye), 5×5→6×6, 1-2 renk, kaza yok, viyadük yok.
    /// Faz 2: Seviye 12-27 (16 seviye), 7×7, 2-3 renk, kaza aktif, viyadük 3.
    /// Faz 3: Seviye 28-44 (17 seviye), 8×8→9×9, 3-4 renk, tüm sabit engeller, 3-4 viyadük.
    /// Faz 4: Seviye 45-59 (15 seviye), 10×10, 4-5 renk, hareketli engeller dahil.
    /// </summary>
    public enum GamePhase
    {
        /// <summary>Öğretme + bağımlılık (Gün 1-3, Seviye 1-12).</summary>
        Phase1 = 1,
        /// <summary>Düğüm ve çözüm (Gün 4-8, Seviye 13-28).</summary>
        Phase2 = 2,
        /// <summary>Şehir planlamacısı (Gün 9-15, Seviye 29-45).</summary>
        Phase3 = 3,
        /// <summary>Endgame + hareketli engeller (Gün 16-20+, Seviye 46-60).</summary>
        Phase4 = 4,
    }

    [System.Serializable]
    public struct PhaseDefinition
    {
        public GamePhase Phase;
        public int StartLevelIndex;   // 0-based inclusive
        public int EndLevelIndex;     // 0-based inclusive
        public int GridSizeMin;
        public int GridSizeMax;
        public int ColorCountMin;
        public int ColorCountMax;
        public int BridgeCountMin;
        public int BridgeCountMax;
        public bool RequireFullCoverage;
        public bool ObstaclesEnabled;     // Gölet/Park/İnşaat
        public bool OneWayEnabled;
        public bool FerryEnabled;
        public bool NarrowPassEnabled;

        public static PhaseDefinition Phase1Standard =>
            new PhaseDefinition
            {
                Phase = GamePhase.Phase1,
                StartLevelIndex = 0, EndLevelIndex = 11,
                GridSizeMin = 5, GridSizeMax = 6,
                ColorCountMin = 1, ColorCountMax = 2,
                BridgeCountMin = 0, BridgeCountMax = 0,
                RequireFullCoverage = false,
                ObstaclesEnabled = false, OneWayEnabled = false,
                FerryEnabled = false, NarrowPassEnabled = false,
            };

        public static PhaseDefinition Phase2Standard =>
            new PhaseDefinition
            {
                Phase = GamePhase.Phase2,
                StartLevelIndex = 12, EndLevelIndex = 27,
                GridSizeMin = 7, GridSizeMax = 7,
                ColorCountMin = 2, ColorCountMax = 3,
                BridgeCountMin = 1, BridgeCountMax = 2,
                RequireFullCoverage = false,
                ObstaclesEnabled = false, OneWayEnabled = false,
                FerryEnabled = false, NarrowPassEnabled = false,
            };

        public static PhaseDefinition Phase3Standard =>
            new PhaseDefinition
            {
                Phase = GamePhase.Phase3,
                StartLevelIndex = 28, EndLevelIndex = 44,
                GridSizeMin = 8, GridSizeMax = 9,
                ColorCountMin = 3, ColorCountMax = 4,
                BridgeCountMin = 2, BridgeCountMax = 3,
                RequireFullCoverage = true,
                ObstaclesEnabled = true, OneWayEnabled = true,
                FerryEnabled = false, NarrowPassEnabled = false,
            };

        public static PhaseDefinition Phase4Standard =>
            new PhaseDefinition
            {
                Phase = GamePhase.Phase4,
                StartLevelIndex = 45, EndLevelIndex = 59,
                GridSizeMin = 10, GridSizeMax = 10,
                ColorCountMin = 4, ColorCountMax = 5,
                BridgeCountMin = 3, BridgeCountMax = 4,
                RequireFullCoverage = true,
                ObstaclesEnabled = true, OneWayEnabled = true,
                FerryEnabled = true, NarrowPassEnabled = true,
            };

        public static readonly PhaseDefinition[] AllPhases =
            { Phase1Standard, Phase2Standard, Phase3Standard, Phase4Standard };

        public static PhaseDefinition GetPhaseForLevel(int levelIndex)
        {
            for (int i = 0; i < AllPhases.Length; i++)
            {
                var p = AllPhases[i];
                if (levelIndex >= p.StartLevelIndex && levelIndex <= p.EndLevelIndex)
                    return p;
            }
            // Seviye 60+ sonsuz döngü/daily crisis — Faz 4'e fallback.
            return Phase4Standard;
        }
    }
}
