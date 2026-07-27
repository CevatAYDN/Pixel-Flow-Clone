using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using PixelFlow.Data;
using PixelFlow.Services;

namespace PixelFlow.Editor
{
    /// <summary>
    /// Procedural level generation tool — 50+ authored seviye üretimi.
    /// Tüm parametreler GameConfig ve PhaseConfig'den okunur (sıfır hardcode).
    /// Her seviye RuntimePathSolver ile doğrulanır (çözülebilirlik testi).
    /// </summary>
    public class GenerateLevels : EditorWindow
    {
        private int _targetLevelCount = 150;
        private int _currentLevelCount = 0;
        private string _outputPath = "Assets/Resources/Levels";
        private bool _generateAll = true;
        private int _startFromLevel = 4;
        private Vector2 _scrollPos;
        private bool _verboseLogging = false;

        [MenuItem("PixelFlow/Level Generator")]
        public static void ShowWindow()
        {
            PixelFlowSetupWindow.OpenTab(2);
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            GUILayout.Label("Procedural Level Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Config bilgisi
            var gameConfig = Resources.Load<GameConfig>("Configs/GameConfig");
            if (gameConfig != null)
            {
                EditorGUILayout.LabelField("GameConfig VehicleSpeed", gameConfig.VehicleSpeed.ToString());
                EditorGUILayout.LabelField("Default Unlocked Levels", gameConfig.DefaultUnlockedLevels.ToString());
            }
            else
            {
                EditorGUILayout.HelpBox("GameConfig bulunamadı! Önce 'Data Yöneticisi'nden oluşturun.", MessageType.Warning);
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Target Level Count", _targetLevelCount.ToString());
            EditorGUILayout.LabelField("Current Level Count", _currentLevelCount.ToString());
            EditorGUILayout.LabelField("Levels to Generate", Mathf.Max(0, _targetLevelCount - _currentLevelCount).ToString());

            GUILayout.Space(10);

            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);
            _generateAll = EditorGUILayout.Toggle("Generate All Missing Levels", _generateAll);

            if (!_generateAll)
            {
                _startFromLevel = EditorGUILayout.IntField("Start From Level", _startFromLevel);
            }

            _verboseLogging = EditorGUILayout.Toggle("Verbose Logging", _verboseLogging);

            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(gameConfig == null);

            if (GUILayout.Button("Scan Existing Levels", GUILayout.Height(30)))
            {
                ScanExistingLevels();
            }

            if (GUILayout.Button("Generate Missing Levels", GUILayout.Height(30)))
            {
                GenerateMissingLevels();
            }

            if (GUILayout.Button("Validate All Levels (Solver Test)", GUILayout.Height(30)))
            {
                ValidateAllLevels();
            }

            if (GUILayout.Button("Generate Phase Definitions", GUILayout.Height(30)))
            {
                GeneratePhaseDefinitions();
            }

            if (GUILayout.Button("Regenerate LevelCatalog", GUILayout.Height(30)))
            {
                RegenerateLevelCatalog();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndScrollView();
        }

        private void ScanExistingLevels()
        {
            if (!Directory.Exists(_outputPath))
            {
                Debug.LogWarning($"Output path does not exist: {_outputPath}");
                return;
            }

            var files = Directory.GetFiles(_outputPath, "Level*.asset");
            _currentLevelCount = files.Length;
            Debug.Log($"[GenerateLevels] Found {_currentLevelCount} existing levels in {_outputPath}");
        }

        private void GenerateMissingLevels()
        {
            ScanExistingLevels();

            if (_currentLevelCount >= _targetLevelCount)
            {
                Debug.Log("[GenerateLevels] Already have enough levels!");
                return;
            }

            var gameConfig = Resources.Load<GameConfig>("Configs/GameConfig");
            if (gameConfig == null)
            {
                Debug.LogError("[GenerateLevels] GameConfig bulunamadı! Önce Data Yöneticisi'nden oluşturun.");
                return;
            }

            var phaseConfig = Resources.Load<PhaseConfigAsset>("Configs/PhaseConfig");
            if (phaseConfig == null)
            {
                Debug.LogError("[GenerateLevels] PhaseConfig bulunamadı! Önce Phase Asset Generator çalıştırın.");
                return;
            }

            int startLevel = _generateAll ? _currentLevelCount + 1 : _startFromLevel;
            int levelsToGenerate = Mathf.Max(0, _targetLevelCount - startLevel + 1);

            var solver = new RuntimePathSolver();
            var progression = new LevelProgressionService(solver, phaseConfig);
            var generator = new ProceduralLevelGenerator(solver);
            int generated = 0;
            int failed = 0;
            int validated = 0;

            for (int levelIndex = startLevel; levelIndex <= _targetLevelCount; levelIndex++)
            {
                string fileName = $"Level{levelIndex}.asset";
                string filePath = Path.Combine(_outputPath, fileName);

                if (File.Exists(filePath))
                {
                    if (_verboseLogging) Debug.Log($"[GenerateLevels] Level {levelIndex} already exists, skipping");
                    continue;
                }

                var param = progression.GetDifficultyForLevel(levelIndex - 1);

                var level = generator.Generate(param);
                if (level != null)
                {
                    level.levelIndex = levelIndex - 1;
                    level.name = $"Level{levelIndex}";

                    // Solve test — sadece çözülebilir seviyeleri kaydet
                    if (solver.Solve(level, out _))
                    {
                        AssetDatabase.CreateAsset(level, $"{_outputPath}/{fileName}");
                        generated++;
                        validated++;

                        if (generated % 10 == 0)
                        {
                            AssetDatabase.SaveAssets();
                            Debug.Log($"[GenerateLevels] Generated {generated}/{levelsToGenerate} levels...");
                        }
                    }
                    else
                    {
                        failed++;
                        if (_verboseLogging)
                            Debug.LogWarning($"[GenerateLevels] Level {levelIndex} UNSOLVABLE, regenerating...");

                        // Tekrar dene (max 3 deneme)
                        bool solved = false;
                        for (int attempt = 0; attempt < 3; attempt++)
                        {
                            level = generator.Generate(param);
                            if (level != null && solver.Solve(level, out _))
                            {
                                level.levelIndex = levelIndex - 1;
                                level.name = $"Level{levelIndex}";
                                AssetDatabase.CreateAsset(level, $"{_outputPath}/{fileName}");
                                generated++;
                                validated++;
                                solved = true;
                                break;
                            }
                        }

                        if (!solved)
                        {
                            Debug.LogWarning($"[GenerateLevels] Failed to generate solvable level {levelIndex} after 3 attempts");
                        }
                    }
                }
                else
                {
                    failed++;
                    Debug.LogWarning($"[GenerateLevels] Generator returned null for level {levelIndex}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = $"Generated: {generated}\nValidated (Solvable): {validated}\nFailed: {failed}\nTotal: {_currentLevelCount + generated}";
            Debug.Log($"[GenerateLevels] Generation complete!\n{msg}");
        }

        private void ValidateAllLevels()
        {
            if (!Directory.Exists(_outputPath))
            {
                Debug.LogWarning($"Output path does not exist: {_outputPath}");
                return;
            }

            var files = Directory.GetFiles(_outputPath, "Level*.asset");
            var solver = new RuntimePathSolver();
            int validated = 0;
            int failed = 0;
            int skipped = 0;

            foreach (var file in files)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(file);
                if (level != null)
                {
                    if (solver.Solve(level, out _))
                    {
                        validated++;
                        if (_verboseLogging)
                            Debug.Log($"[Validate] {Path.GetFileName(file)} - SOLVABLE");
                    }
                    else
                    {
                        failed++;
                        Debug.LogError($"[Validate] {Path.GetFileName(file)} - UNSOLVABLE!");
                    }
                }
                else
                {
                    skipped++;
                }
            }

            string msg = $"Validated (Solvable): {validated}\nFailed (Unsolvable): {failed}\nSkipped (Load Error): {skipped}\nTotal: {files.Length}";
            Debug.Log($"[ValidateAllLevels] {msg}");
        }

        private void GeneratePhaseDefinitions()
        {
            string phaseConfigPath = "Assets/Resources/Configs/PhaseConfig.asset";
            var phaseConfig = AssetDatabase.LoadAssetAtPath<PhaseConfigAsset>(phaseConfigPath);

            if (phaseConfig == null)
            {
                phaseConfig = ScriptableObject.CreateInstance<PhaseConfigAsset>();
                AssetDatabase.CreateAsset(phaseConfig, phaseConfigPath);
            }

            // Phase 1: Tutorial (Seviye 1-12)
            phaseConfig.Phase1 = CreatePhaseAsset(1, 0, 11, 5, 5, 1, 1, 0, 0, false, false, false, false, false);
            phaseConfig.Phase1.name = "Phase1_Levels1-12";

            // Phase 2: Multiple Colors (Seviye 13-28)
            phaseConfig.Phase2 = CreatePhaseAsset(2, 12, 27, 5, 6, 2, 3, 0, 0, false, false, false, false, false);
            phaseConfig.Phase2.name = "Phase2_Levels13-28";

            // Phase 3: Bridges (Seviye 29-45)
            phaseConfig.Phase3 = CreatePhaseAsset(3, 28, 44, 6, 7, 2, 3, 1, 2, false, false, false, false, false);
            phaseConfig.Phase3.name = "Phase3_Levels29-45";

            // Phase 4: OneWay + Obstacles (Seviye 46-60)
            phaseConfig.Phase4 = CreatePhaseAsset(4, 45, 59, 7, 9, 3, 5, 1, 3, true, true, false, false, false);
            phaseConfig.Phase4.name = "Phase4_Levels46-60";

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GenerateLevels] Phase definitions generated successfully");
        }

        private PhaseDefinitionAsset CreatePhaseAsset(int phase, int start, int end, int gridMin, int gridMax,
            int colorMin, int colorMax, int bridgeMin, int bridgeMax, bool fullCoverage,
            bool obstacles, bool oneWay, bool ferry, bool narrow = false)
        {
            var asset = ScriptableObject.CreateInstance<PhaseDefinitionAsset>();
            asset.Phase = (GamePhase)phase;
            asset.StartLevelIndex = start;
            asset.EndLevelIndex = end;
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
            asset.NarrowPassEnabled = narrow;
            asset.name = $"Phase{phase}_Levels{start + 1}-{end + 1}";
            return asset;
        }

        /// <summary>
        /// LevelCatalog'u yeniden oluştur — tüm seviyeleri tarar ve catalog'a ekler.
        /// </summary>
        private void RegenerateLevelCatalog()
        {
            string catalogPath = "Assets/Resources/Configs/LevelCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogAsset>(catalogPath);
            var phaseConfig = Resources.Load<PhaseConfigAsset>("Configs/PhaseConfig");
            if (phaseConfig == null)
            {
                Debug.LogError("[RegenerateLevelCatalog] PhaseConfig bulunamadı! Önce Phase Asset Generator çalıştırın.");
                return;
            }
            var progression = new LevelProgressionService(new RuntimePathSolver(), phaseConfig, catalog);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            // Mevcut seviyeleri tara
            if (!Directory.Exists(_outputPath))
            {
                Debug.LogWarning($"Output path does not exist: {_outputPath}");
                return;
            }

            var files = Directory.GetFiles(_outputPath, "Level*.asset")
                .OrderBy(f => int.TryParse(Path.GetFileNameWithoutExtension(f).Replace("Level", ""), out int idx) ? idx : 999)
                .ToList();

            catalog.Levels.Clear();

            foreach (var file in files)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(file);
                if (level != null)
                {
                    var entry = new LevelCatalogAsset.LevelCatalogEntry
                    {
                        LevelIndex = level.levelIndex,
                        AuthoredLevel = level,
                        UseProceduralFallback = false
                    };
                    catalog.Levels.Add(entry);
                }
            }

            // Eksik seviyeler için procedural fallback ekle
            int maxIndexed = files.Count > 0 ? files.Count : 0;
            for (int i = maxIndexed; i < _targetLevelCount; i++)
            {
                var entry = new LevelCatalogAsset.LevelCatalogEntry
                {
                    LevelIndex = i,
                    UseProceduralFallback = true,
                    ProceduralDifficulty = progression.GetDifficultyForLevel(i)
                };
                catalog.Levels.Add(entry);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RegenerateLevelCatalog] Catalog updated with {catalog.Levels.Count} entries");
        }
    }
}
