#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PixelFlow.Data;
using System.Linq;
using PixelFlow.Services;

namespace PixelFlow.Editor
{
    /// <summary>
    /// LevelCatalog düzeltici — procedural entry'lerin DifficultyParams'ini düzeltir.
    /// Çalıştırma: Pixel Flow/Config Validator/Fix LevelCatalog Procedural Entries
    /// </summary>
    public class LevelCatalogFixer : EditorWindow
    {
        [MenuItem("Pixel Flow/Config Validator/Fix LevelCatalog Procedural Entries")]
        public static void FixProceduralEntries()
        {
            var catalog = Resources.Load<LevelCatalogAsset>("Configs/LevelCatalog");
            if (catalog == null)
            {
                Debug.LogError("[LevelCatalogFixer] LevelCatalog.asset bulunamadı!");
                return;
            }

            int fixedCount = 0;
            int totalCount = 0;

            foreach (var entry in catalog.Levels)
            {
                if (entry == null) continue;
                totalCount++;

                if (entry.UseProceduralFallback)
                {
                    bool needsFix = entry.ProceduralDifficulty.gridWidth == 0 ||
                                    entry.ProceduralDifficulty.gridHeight == 0 ||
                                    entry.ProceduralDifficulty.colorCount == 0;

                    if (needsFix)
                    {
                        entry.ProceduralDifficulty = GetCorrectDifficultyForLevel(entry.LevelIndex + 1);
                        fixedCount++;
                    }
                }
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LevelCatalogFixer] Fixed {fixedCount}/{totalCount} procedural entries.");
            EditorUtility.DisplayDialog(
                "LevelCatalog Düzeltildi",
                $"Toplam: {totalCount}\nDüzeltilen: {fixedCount}",
                "Tamam"
            );
        }

        [MenuItem("Pixel Flow/Config Validator/Clean Empty LevelCatalog Entries")]
        public static void CleanEmptyEntries()
        {
            var catalog = Resources.Load<LevelCatalogAsset>("Configs/LevelCatalog");
            if (catalog == null)
            {
                Debug.LogError("[LevelCatalogFixer] LevelCatalog.asset bulunamadı!");
                return;
            }

            int initialCount = catalog.Levels.Count;

            // Remove null entries
            catalog.Levels.RemoveAll(e => e == null);

            // Remove entries with invalid level index
            catalog.Levels.RemoveAll(e => e.LevelIndex < 0);

            // Trim to 150 levels (launch target)
            if (catalog.Levels.Count > 150)
            {
                catalog.Levels = catalog.Levels.Take(150).ToList();
            }

            // Re-index levels to be sequential
            for (int i = 0; i < catalog.Levels.Count; i++)
            {
                catalog.Levels[i].LevelIndex = i;
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LevelCatalogFixer] Cleaned: {initialCount} -> {catalog.Levels.Count} entries.");
            EditorUtility.DisplayDialog(
                "LevelCatalog Temizlendi",
                $"Başlangıç: {initialCount}\nSon: {catalog.Levels.Count}",
                "Tamam"
            );
        }

        [MenuItem("Pixel Flow/Config Validator/Regenerate LevelCatalog from Levels Folder")]
        public static void RegenerateFromLevelsFolder()
        {
            var catalog = Resources.Load<LevelCatalogAsset>("Configs/LevelCatalog");
            if (catalog == null)
            {
                Debug.LogError("[LevelCatalogFixer] LevelCatalog.asset bulunamadı! Önce oluşturun.");
                return;
            }

            // Load all LevelData assets
            var levelGuids = AssetDatabase.FindAssets("t:LevelData");
            var levels = levelGuids
                .Select(guid => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(l => l != null)
                .OrderBy(l => l.levelIndex)
                .ToList();

            catalog.Levels.Clear();

            // Add authored levels
            foreach (var level in levels)
            {
                var entry = new LevelCatalogAsset.LevelCatalogEntry
                {
                    LevelIndex = level.levelIndex,
                    AuthoredLevel = level,
                    UseProceduralFallback = false,
                    ProceduralDifficulty = default
                };
                catalog.Levels.Add(entry);
            }

            // Add procedural fallback for missing levels (up to 150)
            int maxIndexed = levels.Count > 0 ? levels.Max(l => l.levelIndex) : -1;
            for (int i = maxIndexed + 1; i < 150; i++)
            {
                var entry = new LevelCatalogAsset.LevelCatalogEntry
                {
                    LevelIndex = i,
                    UseProceduralFallback = true,
                    ProceduralDifficulty = GetCorrectDifficultyForLevel(i + 1)
                };
                catalog.Levels.Add(entry);
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LevelCatalogFixer] Regenerated: {levels.Count} authored + {150 - maxIndexed - 1} procedural = {catalog.Levels.Count} total");
            EditorUtility.DisplayDialog(
                "LevelCatalog Yeniden Oluşturuldu",
                $"Authored: {levels.Count}\nProcedural: {150 - maxIndexed - 1}\nTotal: {catalog.Levels.Count}",
                "Tamam"
            );
        }

        private static DifficultyParams GetCorrectDifficultyForLevel(int levelIndex)
        {
            if (levelIndex <= 5) return new DifficultyParams(5, 5, 1, 0, false);
            if (levelIndex <= 15) return new DifficultyParams(6, 6, 2, 0, false);
            if (levelIndex <= 30) return new DifficultyParams(7, 7, 2, 1, false);
            if (levelIndex <= 50) return new DifficultyParams(8, 8, 3, 2, true);
            if (levelIndex <= 75) return new DifficultyParams(9, 9, 4, 3, true, true);
            if (levelIndex <= 100) return new DifficultyParams(10, 10, 5, 4, true, true, true, false);
            if (levelIndex <= 120) return new DifficultyParams(10, 10, 5, 4, true, true, true, true);
            return new DifficultyParams(10, 10, 5, 5, true, true, true, true);
        }
    }
}
#endif
