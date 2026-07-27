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
                        var phaseConfig = Resources.Load<PhaseConfigAsset>("Configs/PhaseConfig");
                        if (phaseConfig == null)
                        {
                            Debug.LogError("[LevelCatalogFixer] PhaseConfig bulunamadı! Procedural entry'ler düzeltilemez.");
                            return;
                        }

                        var phase = phaseConfig.GetPhaseForLevel(entry.LevelIndex + 1);
                        if (phase == null)
                        {
                            Debug.LogError($"[LevelCatalogFixer] Phase bulunamadı: level {entry.LevelIndex + 1}");
                            return;
                        }

                        entry.ProceduralDifficulty = LevelProgressionService.PhaseToDifficulty(phase.ToStruct(), entry.LevelIndex + 1);
                        fixedCount++;
                    }
                }
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LevelCatalogFixer] Fixed {fixedCount}/{totalCount} procedural entries.");
        }

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
        }

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
                var phaseConfig = Resources.Load<PhaseConfigAsset>("Configs/PhaseConfig");
                if (phaseConfig == null)
                {
                    Debug.LogError("[LevelCatalogFixer] PhaseConfig bulunamadı! Procedural fallback üretilemez.");
                    return;
                }

                var phase = phaseConfig.GetPhaseForLevel(i + 1);
                if (phase == null)
                {
                    Debug.LogError($"[LevelCatalogFixer] Phase bulunamadı: level {i + 1}");
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

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int proceduralCount = catalog.Levels.Count - levels.Count;
            Debug.Log($"[LevelCatalogFixer] Regenerated: {levels.Count} authored + {proceduralCount} procedural = {catalog.Levels.Count} total");
        }

    }
}
#endif
