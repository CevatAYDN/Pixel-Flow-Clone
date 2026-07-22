using UnityEditor;
using UnityEngine;
using PixelFlow.Data;

namespace PixelFlow.Editor
{
    /// <summary>
    /// Yeni oluşturulan ScriptableObject asset'lerini Resources/ klasörüne kaydeder.
    /// Tools > PixelFlow > Generate Missing Assets menüsünden çalıştırılır.
    /// </summary>
    public static class GenerateMissingAssets
    {
        private const string ConfigsPath = "Assets/Resources/Configs";

        [MenuItem("Tools/PixelFlow/Generate Missing Assets")]
        public static void Generate()
        {
            // Ensure Configs folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Configs"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateFolder("Assets/Resources", "Configs");
            }

            CreateAssetIfMissing<ColorBlindPaletteAsset>("Configs/ColorBlindPalette");
            CreateAssetIfMissing<EconomyConfigAsset>("Configs/EconomyConfig");
            CreateAssetIfMissing<ThemePaletteAsset>("Configs/ThemePalette");
            CreateAssetIfMissing<VehicleMaterialConfigAsset>("Configs/VehicleMaterialConfig");
            CreateAssetIfMissing<LevelCatalogAsset>("Configs/LevelCatalog");

            AssetDatabase.SaveAssets();
            Debug.Log("[PixelFlow] All missing ScriptableObject assets generated in Configs/.");
        }

        private static void CreateAssetIfMissing<T>(string assetPath) where T : ScriptableObject
        {
            string fullPath = $"{ConfigsPath}/{assetPath.Replace("Configs/", "")}.asset";

            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<T>(fullPath);
            if (existing != null)
            {
                Debug.Log($"[PixelFlow] {assetPath}.asset already exists at {fullPath}. Skipping.");
                return;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = assetPath;
            AssetDatabase.CreateAsset(asset, fullPath);
            Debug.Log($"[PixelFlow] Created {assetPath}.asset at {fullPath}");
        }
    }
}
