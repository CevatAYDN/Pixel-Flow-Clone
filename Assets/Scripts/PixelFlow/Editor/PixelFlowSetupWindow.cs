#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Views;
using PixelFlow.Data;
using PixelFlow.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace PixelFlow.Editor
{
    public partial class PixelFlowSetupWindow : EditorWindow
    {
        [MenuItem("Pixel Flow/Kurulum Yardımcısı")]
        public static void ShowWindow()
        {
            var window = GetWindow<PixelFlowSetupWindow>("Pixel Flow Kontrol Merkezi");
            window.minSize = new Vector2(600, 700);
            window.RefreshData();
        }

        // ─── Tanılama Durumları ───
        private bool _prefabsOk, _cellWarningIconOk;
        private bool _rootOk, _contextDataOk;
        private bool _gridViewOk, _canvasOk, _hudOk, _eventSystemOk;
        private bool _soundOk, _themeOk, _bootstrapperOk, _levelsOk;
        private bool _dailyCrisisOk, _confettiOk, _bloomFlashOk;
        private bool _tutorialOk, _settingsViewOk;
        private bool _globalVolumeOk, _cameraControllerOk;

        // ─── Seviye Oluşturucu Alanları ───
        private int _newLevelIndex = 1;
        private int _newWidth = 5;
        private int _newHeight = 5;

        // ─── Prosedürel Üretim Alanları ───
        private int _procSeed = 0;
        private bool _procUseSeed = false;
        private int _procBatchCount = 5;
        private int _procStartIndex = 1;
        private int _procSelectedDifficulty = 0; // 0=Faz1, 1=Faz2, 2=Faz3, 3=Faz4, 4=Özel

        // ─── Seviye Listesi ───
        private List<LevelData> _cachedLevels = new List<LevelData>();
        private Vector2 _scrollPos;

        // ─── Stiller ───
        private GUIStyle _headerStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _okBadgeStyle;
        private GUIStyle _warnBadgeStyle;
        private GUIStyle _errorBadgeStyle;
        private GUIStyle _titleBannerStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _miniInfoStyle;

        // ─── Sekme Seçimi ───
        private int _selectedTab = 0;
        private readonly string[] _tabNames = {
            "🕹️ Oyun Kontrol", "🔍 Sahne Tanılama", "🎮 Seviye Stüdyosu",
            "🧩 Toplu Çözücü", "📦 Data Yöneticisi", "💰 Ekonomi & Isı Haritası",
            "🔬 Nexus İzleyici", "⚡ Performans"
        };

        // ─── Çözücü Önbelleği ───
        private Dictionary<LevelData, bool> _solvabilityCache = new Dictionary<LevelData, bool>();
        private string _batchSolveStatusMessage = "";

        // ─── Sinyal Paneli Durumu ───
        private bool _signalPanelOpen = false;

        // ─── Performans İzleyici ───
        private readonly List<string> _signalLog = new List<string>();
        private const int MaxSignalLogEntries = 20;

        // ─── VehicleSimulator Runtime Kontrolleri ───
        private float _vehicleSpeedMultiplier = 1.0f;
        private bool _vehicleSpawnEnabled = true;
        private float _cachedBaseSpeed = -1f;
        private float _cachedBaseSpawnInterval = -1f;

        // ─── Frame Step ───
        private bool _frameStepQueued = false;

        // ─── Batch Level Duplication ───
        private int _dupSourceIndex = 0;
        private int _dupTargetIndex = 1;
        private int _dupBatchCount = 1;

        [MenuItem("PixelFlow/Create GameConfig Asset")]
        private static void CreateGameConfigAsset()
        {
            var existing = UnityEngine.Resources.Load<PixelFlow.Data.GameConfig>("Configs/GameConfig");
            if (existing != null)
            {
                Debug.Log("[PixelFlow] GameConfig.asset already exists at Resources/Configs/GameConfig.asset");
                return;
            }
            var config = ScriptableObject.CreateInstance<PixelFlow.Data.GameConfig>();
            string path = "Assets/Resources/Configs/GameConfig.asset";
            System.IO.Directory.CreateDirectory("Assets/Resources/Configs");
            UnityEditor.AssetDatabase.CreateAsset(config, path);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"[PixelFlow] GameConfig.asset created at {path}");
        }

        // ─── Repaint Optimizasyonu ───
        private float _lastDiagnosticTime = -10f;
        private const float DiagnosticCooldown = 0.5f;
        private bool _wasPlaying;

        private void OnEnable() { RefreshData(); _wasPlaying = Application.isPlaying; _cachedBaseSpeed = -1f; _cachedBaseSpawnInterval = -1f; }
        private void OnFocus() { _dataCacheDirty = true; RefreshData(); }

        private void OnInspectorUpdate()
        {
            bool isPlaying = Application.isPlaying;
            // Sadece Play/Edit geçişinde veya oyun çalışırken saniyede 4 kez repaint
            if (isPlaying != _wasPlaying)
            {
                _wasPlaying = isPlaying;
                Repaint();
            }
            else if (isPlaying && Time.frameCount % 15 == 0) // ~saniyede 4 kez
            {
                Repaint();
            }
        }

        private void RefreshData()
        {
            RefreshLevelsCache();
            float now = (float)EditorApplication.timeSinceStartup;
            if (now - _lastDiagnosticTime >= DiagnosticCooldown)
            {
                RunDiagnostics();
                _lastDiagnosticTime = now;
            }
        }

        // ═══════════════════════════════════════════════════
        // TANILAMA SİSTEMİ
        // ═══════════════════════════════════════════════════

        private void RunDiagnostics()
        {
            var cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CellView.prefab");
            _prefabsOk = cellPrefab != null;
            _cellWarningIconOk = false;
            if (_prefabsOk && cellPrefab.GetComponent<CellView>() != null)
            {
                var cellSo = new SerializedObject(cellPrefab.GetComponent<CellView>());
                _cellWarningIconOk = cellSo.FindProperty("_warningRenderer")?.objectReferenceValue != null;
            }

            var root = Object.FindAnyObjectByType<Root>(FindObjectsInactive.Include);
            _rootOk = root != null;
            _contextDataOk = root?.ContextData != null;

            var grid = Object.FindAnyObjectByType<GridView>(FindObjectsInactive.Include);
            _gridViewOk = grid != null;
            if (grid != null)
            {
                var so = new SerializedObject(grid);
                _gridViewOk = so.FindProperty("_gridContainer")?.objectReferenceValue != null
                           && so.FindProperty("_cellPrefab")?.objectReferenceValue != null;
            }

            _canvasOk = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include) != null;

            var hud = Object.FindAnyObjectByType<HUDView>(FindObjectsInactive.Include);
            _hudOk = false;
            if (hud != null)
            {
                var so = new SerializedObject(hud);
                _hudOk = so.FindProperty("_hintButton")?.objectReferenceValue != null
                      && so.FindProperty("_completionPanel")?.objectReferenceValue != null;
            }

            var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
            _eventSystemOk = es != null && es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() != null;

            _soundOk = Object.FindAnyObjectByType<SoundHandlerView>(FindObjectsInactive.Include) != null;
            _themeOk = Object.FindAnyObjectByType<ThemeHandlerView>(FindObjectsInactive.Include) != null;

            var boot = Object.FindAnyObjectByType<GameBootstrapper>(FindObjectsInactive.Include);
            _bootstrapperOk = boot != null && boot.initialLevel != null;
            _levelsOk = _cachedLevels.Count > 0 && boot?.initialLevel != null;

            _dailyCrisisOk = Object.FindAnyObjectByType<DailyCrisisView>(FindObjectsInactive.Include) != null;
            _confettiOk = Object.FindAnyObjectByType<ConfettiView>(FindObjectsInactive.Include) != null;
            _bloomFlashOk = Object.FindAnyObjectByType<BloomFlashView>(FindObjectsInactive.Include) != null;
            _tutorialOk = Object.FindAnyObjectByType<TutorialView>(FindObjectsInactive.Include) != null;
            _settingsViewOk = Object.FindAnyObjectByType<SettingsView>(FindObjectsInactive.Include) != null;

            _globalVolumeOk = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include).Any(go =>
                go.name.Contains("Volume") || go.GetComponent("Volume") != null);
            var mainCam = Camera.main ?? Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            _cameraControllerOk = mainCam != null && mainCam.GetComponent<CameraController>() != null;
        }

        private void RefreshLevelsCache()
        {
            _cachedLevels.Clear();
            var guids = AssetDatabase.FindAssets("t:LevelData");
            foreach (var guid in guids)
            {
                var lvl = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                if (lvl != null) _cachedLevels.Add(lvl);
            }
            _cachedLevels = _cachedLevels.OrderBy(l => l.levelIndex).ToList();
        }

        // ═══════════════════════════════════════════════════
        // ANA GUI
        // ═══════════════════════════════════════════════════

        private void OnGUI()
        {
            InitStyles();

            GUILayout.BeginVertical(_titleBannerStyle);
            GUILayout.Label("PIXEL FLOW KONTROL MERKEZİ", _headerStyle);
            GUILayout.Label("Canlı Oyun Yönetimi • Sahne Kurulumu • Seviye Stüdyosu • Nexus İzleyici", _miniInfoStyle);
            GUILayout.EndVertical();

            GUILayout.Space(4);
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, GUILayout.Height(26));
            GUILayout.Space(6);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            switch (_selectedTab)
            {
                case 0: DrawGameControllerTab(); break;
                case 1: DrawDiagnosticsTab(); break;
                case 2: DrawLevelStudioTab(); break;
            case 3: DrawBatchSolverTab(); break;
            case 4: DrawDataManagerTab(); break;
            case 5: DrawEconomyAnalyticsTab(); break;
            case 6: DrawNexusInspectorTab(); break;
            case 7: DrawPerformanceTab(); break;
            }

            GUILayout.EndScrollView();
        }

        // ═══════════════════════════════════════════════════
        // YARDIMCI FONKSİYONLAR & EYLEMLER
        // ═══════════════════════════════════════════════════

        private void PlayLevel(LevelData level)
        {
            if (level == null) return;
            if (Application.isPlaying)
            {
                DispatchSignal(new LoadLevelSignal { LevelToLoad = level });
                Debug.Log($"[PixelFlow] Seviye {level.levelIndex} ({level.name}) için LoadLevelSignal gönderildi.");
            }
            else
            {
                var bootstrapper = Object.FindAnyObjectByType<GameBootstrapper>();
                if (bootstrapper == null) { SetupScene(); bootstrapper = Object.FindAnyObjectByType<GameBootstrapper>(); }
                if (bootstrapper != null) { Undo.RecordObject(bootstrapper, "Başlangıç Seviyesi Ayarla"); bootstrapper.initialLevel = level; EditorUtility.SetDirty(bootstrapper); }
                EditorApplication.isPlaying = true;
                Debug.Log($"[PixelFlow] {level.name} atandı ve Play Mode başlatıldı.");
            }
        }

        private void CompleteCurrentLevel()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[PixelFlow] Kazanma simülasyonu için Play Mode gereklidir."); return; }
            var stateModel = GetModel<IGameStateModel>();
            if (stateModel != null) { stateModel.SetState(GameState.LevelCompleted); DispatchSignal(new LevelCompletedSignal()); Debug.Log("[PixelFlow] Seviye tamamlandı."); }
        }

        private void RestartCurrentLevel()
        {
            if (!Application.isPlaying) return;
            var lm = GetModel<ILevelModel>();
            if (lm?.CurrentLevel != null) DispatchSignal(new LoadLevelSignal { LevelToLoad = lm.CurrentLevel });
        }

        private void UnlockAllLevels()
        {
            int maxCount = Mathf.Max(1, _cachedLevels.Count);
            GetModel<IProgressModel>()?.UnlockLevel(maxCount);
            var prefs = GetPrefsService();
            if (prefs != null)
            {
                prefs.SetInt("UnlockedLevels", maxCount);
                prefs.SetInt("NT_UnlockedLevels", maxCount);
                prefs.Save();
            }
            else
            {
                PlayerPrefs.SetInt("UnlockedLevels", maxCount);
                PlayerPrefs.SetInt("NT_UnlockedLevels", maxCount);
                PlayerPrefs.Save();
            }
            Debug.Log($"[PixelFlow] Tüm {maxCount} seviye açıldı.");
        }

        private void ResetProgress()
        {
            var prefs = GetPrefsService();
            prefs.SetInt("UnlockedLevels", 1); prefs.SetInt("NT_UnlockedLevels", 1);
            prefs.DeleteKey("NT_PuzzleSave_"); prefs.Save();
            Debug.Log("[PixelFlow] İlerleme Seviye 1'e sıfırlandı.");
        }

        private void ForceSaveGame()
        {
            var grid = GetModel<IGridModel>(); var session = GetModel<IGameSessionModel>(); var level = GetModel<ILevelModel>();
            if (grid != null && session != null && level?.CurrentLevel != null)
            {
                GridStateSerializer.Save(grid, session, level, GetPrefsService());
                Debug.Log("[PixelFlow] Oyun durumu zorla kaydedildi.");
            }
        }

        private void WipeSaveData()
        {
            if (!EditorUtility.DisplayDialog("Kayıt & PlayerPrefs Temizle", "Tüm kaydedilmiş ilerlemeyi silmek istediğinizden emin misiniz?", "Evet", "Hayır")) return;

            // Nexus IPlayerPrefsService — null ise fallback'e geç
            var prefs = GetPrefsService();
            if (prefs != null)
            {
                foreach (var key in new[] { "UnlockedLevels", "NT_UnlockedLevels", "NT_PuzzleSave_", "VehicleStyle", "HintCount", "AppTheme", "ColorBlindMode", "MasterVolume", "SfxVolume", "MusicVolume", "HapticsDisabled" })
                    prefs.DeleteKey(key);
                prefs.Save();
            }

            // SecureData klasörünü temizle
            string secureDataFolder = Path.Combine(Application.persistentDataPath, "SecureData");
            if (Directory.Exists(secureDataFolder)) { try { Directory.Delete(secureDataFolder, true); Directory.CreateDirectory(secureDataFolder); } catch { } }

            // Raw PlayerPrefs fallback (her zaman güvenli)
            PlayerPrefs.DeleteAll(); PlayerPrefs.Save();
            Debug.Log("[PixelFlow] Tüm kayıtlı veriler temizlendi.");
        }

        private void DispatchSignal<TSignal>(TSignal signal) where TSignal : struct
        {
            var root = Object.FindAnyObjectByType<Root>();
            if (root?.IsInitialized == true && root.Context != null)
            {
                var bus = root.Context.Container.Resolve<ISignalBus>();
                if (bus != null) { bus.Fire(signal); LogSignal(typeof(TSignal).Name); return; }
            }
            Debug.LogWarning("[PixelFlow] Nexus Root başlatılmamış.");
        }

        private void LogSignal(string signalName)
        {
            _signalLog.Insert(0, $"[{System.DateTime.Now:HH:mm:ss}] {signalName}");
            if (_signalLog.Count > MaxSignalLogEntries) _signalLog.RemoveAt(_signalLog.Count - 1);
        }

        private TModel GetModel<TModel>() where TModel : class
        {
            var root = Object.FindAnyObjectByType<Root>();
            return root?.IsInitialized == true && root.Context != null ? root.Context.Container.Resolve<TModel>() : null;
        }

        private T GetService<T>() where T : class => GetModel<T>();

        private LevelData ResolveLevelByIndex(int index)
        {
            if (_cachedLevels.Count > 0)
            {
                var match = _cachedLevels.FirstOrDefault(l => l != null && l.levelIndex == index);
                return match ?? _cachedLevels[0];
            }
            return Resources.Load<LevelData>("Levels/Level1");
        }

        private IPlayerPrefsService GetPrefsService()
        {
            var root = Object.FindAnyObjectByType<Root>();
            return root?.IsInitialized == true ? root.Context.Container.Resolve<IPlayerPrefsService>() : null;
        }

        // ─── Çözücü ───

        /// <summary>Zorla tanılama yenile — RefreshData cooldown'una takılmaz</summary>
        private void ForceDiagnostics()
        {
            _lastDiagnosticTime = -10f;
            RunDiagnostics();
        }

        private void RunBatchSolver()
        {
            var solver = new RuntimePathSolver();
            int solvableCount = 0;
            _solvabilityCache.Clear();
            foreach (var lvl in _cachedLevels) { if (lvl == null) continue; bool ok = solver.Solve(lvl, out _); _solvabilityCache[lvl] = ok; if (ok) solvableCount++; }
            _batchSolveStatusMessage = $"Toplu çözücü tamamlandı: {solvableCount} / {_cachedLevels.Count} seviye çözülebilir.";
            Debug.Log($"[PixelFlow] {_batchSolveStatusMessage}");
        }

        private void AutoFixMissingSolutions()
        {
            var solver = new RuntimePathSolver();
            int fixedCount = 0;
            foreach (var lvl in _cachedLevels)
            {
                if (lvl == null || !solver.Solve(lvl, out var solutions)) continue;
                Undo.RecordObject(lvl, "Seviye Otomatik Çözüm");
                lvl.solutions = solutions.Select(kvp => new PathSolution { color = kvp.Key, pathPositions = new List<Vector2Int>(kvp.Value) }).ToList();
                EditorUtility.SetDirty(lvl);
                fixedCount++;
            }
            AssetDatabase.SaveAssets();
            _batchSolveStatusMessage = $"{fixedCount} LevelData varlığına çözüm yazıldı.";
            Debug.Log($"[PixelFlow] {_batchSolveStatusMessage}");
            RunBatchSolver();
        }

        // ─── Zorluk Hesaplayıcılar ───

        private static int CalculateComplexityScore(LevelData lvl)
        {
            int area = lvl.width * lvl.height;
            int nodes = lvl.initialNodes?.Count ?? 0;
            int bridges = lvl.bridgePositions?.Count ?? 0;
            int obstacles = lvl.obstacles?.Count ?? 0;
            return (area * 2) + (nodes * 8) + (bridges * 6) + (obstacles * 4) - (lvl.viaductLimit * 3);
        }

        private static string GetDifficultyTierName(int score)
        {
            if (score < 25) return "Kolay";
            if (score < 42) return "Orta";
            if (score < 62) return "Zor";
            if (score < 85) return "Uzman";
            return "Usta";
        }

        private static Color GetDifficultyTierColor(int score)
        {
            if (score < 25) return new Color(0.12f, 0.65f, 0.22f);
            if (score < 42) return new Color(0.2f, 0.6f, 1f);
            if (score < 62) return new Color(0.9f, 0.6f, 0.1f);
            return new Color(0.85f, 0.2f, 0.18f);
        }

        // ─── GUI Yardımcıları ───

        private void DrawDiagnosticRow(string name, bool status, System.Action fixAction)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(260));
            if (status) GUILayout.Label("[TAMAM]", _okBadgeStyle, GUILayout.Width(70));
            else
            {
                GUILayout.Label("[EKSİK]", _errorBadgeStyle, GUILayout.Width(70));
                if (GUILayout.Button("Düzelt", GUILayout.Height(18), GUILayout.Width(60))) { fixAction?.Invoke(); RefreshData(); }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawInfoRow(string label, string value, GUIStyle valueStyle = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(110));
            GUILayout.Label(value, valueStyle ?? EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
        }

        private void DrawSignalButton(string label, System.Action action)
        {
            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button(label, GUILayout.Height(22))) action?.Invoke();
            GUI.enabled = true;
        }

        private void DrawLevelTableHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Svye", EditorStyles.boldLabel, GUILayout.Width(35));
            GUILayout.Label("Faz", EditorStyles.boldLabel, GUILayout.Width(55));
            GUILayout.Label("İsim", EditorStyles.boldLabel, GUILayout.Width(110));
            GUILayout.Label("Izgara", EditorStyles.boldLabel, GUILayout.Width(45));
            GUILayout.Label("Düğüm", EditorStyles.boldLabel, GUILayout.Width(40));
            GUILayout.Label("Köprü", EditorStyles.boldLabel, GUILayout.Width(40));
            GUILayout.Label("İşlemler", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
        }

        private void DrawLevelTableRow(LevelData lvl, bool showLaunch)
        {
            Color phaseColor = PhaseAssetGenerator.GetPhaseColor(
                PhaseAssetGenerator.GetPhaseForLevel(lvl.levelIndex));
            string phaseShort = PhaseAssetGenerator.GetPhaseForLevel(lvl.levelIndex)
                .ToString().Replace("Phase", "F");

            GUILayout.BeginHorizontal();
            GUILayout.Label((lvl.levelIndex + 1).ToString(), GUILayout.Width(35));
            GUILayout.Label(phaseShort, new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = phaseColor }, fontSize = 10 }, GUILayout.Width(55));
            GUILayout.Label(lvl.name, GUILayout.Width(110));
            GUILayout.Label($"{lvl.width}x{lvl.height}", GUILayout.Width(45));
            GUILayout.Label(lvl.initialNodes?.Count.ToString() ?? "0", GUILayout.Width(40));
            GUILayout.Label(lvl.bridgePositions?.Count.ToString() ?? "0", GUILayout.Width(40));
            if (showLaunch)
            {
                GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
                if (GUILayout.Button("▶ Başlat", GUILayout.Height(20), GUILayout.Width(60))) PlayLevel(lvl);
                GUI.backgroundColor = Color.white;
            }
            if (GUILayout.Button("Seç", GUILayout.Height(18), GUILayout.Width(40))) { Selection.activeObject = lvl; EditorGUIUtility.PingObject(lvl); }
            if (GUILayout.Button("Düzenle", GUILayout.Height(18), GUILayout.Width(55))) { Selection.activeObject = lvl; EditorGUIUtility.PingObject(lvl); }
            GUILayout.EndHorizontal();
        }

        private void DrawNexusResolveStatus<T>(NexusDI container, string label) where T : class
        {
            GUILayout.BeginHorizontal();
            try
            {
                var instance = container.Resolve<T>();
                if (instance != null) { GUILayout.Label("✔", _okBadgeStyle, GUILayout.Width(20)); GUILayout.Label(label, GUILayout.Width(280)); GUILayout.Label(instance.GetType().Name, EditorStyles.miniLabel); }
                else { GUILayout.Label("✖", _errorBadgeStyle, GUILayout.Width(20)); GUILayout.Label(label, GUILayout.Width(280)); GUILayout.Label("null döndü", EditorStyles.miniLabel); }
            }
            catch { GUILayout.Label("⚠", _errorBadgeStyle, GUILayout.Width(20)); GUILayout.Label(label, GUILayout.Width(280)); GUILayout.Label("kayıtlı değil", EditorStyles.miniLabel); }
            GUILayout.EndHorizontal();
        }

        private void SetVehicleStyle(VehicleStyle style)
        {
            var settingsModel = GetModel<ISettingsModel>();
            if (settingsModel != null)
            {
                settingsModel.SetVehicleStyle(style);
            }
            else
            {
                var prefs = GetPrefsService();
                if (prefs != null)
                    prefs.SetInt("VehicleStyle", (int)style);
                else
                    PlayerPrefs.SetInt("VehicleStyle", (int)style);
            }
            Debug.Log($"[PixelFlow] Araç stili değiştirildi: {style}");
        }

        // ─── Stil Başlatma ───

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            _headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.7f, 1f) : new Color(0.05f, 0.25f, 0.5f);

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            _sectionHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.6f, 0.85f, 1f) : new Color(0.1f, 0.35f, 0.6f);

            _cardStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(10, 10, 8, 8) };
            _okBadgeStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.12f, 0.65f, 0.22f) }, fontStyle = FontStyle.Bold };
            _warnBadgeStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.9f, 0.6f, 0.1f) }, fontStyle = FontStyle.Bold };
            _errorBadgeStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.85f, 0.2f, 0.18f) }, fontStyle = FontStyle.Bold };
            _titleBannerStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(12, 12, 8, 8) };
            _miniInfoStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
        }
    }
}
#endif
