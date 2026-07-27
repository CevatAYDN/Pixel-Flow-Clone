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

            // ThemePalette
            if (!EnsureAsset<ThemePaletteAsset>("ThemePalette", ref alreadyExists))
            {
                CreateAndSave<ThemePaletteAsset>("ThemePalette");
                created++;
            }

            // VehicleMaterialConfig
            if (!EnsureAsset<VehicleMaterialConfigAsset>("VehicleMaterialConfig", ref alreadyExists))
            {
                CreateAndSave<VehicleMaterialConfigAsset>("VehicleMaterialConfig");
                created++;
            }

            // VehicleVisualConfig
            if (!EnsureAsset<VehicleVisualConfigAsset>("VehicleVisualConfig", ref alreadyExists))
            {
                CreateAndSave<VehicleVisualConfigAsset>("VehicleVisualConfig");
                created++;
            }

            // ColorBlindPalette
            if (!EnsureAsset<ColorBlindPaletteAsset>("ColorBlindPalette", ref alreadyExists))
            {
                CreateAndSave<ColorBlindPaletteAsset>("ColorBlindPalette");
                created++;
            }

            // EconomyConfig
            if (!EnsureAsset<EconomyConfigAsset>("EconomyConfig", ref alreadyExists))
            {
                CreateAndSave<EconomyConfigAsset>("EconomyConfig");
                created++;
            }

            // LevelCatalog
            if (!EnsureAsset<LevelCatalogAsset>("LevelCatalog", ref alreadyExists))
            {
                CreateAndSave<LevelCatalogAsset>("LevelCatalog");
                created++;
            }

            // PhaseConfig
            if (!EnsureAsset<PhaseConfigAsset>("PhaseConfig", ref alreadyExists))
            {
                CreateAndSave<PhaseConfigAsset>("PhaseConfig");
                created++;
            }

            if (!EnsureAsset<DifficultyFormulaConfigAsset>("DifficultyFormulaConfig", ref alreadyExists))
            {
                CreateAndSave<DifficultyFormulaConfigAsset>("DifficultyFormulaConfig");
                created++;
            }

            if (!EnsureAsset<DefaultSkinIdsConfigAsset>("DefaultSkinIdsConfig", ref alreadyExists))
            {
                CreateAndSave<DefaultSkinIdsConfigAsset>("DefaultSkinIdsConfig");
                created++;
            }

            if (!EnsureAsset<BouncyPhysicsConfigAsset>("BouncyPhysicsConfig", ref alreadyExists))
            {
                CreateAndSave<BouncyPhysicsConfigAsset>("BouncyPhysicsConfig");
                created++;
            }

            if (!EnsureAsset<StarCriteriaConfigAsset>("StarCriteriaConfig", ref alreadyExists))
            {
                CreateAndSave<StarCriteriaConfigAsset>("StarCriteriaConfig");
                created++;
            }

            if (!EnsureAsset<RushHourConfigAsset>("RushHourConfig", ref alreadyExists))
            {
                CreateAndSave<RushHourConfigAsset>("RushHourConfig");
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AssetCreator] Created: {created}, Already Exists: {alreadyExists}");
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
