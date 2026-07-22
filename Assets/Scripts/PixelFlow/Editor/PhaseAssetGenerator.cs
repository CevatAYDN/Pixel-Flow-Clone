#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PixelFlow.Data;
using PixelFlow.Services;

namespace PixelFlow.Editor
{
    /// <summary>
    /// GDD §3.6: Default PhaseDefinitionAsset'leri oluşturup Resources'a kaydeder.
    /// Tools > PixelFlow > Generate Phase Assets menüsünden çalıştırılır.
    /// </summary>
    public static class PhaseAssetGenerator
    {
        private const string PhasesPath = "Assets/Resources/Configs";

        [MenuItem("Tools/PixelFlow/Generate Phase Assets", false, 100)]
        public static void GeneratePhaseAssets()
        {
            // Ensure Configs folder exists
            if (!AssetDatabase.IsValidFolder(PhasesPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateFolder("Assets/Resources", "Configs");
            }

            // Create Phase 1 (Levels 1-12) — index 0-11
            var p1 = CreatePhase("Phase1_Levels1-12", GamePhase.Phase1, 0, 11,
                5, 5, 1, 2, 0, 0, false, false, false, false, false);

            // Create Phase 2 (Levels 13-28) — index 12-27
            var p2 = CreatePhase("Phase2_Levels13-28", GamePhase.Phase2, 12, 27,
                6, 7, 2, 3, 3, 3, false, true, false, false, false);

            // Create Phase 3 (Levels 29-45) — index 28-44
            var p3 = CreatePhase("Phase3_Levels29-45", GamePhase.Phase3, 28, 44,
                7, 9, 3, 4, 2, 4, true, true, true, false, false);

            // Create Phase 4 (Levels 46-60+) — index 45-59
            var p4 = CreatePhase("Phase4_Levels46-60", GamePhase.Phase4, 45, 59,
                9, 10, 4, 5, 1, 5, true, true, true, true, true);

            // Create the PhaseConfig container in Configs/
            var config = ScriptableObject.CreateInstance<PhaseConfigAsset>();
            config.Phase1 = p1;
            config.Phase2 = p2;
            config.Phase3 = p3;
            config.Phase4 = p4;

            string configPath = $"{PhasesPath}/PhaseConfig.asset";
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PhaseAssetGenerator] PhaseConfig.asset created at {configPath}");
            EditorUtility.DisplayDialog("Pixel Flow", "Phase assets generated in Configs/! Seviye Stüdyosu'nu yenileyin.", "OK");
        }

        /// <summary>Seviye indeksine göre hangi fazda olduğunu belirler (ScriptableObject yüklemeden)</summary>
        public static GamePhase GetPhaseForLevel(int levelIndex)
        {
            if (levelIndex <= 11) return GamePhase.Phase1;
            if (levelIndex <= 27) return GamePhase.Phase2;
            if (levelIndex <= 44) return GamePhase.Phase3;
            return GamePhase.Phase4;
        }

        /// <summary>Faz adını döndürür</summary>
        public static string GetPhaseName(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Phase1: return "Faz 1 - Giriş";
                case GamePhase.Phase2: return "Faz 2 - Keşif";
                case GamePhase.Phase3: return "Faz 3 - Ustalık";
                case GamePhase.Phase4: return "Faz 4 - Uzmanlık";
                default: return phase.ToString();
            }
        }

        /// <summary>Faz rengini döndürür (görsel gösterim için)</summary>
        public static Color GetPhaseColor(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Phase1: return new Color(0.3f, 0.7f, 0.3f); // Yeşil
                case GamePhase.Phase2: return new Color(0.3f, 0.6f, 1.0f); // Mavi
                case GamePhase.Phase3: return new Color(0.9f, 0.6f, 0.1f); // Turuncu
                case GamePhase.Phase4: return new Color(0.85f, 0.2f, 0.2f); // Kırmızı
                default: return Color.gray;
            }
        }

        /// <summary>Fazın varsayılan parametrelerini DifficultyParams olarak döndürür</summary>
        public static DifficultyParams GetDefaultParamsForPhase(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Phase1:
                    return new DifficultyParams(5, 5, 2, 0, false, false, false, false);
                case GamePhase.Phase2:
                    return new DifficultyParams(6, 7, 3, 2, false, true, false, false);
                case GamePhase.Phase3:
                    return new DifficultyParams(7, 9, 4, 3, true, true, true, false);
                case GamePhase.Phase4:
                    return new DifficultyParams(9, 10, 5, 4, true, true, true, true);
                default:
                    return new DifficultyParams(5, 5, 2, 0, false, false, false, false);
            }
        }

        private static PhaseDefinitionAsset CreatePhase(
            string name, GamePhase phase, int startIdx, int endIdx,
            int gridMin, int gridMax, int colorMin, int colorMax,
            int bridgeMin, int bridgeMax, bool fullCoverage,
            bool obstacles, bool oneWay, bool ferry, bool narrowPass)
        {
            var asset = ScriptableObject.CreateInstance<PhaseDefinitionAsset>();
            asset.name = name;
            asset.Phase = phase;
            asset.StartLevelIndex = startIdx;
            asset.EndLevelIndex = endIdx;
            asset.GridSizeMin = gridMin;
            asset.GridSizeMax = gridMax;
            asset.ColorCountMin = colorMin;
            asset.ColorCountMax = colorMax;
            asset.BridgeCountMin = bridgeMin;
            asset.BridgeCountMax = bridgeMax;
            asset.RequireFullCoverage = fullCoverage;
            asset.ObstaclesEnabled = obstacles;
            asset.OneWayEnabled = oneWay;
            asset.FerryEnabled = ferry;
            asset.NarrowPassEnabled = narrowPass;

            AssetDatabase.CreateAsset(asset, $"{PhasesPath}/{name}.asset");
            return asset;
        }
    }
}
#endif