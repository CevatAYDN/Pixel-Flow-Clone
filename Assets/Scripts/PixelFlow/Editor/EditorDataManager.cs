#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PixelFlow.Data;
using PixelFlow.Services;
using System.Collections.Generic;
using System.Linq;

namespace PixelFlow.Editor
{
    /// <summary>
    /// Editör araçları için veri doğrulama ve yönetim merkezi.
    /// Tüm config asset'lerini tek panelden yönetir, sıfır hardcode.
    /// </summary>
    public class EditorDataManager : EditorWindow
    {
        private static Dictionary<string, bool> _assetStatusCache = new Dictionary<string, bool>();
        private static bool _cacheInitialized;

        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow<EditorDataManager>("Editör Veri Yöneticisi");
            window.minSize = new Vector2(600, 400);
        }

        private void OnGUI()
        {
            GUILayout.Label("Editör Veri Yöneticisi", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Asset durumu kontrolü
            if (!_cacheInitialized)
            {
                CheckAllAssets();
                _cacheInitialized = true;
            }

            GUILayout.Label("Config Asset Durumu", EditorStyles.boldLabel);
            foreach (var kvp in _assetStatusCache)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(kvp.Key, GUILayout.MinWidth(150));
                EditorGUILayout.LabelField(kvp.Value ? "✅ Hazır" : "❌ Eksik", kvp.Value ? EditorStyles.boldLabel : EditorStyles.wordWrappedLabel);
                if (!kvp.Value)
                {
                    if (GUILayout.Button("Oluştur", GUILayout.MinWidth(80)))
                    {
                        CreateAsset(kvp.Key);
                        CheckAllAssets();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Tüm Asset'leri Oluştur"))
            {
                CreateAllAssets();
                CheckAllAssets();
            }
            if (GUILayout.Button("Level Catalog'u Yeniden Oluştur"))
            {
                RegenerateLevelCatalog();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Seviye İstatistikleri", EditorStyles.boldLabel);
            DrawLevelStats();
        }

        private void CheckAllAssets()
        {
            _assetStatusCache.Clear();
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

        private bool CheckAssetExists<T>(string path) where T : ScriptableObject
        {
            return Resources.Load<T>(path) != null;
        }

        private void CreateAsset(string name)
        {
            switch (name)
            {
                case "GameConfig": CreateIfMissing<GameConfig>("Configs/GameConfig", "Merkezi oyun konfigürasyonu"); break;
                case "ThemePalette": CreateIfMissing<ThemePaletteAsset>("Configs/ThemePalette", "Tema renk paletleri"); break;
                case "VehicleMaterialConfig": CreateIfMissing<VehicleMaterialConfigAsset>("Configs/VehicleMaterialConfig", "Araç materyal renkleri"); break;
                case "VehicleVisualConfig": CreateIfMissing<VehicleVisualConfigAsset>("Configs/VehicleVisualConfig", "Araç görsel parametreleri"); break;
                case "ColorBlindPalette": CreateIfMissing<ColorBlindPaletteAsset>("Configs/ColorBlindPalette", "Renk körlüğü paleti"); break;
                case "EconomyConfig": CreateIfMissing<EconomyConfigAsset>("Configs/EconomyConfig", "Ekonomi dengesi"); break;
                case "LevelCatalog": CreateIfMissing<LevelCatalogAsset>("Configs/LevelCatalog", "Seviye kataloğu"); break;
                case "PhaseConfig": CreateIfMissing<PhaseConfigAsset>("Configs/PhaseConfig", "Faz tanımları"); break;
                case "DifficultyFormulaConfig": CreateIfMissing<DifficultyFormulaConfigAsset>("Configs/DifficultyFormulaConfig", "Zorluk formülü"); break;
                case "DefaultSkinIdsConfig": CreateIfMissing<DefaultSkinIdsConfigAsset>("Configs/DefaultSkinIdsConfig", "Varsayılan skin ID'leri"); break;
                case "BouncyPhysicsConfig": CreateIfMissing<BouncyPhysicsConfigAsset>("Configs/BouncyPhysicsConfig", "Zıplama fizik ayarları"); break;
                case "StarCriteriaConfig": CreateIfMissing<StarCriteriaConfigAsset>("Configs/StarCriteriaConfig", "Yıldız kriterleri"); break;
                case "RushHourConfig": CreateIfMissing<RushHourConfigAsset>("Configs/RushHourConfig", "Rush Hour ayarları"); break;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreateIfMissing<T>(string resourcePath, string description) where T : ScriptableObject
        {
            if (Resources.Load<T>(resourcePath) != null) return;
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = typeof(T).Name;
            string fullPath = $"Assets/Resources/{resourcePath}.asset";
            AssetDatabase.CreateAsset(asset, fullPath);
            Debug.Log($"[EditorDataManager] Oluşturuldu: {fullPath} ({description})");
        }

        private void CreateAllAssets()
        {
            string configsPath = "Assets/Resources/Configs";
            System.IO.Directory.CreateDirectory(configsPath);

            CreateAsset("GameConfig");
            CreateAsset("ThemePalette");
            CreateAsset("VehicleMaterialConfig");
            CreateAsset("VehicleVisualConfig");
            CreateAsset("ColorBlindPalette");
            CreateAsset("EconomyConfig");
            CreateAsset("LevelCatalog");
            CreateAsset("PhaseConfig");
            CreateAsset("DifficultyFormulaConfig");
            CreateAsset("DefaultSkinIdsConfig");
            CreateAsset("BouncyPhysicsConfig");
            CreateAsset("StarCriteriaConfig");
            CreateAsset("RushHourConfig");

            Debug.Log("[EditorDataManager] Tüm asset işlemleri tamamlandı.");
        }


        private void RegenerateLevelCatalog()
        {
            var catalog = Resources.Load<LevelCatalogAsset>("Configs/LevelCatalog");
            if (catalog == null)
            {
                Debug.LogError("[EditorDataManager] LevelCatalog bulunamadı!");
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
                    Debug.LogError("[EditorDataManager] PhaseConfig bulunamadı! Procedural fallback üretilemez.");
                    return;
                }

                var phase = phaseConfig.GetPhaseForLevel(i + 1);
                if (phase == null)
                {
                    Debug.LogError($"[EditorDataManager] Phase bulunamadı: level {i + 1}");
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
            Debug.Log($"[EditorDataManager] LevelCatalog güncellendi: {catalog.Levels.Count} giriş");
        }

        private void DrawLevelStats()
        {
            var levels = AssetDatabase.FindAssets("t:LevelData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(l => l != null)
                .ToList();

            EditorGUILayout.LabelField("Toplam Seviye:", levels.Count.ToString());

            if (levels.Count > 0)
            {
                int minIndex = levels.Min(l => l.levelIndex);
                int maxIndex = levels.Max(l => l.levelIndex);
                EditorGUILayout.LabelField("İndeks Aralığı:", $"{minIndex} - {maxIndex}");

                var config = Resources.Load<GameConfig>("Configs/GameConfig");
                var solver = new RuntimePathSolver();
                if (config != null) solver.SetEditorConfig(config);
                int solvable = 0;
                foreach (var level in levels)
                {
                    if (solver.Solve(level, out _)) solvable++;
                }
                EditorGUILayout.LabelField("Çözülebilir:", $"{solvable}/{levels.Count}");
            }
        }
    }
}
#endif
