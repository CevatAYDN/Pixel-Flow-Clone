#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PixelFlow.Data;

namespace PixelFlow.Editor
{
    /// <summary>
    /// GDD §3.6: Default PhaseDefinitionAsset'leri oluşturup Resources'a kaydeder.
    /// Tools > PixelFlow > Generate Phase Assets menüsünden çalıştırılır.
    /// </summary>
    public static class PhaseAssetGenerator
    {
        private const string ResourcesPath = "Assets/Resources";

        [MenuItem("Tools/PixelFlow/Generate Phase Assets", false, 100)]
        public static void GeneratePhaseAssets()
        {
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // Create Phase 1 (Levels 1-12)
            var p1 = CreatePhase("Phase1_Levels1-12", GamePhase.Phase1, 0, 11,
                5, 5, 1, 2, 0, 0, false, false, false, false, false);

            // Create Phase 2 (Levels 13-28)
            var p2 = CreatePhase("Phase2_Levels13-28", GamePhase.Phase2, 12, 27,
                6, 7, 2, 3, 3, 3, false, true, false, false, false);

            // Create Phase 3 (Levels 29-45)
            var p3 = CreatePhase("Phase3_Levels29-45", GamePhase.Phase3, 28, 44,
                7, 9, 3, 4, 2, 4, true, true, true, false, false);

            // Create Phase 4 (Levels 46-60+)
            var p4 = CreatePhase("Phase4_Levels46-60", GamePhase.Phase4, 45, 59,
                9, 10, 4, 5, 1, 5, true, true, true, true, true);

            // Create the PhaseConfig container
            var config = ScriptableObject.CreateInstance<PhaseConfigAsset>();
            config.Phase1 = p1;
            config.Phase2 = p2;
            config.Phase3 = p3;
            config.Phase4 = p4;

            AssetDatabase.CreateAsset(config, $"{ResourcesPath}/PhaseConfig.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PhaseAssetGenerator] Default PhaseConfig.asset created at {ResourcesPath}/PhaseConfig.asset");
            EditorUtility.DisplayDialog("Pixel Flow", "Phase assets generated successfully!", "OK");
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

            AssetDatabase.CreateAsset(asset, $"{ResourcesPath}/{name}.asset");
            return asset;
        }
    }
}
#endif