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
            window.minSize = new Vector2(780, 680);
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
        private int _procSelectedDifficulty = 0;

        // ─── Seviye Listesi ───
        private List<LevelData> _cachedLevels = new List<LevelData>();
        private Vector2 _scrollPos;
        private Vector2 _sidebarScrollPos;

        // ─── Stiller ───
        private GUIStyle _headerStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _okBadgeStyle;
        private GUIStyle _warnBadgeStyle;
        private GUIStyle _errorBadgeStyle;
        private GUIStyle _titleBannerStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _miniInfoStyle;
        private GUIStyle _sidebarHeaderStyle;
        private GUIStyle _sidebarBtnStyle;
        private GUIStyle _sidebarActiveBtnStyle;

        // ─── Sekme Seçimi ───
        private int _selectedTab = 0;

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

        // ─── Interactive Grid Painter State ───
        internal int _painterSelectedLevelIdx = 0;
        internal ColorType _painterSelectedColor = ColorType.Red;
        internal bool _painterIsEraser = false;

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
                Debug.Log("[PixelFlow] GameConfig.asset zaten mevcut: Assets/Resources/Configs/GameConfig.asset");
                return;
            }
            var config = ScriptableObject.CreateInstance<PixelFlow.Data.GameConfig>();
            string path = "Assets/Resources/Configs/GameConfig.asset";
            System.IO.Directory.CreateDirectory("Assets/Resources/Configs");
            UnityEditor.AssetDatabase.CreateAsset(config, path);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"[PixelFlow] GameConfig.asset oluşturuldu: {path}");
        }

        // ─── Repaint Optimizasyonu ───
        private float _lastDiagnosticTime = -10f;
        private const float DiagnosticCooldown = 0.5f;
        private bool _wasPlaying;

        private void OnEnable() { RefreshData(); _wasPlaying = Application.isPlaying; _cachedBaseSpeed = -1f; _cachedBaseSpawnInterval = -1f; }
        private void OnFocus() { RefreshData(); }

        private void OnInspectorUpdate()
        {
            bool isPlaying = Application.isPlaying;
            if (isPlaying != _wasPlaying)
            {
                _wasPlaying = isPlaying;
                Repaint();
            }
            else if (isPlaying && Time.frameCount % 15 == 0)
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
            _contextDataOk = root?.Context != null;

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
        // ANA GUI - MODERN PRO-SKIN KATEGORİZE SIDEBAR DÜZENİ
        // ═══════════════════════════════════════════════════

        private void OnGUI()
        {
            InitStyles();

            // Top Header Banner
            GUILayout.BeginVertical(_titleBannerStyle ?? EditorStyles.helpBox);
            GUILayout.Label("COLOR JAM 3D — KONTROL MERKEZİ", _headerStyle ?? EditorStyles.boldLabel);
            GUILayout.Label("Master Studio Kontrol Paneli • Clean Architecture & No-Code Designer Suite", _miniInfoStyle ?? EditorStyles.miniLabel);
            GUILayout.EndVertical();

            GUILayout.Space(6);

            // Split View: Left Sidebar + Right Content Panel
            GUILayout.BeginHorizontal();

            // ─── LEFT SIDEBAR (Categorized Navigation) ───
            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(200), GUILayout.ExpandHeight(true));
            _sidebarScrollPos = GUILayout.BeginScrollView(_sidebarScrollPos, GUILayout.Width(195));

            DrawSidebarCategory("🎮 OYUN & İÇERİK");
            DrawSidebarButton(0, "🕹️ Oyun Kontrol");
            DrawSidebarButton(2, "🎮 Seviye Stüdyosu");
            DrawSidebarButton(8, "🎨 Garaj Stüdyosu");

            GUILayout.Space(10);
            DrawSidebarCategory("📦 DATA & EKONOMİ");
            DrawSidebarButton(4, "📦 Data Yöneticisi");
            DrawSidebarButton(5, "💰 Ekonomi & Isı Haritası");
            DrawSidebarButton(9, "📺 Reklam Ayarları");

            GUILayout.Space(10);
            DrawSidebarCategory("🔬 MÜHENDİSLİK & TEST");
            DrawSidebarButton(3, "🧩 Toplu Çözücü");
            DrawSidebarButton(1, "🔍 Sahne Tanılama");
            DrawSidebarButton(6, "🔬 Nexus İzleyici");
            DrawSidebarButton(7, "⚡ Performans");
            DrawSidebarButton(10, "🛡️ Pre-Build Validator");

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(6);

            // ─── RIGHT CONTENT PANEL ───
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true));

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
                case 8: DrawGarageSkinStudioTab(); break;
                case 9: DrawAdMonetizationTab(); break;
                case 10: DrawPreBuildValidatorTab(); break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawSidebarCategory(string categoryTitle)
        {
            if (_sidebarHeaderStyle == null) InitStyles();
            GUILayout.Label(categoryTitle, _sidebarHeaderStyle ?? EditorStyles.boldLabel);
            GUILayout.Space(2);
        }

        private void DrawSidebarButton(int tabIndex, string buttonTitle)
        {
            if (_sidebarBtnStyle == null || _sidebarActiveBtnStyle == null) InitStyles();
            bool isActive = _selectedTab == tabIndex;
            GUIStyle style = isActive ? _sidebarActiveBtnStyle : _sidebarBtnStyle;

            if (GUILayout.Button(buttonTitle, style ?? EditorStyles.miniButton, GUILayout.Height(28), GUILayout.ExpandWidth(true)))
            {
                _selectedTab = tabIndex;
                GUI.FocusControl(null);
            }
            GUILayout.Space(2);
        }

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
            }
        }

        private TModel GetModel<TModel>() where TModel : class
        {
            var root = Object.FindAnyObjectByType<Root>(FindObjectsInactive.Include);
            return root?.Context?.Container?.Resolve<TModel>();
        }

        private IPlayerPrefsService GetPrefsService()
        {
            var root = Object.FindAnyObjectByType<Root>(FindObjectsInactive.Include);
            return root?.Context?.Container?.Resolve<IPlayerPrefsService>();
        }

        private void DispatchSignal<TSignal>(TSignal signal) where TSignal : struct
        {
            var bus = Object.FindAnyObjectByType<Root>(FindObjectsInactive.Include)?.Context?.Container?.Resolve<ISignalBus>();
            if (bus != null)
                bus.Fire(signal);
            else
                Debug.LogWarning($"[PixelFlow] SignalBus bulunamadı: {typeof(TSignal).Name}");
        }

        private void InitStyles()
        {
            if (_headerStyle != null && _sidebarHeaderStyle != null && _sidebarBtnStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
            _headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.4f, 0.75f, 1f) : new Color(0.05f, 0.25f, 0.5f);

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            _sectionHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.65f, 0.85f, 1f) : new Color(0.1f, 0.35f, 0.6f);

            _cardStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(12, 12, 10, 10) };
            _okBadgeStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.12f, 0.65f, 0.22f) }, fontStyle = FontStyle.Bold };
            _warnBadgeStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.9f, 0.6f, 0.1f) }, fontStyle = FontStyle.Bold };
            _errorBadgeStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.85f, 0.2f, 0.18f) }, fontStyle = FontStyle.Bold };
            _titleBannerStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(12, 12, 10, 10) };
            _miniInfoStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };

            _sidebarHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(4, 0, 6, 2)
            };
            _sidebarHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.6f, 0.7f, 0.8f) : new Color(0.3f, 0.4f, 0.5f);

            _sidebarBtnStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 6, 4, 4),
                fontSize = 11
            };

            _sidebarActiveBtnStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 6, 4, 4),
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            _sidebarActiveBtnStyle.normal.textColor = Color.white;
            if (_activeBtnTex == null) _activeBtnTex = MakeColorTexture(new Color(0.22f, 0.45f, 0.88f));
            _sidebarActiveBtnStyle.normal.background = _activeBtnTex;
        }

        private static Texture2D _activeBtnTex;
        private static Texture2D MakeColorTexture(Color col)
        {
            var pix = new Color[4];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var result = new Texture2D(2, 2);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        // ─── HELPER UI METHODS FOR PARTIAL CLASSES ───

        private void DrawInfoRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(180));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLevelTableHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("İndeks", GUILayout.Width(50));
            EditorGUILayout.LabelField("İsim", GUILayout.Width(120));
            EditorGUILayout.LabelField("Boyut", GUILayout.Width(60));
            EditorGUILayout.LabelField("Düğüm", GUILayout.Width(50));
            EditorGUILayout.LabelField("Çözülebilir", GUILayout.Width(80));
            EditorGUILayout.LabelField("İşlemler", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLevelTableRow(LevelData level, bool isSolvable)
        {
            if (level == null) return;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(level.levelIndex.ToString(), GUILayout.Width(50));
            EditorGUILayout.LabelField(level.name, GUILayout.Width(120));
            EditorGUILayout.LabelField($"{level.width}x{level.height}", GUILayout.Width(60));
            EditorGUILayout.LabelField((level.initialNodes?.Count ?? 0).ToString(), GUILayout.Width(50));
            EditorGUILayout.LabelField(isSolvable ? "✔ OK" : "❌ UYARI", isSolvable ? _okBadgeStyle : _errorBadgeStyle, GUILayout.Width(80));
            if (GUILayout.Button("Oyna", GUILayout.Width(50))) PlayLevel(level);
            if (GUILayout.Button("Seç", GUILayout.Width(50))) Selection.activeObject = level;
            EditorGUILayout.EndHorizontal();
        }

        private void RunBatchSolver()
        {
            _solvabilityCache.Clear();
            int solved = 0;
            var solver = new PixelFlow.Services.RuntimePathSolver();
            foreach (var lvl in _cachedLevels)
            {
                if (lvl == null) continue;
                bool ok = solver.Solve(lvl, out _);
                _solvabilityCache[lvl] = ok;
                if (ok) solved++;
            }
            _batchSolveStatusMessage = $"Çözüm tamamlandı: {_cachedLevels.Count} seviyeden {solved} tanesi çözülebilir.";
            Debug.Log($"[PixelFlow] {_batchSolveStatusMessage}");
        }

        private void AutoFixMissingSolutions()
        {
            int fixedCount = 0;
            var solver = new PixelFlow.Services.RuntimePathSolver();
            foreach (var lvl in _cachedLevels)
            {
                if (lvl == null) continue;
                if (solver.Solve(lvl, out _))
                {
                    fixedCount++;
                    EditorUtility.SetDirty(lvl);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[PixelFlow] Toplu çözüm kaydı tamamlandı: {fixedCount} seviye güncellendi.");
        }

        private int CalculateComplexityScore(LevelData level)
        {
            if (level == null) return 0;
            int nodeCount = level.initialNodes?.Count ?? 0;
            return (level.width * level.height) + (nodeCount * 5);
        }

        private string GetDifficultyTierName(int score)
        {
            if (score < 30) return "Kolay (Faz 1)";
            if (score < 60) return "Orta (Faz 2)";
            if (score < 90) return "Zor (Faz 3)";
            return "Uzman (Faz 4)";
        }

        private Color GetDifficultyTierColor(int score)
        {
            if (score < 30) return new Color(0.2f, 0.8f, 0.3f);
            if (score < 60) return new Color(0.9f, 0.7f, 0.1f);
            if (score < 90) return new Color(0.9f, 0.4f, 0.1f);
            return new Color(0.9f, 0.2f, 0.2f);
        }

        private void DrawNexusResolveStatus<T>(NexusDI container, string serviceName) where T : class
        {
            bool isResolved = container != null && container.Resolve<T>() != null;
            DrawNexusResolveStatus(serviceName, isResolved);
        }

        private void DrawNexusResolveStatus(string serviceName, bool isResolved)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(serviceName, GUILayout.Width(200));
            EditorGUILayout.LabelField(isResolved ? "✔ Registered / Active" : "❌ Not Found", isResolved ? _okBadgeStyle : _errorBadgeStyle);
            EditorGUILayout.EndHorizontal();
        }

        private LevelData ResolveLevelByIndex(int index)
        {
            return _cachedLevels.FirstOrDefault(l => l.levelIndex == index);
        }

        private void CompleteCurrentLevel()
        {
            DispatchSignal(new LevelCompletedSignal());
            Debug.Log("[PixelFlow] CompleteCurrentLevel signal fired.");
        }

        private void RestartCurrentLevel()
        {
            var levelModel = GetModel<ILevelModel>();
            if (levelModel?.CurrentLevel != null)
            {
                PlayLevel(levelModel.CurrentLevel);
            }
        }

        private void UnlockAllLevels()
        {
            PlayerPrefs.SetInt("UnlockedLevels", _cachedLevels.Count);
            PlayerPrefs.Save();
            Debug.Log("[PixelFlow] Tüm seviyelerin kilitleri açıldı.");
        }

        private void ResetProgress()
        {
            PlayerPrefs.SetInt("UnlockedLevels", 1);
            PlayerPrefs.Save();
            Debug.Log("[PixelFlow] İlerleme sıfırlandı.");
        }

        private void ForceSaveGame()
        {
            PlayerPrefs.Save();
            Debug.Log("[PixelFlow] Oyun kaydedildi.");
        }

        private void WipeSaveData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[PixelFlow] Kayıtlı veriler tamamen temizlendi.");
        }

        private TService GetService<TService>() where TService : class
        {
            var root = Object.FindAnyObjectByType<Root>(FindObjectsInactive.Include);
            return root?.Context?.Container?.Resolve<TService>();
        }

        private void DrawSignalButton(string buttonText, System.Action onClick)
        {
            if (GUILayout.Button(buttonText, GUILayout.Height(24)))
            {
                onClick?.Invoke();
            }
        }

        private void DrawSignalButton<TSignal>(string buttonText, TSignal signal) where TSignal : struct
        {
            if (GUILayout.Button(buttonText, GUILayout.Height(24)))
            {
                DispatchSignal(signal);
            }
        }

        private void DrawDiagnosticRow(string name, bool isOk, System.Action fixAction)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(name, GUILayout.Width(200));
            EditorGUILayout.LabelField(isOk ? "✔ Tamam" : "❌ Eksik", isOk ? _okBadgeStyle : _errorBadgeStyle, GUILayout.Width(90));
            if (!isOk && fixAction != null)
            {
                if (GUILayout.Button("Düzenle", GUILayout.Width(70)))
                {
                    fixAction.Invoke();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDiagnosticRow(string name, bool isOk, string okText = "Tamam", string errText = "Eksik")
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(name, GUILayout.Width(200));
            EditorGUILayout.LabelField(isOk ? $"✔ {okText}" : $"❌ {errText}", isOk ? _okBadgeStyle : _errorBadgeStyle);
            EditorGUILayout.EndHorizontal();
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
    }
}
#endif
