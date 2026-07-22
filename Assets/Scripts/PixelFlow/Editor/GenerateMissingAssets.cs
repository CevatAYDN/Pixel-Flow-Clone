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
        private const string ResourcesPath = "Assets/Resources";

        [MenuItem("Tools/PixelFlow/Generate Missing Assets")]
        public static void Generate()
        {
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder(ResourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            CreateAssetIfMissing<ColorBlindPaletteAsset>("ColorBlindPalette");
            CreateAssetIfMissing<EconomyConfigAsset>("EconomyConfig");
            CreateAssetIfMissing<ThemePaletteAsset>("ThemePalette");
            CreateAssetIfMissing<VehicleMaterialConfigAsset>("VehicleMaterialConfig");
            CreateAssetIfMissing<LevelCatalogAsset>("LevelCatalog");

            AssetDatabase.SaveAssets();
            Debug.Log("[PixelFlow] All missing ScriptableObject assets generated successfully.");
        }

        private static void CreateAssetIfMissing<T>(string assetName) where T : ScriptableObject
        {
            string path = $"{ResourcesPath}/{assetName}.asset";

            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                Debug.Log($"[PixelFlow] {assetName}.asset already exists at {path}. Skipping.");
                return;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[PixelFlow] Created {assetName}.asset at {path}");
        }
    }
}
