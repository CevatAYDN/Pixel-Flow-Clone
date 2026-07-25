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
    /// Procedural level generation tool to generate 150 levels for launch.
    /// Uses the existing ProceduralLevelGenerator with phase-based difficulty parameters.
    /// </summary>
    public class GenerateLevels : EditorWindow
    {
        private int _targetLevelCount = 150;
        private int _currentLevelCount = 0;
        private string _outputPath = "Assets/Resources/Levels";
        private bool _generateAll = true;
        private int _startFromLevel = 4;
        private Vector2 _scrollPos;

        [MenuItem("Pixel Flow/Level Generation/Generate 150 Levels")]
        public static void ShowWindow()
        {
            var window = GetWindow<GenerateLevels>("Level Generator");
            window.minSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            GUILayout.Label("Procedural Level Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Target Level Count", _targetLevelCount.ToString());
            EditorGUILayout.LabelField("Current Level Count", _currentLevelCount.ToString());
            EditorGUILayout.LabelField("Levels to Generate", (_targetLevelCount - _currentLevelCount).ToString());

            GUILayout.Space(10);

            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);
            _generateAll = EditorGUILayout.Toggle("Generate All Missing Levels", _generateAll);
            
            if (!_generateAll)
            {
                _startFromLevel = EditorGUILayout.IntField("Start From Level", _startFromLevel);
            }

            GUILayout.Space(10);

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
                EditorUtility.DisplayDialog("Level Generation", "Already have enough levels!", "OK");
                return;
            }

            int startLevel = _generateAll ? _currentLevelCount + 1 : _startFromLevel;
            int levelsToGenerate = _targetLevelCount - startLevel + 1;

            var generator = new ProceduralLevelGenerator(new RuntimePathSolver());
            int generated = 0;
            int failed = 0;

            for (int levelIndex = startLevel; levelIndex <= _targetLevelCount; levelIndex++)
            {
                string fileName = $"Level{levelIndex}.asset";
                string filePath = Path.Combine(_outputPath, fileName);

                if (File.Exists(filePath))
                {
                    Debug.Log($"[GenerateLevels] Level {levelIndex} already exists, skipping");
                    continue;
                }

                // Determine phase and difficulty params based on level index
                var param = GetDifficultyParamsForLevel(levelIndex);
                
                var level = generator.Generate(param);
                if (level != null)
                {
                    level.levelIndex = levelIndex - 1; // 0-based
                    level.name = $"Level{levelIndex}";

                    // Save as asset
                    AssetDatabase.CreateAsset(level, $"Assets/Resources/Levels/{fileName}");
                    generated++;
                    
                    if (generated % 10 == 0)
                    {
                        AssetDatabase.SaveAssets();
                        Debug.Log($"[GenerateLevels] Generated {generated} levels so far...");
                    }
                }
                else
                {
                    failed++;
                    Debug.LogWarning($"[GenerateLevels] Failed to generate level {levelIndex}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[GenerateLevels] Generation complete! Generated: {generated}, Failed: {failed}");
            EditorUtility.DisplayDialog("Level Generation Complete", 
                $"Generated: {generated}\nFailed: {failed}\nTotal levels: {_targetLevelCount}", "OK");
        }

        private DifficultyParams GetDifficultyParamsForLevel(int levelIndex)
        {
            // Phase 1: Levels 1-12 (indices 0-11) - Tutorial/Easy
            // Phase 2: Levels 13-28 (indices 12-27) - Medium  
            // Phase 3: Levels 29-45 (indices 28-44) - Hard
            // Phase 4: Levels 46-60 (indices 45-59) - Expert
            // Phase 5: Levels 61-90 (indices 60-89) - Master
            // Phase 6: Levels 91-120 (indices 90-119) - Grandmaster
            // Phase 7: Levels 121-150 (indices 120-149) - Legendary

            if (levelIndex <= 12) // Phase 1: Tutorial
            {
                return new DifficultyParams(5, 5, 1, 0, false);
            }
            else if (levelIndex <= 28) // Phase 2: Nodes
            {
                return new DifficultyParams(6, 6, 2, 3, false);
            }
            else if (levelIndex <= 45) // Phase 3: Default
            {
                return new DifficultyParams(7, 7, 3, 3, true, true);
            }
            else if (levelIndex <= 60) // Phase 4: Endgame
            {
                return new DifficultyParams(8, 8, 4, 4, true, true, true, true);
            }
            else if (levelIndex <= 90) // Phase 5: Master
            {
                return new DifficultyParams(9, 9, 5, 4, true, true, true);
            }
            else if (levelIndex <= 120) // Phase 6: Grandmaster
            {
                return new DifficultyParams(10, 10, 5, 5, true, true, true, true);
            }
            else // Phase 7: Legendary (121-150)
            {
                return new DifficultyParams(10, 10, 5, 5, true, true, true, true);
            }
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

            foreach (var file in files)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(file);
                if (level != null)
                {
                    if (solver.Solve(level, out _))
                    {
                        Debug.Log($"[Validate] {Path.GetFileName(file)} - SOLVABLE");
                        validated++;
                    }
                    else
                    {
                        Debug.LogError($"[Validate] {Path.GetFileName(file)} - UNSOLVABLE!");
                        failed++;
                    }
                }
            }

            Debug.Log($"[ValidateAllLevels] Validated: {validated}, Failed: {failed}");
            EditorUtility.DisplayDialog("Validation Complete", 
                $"Validated: {validated}\nFailed: {failed}", "OK");
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

            // Phase 1
            phaseConfig.Phase1 = CreatePhaseAsset(1, 0, 11, 5, 5, 1, 2, 0, 0, false, false, false, false, false);
            phaseConfig.Phase1.name = "Phase1_Levels1-12";

            // Phase 2
            phaseConfig.Phase2 = CreatePhaseAsset(2, 12, 27, 6, 7, 2, 3, 3, 3, false, true, false, false, false);
            phaseConfig.Phase2.name = "Phase2_Levels13-28";

            // Phase 3
            phaseConfig.Phase3 = CreatePhaseAsset(3, 28, 44, 7, 9, 3, 4, 2, 4, true, true, false, false, false);
            phaseConfig.Phase3.name = "Phase3_Levels29-45";

            // Phase 4
            phaseConfig.Phase4 = CreatePhaseAsset(4, 45, 59, 8, 10, 4, 5, 4, 5, true, true, true, true, true);
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
            asset.name = $"Phase{phase}_Levels{start+1}-{end+1}";
            return asset;
        }
    }
}