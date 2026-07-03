#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Nexus.Core;
using PixelFlow.Views;
using PixelFlow.Data;
using PixelFlow.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace PixelFlow.Editor
{
    public class PixelFlowSetupWindow : EditorWindow
    {
        [MenuItem("Pixel Flow/Setup Helper Dashboard")]
        public static void ShowWindow()
        {
            var window = GetWindow<PixelFlowSetupWindow>("Pixel Flow Setup");
            window.minSize = new Vector2(520, 640);
            window.RefreshData();
        }

        // Diagnostics status
        private bool _prefabsOk = false;
        private bool _rootOk = false;
        private bool _contextDataOk = false;
        private bool _gridViewOk = false;
        private bool _canvasOk = false;
        private bool _hudOk = false;
        private bool _eventSystemOk = false;
        private bool _soundOk = false;
        private bool _themeOk = false;
        private bool _bootstrapperOk = false;
        private bool _levelsOk = false;

        // Level Creator Fields
        private int _newLevelIndex = 1;
        private int _newWidth = 5;
        private int _newHeight = 5;

        // Procedural Generation Fields
        private int _procSeed = 0;
        private bool _procUseSeed = false;
        private int _procBatchCount = 5;
        private int _procStartIndex = 1;
        private string _procDifficultyNames = "Easy|Medium|Hard|Expert|Master";
        private int _procSelectedDifficulty = 0;

        // Level List state
        private List<LevelData> _cachedLevels = new List<LevelData>();
        private Vector2 _scrollPos;

        // Styling
        private GUIStyle _headerStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _okBadgeStyle;
        private GUIStyle _warnBadgeStyle;
        private GUIStyle _errorBadgeStyle;
        private GUIStyle _titleBannerStyle;

        private int _selectedTab = 0;
        private readonly string[] _tabNames = {
            "🕹️ Game Controller",
            "🛠️ Diagnostics",
            "🎮 Level Studio",
            "🧩 Batch Solver",
            "💰 Economy & Heatmap"
        };

        private Dictionary<LevelData, bool> _solvabilityCache = new Dictionary<LevelData, bool>();
        private string _batchSolveStatusMessage = "";

        private void OnEnable()
        {
            RefreshData();
        }

        private void OnFocus()
        {
            RefreshData();
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void RefreshData()
        {
            RefreshLevelsCache();
            RunDiagnostics();
        }

        private void RunDiagnostics()
        {
            _prefabsOk = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CellView.prefab") != null;
            
            var root = Object.FindAnyObjectByType<Root>();
            _rootOk = root != null;
            _contextDataOk = _rootOk && root.ContextData != null;

            var grid = Object.FindAnyObjectByType<GridView>();
            if (grid != null)
            {
                SerializedObject so = new SerializedObject(grid);
                var containerProp = so.FindProperty("_gridContainer");
                var prefabProp = so.FindProperty("_cellPrefab");
                _gridViewOk = containerProp != null && containerProp.objectReferenceValue != null &&
                               prefabProp != null && prefabProp.objectReferenceValue != null;
            }
            else
            {
                _gridViewOk = false;
            }

            _canvasOk = Object.FindAnyObjectByType<Canvas>() != null;

            var hud = Object.FindAnyObjectByType<HUDView>();
            if (hud != null)
            {
                SerializedObject so = new SerializedObject(hud);
                _hudOk = so.FindProperty("_hintButton").objectReferenceValue != null &&
                         so.FindProperty("_hintCountText").objectReferenceValue != null &&
                         so.FindProperty("_completionPanel").objectReferenceValue != null;
            }
            else
            {
                _hudOk = false;
            }

            var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            _eventSystemOk = es != null && es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() != null;

            _soundOk = Object.FindAnyObjectByType<SoundHandlerView>() != null;
            _themeOk = Object.FindAnyObjectByType<ThemeHandlerView>() != null;

            var boot = Object.FindAnyObjectByType<GameBootstrapper>();
            _bootstrapperOk = boot != null && boot.initialLevel != null;
            _levelsOk = _cachedLevels.Count > 0 && boot != null && boot.initialLevel != null;
        }

        private void RefreshLevelsCache()
        {
            _cachedLevels.Clear();
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var lvl = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (lvl != null)
                {
                    _cachedLevels.Add(lvl);
                }
            }
            _cachedLevels = _cachedLevels.OrderBy(l => l.levelIndex).ToList();
        }

        private void OnGUI()
        {
            InitStyles();

            // Banner Title Card
            GUILayout.BeginVertical(_titleBannerStyle);
            GUILayout.Label("PIXEL FLOW SETUP & GAME ITERATION DASHBOARD", _headerStyle);
            GUILayout.Label("Live Game Management, AAA+ Scene Setup, Level Studio & Auto-Solver.", EditorStyles.miniLabel);
            GUILayout.EndVertical();

            GUILayout.Space(5);

            // Tab Navigation Bar
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, GUILayout.Height(28));
            GUILayout.Space(8);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            switch (_selectedTab)
            {
                case 0:
                    DrawGameControllerTab();
                    break;
                case 1:
                    DrawDiagnosticsTab();
                    break;
                case 2:
                    DrawLevelStudioTab();
                    break;
                case 3:
                    DrawBatchSolverTab();
                    break;
                case 4:
                    DrawEconomyAnalyticsTab();
                    break;
            }

            GUILayout.EndScrollView();
        }

        // ─────────────────────────────────────────────────────────
        // TAB 0: GAME ITERATION & CANLI YÖNETİM
        // ─────────────────────────────────────────────────────────

        private void DrawGameControllerTab()
        {
            // 1. Live Runtime Status Card
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Live Runtime & Game State Monitor", EditorStyles.boldLabel);
            GUILayout.Space(5);

            bool isPlaying = Application.isPlaying;
            string playStatus = isPlaying ? "PLAYING (Live)" : "EDIT MODE (Stopped)";
            Color statusColor = isPlaying ? new Color(0.2f, 0.8f, 0.3f) : new Color(0.9f, 0.6f, 0.1f);

            GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = statusColor } };
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Engine Mode:", GUILayout.Width(110));
            GUILayout.Label(playStatus, statusStyle);
            GUILayout.EndHorizontal();

            var stateModel = GetModel<IGameStateModel>();
            var levelModel = GetModel<ILevelModel>();
            var progressModel = GetModel<IProgressModel>();
            var economyModel = GetModel<ICityEconomyModel>();

            string currentState = stateModel != null ? stateModel.CurrentState.ToString() : "Not Initialized";
            string currentLvlInfo = levelModel != null && levelModel.CurrentLevel != null
                ? $"Level {levelModel.CurrentLevel.levelIndex + 1} ({levelModel.CurrentLevel.name})"
                : (Object.FindAnyObjectByType<GameBootstrapper>()?.initialLevel != null
                    ? $"Boot Initial: {Object.FindAnyObjectByType<GameBootstrapper>().initialLevel.name}"
                    : "None");

            int unlockedLvl = progressModel != null ? progressModel.UnlockedLevels : PlayerPrefs.GetInt("NT_UnlockedLevels", 1);
            int coins = economyModel != null ? economyModel.Coins : PlayerPrefs.GetInt("NT_Coins", 0);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Game State:", GUILayout.Width(110));
            GUILayout.Label(currentState, EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Loaded Level:", GUILayout.Width(110));
            GUILayout.Label(currentLvlInfo);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Unlocked Level:", GUILayout.Width(110));
            GUILayout.Label($"Level {unlockedLvl + 1}");
            GUILayout.Label("Coins:", GUILayout.Width(50));
            GUILayout.Label($"{coins:N0} c");
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // 2. Live Game Controller & Triggers Card
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Live Gameplay Controls & Triggers", EditorStyles.boldLabel);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = isPlaying ? new Color(0.2f, 0.7f, 1f) : new Color(0.2f, 0.8f, 0.3f);
            if (GUILayout.Button(isPlaying ? "▶ Reload Unlocked Level" : "▶ Start Play Mode (Level 1)", GUILayout.Height(32)))
            {
                if (!isPlaying)
                {
                    EditorApplication.isPlaying = true;
                }
                else
                {
                    int idx = progressModel != null ? progressModel.UnlockedLevels : 0;
                    var lvl = ResolveLevelByIndex(idx);
                    if (lvl != null) PlayLevel(lvl);
                }
            }
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.3f);
            if (GUILayout.Button("⏭️ Complete Level (Simulate Win)", GUILayout.Height(32)))
            {
                CompleteCurrentLevel();
            }
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("🔄 Restart Level", GUILayout.Height(32)))
            {
                RestartCurrentLevel();
            }
            if (GUILayout.Button("🏠 Return to Hub", GUILayout.Height(32)))
            {
                ReturnToHub();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("💡 Give Free Hint", GUILayout.Height(28)))
            {
                DispatchSignal(new RequestHintSignal());
            }
            if (GUILayout.Button("↩️ Undo", GUILayout.Height(28)))
            {
                DispatchSignal(new UndoSignal());
            }
            if (GUILayout.Button("↪️ Redo", GUILayout.Height(28)))
            {
                DispatchSignal(new RedoSignal());
            }
            if (GUILayout.Button("➕ Add +50,000 Coins", GUILayout.Height(28)))
            {
                AddTestCoins(50000);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔓 Unlock All Levels", GUILayout.Height(28)))
            {
                UnlockAllLevels();
            }
            if (GUILayout.Button("🔒 Reset Progress to Lvl 1", GUILayout.Height(28)))
            {
                ResetProgress();
            }
            if (GUILayout.Button("💾 Force Save State", GUILayout.Height(28)))
            {
                ForceSaveGame();
            }
            if (GUILayout.Button("🗑️ Wipe PlayerPrefs & Save", GUILayout.Height(28)))
            {
                WipeSaveData();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // 3. Direct Level Selector & Launcher Panel
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label($"Quick Level Launcher ({_cachedLevels.Count} Levels)", EditorStyles.boldLabel);
            GUILayout.Label("Click 'Launch' to play any level immediately in Play Mode.", EditorStyles.miniLabel);
            GUILayout.Space(6);

            if (_cachedLevels.Count == 0)
            {
                EditorGUILayout.HelpBox("No LevelData assets found. Click below to generate default levels.", MessageType.Warning);
                if (GUILayout.Button("Create Phase 1+2 Level Pack (12 Levels)", GUILayout.Height(30)))
                {
                    CreatePhase1And2HandCraftedPack();
                    RefreshData();
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Lvl", EditorStyles.boldLabel, GUILayout.Width(35));
                GUILayout.Label("Name", EditorStyles.boldLabel, GUILayout.Width(130));
                GUILayout.Label("Grid", EditorStyles.boldLabel, GUILayout.Width(50));
                GUILayout.Label("Nodes", EditorStyles.boldLabel, GUILayout.Width(45));
                GUILayout.Label("Bridges", EditorStyles.boldLabel, GUILayout.Width(50));
                GUILayout.Label("Direct Launch Action", EditorStyles.boldLabel);
                GUILayout.EndHorizontal();

                for (int i = 0; i < _cachedLevels.Count; i++)
                {
                    var lvl = _cachedLevels[i];
                    if (lvl == null) continue;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label((lvl.levelIndex + 1).ToString(), GUILayout.Width(35));
                    GUILayout.Label(lvl.name, GUILayout.Width(130));
                    GUILayout.Label($"{lvl.width}x{lvl.height}", GUILayout.Width(50));
                    GUILayout.Label(lvl.initialNodes != null ? lvl.initialNodes.Count.ToString() : "0", GUILayout.Width(45));
                    GUILayout.Label(lvl.bridgePositions != null ? lvl.bridgePositions.Count.ToString() : "0", GUILayout.Width(50));

                    GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
                    if (GUILayout.Button($"▶ Launch & Play Level {lvl.levelIndex + 1}", GUILayout.Height(20)))
                    {
                        PlayLevel(lvl);
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // 4. Bootstrapper Target Configuration Card
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Bootstrapper Target Configuration", EditorStyles.boldLabel);
            GUILayout.Space(5);

            var bootstrapper = Object.FindAnyObjectByType<GameBootstrapper>();
            if (bootstrapper != null)
            {
                EditorGUI.BeginChangeCheck();
                var newInitial = (LevelData)EditorGUILayout.ObjectField("Bootstrapper Initial Level", bootstrapper.initialLevel, typeof(LevelData), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(bootstrapper, "Change Initial Level");
                    bootstrapper.initialLevel = newInitial;
                    EditorUtility.SetDirty(bootstrapper);
                }

                if (GUILayout.Button("Assign Level 1 as Bootstrapper Target", GUILayout.Height(24)))
                {
                    var lvl1 = ResolveLevelByIndex(0);
                    if (lvl1 != null)
                    {
                        Undo.RecordObject(bootstrapper, "Assign Level 1");
                        bootstrapper.initialLevel = lvl1;
                        EditorUtility.SetDirty(bootstrapper);
                        Debug.Log($"[PixelFlowSetupWindow] Assigned {lvl1.name} to GameBootstrapper.initialLevel");
                        RefreshData();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("GameBootstrapper component missing in current scene. Go to Diagnostics tab to auto-create.", MessageType.Warning);
            }
            GUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────────────────
        // TAB 1: DIAGNOSTICS & SAHNE KURULUMU
        // ─────────────────────────────────────────────────────────

        private void DrawDiagnosticsTab()
        {
            // 1. Scene setup and Diagnostics Card
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Scene Health & System Diagnostics", EditorStyles.boldLabel);
            GUILayout.Space(5);

            DrawDiagnosticRow("Base Prefabs (CellView)", _prefabsOk, GeneratePrefabs);
            DrawDiagnosticRow("Scene Root Context", _rootOk, SetupScene);
            DrawDiagnosticRow("Context Data Configuration", _contextDataOk, SetupScene);
            DrawDiagnosticRow("GridView Component & Layout", _gridViewOk, SetupScene);
            DrawDiagnosticRow("Canvas UI Wrapper", _canvasOk, SetupScene);
            DrawDiagnosticRow("HUDView Control Panel", _hudOk, SetupScene);
            DrawDiagnosticRow("EventSystem (Input System Model)", _eventSystemOk, SetupScene);
            DrawDiagnosticRow("Audio & Sound System Handler", _soundOk, SetupScene);
            DrawDiagnosticRow("Color Theme Handler", _themeOk, SetupScene);
            DrawDiagnosticRow("Game Lifecycle Bootstrapper", _bootstrapperOk, SetupScene);
            DrawDiagnosticRow("Level Data Registry & Initial Level", _levelsOk, SetupScene);

            GUILayout.Space(12);

            bool allOk = _prefabsOk && _rootOk && _contextDataOk && _gridViewOk && _canvasOk && _hudOk && _eventSystemOk && _soundOk && _themeOk && _bootstrapperOk && _levelsOk;
            if (allOk)
            {
                EditorGUILayout.HelpBox("✔ Everything is configured perfectly. Ready to play!", MessageType.Info);
            }
            else
            {
                GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
                if (GUILayout.Button("One-Click Auto-Setup Game Scene", GUILayout.Height(35)))
                {
                    GeneratePrefabs();
                    SetupScene();
                    RefreshData();
                }
                GUI.backgroundColor = Color.white;
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Quick Developer Tools Card
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Developer Quick Utilities", EditorStyles.boldLabel);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear PlayerPrefs & Save Data", GUILayout.Height(28)))
            {
                WipeSaveData();
            }
            if (GUILayout.Button("Add +50,000 Test Coins", GUILayout.Height(28)))
            {
                AddTestCoins(50000);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────────────────
        // TAB 2: LEVEL STUDIO & CREATOR
        // ─────────────────────────────────────────────────────────

        private void DrawLevelStudioTab()
        {
            // 1. Level Creation panel
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Create Custom Level Asset", EditorStyles.boldLabel);
            GUILayout.Space(5);

            _newLevelIndex = EditorGUILayout.IntField("New Level Index", _newLevelIndex);
            _newWidth = EditorGUILayout.IntSlider("Grid Width", _newWidth, 3, 10);
            _newHeight = EditorGUILayout.IntSlider("Grid Height", _newHeight, 3, 10);

            GUILayout.Space(8);
            if (GUILayout.Button("Generate Empty Level Asset", GUILayout.Height(28)))
            {
                CreateCustomLevel(_newLevelIndex, _newWidth, _newHeight);
                RefreshData();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // 2. Procedural Level Generation Panel
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Procedural Level Generator", EditorStyles.boldLabel);
            GUILayout.Space(5);

            _procSelectedDifficulty = GUILayout.SelectionGrid(
                _procSelectedDifficulty,
                _procDifficultyNames.Split('|'),
                5, GUILayout.Height(22));

            _procUseSeed = EditorGUILayout.Toggle("Use Fixed Seed", _procUseSeed);
            if (_procUseSeed)
                _procSeed = EditorGUILayout.IntField("Seed", _procSeed);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Single", GUILayout.Height(28)))
            {
                GenerateProceduralLevel(_procSelectedDifficulty, _procUseSeed ? _procSeed : (int?)null, _newLevelIndex);
                RefreshData();
            }
            _procStartIndex = EditorGUILayout.IntField("Start Index", _procStartIndex, GUILayout.Width(80));
            _procBatchCount = EditorGUILayout.IntField("Count", _procBatchCount, GUILayout.Width(60));
            if (GUILayout.Button("Generate Batch", GUILayout.Height(28)))
            {
                GenerateProceduralBatch(_procSelectedDifficulty, _procUseSeed ? _procSeed : (int?)null,
                    _procStartIndex, _procBatchCount);
                RefreshData();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // 3. Level Database Manager Panel
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label($"Project Level Registry ({_cachedLevels.Count} Levels)", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (_cachedLevels.Count == 0)
            {
                GUILayout.Label("No LevelData assets found in this project.", EditorStyles.miniLabel);
                GUILayout.Space(5);
                if (GUILayout.Button("Create Initial 3-Level Beginner Pack", GUILayout.Height(25)))
                {
                    CreateThreeLevelPack();
                    RefreshData();
                }
                GUILayout.Space(3);
                if (GUILayout.Button("Create Phase 1+2 Hand-Crafted Pack (12 levels)", GUILayout.Height(25)))
                {
                    CreatePhase1And2HandCraftedPack();
                    RefreshData();
                }
            }
            else
            {
                // Level list table header
                GUILayout.BeginHorizontal();
                GUILayout.Label("Index", EditorStyles.boldLabel, GUILayout.Width(45));
                GUILayout.Label("Name", EditorStyles.boldLabel, GUILayout.Width(130));
                GUILayout.Label("Grid", EditorStyles.boldLabel, GUILayout.Width(50));
                GUILayout.Label("Nodes", EditorStyles.boldLabel, GUILayout.Width(45));
                GUILayout.Label("Bridges", EditorStyles.boldLabel, GUILayout.Width(50));
                GUILayout.Label("Actions", EditorStyles.boldLabel);
                GUILayout.EndHorizontal();

                // List levels
                for (int i = 0; i < _cachedLevels.Count; i++)
                {
                    var lvl = _cachedLevels[i];
                    if (lvl == null) continue;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(lvl.levelIndex.ToString(), GUILayout.Width(45));
                    GUILayout.Label(lvl.name, GUILayout.Width(130));
                    GUILayout.Label($"{lvl.width}x{lvl.height}", GUILayout.Width(50));
                    GUILayout.Label(lvl.initialNodes != null ? lvl.initialNodes.Count.ToString() : "0", GUILayout.Width(45));
                    GUILayout.Label(lvl.bridgePositions != null ? lvl.bridgePositions.Count.ToString() : "0", GUILayout.Width(50));

                    GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
                    if (GUILayout.Button("▶ Play", GUILayout.Height(18), GUILayout.Width(50)))
                    {
                        PlayLevel(lvl);
                    }
                    GUI.backgroundColor = Color.white;

                    if (GUILayout.Button("Select", GUILayout.Height(18), GUILayout.Width(55)))
                    {
                        Selection.activeObject = lvl;
                        EditorGUIUtility.PingObject(lvl);
                    }
                    if (GUILayout.Button("Edit", GUILayout.Height(18), GUILayout.Width(45)))
                    {
                        Selection.activeObject = lvl;
                        EditorGUIUtility.PingObject(lvl);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────────────────
        // TAB 3: BATCH SOLVER & AUDITOR
        // ─────────────────────────────────────────────────────────

        private void DrawBatchSolverTab()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Batch Auto-Solver & Level Integrity Auditor", EditorStyles.boldLabel);
            GUILayout.Label("Validate mathematically that all levels in the project can be solved.", EditorStyles.miniLabel);
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Run Batch Solver on ALL Levels", GUILayout.Height(32)))
            {
                RunBatchSolver();
            }
            if (GUILayout.Button("Auto-Fix & Generate Missing Solutions", GUILayout.Height(32)))
            {
                AutoFixMissingSolutions();
            }
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_batchSolveStatusMessage))
            {
                GUILayout.Space(6);
                EditorGUILayout.HelpBox(_batchSolveStatusMessage, MessageType.Info);
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Solver Results Table
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label($"Solvability Status Audit ({_cachedLevels.Count} Levels)", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (_cachedLevels.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Level", EditorStyles.boldLabel, GUILayout.Width(60));
                GUILayout.Label("Grid Size", EditorStyles.boldLabel, GUILayout.Width(60));
                GUILayout.Label("Solvability Status", EditorStyles.boldLabel, GUILayout.Width(160));
                GUILayout.Label("Solution Count", EditorStyles.boldLabel, GUILayout.Width(100));
                GUILayout.Label("Action", EditorStyles.boldLabel);
                GUILayout.EndHorizontal();

                var solver = new RuntimePathSolver();

                foreach (var lvl in _cachedLevels)
                {
                    if (lvl == null) continue;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Lvl {lvl.levelIndex}", GUILayout.Width(60));
                    GUILayout.Label($"{lvl.width}x{lvl.height}", GUILayout.Width(60));

                    if (!_solvabilityCache.TryGetValue(lvl, out bool isSolvable))
                    {
                        isSolvable = solver.Solve(lvl, out _);
                        _solvabilityCache[lvl] = isSolvable;
                    }

                    if (isSolvable)
                    {
                        GUILayout.Label("✔ SOLVABLE", _okBadgeStyle, GUILayout.Width(160));
                    }
                    else
                    {
                        GUILayout.Label("✖ UNSOLVABLE!", _errorBadgeStyle, GUILayout.Width(160));
                    }

                    int solutionCount = lvl.solutions != null ? lvl.solutions.Count : 0;
                    string solLabel = solutionCount > 0 ? $"{solutionCount} colors solved" : "No saved solution";
                    GUILayout.Label(solLabel, GUILayout.Width(100));

                    if (GUILayout.Button("Inspect", GUILayout.Height(18), GUILayout.Width(60)))
                    {
                        Selection.activeObject = lvl;
                        EditorGUIUtility.PingObject(lvl);
                    }

                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────────────────
        // TAB 4: ECONOMY ANALYTICS & HEATMAP
        // ─────────────────────────────────────────────────────────

        private void DrawEconomyAnalyticsTab()
        {
            // Level Difficulty Heatmap & Score
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Level Complexity & Difficulty Heatmap", EditorStyles.boldLabel);
            GUILayout.Label("Calculated score based on grid area, node count, and bridge density.", EditorStyles.miniLabel);
            GUILayout.Space(8);

            if (_cachedLevels.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Level", EditorStyles.boldLabel, GUILayout.Width(60));
                GUILayout.Label("Grid Area", EditorStyles.boldLabel, GUILayout.Width(70));
                GUILayout.Label("Complexity Score", EditorStyles.boldLabel, GUILayout.Width(110));
                GUILayout.Label("Difficulty Tier", EditorStyles.boldLabel, GUILayout.Width(110));
                GUILayout.Label("Coverage Rule", EditorStyles.boldLabel);
                GUILayout.EndHorizontal();

                foreach (var lvl in _cachedLevels)
                {
                    if (lvl == null) continue;
                    int score = CalculateComplexityScore(lvl);
                    string tierName = GetDifficultyTierName(score);
                    Color tierColor = GetDifficultyTierColor(score);

                    GUIStyle tierStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = tierColor } };

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Lvl {lvl.levelIndex}", GUILayout.Width(60));
                    GUILayout.Label($"{lvl.width}x{lvl.height} ({lvl.width * lvl.height})", GUILayout.Width(70));
                    GUILayout.Label($"{score} pts", GUILayout.Width(110));
                    GUILayout.Label(tierName, tierStyle, GUILayout.Width(110));
                    GUILayout.Label(lvl.requireFullGridCoverage ? "Full Grid (100%)" : "Flexible Connect");
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Economy Simulation Breakdown Table
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Idle Economy Balance Sheet (Tier 1-10 Cost Projection)", EditorStyles.boldLabel);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Tier", EditorStyles.boldLabel, GUILayout.Width(40));
            GUILayout.Label("Storage Cap", EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("Rate Cost", EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("Storage Cost", EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("Viaduct Cost", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            float baseRateCost = 250f;
            float baseStorageCost = 150f;
            float baseViaductCost = 500f;
            int[] storageCaps = { 1000, 2500, 5000, 10000, 25000, 50000, 100000, 200000, 500000, 1000000 };

            for (int lvl = 0; lvl < 10; lvl++)
            {
                int rateC = Mathf.RoundToInt(baseRateCost * Mathf.Pow(1.35f, lvl));
                int storageC = Mathf.RoundToInt(baseStorageCost * Mathf.Pow(1.35f, lvl));
                int viaductC = Mathf.RoundToInt(baseViaductCost * Mathf.Pow(1.35f, lvl));

                GUILayout.BeginHorizontal();
                GUILayout.Label($"T{lvl + 1}", GUILayout.Width(40));
                GUILayout.Label($"{storageCaps[lvl]:N0}", GUILayout.Width(90));
                GUILayout.Label($"{rateC:N0} c", GUILayout.Width(90));
                GUILayout.Label($"{storageC:N0} c", GUILayout.Width(90));
                GUILayout.Label($"{viaductC:N0} c");
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────────────────
        // HELPER FUNCTIONS & ACTIONS
        // ─────────────────────────────────────────────────────────

        private void PlayLevel(LevelData level)
        {
            if (level == null) return;

            if (Application.isPlaying)
            {
                DispatchSignal(new LoadLevelSignal { LevelToLoad = level });
                Debug.Log($"[PixelFlowSetupWindow] Dispatched LoadLevelSignal for level {level.levelIndex} ({level.name})");
            }
            else
            {
                var bootstrapper = Object.FindAnyObjectByType<GameBootstrapper>();
                if (bootstrapper == null)
                {
                    SetupScene();
                    bootstrapper = Object.FindAnyObjectByType<GameBootstrapper>();
                }
                if (bootstrapper != null)
                {
                    Undo.RecordObject(bootstrapper, "Set Initial Level");
                    bootstrapper.initialLevel = level;
                    EditorUtility.SetDirty(bootstrapper);
                }
                EditorApplication.isPlaying = true;
                Debug.Log($"[PixelFlowSetupWindow] Assigned {level.name} to GameBootstrapper and started PlayMode.");
            }
        }

        private void CompleteCurrentLevel()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PixelFlowSetupWindow] Win simulation requires PlayMode.");
                return;
            }
            var stateModel = GetModel<IGameStateModel>();
            if (stateModel != null)
            {
                stateModel.SetState(GameState.LevelCompleted);
                DispatchSignal(new LevelCompletedSignal());
                Debug.Log("[PixelFlowSetupWindow] Level completed signal fired.");
            }
        }

        private void RestartCurrentLevel()
        {
            if (!Application.isPlaying) return;
            var levelModel = GetModel<ILevelModel>();
            if (levelModel != null && levelModel.CurrentLevel != null)
            {
                DispatchSignal(new LoadLevelSignal { LevelToLoad = levelModel.CurrentLevel });
            }
        }

        private void ReturnToHub()
        {
            if (Application.isPlaying)
            {
                DispatchSignal(new RequestReturnToHubSignal());
            }
        }

        private void AddTestCoins(int amount)
        {
            var economyModel = GetModel<ICityEconomyModel>();
            if (economyModel != null)
            {
                economyModel.AddCoins(amount);
            }
            else
            {
                int current = PlayerPrefs.GetInt("NT_Coins", 0);
                PlayerPrefs.SetInt("NT_Coins", current + amount);
                PlayerPrefs.Save();
            }
            Debug.Log($"[PixelFlowSetupWindow] Added +{amount} coins.");
        }

        private void UnlockAllLevels()
        {
            int maxCount = Mathf.Max(1, _cachedLevels.Count);
            var progressModel = GetModel<IProgressModel>();
            if (progressModel != null)
            {
                progressModel.UnlockLevel(maxCount);
            }
            PlayerPrefs.SetInt("UnlockedLevels", maxCount);
            PlayerPrefs.SetInt("NT_UnlockedLevels", maxCount);
            PlayerPrefs.Save();
            Debug.Log($"[PixelFlowSetupWindow] Unlocked all {maxCount} levels.");
        }

        private void ResetProgress()
        {
            PlayerPrefs.SetInt("UnlockedLevels", 1);
            PlayerPrefs.SetInt("NT_UnlockedLevels", 1);
            PlayerPrefs.DeleteKey("NT_PuzzleSave_");
            PlayerPrefs.Save();
            Debug.Log("[PixelFlowSetupWindow] Progress reset to Level 1.");
        }

        private void ForceSaveGame()
        {
            var grid = GetModel<IGridModel>();
            var session = GetModel<IGameSessionModel>();
            var level = GetModel<ILevelModel>();
            if (grid != null && session != null && level != null && level.CurrentLevel != null)
            {
                GridStateSerializer.Save(grid, session, level);
                Debug.Log("[PixelFlowSetupWindow] Game state forcibly saved.");
            }
        }

        private void WipeSaveData()
        {
            if (EditorUtility.DisplayDialog("Clear Save & PlayerPrefs", "Are you sure you want to delete all saved progress and player prefs?", "Yes", "No"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("[PixelFlowSetupWindow] PlayerPrefs wiped clean.");
            }
        }

        private void DispatchSignal<TSignal>(TSignal signal) where TSignal : struct
        {
            var root = Object.FindAnyObjectByType<Root>();
            if (root != null && root.IsInitialized && root.Context != null)
            {
                var bus = root.Context.Container.Resolve<ISignalBus>();
                if (bus != null)
                {
                    bus.Fire(signal);
                    return;
                }
            }
            Debug.LogWarning("[PixelFlowSetupWindow] Nexus Root not initialized or signal bus not found.");
        }

        private TModel GetModel<TModel>() where TModel : class
        {
            var root = Object.FindAnyObjectByType<Root>();
            if (root != null && root.IsInitialized && root.Context != null)
            {
                return root.Context.Container.Resolve<TModel>();
            }
            return null;
        }

        private LevelData ResolveLevelByIndex(int index)
        {
            if (_cachedLevels.Count > 0)
            {
                var match = _cachedLevels.FirstOrDefault(l => l != null && l.levelIndex == index);
                if (match != null) return match;
                return _cachedLevels[0];
            }
            return Resources.Load<LevelData>("Levels/Level1");
        }

        private void RunBatchSolver()
        {
            var solver = new RuntimePathSolver();
            int solvableCount = 0;
            _solvabilityCache.Clear();

            foreach (var lvl in _cachedLevels)
            {
                if (lvl == null) continue;
                bool ok = solver.Solve(lvl, out _);
                _solvabilityCache[lvl] = ok;
                if (ok) solvableCount++;
            }

            _batchSolveStatusMessage = $"Batch Solver finished: {solvableCount} / {_cachedLevels.Count} levels are solvable.";
            Debug.Log($"[PixelFlowSetupWindow] {_batchSolveStatusMessage}");
        }

        private void AutoFixMissingSolutions()
        {
            var solver = new RuntimePathSolver();
            int fixedCount = 0;

            foreach (var lvl in _cachedLevels)
            {
                if (lvl == null) continue;
                if (solver.Solve(lvl, out var solutions))
                {
                    Undo.RecordObject(lvl, "Auto-Solve Level");
                    lvl.solutions = solutions.Select(kvp => new PathSolution
                    {
                        color = kvp.Key,
                        pathPositions = new List<Vector2Int>(kvp.Value)
                    }).ToList();
                    EditorUtility.SetDirty(lvl);
                    fixedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            _batchSolveStatusMessage = $"Successfully solved & wrote solutions to {fixedCount} LevelData assets.";
            Debug.Log($"[PixelFlowSetupWindow] {_batchSolveStatusMessage}");
            RunBatchSolver();
        }

        private static int CalculateComplexityScore(LevelData lvl)
        {
            int area = lvl.width * lvl.height;
            int nodes = lvl.initialNodes != null ? lvl.initialNodes.Count : 0;
            int bridges = lvl.bridgePositions != null ? lvl.bridgePositions.Count : 0;
            int obstacles = lvl.obstacles != null ? lvl.obstacles.Count : 0;
            return (area * 2) + (nodes * 8) + (bridges * 6) + (obstacles * 4) - (lvl.viaductLimit * 3);
        }

        private static string GetDifficultyTierName(int score)
        {
            if (score < 25) return "Easy";
            if (score < 42) return "Medium";
            if (score < 62) return "Hard";
            if (score < 85) return "Expert";
            return "Master";
        }

        private static Color GetDifficultyTierColor(int score)
        {
            if (score < 25) return new Color(0.12f, 0.65f, 0.22f); // Green
            if (score < 42) return new Color(0.2f, 0.6f, 1f);     // Blue
            if (score < 62) return new Color(0.9f, 0.6f, 0.1f);    // Orange
            return new Color(0.85f, 0.2f, 0.18f);                  // Red
        }

        private void DrawDiagnosticRow(string name, bool status, System.Action fixAction)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(250));

            if (status)
            {
                GUILayout.Label("[OK]", _okBadgeStyle, GUILayout.Width(70));
            }
            else
            {
                GUILayout.Label("[MISSING]", _errorBadgeStyle, GUILayout.Width(70));
                if (GUILayout.Button("Fix", GUILayout.Height(18), GUILayout.Width(60)))
                {
                    fixAction.Invoke();
                    RefreshData();
                }
            }
            GUILayout.EndHorizontal();
        }

        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter
                };
                if (EditorGUIUtility.isProSkin)
                    _headerStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);
                else
                    _headerStyle.normal.textColor = new Color(0.05f, 0.25f, 0.5f);
            }

            if (_cardStyle == null)
            {
                _cardStyle = new GUIStyle(GUI.skin.box);
                _cardStyle.padding = new RectOffset(10, 10, 10, 10);
                _cardStyle.margin = new RectOffset(6, 6, 6, 6);
            }

            if (_titleBannerStyle == null)
            {
                _titleBannerStyle = new GUIStyle(GUI.skin.box);
                _titleBannerStyle.padding = new RectOffset(8, 8, 8, 8);
                _titleBannerStyle.margin = new RectOffset(6, 6, 4, 4);
                if (EditorGUIUtility.isProSkin)
                    _titleBannerStyle.normal.background = Texture2D.grayTexture;
            }

            if (_okBadgeStyle == null)
            {
                _okBadgeStyle = new GUIStyle(EditorStyles.label);
                _okBadgeStyle.normal.textColor = new Color(0.12f, 0.65f, 0.22f);
                _okBadgeStyle.fontStyle = FontStyle.Bold;
            }

            if (_errorBadgeStyle == null)
            {
                _errorBadgeStyle = new GUIStyle(EditorStyles.label);
                _errorBadgeStyle.normal.textColor = new Color(0.85f, 0.2f, 0.18f);
                _errorBadgeStyle.fontStyle = FontStyle.Bold;
            }
        }

        private void CreateCustomLevel(int index, int w, int h)
        {
            string folder = "Assets/Resources/Levels";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            LevelData asset = ScriptableObject.CreateInstance<LevelData>();
            asset.levelIndex = index;
            asset.width = w;
            asset.height = h;
            asset.viaductLimit = 3;

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/Level{index}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"[PixelFlowSetupWindow] Generated empty level index {index} ({w}x{h}) at {path}");
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void GeneratePrefabs()
        {
            Debug.Log("[PixelFlowSetupWindow] Generating Base Prefabs...");
            if (!Directory.Exists("Assets/Prefabs"))
            {
                Directory.CreateDirectory("Assets/Prefabs");
            }

            string cellPrefabPath = "Assets/Prefabs/CellView.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(cellPrefabPath) == null)
            {
                GameObject cellObj = new GameObject("CellView");
                var cellView = cellObj.AddComponent<CellView>();
                cellObj.AddComponent<BoxCollider2D>(); 
                
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(cellObj.transform);
                var bgRenderer = bgObj.AddComponent<SpriteRenderer>();

                GameObject dotObj = new GameObject("Dot");
                dotObj.transform.SetParent(cellObj.transform);
                var dotRenderer = dotObj.AddComponent<SpriteRenderer>();

                GameObject bridgeObj = new GameObject("Bridge");
                bridgeObj.transform.SetParent(cellObj.transform);
                var bridgeRenderer = bridgeObj.AddComponent<SpriteRenderer>();

                SerializedObject so = new SerializedObject(cellView);
                so.FindProperty("_bgRenderer").objectReferenceValue = bgRenderer;
                so.FindProperty("_dotRenderer").objectReferenceValue = dotRenderer;
                so.FindProperty("_bridgeRenderer").objectReferenceValue = bridgeRenderer;
                so.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(cellObj, cellPrefabPath);
                DestroyImmediate(cellObj);
                Debug.Log("[PixelFlowSetupWindow] CellView prefab created at: " + cellPrefabPath);
            }
        }

        private void SetupScene()
        {
            Debug.Log("[PixelFlowSetupWindow] Starting scene setup...");

            // 0. Ensure levels exist
            RefreshLevelsCache();
            if (_cachedLevels.Count == 0)
            {
                CreatePhase1And2HandCraftedPack();
                RefreshLevelsCache();
            }

            // 1. Context setup
            Root context = Object.FindAnyObjectByType<Root>();
            if (context == null)
            {
                GameObject contextObj = new GameObject("PixelFlow_Context");
                context = contextObj.AddComponent<Root>();
                contextObj.AddComponent<GameContextLifecycle>();
                Undo.RegisterCreatedObjectUndo(contextObj, "Create Context");
            }

            if (context != null && context.ContextData == null)
            {
                string settingsFolder = "Assets/Settings";
                if (!AssetDatabase.IsValidFolder(settingsFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "Settings");
                }

                string assetPath = "Assets/Settings/PixelFlowContextData.asset";
                ContextData contextDataAsset = AssetDatabase.LoadAssetAtPath<ContextData>(assetPath);
                if (contextDataAsset == null)
                {
                    contextDataAsset = ScriptableObject.CreateInstance<ContextData>();
                    AssetDatabase.CreateAsset(contextDataAsset, assetPath);
                    AssetDatabase.SaveAssets();
                }

                SerializedObject serializedContext = new SerializedObject(context);
                SerializedProperty contextDataProp = serializedContext.FindProperty("contextData");
                if (contextDataProp != null)
                {
                    serializedContext.Update();
                    contextDataProp.objectReferenceValue = contextDataAsset;
                    serializedContext.ApplyModifiedProperties();
                    EditorUtility.SetDirty(context);
                }
            }

            // 2. GridView setup
            GridView gridView = Object.FindAnyObjectByType<GridView>();
            GameObject gridObj;
            if (gridView == null)
            {
                gridObj = new GameObject("GridView");
                gridView = gridObj.AddComponent<GridView>();
                Undo.RegisterCreatedObjectUndo(gridObj, "Create GridView");
            }
            else
            {
                gridObj = gridView.gameObject;
            }

            Transform container = gridObj.transform.Find("CellsContainer");
            if (container == null)
            {
                GameObject containerObj = new GameObject("CellsContainer");
                container = containerObj.transform;
                container.SetParent(gridObj.transform);
            }

            CellView cellPrefab = AssetDatabase.LoadAssetAtPath<CellView>("Assets/Prefabs/CellView.prefab");

            SerializedObject gridSo = new SerializedObject(gridView);
            gridSo.FindProperty("_gridContainer").objectReferenceValue = container;
            gridSo.FindProperty("_cellPrefab").objectReferenceValue = cellPrefab;
            gridSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(gridView);

            // 3. UI setup (Canvas & HUDView)
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            GameObject canvasObj;
            if (canvas == null)
            {
                canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }
            else
            {
                canvasObj = canvas.gameObject;
            }

            var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
            }
            else
            {
                var standaloneModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (standaloneModule != null)
                {
                    Undo.DestroyObjectImmediate(standaloneModule);
                    eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }

            HUDView hudView = Object.FindAnyObjectByType<HUDView>();
            GameObject hudObj;
            if (hudView == null)
            {
                hudObj = new GameObject("HUDView", typeof(RectTransform));
                hudObj.transform.SetParent(canvasObj.transform, false);
                hudView = hudObj.AddComponent<HUDView>();
                RectTransform hudRect = hudObj.GetComponent<RectTransform>();
                hudRect.anchorMin = Vector2.zero;
                hudRect.anchorMax = Vector2.one;
                hudRect.sizeDelta = Vector2.zero;
            }
            else
            {
                hudObj = hudView.gameObject;
                hudObj.transform.SetParent(canvasObj.transform, false);
            }

            // Hint Button setup
            Transform hintBtnTransform = hudObj.transform.Find("HintButton");
            GameObject hintBtnObj = hintBtnTransform != null ? hintBtnTransform.gameObject : new GameObject("HintButton", typeof(RectTransform));
            hintBtnObj.transform.SetParent(hudObj.transform, false);

            Image hintImg = hintBtnObj.GetComponent<Image>() ?? hintBtnObj.AddComponent<Image>();
            hintImg.color = new Color(0.15f, 0.15f, 0.18f, 1f);
            Button hintBtn = hintBtnObj.GetComponent<Button>() ?? hintBtnObj.AddComponent<Button>();

            RectTransform hintRect = hintBtnObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 60f);
            hintRect.sizeDelta = new Vector2(160f, 50f);

            // Hint Count Text setup
            Transform hintTextTransform = hintBtnObj.transform.Find("HintCountText");
            GameObject hintTextObj = hintTextTransform != null ? hintTextTransform.gameObject : new GameObject("HintCountText", typeof(RectTransform));
            hintTextObj.transform.SetParent(hintBtnObj.transform, false);

            Text hintText = hintTextObj.GetComponent<Text>() ?? hintTextObj.AddComponent<Text>();
            hintText.text = "HINT (3)";
            hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            hintText.fontSize = 18;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = Color.white;

            RectTransform textRect = hintTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // Completion Panel setup
            Transform compPanelTransform = hudObj.transform.Find("CompletionPanel");
            GameObject completionPanel = compPanelTransform != null ? compPanelTransform.gameObject : new GameObject("CompletionPanel", typeof(RectTransform));
            completionPanel.transform.SetParent(hudObj.transform, false);

            Image panelImg = completionPanel.GetComponent<Image>() ?? completionPanel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);

            RectTransform panelRect = completionPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            completionPanel.SetActive(false);

            // Completion Text setup
            Transform compTextTransform = completionPanel.transform.Find("CompletionText");
            GameObject completionTextObj = compTextTransform != null ? compTextTransform.gameObject : new GameObject("CompletionText", typeof(RectTransform));
            completionTextObj.transform.SetParent(completionPanel.transform, false);

            Text compText = completionTextObj.GetComponent<Text>() ?? completionTextObj.AddComponent<Text>();
            compText.text = "Tebrikler!";
            compText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            compText.fontSize = 32;
            compText.color = new Color(0.2f, 0.85f, 0.3f);
            compText.alignment = TextAnchor.MiddleCenter;

            RectTransform compTextRect = completionTextObj.GetComponent<RectTransform>();
            compTextRect.anchorMin = new Vector2(0f, 0.6f);
            compTextRect.anchorMax = new Vector2(1f, 0.9f);
            compTextRect.sizeDelta = Vector2.zero;

            // Next Level Button setup
            Transform nextLvlBtnTransform = completionPanel.transform.Find("NextLevelButton");
            GameObject nextLvlBtnObj = nextLvlBtnTransform != null ? nextLvlBtnTransform.gameObject : new GameObject("NextLevelButton", typeof(RectTransform));
            nextLvlBtnObj.transform.SetParent(completionPanel.transform, false);

            Image nextLvlImg = nextLvlBtnObj.GetComponent<Image>() ?? nextLvlBtnObj.AddComponent<Image>();
            nextLvlImg.color = new Color(0.15f, 0.6f, 0.25f, 1f);

            Button nextLvlBtn = nextLvlBtnObj.GetComponent<Button>() ?? nextLvlBtnObj.AddComponent<Button>();

            RectTransform nextLvlRect = nextLvlBtnObj.GetComponent<RectTransform>();
            nextLvlRect.anchorMin = new Vector2(0.5f, 0.4f);
            nextLvlRect.anchorMax = new Vector2(0.5f, 0.4f);
            nextLvlRect.pivot = new Vector2(0.5f, 0.5f);
            nextLvlRect.anchoredPosition = new Vector2(0f, 0f);
            nextLvlRect.sizeDelta = new Vector2(180f, 50f);

            Transform nextLvlTextTransform = nextLvlBtnObj.transform.Find("Text");
            GameObject nextLvlTextObj = nextLvlTextTransform != null ? nextLvlTextTransform.gameObject : new GameObject("Text", typeof(RectTransform));
            nextLvlTextObj.transform.SetParent(nextLvlBtnObj.transform, false);

            Text nextLvlText = nextLvlTextObj.GetComponent<Text>() ?? nextLvlTextObj.AddComponent<Text>();
            nextLvlText.text = "SONRAKİ SEVİYE";
            nextLvlText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            nextLvlText.fontSize = 18;
            nextLvlText.alignment = TextAnchor.MiddleCenter;
            nextLvlText.color = Color.white;

            RectTransform nextLvlTextRect = nextLvlTextObj.GetComponent<RectTransform>();
            nextLvlTextRect.anchorMin = Vector2.zero;
            nextLvlTextRect.anchorMax = Vector2.one;
            nextLvlTextRect.sizeDelta = Vector2.zero;

            SerializedObject hudSo = new SerializedObject(hudView);
            hudSo.FindProperty("_hintButton").objectReferenceValue = hintBtn;
            hudSo.FindProperty("_hintCountText").objectReferenceValue = hintText;
            hudSo.FindProperty("_completionPanel").objectReferenceValue = completionPanel;
            hudSo.FindProperty("_completionText").objectReferenceValue = compText;
            hudSo.FindProperty("_nextLevelButton").objectReferenceValue = nextLvlBtn;
            hudSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(hudView);

            // 4. SoundHandlerView setup
            SoundHandlerView soundView = Object.FindAnyObjectByType<SoundHandlerView>();
            if (soundView == null)
            {
                GameObject soundObj = new GameObject("SoundHandlerView");
                soundView = soundObj.AddComponent<SoundHandlerView>();
                Undo.RegisterCreatedObjectUndo(soundObj, "Create SoundHandlerView");
            }

            // 5. ThemeHandlerView setup
            ThemeHandlerView themeView = Object.FindAnyObjectByType<ThemeHandlerView>();
            if (themeView == null)
            {
                GameObject themeObj = new GameObject("ThemeHandlerView");
                themeView = themeObj.AddComponent<ThemeHandlerView>();
                Undo.RegisterCreatedObjectUndo(themeObj, "Create ThemeHandlerView");
            }

            // 6. GameBootstrapper setup
            GameBootstrapper bootstrapper = Object.FindAnyObjectByType<GameBootstrapper>();
            if (bootstrapper == null)
            {
                GameObject bootObj = new GameObject("GameBootstrapper");
                bootstrapper = bootObj.AddComponent<GameBootstrapper>();
                Undo.RegisterCreatedObjectUndo(bootObj, "Create GameBootstrapper");
            }

            // 7. SplashView setup
            SplashView splashView = Object.FindAnyObjectByType<SplashView>();
            if (splashView == null)
            {
                GameObject splashObj = new GameObject("SplashView", typeof(RectTransform));
                splashObj.transform.SetParent(canvasObj.transform, false);
                splashView = splashObj.AddComponent<SplashView>();
                var splashCanvas = splashObj.AddComponent<CanvasGroup>();
                RectTransform splashRect = splashObj.GetComponent<RectTransform>();
                splashRect.anchorMin = Vector2.zero;
                splashRect.anchorMax = Vector2.one;
                splashRect.sizeDelta = Vector2.zero;

                GameObject splashImg = new GameObject("LogoSplash");
                splashImg.transform.SetParent(splashObj.transform, false);
                var img = splashImg.AddComponent<UnityEngine.UI.Image>();
                img.color = new Color(0.12f, 0.08f, 0.22f, 1f);
                var imgRect = img.rectTransform;
                imgRect.anchorMin = new Vector2(0.5f, 0.4f);
                imgRect.anchorMax = new Vector2(0.5f, 0.6f);
                imgRect.sizeDelta = new Vector2(300f, 200f);

                SerializedObject splashSo = new SerializedObject(splashView);
                splashSo.FindProperty("_logoSplash").objectReferenceValue = img;
                splashSo.FindProperty("_canvasGroup").objectReferenceValue = splashCanvas;
                splashSo.ApplyModifiedProperties();
            }

            // 8. CityHubView + HubHUDView setup
            CityHubView cityHub = Object.FindAnyObjectByType<CityHubView>();
            if (cityHub == null)
            {
                GameObject hubObj = new GameObject("CityHubView");
                hubObj.AddComponent<CityHubView>();
                Undo.RegisterCreatedObjectUndo(hubObj, "Create CityHubView");
            }

            HubHUDView hubHUD = Object.FindAnyObjectByType<HubHUDView>();
            if (hubHUD == null)
            {
                GameObject hubHUDObj = new GameObject("HubHUDView", typeof(RectTransform));
                hubHUDObj.transform.SetParent(canvasObj.transform, false);
                hubHUD = hubHUDObj.AddComponent<HubHUDView>();
                RectTransform hubHUDRect = hubHUDObj.GetComponent<RectTransform>();
                hubHUDRect.anchorMin = Vector2.zero;
                hubHUDRect.anchorMax = Vector2.one;
                hubHUDRect.sizeDelta = Vector2.zero;
            }

            // Always target Level 1 (levelIndex == 0) as bootstrapper initial level
            bootstrapper.initialLevel = ResolveLevelByIndex(0);
            
            if (bootstrapper.nexusRoot == null)
            {
                bootstrapper.nexusRoot = context;
            }
            EditorUtility.SetDirty(bootstrapper);

            // 9. Main Camera setup
            if (Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = 5;
                if (Camera.main.GetComponent<PixelFlow.Services.CameraController>() == null)
                    Camera.main.gameObject.AddComponent<PixelFlow.Services.CameraController>();
                EditorUtility.SetDirty(Camera.main);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[PixelFlowSetupWindow] Setup completed successfully.");
        }

        private void CreateThreeLevelPack()
        {
            string folder = "Assets/Resources/Levels";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // Level 1
            LevelData lvl1 = ScriptableObject.CreateInstance<LevelData>();
            lvl1.levelIndex = 0;
            lvl1.width = 5;
            lvl1.height = 5;
            lvl1.initialNodes = new System.Collections.Generic.List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(0, 1), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(0, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(1, 1), color = ColorType.Green },
                new GridNode { position = new Vector2Int(4, 1), color = ColorType.Green },
                new GridNode { position = new Vector2Int(1, 2), color = ColorType.Yellow },
                new GridNode { position = new Vector2Int(4, 2), color = ColorType.Yellow },
                new GridNode { position = new Vector2Int(1, 3), color = ColorType.Orange },
                new GridNode { position = new Vector2Int(4, 3), color = ColorType.Orange },
                new GridNode { position = new Vector2Int(1, 4), color = ColorType.Purple },
                new GridNode { position = new Vector2Int(4, 4), color = ColorType.Purple }
            };
            lvl1.solutions = new System.Collections.Generic.List<PathSolution>
            {
                new PathSolution { color = ColorType.Red, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0), new Vector2Int(4,0) } },
                new PathSolution { color = ColorType.Blue, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(0,3), new Vector2Int(0,4) } },
                new PathSolution { color = ColorType.Green, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(3,1), new Vector2Int(4,1) } },
                new PathSolution { color = ColorType.Yellow, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(3,2), new Vector2Int(4,2) } },
                new PathSolution { color = ColorType.Orange, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(1,3), new Vector2Int(2,3), new Vector2Int(3,3), new Vector2Int(4,3) } },
                new PathSolution { color = ColorType.Purple, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(1,4), new Vector2Int(2,4), new Vector2Int(3,4), new Vector2Int(4,4) } }
            };
            AssetDatabase.CreateAsset(lvl1, $"{folder}/Level1.asset");

            // Level 2
            LevelData lvl2 = ScriptableObject.CreateInstance<LevelData>();
            lvl2.levelIndex = 1;
            lvl2.width = 5;
            lvl2.height = 5;
            lvl2.initialNodes = new System.Collections.Generic.List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(0, 2), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(4, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(1, 2), color = ColorType.Green },
                new GridNode { position = new Vector2Int(4, 1), color = ColorType.Green },
                new GridNode { position = new Vector2Int(3, 2), color = ColorType.Yellow },
                new GridNode { position = new Vector2Int(4, 2), color = ColorType.Yellow }
            };
            lvl2.solutions = new System.Collections.Generic.List<PathSolution>
            {
                new PathSolution { color = ColorType.Red, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0), new Vector2Int(4,0) } },
                new PathSolution { color = ColorType.Blue, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0,2), new Vector2Int(0,3), new Vector2Int(0,4), new Vector2Int(1,4), new Vector2Int(2,4), new Vector2Int(3,4), new Vector2Int(4,4) } },
                new PathSolution { color = ColorType.Green, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(1,2), new Vector2Int(1,3), new Vector2Int(2,3), new Vector2Int(2,2), new Vector2Int(2,1), new Vector2Int(3,1), new Vector2Int(4,1) } },
                new PathSolution { color = ColorType.Yellow, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(3,2), new Vector2Int(3,3), new Vector2Int(4,3), new Vector2Int(4,2) } }
            };
            AssetDatabase.CreateAsset(lvl2, $"{folder}/Level2.asset");

            // Level 3
            LevelData lvl3 = ScriptableObject.CreateInstance<LevelData>();
            lvl3.levelIndex = 2;
            lvl3.width = 5;
            lvl3.height = 5;
            lvl3.bridgePositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(2, 2) };
            lvl3.initialNodes = new System.Collections.Generic.List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 2), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 2), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(2, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Green },
                new GridNode { position = new Vector2Int(0, 1), color = ColorType.Green },
                new GridNode { position = new Vector2Int(3, 0), color = ColorType.Yellow },
                new GridNode { position = new Vector2Int(3, 1), color = ColorType.Yellow },
                new GridNode { position = new Vector2Int(0, 3), color = ColorType.Orange },
                new GridNode { position = new Vector2Int(0, 4), color = ColorType.Orange },
                new GridNode { position = new Vector2Int(3, 3), color = ColorType.Purple },
                new GridNode { position = new Vector2Int(3, 4), color = ColorType.Purple }
            };
            lvl3.solutions = new System.Collections.Generic.List<PathSolution>
            {
                new PathSolution { color = ColorType.Red, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(3,2), new Vector2Int(4,2) } },
                new PathSolution { color = ColorType.Blue, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(2,0), new Vector2Int(2,1), new Vector2Int(2,2), new Vector2Int(2,3), new Vector2Int(2,4) } },
                new PathSolution { color = ColorType.Green, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(0,1) } },
                new PathSolution { color = ColorType.Yellow, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(3,0), new Vector2Int(4,0), new Vector2Int(4,1), new Vector2Int(3,1) } },
                new PathSolution { color = ColorType.Orange, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(0,3), new Vector2Int(1,3), new Vector2Int(1,4), new Vector2Int(0,4) } },
                new PathSolution { color = ColorType.Purple, pathPositions = new System.Collections.Generic.List<Vector2Int> { new Vector2Int(3,3), new Vector2Int(4,3), new Vector2Int(4,4), new Vector2Int(3,4) } }
            };
            AssetDatabase.CreateAsset(lvl3, $"{folder}/Level3.asset");

            // Level Pack
            LevelPack pack = ScriptableObject.CreateInstance<LevelPack>();
            pack.packName = "5x5 Beginner Pack";
            pack.levels = new System.Collections.Generic.List<LevelData> { lvl1, lvl2, lvl3 };
            AssetDatabase.CreateAsset(pack, $"{folder}/MainLevelPack.asset");

            AssetDatabase.SaveAssets();
            Debug.Log("[PixelFlowSetupWindow] Generated Level 1, Level 2, Level 3, and MainLevelPack.asset successfully.");
        }

        private void CreatePhase1And2HandCraftedPack()
        {
            string folder = "Assets/Resources/Levels";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            for (int i = 0; i < _cachedLevels.Count; i++)
            {
                var old = AssetDatabase.GetAssetPath(_cachedLevels[i]);
                if (!string.IsNullOrEmpty(old)) AssetDatabase.DeleteAsset(old);
            }

            var solver = new Services.RuntimePathSolver();
            var generator = new Services.ProceduralLevelGenerator(solver, seed: 12345);

            for (int idx = 0; idx < 12; idx++)
            {
                Services.DifficultyParams param = idx < 5
                    ? new Services.DifficultyParams(5, 5, 1, 0, false)
                    : idx < 9
                        ? new Services.DifficultyParams(5, 5, 2, 0, false)
                        : new Services.DifficultyParams(6, 6, 2, 0, false);

                var level = generator.Generate(param, maxAttempts: 100);
                if (level == null)
                {
                    Debug.LogWarning($"[PixelFlowSetupWindow] Failed to generate level {idx} with param {param.gridWidth}x{param.gridHeight}/{param.colorCount}c. Retrying with different seed...");
                    generator = new Services.ProceduralLevelGenerator(solver, seed: 1000 + idx * 17);
                    level = generator.Generate(param, maxAttempts: 100);
                }
                if (level == null) continue;

                level.levelIndex = idx;
                if (idx >= 11)
                {
                    level.viaductLimit = 3;
                    level.requireFullGridCoverage = false;
                }
                else
                {
                    level.viaductLimit = 0;
                }

                string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/Level{idx + 1}.asset");
                AssetDatabase.CreateAsset(level, path);
            }

            var allLevels = Resources.LoadAll<LevelData>("Levels");
            System.Array.Sort(allLevels, (a, b) => a.levelIndex.CompareTo(b.levelIndex));

            var pack = ScriptableObject.CreateInstance<LevelPack>();
            pack.packName = "Neon Transit Phase 1+2 (12 Levels)";
            pack.levels = new System.Collections.Generic.List<LevelData>(allLevels);
            string packPath = $"{folder}/MainLevelPack.asset";
            AssetDatabase.DeleteAsset(packPath);
            AssetDatabase.CreateAsset(pack, packPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[PixelFlowSetupWindow] Generated {allLevels.Length} hand-crafted-style levels (Phase 1+2).");
        }

        private void GenerateProceduralLevel(int difficultyIndex, int? seed, int levelIndex)
        {
            var param = GetDifficultyParam(difficultyIndex);
            var solver = new RuntimePathSolver();
            var generator = seed.HasValue
                ? new ProceduralLevelGenerator(solver, seed.Value)
                : new ProceduralLevelGenerator(solver);

            var level = generator.Generate(param);
            if (level == null)
            {
                Debug.LogError($"[PixelFlowSetupWindow] Failed to generate level at difficulty {difficultyIndex}.");
                return;
            }

            level.levelIndex = levelIndex;
            level.viaductLimit = param.bridgeCount;

            string folder = "Assets/Resources/Levels";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/ProcLevel_{levelIndex}.asset");
            AssetDatabase.CreateAsset(level, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"[PixelFlowSetupWindow] Generated procedural level {levelIndex} ({param.gridWidth}x{param.gridHeight}, {param.colorCount} colors, {param.bridgeCount} bridges) at {path}");
            Selection.activeObject = level;
            EditorGUIUtility.PingObject(level);
        }

        private void GenerateProceduralBatch(int difficultyIndex, int? seed, int startIndex, int count)
        {
            var param = GetDifficultyParam(difficultyIndex);
            var solver = new RuntimePathSolver();
            var generator = seed.HasValue
                ? new ProceduralLevelGenerator(solver, seed.Value)
                : new ProceduralLevelGenerator(solver);

            string folder = "Assets/Resources/Levels";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            int successCount = 0;
            for (int i = 0; i < count; i++)
            {
                int idx = startIndex + i;
                var level = generator.Generate(param);
                if (level == null) continue;

                level.levelIndex = idx;
                level.viaductLimit = param.bridgeCount;

                string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/ProcLevel_{idx}.asset");
                AssetDatabase.CreateAsset(level, path);
                successCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PixelFlowSetupWindow] Generated {successCount}/{count} procedural levels (difficulty {difficultyIndex}, start={startIndex}).");
        }

        private static Services.DifficultyParams GetDifficultyParam(int index)
        {
            switch (index)
            {
                case 0: return Services.DifficultyParams.Easy;
                case 1: return Services.DifficultyParams.Medium;
                case 2: return Services.DifficultyParams.Hard;
                case 3: return Services.DifficultyParams.Expert;
                default: return Services.DifficultyParams.Master;
            }
        }
    }
}
#endif
