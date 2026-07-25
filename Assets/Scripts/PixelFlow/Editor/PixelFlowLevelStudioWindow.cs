#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PixelFlow.Data;
using PixelFlow.Services;
using UnityEditor;
using UnityEngine;

namespace PixelFlow.Editor
{
    public class PixelFlowLevelStudioWindow : EditorWindow
    {
        private enum StudioTab
        {
            LevelBankAuditor,
            ProceduralStudio,
            BalanceAnalyzer,
            NexusDiagnostics
        }

        private StudioTab _currentTab = StudioTab.LevelBankAuditor;

        // Tab 1: Level Bank
        private List<LevelData> _cachedLevels = new List<LevelData>();
        private Dictionary<LevelData, ValidationResult> _validationCache = new Dictionary<LevelData, ValidationResult>();
        private Vector2 _bankScrollPos;
        private string _searchFilter = "";

        // Tab 2: Procedural Studio
        private int _procSeed = 12345;
        private bool _procUseSeed = false;
        private int _procBatchCount = 5;
        private int _procStartIndex = 1;
        private int _procGridWidth = 5;
        private int _procGridHeight = 5;
        private int _procColorCount = 3;
        private bool _procRequireCoverage = false;
        private string _procStatusMessage = "";

        // Tab 3: Balance Analyzer
        private Vector2 _balanceScrollPos;

        // Tab 4: Diagnostics
        private Vector2 _diagScrollPos;
        private List<string> _diagnosticLogs = new List<string>();

        // Services
        private readonly ILevelValidator _validator = new LevelValidator();
        private readonly PathSolverFactory _solverFactory = new PathSolverFactory();

        // UI Styles
        private GUIStyle _headerStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _badgeStyle;

        [MenuItem("Pixel Flow/Seviye Stüdyosu Bankası (Level Bank Studio)", false, 10)]
        public static void ShowWindow()
        {
            PixelFlowSetupWindow.OpenTab(1);
        }

        private void OnEnable()
        {
            RefreshLevelBank();
        }

        private void RefreshLevelBank()
        {
            _cachedLevels.Clear();
            _validationCache.Clear();

            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null)
                {
                    _cachedLevels.Add(level);
                }
            }

            _cachedLevels.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));
        }

        private void OnGUI()
        {
            InitStyles();

            // Header Banner
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Pixel Flow Level Studio & Architecture Hub", _headerStyle);
            GUILayout.Label($"Total Registered Levels: {_cachedLevels.Count} | Engine: Unity 6 Nexus Architecture", EditorStyles.centeredGreyMiniLabel);
            GUILayout.EndVertical();

            GUILayout.Space(5);

            // Tab Buttons
            GUILayout.BeginHorizontal();
            StudioTab[] tabs = (StudioTab[])System.Enum.GetValues(typeof(StudioTab));
            foreach (var tab in tabs)
            {
                bool isSelected = _currentTab == tab;
                GUI.backgroundColor = isSelected ? new Color(0.2f, 0.65f, 1f) : Color.white;
                if (GUILayout.Button(GetTabTitle(tab), GUILayout.Height(32)))
                {
                    _currentTab = tab;
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            switch (_currentTab)
            {
                case StudioTab.LevelBankAuditor:
                    DrawLevelBankAuditorTab();
                    break;
                case StudioTab.ProceduralStudio:
                    DrawProceduralStudioTab();
                    break;
                case StudioTab.BalanceAnalyzer:
                    DrawBalanceAnalyzerTab();
                    break;
                case StudioTab.NexusDiagnostics:
                    DrawNexusDiagnosticsTab();
                    break;
            }
        }

        private void DrawLevelBankAuditorTab()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Level Bank Auditor & Batch Solver", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh Assets", GUILayout.Width(120), GUILayout.Height(24)))
            {
                RefreshLevelBank();
            }
            if (GUILayout.Button("Run Batch Auditor", GUILayout.Width(140), GUILayout.Height(24)))
            {
                RunBatchAuditor();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            _searchFilter = EditorGUILayout.TextField("Search Level (Index/Name):", _searchFilter);
            GUILayout.EndVertical();

            GUILayout.Space(5);

            _bankScrollPos = GUILayout.BeginScrollView(_bankScrollPos);

            var filteredLevels = _cachedLevels.Where(l => 
                string.IsNullOrEmpty(_searchFilter) || 
                l.levelIndex.ToString().Contains(_searchFilter) || 
                l.name.ToLower().Contains(_searchFilter.ToLower())).ToList();

            foreach (var lvl in filteredLevels)
            {
                GUILayout.BeginVertical(_cardStyle);
                GUILayout.BeginHorizontal();

                // Selection / Focus button
                if (GUILayout.Button($"Level {lvl.levelIndex} ({lvl.name})", EditorStyles.label, GUILayout.Width(180)))
                {
                    Selection.activeObject = lvl;
                    EditorGUIUtility.PingObject(lvl);
                }

                GUILayout.Label($"Size: {lvl.width}x{lvl.height}", GUILayout.Width(75));
                GUILayout.Label($"Viaducts: {lvl.viaductLimit}", GUILayout.Width(80));

                if (_validationCache.TryGetValue(lvl, out var valResult))
                {
                    GUI.color = valResult.IsValid ? (valResult.IsSolvable ? new Color(0.2f, 0.8f, 0.3f) : new Color(0.9f, 0.6f, 0.1f)) : new Color(0.9f, 0.2f, 0.2f);
                    string statusText = valResult.IsValid ? (valResult.IsSolvable ? "✔ Solvable" : "⚠ Unsolvable") : "✘ Error";
                    GUILayout.Label(statusText, EditorStyles.boldLabel, GUILayout.Width(100));
                    GUI.color = Color.white;

                    GUILayout.Label($"Score: {valResult.ComplexityScore}", GUILayout.Width(80));
                }
                else
                {
                    GUILayout.Label("Unvalidated", EditorStyles.miniLabel, GUILayout.Width(100));
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = lvl;
                }
                if (GUILayout.Button("Validate", GUILayout.Width(65)))
                {
                    _validationCache[lvl] = _validator.Validate(lvl);
                }

                GUILayout.EndHorizontal();

                if (_validationCache.TryGetValue(lvl, out var res) && res.Issues.Count > 0)
                {
                    foreach (var issue in res.Issues)
                    {
                        MessageType msgType = issue.Severity == ValidationSeverity.Error ? MessageType.Error :
                                             issue.Severity == ValidationSeverity.Warning ? MessageType.Warning : MessageType.Info;
                        EditorGUILayout.HelpBox(issue.Message, msgType);
                    }
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
        }

        private void RunBatchAuditor()
        {
            int total = _cachedLevels.Count;
            for (int i = 0; i < total; i++)
            {
                var lvl = _cachedLevels[i];
                EditorUtility.DisplayProgressBar("Auditing Level Bank", $"Validating Level {lvl.levelIndex} ({i+1}/{total})...", (float)i / total);
                _validationCache[lvl] = _validator.Validate(lvl);
            }
            EditorUtility.ClearProgressBar();
        }

        private void DrawProceduralStudioTab()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Procedural Level Generator Studio", EditorStyles.boldLabel);
            GUILayout.Space(5);

            _procStartIndex = EditorGUILayout.IntField("Start Level Index", _procStartIndex);
            _procBatchCount = EditorGUILayout.IntSlider("Batch Count", _procBatchCount, 1, 50);
            _procGridWidth = EditorGUILayout.IntSlider("Grid Width", _procGridWidth, 3, 10);
            _procGridHeight = EditorGUILayout.IntSlider("Grid Height", _procGridHeight, 3, 10);
            _procColorCount = EditorGUILayout.IntSlider("Color Count", _procColorCount, 2, 5);
            _procRequireCoverage = EditorGUILayout.Toggle("Require Full Coverage", _procRequireCoverage);

            GUILayout.Space(5);
            _procUseSeed = EditorGUILayout.Toggle("Use Fixed Seed", _procUseSeed);
            if (_procUseSeed)
            {
                _procSeed = EditorGUILayout.IntField("Seed", _procSeed);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Generate & Validate Batch", GUILayout.Height(36)))
            {
                GenerateProceduralBatch();
            }

            if (!string.IsNullOrEmpty(_procStatusMessage))
            {
                GUILayout.Space(5);
                EditorGUILayout.HelpBox(_procStatusMessage, MessageType.Info);
            }

            GUILayout.EndVertical();
        }

        private void GenerateProceduralBatch()
        {
            int created = 0;
            int seed = _procUseSeed ? _procSeed : Random.Range(1000, 99999);

            for (int i = 0; i < _procBatchCount; i++)
            {
                int lvlIndex = _procStartIndex + i;
                EditorUtility.DisplayProgressBar("Generating Levels", $"Generating Level {lvlIndex}...", (float)i / _procBatchCount);

                var gen = new ProceduralLevelGenerator(new RuntimePathSolver(), seed + i);
                var param = new DifficultyParams(_procGridWidth, _procGridHeight, _procColorCount, 0, _procRequireCoverage);
                var lvl = gen.Generate(param);
                if (lvl != null)
                {
                    lvl.levelIndex = lvlIndex;
                    lvl.requireFullGridCoverage = _procRequireCoverage;
                    string dirPath = "Assets/Resources/Levels";
                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }

                    string assetPath = $"{dirPath}/LevelData_{lvlIndex}.asset";
                    AssetDatabase.CreateAsset(lvl, assetPath);
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            RefreshLevelBank();
            _procStatusMessage = $"Successfully generated {created} solvable levels starting at index {_procStartIndex}.";
        }

        private void DrawBalanceAnalyzerTab()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Level Difficulty & Balance Distribution", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (_cachedLevels.Count == 0)
            {
                EditorGUILayout.HelpBox("No level assets found in project.", MessageType.Warning);
                GUILayout.EndVertical();
                return;
            }

            _balanceScrollPos = GUILayout.BeginScrollView(_balanceScrollPos);

            int easyCount = 0, mediumCount = 0, hardCount = 0, expertCount = 0;
            foreach (var lvl in _cachedLevels)
            {
                int score = (lvl.width * lvl.height * 2) + (lvl.initialNodes.Count * 4) - (lvl.viaductLimit * 3);
                if (score < 25) easyCount++;
                else if (score < 42) mediumCount++;
                else if (score < 62) hardCount++;
                else expertCount++;
            }

            GUILayout.Label($"Easy Levels: {easyCount}", EditorStyles.boldLabel);
            GUILayout.Label($"Medium Levels: {mediumCount}", EditorStyles.boldLabel);
            GUILayout.Label($"Hard Levels: {hardCount}", EditorStyles.boldLabel);
            GUILayout.Label($"Expert/Master Levels: {expertCount}", EditorStyles.boldLabel);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawNexusDiagnosticsTab()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Nexus Framework & Scene Architecture Diagnostics", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (GUILayout.Button("Run Full System Diagnostic", GUILayout.Height(30)))
            {
                RunFullDiagnostics();
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);
            _diagScrollPos = GUILayout.BeginScrollView(_diagScrollPos);

            foreach (var log in _diagnosticLogs)
            {
                MessageType type = log.StartsWith("✔") ? MessageType.Info : MessageType.Warning;
                EditorGUILayout.HelpBox(log, type);
            }

            GUILayout.EndScrollView();
        }

        private void RunFullDiagnostics()
        {
            _diagnosticLogs.Clear();

            var lifecycle = FindAnyObjectByType<GameContextLifecycle>();
            if (lifecycle != null)
            {
                _diagnosticLogs.Add("✔ GameContextLifecycle present in current scene.");
            }
            else
            {
                _diagnosticLogs.Add("⚠ GameContextLifecycle missing in current scene!");
            }

            var bootstrapper = FindAnyObjectByType<GameBootstrapper>();
            if (bootstrapper != null)
            {
                _diagnosticLogs.Add("✔ GameBootstrapper present in current scene.");
            }
            else
            {
                _diagnosticLogs.Add("⚠ GameBootstrapper missing in current scene!");
            }

            _diagnosticLogs.Add($"✔ Total level assets indexed: {_cachedLevels.Count}");
        }

        private string GetTabTitle(StudioTab tab)
        {
            switch (tab)
            {
                case StudioTab.LevelBankAuditor: return "Level Bank Auditor";
                case StudioTab.ProceduralStudio: return "Procedural Studio";
                case StudioTab.BalanceAnalyzer: return "Balance Analyzer";
                case StudioTab.NexusDiagnostics: return "Nexus Diagnostics";
                default: return tab.ToString();
            }
        }

        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter
                };
                if (EditorGUIUtility.isProSkin)
                    _headerStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);
                else
                    _headerStyle.normal.textColor = new Color(0f, 0.3f, 0.6f);
            }

            if (_cardStyle == null)
            {
                _cardStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(4, 4, 4, 4)
                };
            }

            if (_badgeStyle == null)
            {
                _badgeStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }
        }
    }
}
#endif
