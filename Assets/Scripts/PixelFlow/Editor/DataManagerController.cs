#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using System.Collections.Generic;
using System.Linq;

namespace PixelFlow.Editor
{
    /// <summary>
    /// Merkezi veri yöneticisi — tüm ScriptableObject'leri tek panelden oluşturur, düzenler ve doğrular.
    /// Sıfır hardcode — her şey data-driven.
    /// </summary>
    public class DataManagerController
    {
        private static Dictionary<string, bool> _assetStatusCache = new Dictionary<string, bool>();

        public static void RefreshAssetStatus()
        {
            _assetStatusCache.Clear();
            CheckAllAssets();
            Debug.Log("[DataManager] Asset durumu güncellendi: " + _assetStatusCache.Count + " varlık kontrol edildi.");
        }

        public static void CheckAllAssets()
        {
            _assetStatusCache["GameConfig"] = CheckAssetExists<GameConfig>("Configs/GameConfig");
            _assetStatusCache["ThemePalette"] = CheckAssetExists<ThemePaletteAsset>("Configs/ThemePalette");
            _assetStatusCache["VehicleMaterialConfig"] = CheckAssetExists<VehicleMaterialConfigAsset>("Configs/VehicleMaterialConfig");
            _assetStatusCache["VehicleVisualConfig"] = CheckAssetExists<VehicleVisualConfigAsset>("Configs/VehicleVisualConfig");
            _assetStatusCache["ColorBlindPalette"] = CheckAssetExists<ColorBlindPaletteAsset>("Configs/ColorBlindPalette");
            _assetStatusCache["EconomyConfig"] = CheckAssetExists<EconomyConfigAsset>("Configs/EconomyConfig");
            _assetStatusCache["LevelCatalog"] = CheckAssetExists<LevelCatalogAsset>("Configs/LevelCatalog");
            _assetStatusCache["PhaseConfig"] = CheckAssetExists<PhaseConfigAsset>("Configs/PhaseConfig");
            _assetStatusCache["DifficultyFormulaConfig"] = CheckAssetExists<DifficultyFormulaConfigAsset>("Configs/DifficultyFormulaConfig");
            _assetStatusCache["DefaultSkinIdsConfig"] = CheckAssetExists<DefaultSkinIdsConfigAsset>("Configs/DefaultSkinIdsConfig");
            _assetStatusCache["BouncyPhysicsConfig"] = CheckAssetExists<BouncyPhysicsConfigAsset>("Configs/BouncyPhysicsConfig");
            _assetStatusCache["StarCriteriaConfig"] = CheckAssetExists<StarCriteriaConfigAsset>("Configs/StarCriteriaConfig");
            _assetStatusCache["RushHourConfig"] = CheckAssetExists<RushHourConfigAsset>("Configs/RushHourConfig");
        }

        private static bool CheckAssetExists<T>(string path) where T : ScriptableObject
        {
            var asset = Resources.Load<T>(path);
            return asset != null;
        }

        public static bool IsAssetReady<T>(string name) where T : ScriptableObject
        {
            if (!_assetStatusCache.ContainsKey(name))
            {
                CheckAllAssets();
            }
            return _assetStatusCache.GetValueOrDefault(name, false);
        }

        public static Dictionary<string, bool> GetAllAssetStatus()
        {
            CheckAllAssets();
            return new Dictionary<string, bool>(_assetStatusCache);
        }

        /// <summary>
        /// Tüm config asset'lerini oluşturur — sıfır hardcode, data-driven.
        /// </summary>
        public static void CreateAllConfigAssets()
        {
            int created = 0;
            string configsPath = "Assets/Resources/Configs";

            System.IO.Directory.CreateDirectory(configsPath);

            created += CreateIfMissing<GameConfig>("Configs/GameConfig", "Merkezi oyun konfigürasyonu");
            created += CreateIfMissing<ThemePaletteAsset>("Configs/ThemePalette", "Tema renk paletleri");
            created += CreateIfMissing<VehicleMaterialConfigAsset>("Configs/VehicleMaterialConfig", "Araç materyal renkleri");
            created += CreateIfMissing<VehicleVisualConfigAsset>("Configs/VehicleVisualConfig", "Araç görsel parametreleri");
            created += CreateIfMissing<ColorBlindPaletteAsset>("Configs/ColorBlindPalette", "Renk körlüğü paleti");
            created += CreateIfMissing<EconomyConfigAsset>("Configs/EconomyConfig", "Ekonomi dengesi");
            created += CreateIfMissing<LevelCatalogAsset>("Configs/LevelCatalog", "Seviye kataloğu");
            created += CreateIfMissing<PhaseConfigAsset>("Configs/PhaseConfig", "Faz tanımları");
            created += CreateIfMissing<DifficultyFormulaConfigAsset>("Configs/DifficultyFormulaConfig", "Zorluk formülü");
            created += CreateIfMissing<DefaultSkinIdsConfigAsset>("Configs/DefaultSkinIdsConfig", "Varsayılan skin ID'leri");
            created += CreateIfMissing<BouncyPhysicsConfigAsset>("Configs/BouncyPhysicsConfig", "Zıplama fizik ayarları");
            created += CreateIfMissing<StarCriteriaConfigAsset>("Configs/StarCriteriaConfig", "Yıldız kriterleri");
            created += CreateIfMissing<RushHourConfigAsset>("Configs/RushHourConfig", "Rush Hour ayarları");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[DataManager] {created} config asset oluşturuldu.");
        }

        private static int CreateIfMissing<T>(string resourcePath, string description) where T : ScriptableObject
        {
            var existing = Resources.Load<T>(resourcePath);
            if (existing != null)
            {
                Debug.Log($"[DataManager] Zaten mevcut: {resourcePath} ({description})");
                return 0;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = typeof(T).Name;
            string fullPath = $"Assets/Resources/{resourcePath}.asset";
            AssetDatabase.CreateAsset(asset, fullPath);
            Debug.Log($"[DataManager] Oluşturuldu: {fullPath} ({description})");
            return 1;
        }

        /// <summary>
        /// Seviye kataloğunu yeniden oluşturur — tüm LevelData asset'lerini tarar.
        /// </summary>
        public static void RegenerateLevelCatalog()
        {
            var catalog = Resources.Load<LevelCatalogAsset>("Configs/LevelCatalog");
            if (catalog == null)
            {
                Debug.LogError("[DataManager] LevelCatalog bulunamadı! Önce 'Create All Config Assets' çalıştırın.");
                return;
            }

            var levels = AssetDatabase.FindAssets("t:LevelData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(l => l != null)
                .OrderBy(l => l.levelIndex)
                .ToList();

            catalog.Levels.Clear();

            foreach (var level in levels)
            {
                var entry = new LevelCatalogAsset.LevelCatalogEntry
                {
                    LevelIndex = level.levelIndex,
                    AuthoredLevel = level,
                    UseProceduralFallback = false
                };
                catalog.Levels.Add(entry);
            }

            // Procedural fallback ekle
            int maxIndexed = levels.Count > 0 ? levels.Max(l => l.levelIndex) : -1;
            for (int i = maxIndexed + 1; i < 150; i++)
            {
                var phaseConfig = Resources.Load<PhaseConfigAsset>("Configs/PhaseConfig");
                if (phaseConfig == null)
                {
                    Debug.LogError("[DataManager] PhaseConfig bulunamadı! Procedural fallback üretilemez.");
                    return;
                }

                var phase = phaseConfig.GetPhaseForLevel(i + 1);
                if (phase == null)
                {
                    Debug.LogError($"[DataManager] Phase bulunamadı: level {i + 1}");
                    return;
                }

                var entry = new LevelCatalogAsset.LevelCatalogEntry
                {
                    LevelIndex = i,
                    UseProceduralFallback = true,
                    ProceduralDifficulty = LevelProgressionService.PhaseToDifficulty(phase.ToStruct(), i + 1)
                };
                catalog.Levels.Add(entry);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[DataManager] LevelCatalog güncellendi: {catalog.Levels.Count} giriş");
        }

        /// <summary>
        /// Eksik LevelData referanslarını düzeltir — LevelCatalog'ta null AuthoredLevel olanları tarar.
        /// </summary>
        public static void FixMissingLevelReferences()
        {
            var catalog = Resources.Load<LevelCatalogAsset>("Configs/LevelCatalog");
            if (catalog == null)
            {
                Debug.LogError("[DataManager] LevelCatalog bulunamadı!");
                return;
            }

            int fixedCount = 0;
            foreach (var entry in catalog.Levels)
            {
                if (entry != null && !entry.UseProceduralFallback && entry.AuthoredLevel == null)
                {
                    var level = Resources.Load<LevelData>($"Levels/Level{entry.LevelIndex + 1}");
                    if (level != null)
                    {
                        entry.AuthoredLevel = level;
                        fixedCount++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DataManager] {fixedCount} seviye referansı düzeltildi.");
        }
    }
}
#endif
