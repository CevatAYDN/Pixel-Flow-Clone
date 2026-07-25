#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PixelFlow.Data;

namespace PixelFlow.Editor
{
    /// <summary>
    /// Asset oluşturucu — tüm ScriptableObject varlıklarını Resources'tan yükler,
    /// yoksa otomatik oluşturur. Sıfır hardcode, her şey data-driven.
    /// </summary>
    public static class AssetCreator
    {
        private const string ConfigsPath = "Assets/Resources/Configs";

        [MenuItem("Pixel Flow/Asset Creator/Create All Configs")]
        public static void CreateAllConfigs()
        {
            int created = 0;
            int alreadyExists = 0;

            // GameConfig
            if (!EnsureAsset<GameConfig>("GameConfig", ref alreadyExists))
            {
                CreateAndSave<GameConfig>("GameConfig");
                created++;
            }
            else
            {
                alreadyExists++;
            }

            // ThemePalette
            if (!EnsureAsset<ThemePaletteAsset>("ThemePalette", ref alreadyExists))
            {
                CreateAndSave<ThemePaletteAsset>("ThemePalette");
                created++;
            }
            else
            {
                alreadyExists++;
            }

            // VehicleMaterialConfig
            if (!EnsureAsset<VehicleMaterialConfigAsset>("VehicleMaterialConfig", ref alreadyExists))
            {
                CreateAndSave<VehicleMaterialConfigAsset>("VehicleMaterialConfig");
                created++;
            }
            else
            {
                alreadyExists++;
            }

            // VehicleVisualConfig
            if (!EnsureAsset<VehicleVisualConfigAsset>("VehicleVisualConfig", ref alreadyExists))
            {
                CreateAndSave<VehicleVisualConfigAsset>("VehicleVisualConfig");
                created++;
            }
            else
            {
                alreadyExists++;
            }

            // ColorBlindPalette
            if (!EnsureAsset<ColorBlindPaletteAsset>("ColorBlindPalette", ref alreadyExists))
            {
                CreateAndSave<ColorBlindPaletteAsset>("ColorBlindPalette");
                created++;
            }
            else
            {
                alreadyExists++;
            }

            // EconomyConfig
            if (!EnsureAsset<EconomyConfigAsset>("EconomyConfig", ref alreadyExists))
            {
                CreateAndSave<EconomyConfigAsset>("EconomyConfig");
                created++;
            }
            else
            {
                alreadyExists++;
            }

            // LevelCatalog
            if (!EnsureAsset<LevelCatalogAsset>("LevelCatalog", ref alreadyExists))
            {
                CreateAndSave<LevelCatalogAsset>("LevelCatalog");
                created++;
            }
            else
            {
                alreadyExists++;
            }

            // PhaseConfig
            if (!EnsureAsset<PhaseConfigAsset>("PhaseConfig", ref alreadyExists))
            {
                CreateAndSave<PhaseConfigAsset>("PhaseConfig");
                created++;
            }
            else
            {
                alreadyExists++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AssetCreator] Created: {created}, Already Exists: {alreadyExists}");
            EditorUtility.DisplayDialog("Asset Creator", $"Created: {created}\nAlready Exists: {alreadyExists}", "OK");
        }

        private static bool EnsureAsset<T>(string name, ref int alreadyExistsCount) where T : ScriptableObject
        {
            var asset = Resources.Load<T>($"Configs/{name}");
            if (asset != null)
            {
                alreadyExistsCount++;
                return true;
            }
            return false;
        }

        private static void CreateAndSave<T>(string name) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;

            string path = $"{ConfigsPath}/{name}.asset";
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[AssetCreator] Created: {path}");
        }

        [MenuItem("Pixel Flow/Asset Creator/Create Vehicle Visual Config Only")]
        public static void CreateVehicleVisualConfigOnly()
        {
            int alreadyExists = 0;
            if (!EnsureAsset<VehicleVisualConfigAsset>("VehicleVisualConfig", ref alreadyExists))
            {
                CreateAndSave<VehicleVisualConfigAsset>("VehicleVisualConfig");
                Debug.Log("[AssetCreator] VehicleVisualConfig created!");
            }
            else
            {
                Debug.Log("[AssetCreator] VehicleVisualConfig already exists.");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
