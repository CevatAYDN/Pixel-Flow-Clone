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
    /// Config Validator — tüm config asset'lerini doğrular ve hataları düzeltir.
    /// Çalıştırma: Pixel Flow/Config Validator/Validate & Fix All Configs
    /// </summary>
    public class ConfigValidator : EditorWindow
    {
        private List<string> _errors = new List<string>();
        private List<string> _warnings = new List<string>();
        private Vector2 _scrollPos;

        public static void ShowWindow()
        {
            var window = GetWindow<ConfigValidator>("Config Validator");
            window.minSize = new Vector2(600, 500);
        }

        private void OnGUI()
        {
            GUILayout.Label("Config Validator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Validate & Fix All Configs"))
            {
                ValidateAndFixAll();
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField($"Errors: {_errors.Count}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Warnings: {_warnings.Count}", EditorStyles.boldLabel);

            GUILayout.Space(10);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var error in _errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            foreach (var warning in _warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            if (_errors.Count == 0 && _warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("All configs are valid!", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void ValidateAndFixAll()
        {
            _errors.Clear();
            _warnings.Clear();

            // 1. GameConfig validation
            ValidateGameConfig();

            // 2. LevelCatalog validation
            ValidateLevelCatalog();

            // 3. VehicleVisualConfig validation
            ValidateVehicleVisualConfig();

            // 4. ThemePalette validation
            ValidateThemePalettes();

            // 5. EconomyConfig validation
            ValidateEconomyConfig();

            // 6. PhaseConfig validation
            ValidatePhaseConfig();

            // Auto-fix critical issues
            if (_errors.Any())
            {
                Debug.LogWarning($"[ConfigValidator] Found {_errors.Count} errors. Auto-fixing immediately to avoid modal dialog failures.");
                AutoFixAll();
            }

            Repaint();
        }

        private void ValidateGameConfig()
        {
            var config = Resources.Load<GameConfig>("Configs/GameConfig");
            if (config == null)
            {
                _errors.Add("GameConfig.asset bulunamadı! Pixel Flow/Data/Create All Config Assets çalıştırın.");
                return;
            }

            // Check for zero or negative values that would break gameplay
            if (config.VehicleSpeed <= 0f)
                _errors.Add($"GameConfig.VehicleSpeed <= 0! Mevcut: {config.VehicleSpeed}");

            if (config.SpawnInterval <= 0f)
                _errors.Add($"GameConfig.SpawnInterval <= 0! Mevcut: {config.SpawnInterval}");

            if (config.MaxSimulationSafetyDuration <= 0f)
                _errors.Add($"GameConfig.MaxSimulationSafetyDuration <= 0! Mevcut: {config.MaxSimulationSafetyDuration}");

            if (config.AudioPoolSize <= 0)
                _errors.Add($"GameConfig.AudioPoolSize <= 0! Mevcut: {config.AudioPoolSize}");

            if (config.PathSolverMaxIterations <= 0)
                _errors.Add($"GameConfig.PathSolverMaxIterations <= 0! Mevcut: {config.PathSolverMaxIterations}");

            // Warnings for potentially problematic values
            if (config.DefaultUnlockedLevels > 10)
                _warnings.Add($"GameConfig.DefaultUnlockedLevels çok yüksek: {config.DefaultUnlockedLevels}");

            if (config.InterstitialLevelInterval < 1)
                _errors.Add($"GameConfig.InterstitialLevelInterval < 1! Mevcut: {config.InterstitialLevelInterval}");
        }

        private void ValidateLevelCatalog()
        {
            var catalog = Resources.Load<LevelCatalogAsset>("Configs/LevelCatalog");
            if (catalog == null)
            {
                _errors.Add("LevelCatalog.asset bulunamadı!");
                return;
            }

            if (catalog.Levels == null || catalog.Levels.Count == 0)
            {
                _errors.Add("LevelCatalog.Levels boş! Seviye tanımları yok.");
                return;
            }

            // Check for duplicate level indices
            var indices = catalog.Levels.Select(e => e?.LevelIndex).ToList();
            var duplicates = indices.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Any())
            {
                _errors.Add($"LevelCatalog'da tekrarlanan level index'leri: {string.Join(", ", duplicates)}");
            }

            // Check for procedural entries with default DifficultyParams
            int badProceduralCount = 0;
            foreach (var entry in catalog.Levels)
            {
                if (entry == null) continue;

                if (entry.UseProceduralFallback)
                {
                    if (entry.ProceduralDifficulty.gridWidth == 0 || entry.ProceduralDifficulty.gridHeight == 0)
                    {
                        badProceduralCount++;
                    }
                }
                else
                {
                    if (entry.AuthoredLevel == null)
                    {
                        _errors.Add($"LevelCatalog LevelIndex {entry.LevelIndex}: AuthoredLevel NULL!");
                    }
                }
            }

            if (badProceduralCount > 0)
            {
                _errors.Add($"LevelCatalog'da {badProceduralCount} procedural entry'de default DifficultyParams (gridWidth=0)! Bunlar düzeltilmeli.");
            }

            // Check for excessive entries
            if (catalog.Levels.Count > 200)
            {
                _warnings.Add($"LevelCatalog'da {catalog.Levels.Count} entry var. Launch için 150 hedefleniyor.");
            }

            // Count authored vs procedural
            int authoredCount = catalog.Levels.Count(e => !e.UseProceduralFallback && e.AuthoredLevel != null);
            int proceduralCount = catalog.Levels.Count(e => e.UseProceduralFallback);
            _warnings.Add($"LevelCatalog: {authoredCount} authored, {proceduralCount} procedural fallback");
        }

        private void ValidateVehicleVisualConfig()
        {
            var config = Resources.Load<VehicleVisualConfigAsset>("Configs/VehicleVisualConfig");
            if (config == null)
            {
                _errors.Add("VehicleVisualConfig.asset bulunamadı!");
                return;
            }

            // Check for zero or negative sizes
            if (config.CarBodySize.x <= 0 || config.CarBodySize.y <= 0 || config.CarBodySize.z <= 0)
                _errors.Add("VehicleVisualConfig.CarBodySize negatif veya sıfır!");

            if (config.TrainBodySize.x <= 0 || config.TrainBodySize.y <= 0 || config.TrainBodySize.z <= 0)
                _errors.Add("VehicleVisualConfig.TrainBodySize negatif veya sıfır!");

            // Check wheel positions
            if (config.CarWheelXPositions == null || config.CarWheelXPositions.Count == 0)
                _errors.Add("VehicleVisualConfig.CarWheelXPositions boş!");

            if (config.TrainLocoWheelPositions == null || config.TrainLocoWheelPositions.Count == 0)
                _errors.Add("VehicleVisualConfig.TrainLocoWheelPositions boş!");
        }

        private void ValidateThemePalettes()
        {
            var mainPalette = Resources.Load<ThemePaletteAsset>("Configs/ThemePalette");
            if (mainPalette == null)
            {
                _errors.Add("ThemePalette.asset bulunamadı!");
                return;
            }

            // Check for clear colors where they shouldn't be
            if (mainPalette.OneWay.Background == Color.clear)
                _warnings.Add("ThemePalette.OneWay.Background Color.clear — intentional?");
        }

        private void ValidateEconomyConfig()
        {
            var config = Resources.Load<EconomyConfigAsset>("Configs/EconomyConfig");
            if (config == null)
            {
                _errors.Add("EconomyConfig.asset bulunamadı!");
                return;
            }

            if (config.ViaductBonusDivisor <= 0)
                _errors.Add("EconomyConfig.ViaductBonusDivisor <= 0!");

            if (config.ThreeStarsMaxViaducts < 0 || config.TwoStarsMaxViaducts < 0)
                _errors.Add("EconomyConfig star max viaducts negatif!");

            if (config.BaseScorePerCell <= 0)
                _errors.Add("EconomyConfig.BaseScorePerCell <= 0!");
        }

        private void ValidatePhaseConfig()
        {
            var config = Resources.Load<PhaseConfigAsset>("Configs/PhaseConfig");
            if (config == null)
            {
                _errors.Add("PhaseConfig.asset bulunamadı!");
                return;
            }

            if (config.Phase1 == null)
                _errors.Add("PhaseConfig.Phase1 null!");

            if (config.Phase2 == null)
                _errors.Add("PhaseConfig.Phase2 null!");

            if (config.Phase3 == null)
                _errors.Add("PhaseConfig.Phase3 null!");

            if (config.Phase4 == null)
                _errors.Add("PhaseConfig.Phase4 null!");
        }

        private void AutoFixAll()
        {
            // Fix LevelCatalog procedural entries
            FixLevelCatalogProceduralEntries();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _errors.Clear();
            _warnings.Clear();
            ValidateAndFixAll(); // Re-validate after fix
        }

        private void FixLevelCatalogProceduralEntries()
        {
            var catalog = Resources.Load<LevelCatalogAsset>("Configs/LevelCatalog");
            if (catalog == null) return;

            int fixedCount = 0;
            foreach (var entry in catalog.Levels)
            {
                if (entry == null || !entry.UseProceduralFallback) continue;

                // Fix default DifficultyParams
                if (entry.ProceduralDifficulty.gridWidth == 0 || entry.ProceduralDifficulty.gridHeight == 0)
                {
                    var phaseConfig = Resources.Load<PhaseConfigAsset>("Configs/PhaseConfig");
                    if (phaseConfig == null)
                    {
                        _errors.Add("PhaseConfig.asset bulunamadı! Procedural entry'ler düzeltilemez.");
                        return;
                    }

                    var phase = phaseConfig.GetPhaseForLevel(entry.LevelIndex + 1);
                    if (phase == null)
                    {
                        _errors.Add($"Phase bulunamadı: level {entry.LevelIndex + 1}");
                        return;
                    }

                    entry.ProceduralDifficulty = LevelProgressionService.PhaseToDifficulty(phase.ToStruct(), entry.LevelIndex + 1);
                    fixedCount++;
                }
            }

            if (fixedCount > 0)
            {
                Debug.Log($"[ConfigValidator] {fixedCount} procedural entry fixed!");
                EditorUtility.SetDirty(catalog);
            }
        }

    }
}
#endif
