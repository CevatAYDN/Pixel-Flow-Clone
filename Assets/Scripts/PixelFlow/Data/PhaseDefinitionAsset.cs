using UnityEngine;
using System.Collections.Generic;

namespace PixelFlow.Data
{
    /// <summary>
    /// GDD §3.5: Editörden yönetilebilir, data-driven Phase tanımı.
    /// LevelProgressionService bu asset'leri kullanarak seviye ilerleme eğrisini
    /// kod değişikliği olmadan konfigüre eder.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PhaseDefinition_New",
        menuName = "PixelFlow/Phase Definition")]
    public class PhaseDefinitionAsset : ScriptableObject
    {
        [Header("Phase Identity")]
        [Tooltip("Hangi oyun fazı (Phase1-4)")]
        public GamePhase Phase;

        [Header("Level Range (0-based inclusive)")]
        [Tooltip("Bu fazın başladığı seviye indeksi (0 = Level 1)")]
        [Min(0)]
        public int StartLevelIndex;

        [Tooltip("Bu fazın bittiği seviye indeksi (0-based)")]
        [Min(0)]
        public int EndLevelIndex;

        [Header("Grid Configuration")]
        [Tooltip("Minimum grid boyutu")]
        [Range(4, 20)]
        public int GridSizeMin = 5;

        [Tooltip("Maximum grid boyutu")]
        [Range(4, 20)]
        public int GridSizeMax = 5;

        [Header("Color Configuration")]
        [Tooltip("Minimum renk sayısı")]
        [Range(1, 5)]
        public int ColorCountMin = 1;

        [Tooltip("Maximum renk sayısı")]
        [Range(1, 5)]
        public int ColorCountMax = 2;

        [Header("Viaduct Configuration")]
        [Tooltip("Minimum viyadük hakkı")]
        [Min(0)]
        public int BridgeCountMin;

        [Tooltip("Maximum viyadük hakkı")]
        [Min(0)]
        public int BridgeCountMax;

        [Header("Win Condition")]
        [Tooltip("Tam solusyonun grid'in tamamını kaplaması gerekir mi?")]
        public bool RequireFullCoverage;

        [Header("Obstacles")]
        public bool ObstaclesEnabled;
        public bool OneWayEnabled;
        public bool FerryEnabled;
        public bool NarrowPassEnabled;

        [Header("Auto-Load Target Flow Score")]
        [Tooltip("0 = formülden otomatik belirle")]
        [Min(0)]
        public int OverrideTargetFlowScore;

        /// <summary>
        /// Seviye indeksine göre bu fazın geçerli olup olmadığını kontrol eder.
        /// </summary>
        public bool ContainsLevel(int levelIndex)
        {
            return levelIndex >= StartLevelIndex && levelIndex <= EndLevelIndex;
        }

        /// <summary>
        /// Bu asset'ten bir PhaseDefinition struct oluşturur.
        /// </summary>
        public PhaseDefinition ToStruct()
        {
            return new PhaseDefinition
            {
                Phase = Phase,
                StartLevelIndex = StartLevelIndex,
                EndLevelIndex = EndLevelIndex,
                GridSizeMin = GridSizeMin,
                GridSizeMax = GridSizeMax,
                ColorCountMin = ColorCountMin,
                ColorCountMax = ColorCountMax,
                BridgeCountMin = BridgeCountMin,
                BridgeCountMax = BridgeCountMax,
                RequireFullCoverage = RequireFullCoverage,
                ObstaclesEnabled = ObstaclesEnabled,
                OneWayEnabled = OneWayEnabled,
                FerryEnabled = FerryEnabled,
                NarrowPassEnabled = NarrowPassEnabled,
            };
        }
    }

    /// <summary>
    /// Tüm fazların bir arada tutulduğu konteyner ScriptableObject.
    /// GameContext'te referans edilir; LevelProgressionService bunu okuyup
    /// seviye progression'ını yönetir.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PhaseConfig.asset",
        menuName = "PixelFlow/Phase Configuration")]
    public class PhaseConfigAsset : ScriptableObject
    {
        [Tooltip("Faz 1 (Seviye 1-12)")]
        public PhaseDefinitionAsset Phase1;

        [Tooltip("Faz 2 (Seviye 13-28)")]
        public PhaseDefinitionAsset Phase2;

        [Tooltip("Faz 3 (Seviye 29-45)")]
        public PhaseDefinitionAsset Phase3;

        [Tooltip("Faz 4 (Seviye 46-60+)")]
        public PhaseDefinitionAsset Phase4;

        public IEnumerable<PhaseDefinitionAsset> AllPhases
        {
            get
            {
                if (Phase1 != null) yield return Phase1;
                if (Phase2 != null) yield return Phase2;
                if (Phase3 != null) yield return Phase3;
                if (Phase4 != null) yield return Phase4;
            }
        }

        /// <summary>
        /// Seviye indeksine göre hangi fazda olduğumuzu bulur.
        /// </summary>
        public PhaseDefinitionAsset GetPhaseForLevel(int levelIndex)
        {
            foreach (var phase in AllPhases)
            {
                if (phase.ContainsLevel(levelIndex))
                    return phase;
            }
            return Phase4; // Fallback: 60+ seviyeler Faz 4
        }

        public PhaseDefinition[] ToStructArray()
        {
            var list = new List<PhaseDefinition>();
            foreach (var asset in AllPhases)
            {
                if (asset != null)
                    list.Add(asset.ToStruct());
            }
            return list.ToArray();
        }
    }
}