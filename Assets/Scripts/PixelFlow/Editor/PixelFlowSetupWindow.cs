#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Button = UnityEngine.UIElements.Button;
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

namespace PixelFlow.Editor
{
    public partial class PixelFlowSetupWindow : EditorWindow
    {
        [MenuItem("Pixel Flow/Kurulum Yardımcısı")]
        public static void ShowWindow()
        {
            var window = GetWindow<PixelFlowSetupWindow>("Pixel Flow Kontrol Merkezi");
            window.minSize = new Vector2(850, 720);
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

        // ─── Seviye Listesi & Çözücü Önbelleği ───
        private List<LevelData> _cachedLevels = new List<LevelData>();
        private Dictionary<LevelData, bool> _solvabilityCache = new Dictionary<LevelData, bool>();
        private string _batchSolveStatusMessage = "";
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
        private int _selectedTab = 1; // Default to Level Studio

        // ─── Painter State ───
        internal int _painterSelectedLevelIdx = 0;
        internal ColorType _painterSelectedColor = ColorType.Red;
        internal bool _painterIsEraser = false;
        internal ObstacleType _painterSelectedObstacle = ObstacleType.None;

        // ─── Batch Level Duplication ───
        private int _dupSourceIndex = 0;
        private int _dupTargetIndex = 1;
        private int _dupBatchCount = 1;

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
        private bool _frameStepQueued = false;

        // ─── UI Toolkit Containers ───
        private VisualElement _contentContainer;
        private List<Button> _sidebarButtons = new List<Button>();

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

        private void CreateGUI()
        {
            RefreshData();

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/PixelFlow/Editor/PixelFlowSetupWindow.uss");
            if (styleSheet != null) rootVisualElement.styleSheets.Add(styleSheet);

            rootVisualElement.style.backgroundColor = new StyleColor(new Color(0.06f, 0.09f, 0.16f)); // #0F172A
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.paddingTop = 8;
            rootVisualElement.style.paddingBottom = 8;
            rootVisualElement.style.paddingLeft = 8;
            rootVisualElement.style.paddingRight = 8;
            rootVisualElement.style.minWidth = 0;
            rootVisualElement.style.minHeight = 0;

            rootVisualElement.Add(BuildHeader());

            var workspace = new VisualElement();
            workspace.style.flexDirection = FlexDirection.Row;
            workspace.style.flexGrow = 1;
            workspace.style.flexShrink = 1;
            workspace.style.minWidth = 0;
            workspace.style.minHeight = 0;

            workspace.Add(BuildSidebar());

            _contentContainer = new ScrollView();
            _contentContainer.style.flexGrow = 1;
            _contentContainer.style.flexShrink = 1;
            _contentContainer.style.minWidth = 0;
            _contentContainer.style.paddingLeft = 12;
            _contentContainer.style.paddingRight = 12;
            workspace.Add(_contentContainer);

            rootVisualElement.Add(workspace);
            SelectTab(_selectedTab);
        }

        private VisualElement BuildHeader()
        {
            var header = new VisualElement();
            header.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.23f));
            SetStyleBorder(header, new Color(0.2f, 0.25f, 0.33f), 1f);
            header.style.borderTopLeftRadius = 12;
            header.style.borderTopRightRadius = 12;
            header.style.borderBottomLeftRadius = 12;
            header.style.borderBottomRightRadius = 12;
            header.style.paddingTop = 12;
            header.style.paddingBottom = 12;
            header.style.paddingLeft = 16;
            header.style.paddingRight = 16;
            header.style.marginBottom = 10;
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;

            var left = new VisualElement();
            left.style.flexDirection = FlexDirection.Row;
            left.style.alignItems = Align.Center;

            var logo = new Label("🏎️");
            logo.style.fontSize = 24;
            logo.style.marginRight = 10;
            left.Add(logo);

            var titleGroup = new VisualElement();
            var title = new Label("COLOR JAM 3D MASTER STUDIO");
            title.style.fontSize = 15;
            title.style.color = new StyleColor(new Color(0.97f, 0.98f, 0.99f));
            titleGroup.Add(title);

            var subtitle = new Label("Canlı UI Toolkit No-Code Tasarımcı ve Mühendislik Paneli (v6.0.0)");
            subtitle.style.fontSize = 11;
            subtitle.style.color = new StyleColor(new Color(0.58f, 0.64f, 0.72f));
            titleGroup.Add(subtitle);

            left.Add(titleGroup);
            header.Add(left);

            var badge = new Label("● Live Sync");
            badge.style.backgroundColor = new StyleColor(new Color(0.06f, 0.73f, 0.51f, 0.15f));
            badge.style.color = new StyleColor(new Color(0.2f, 0.83f, 0.6f));
            badge.style.paddingTop = 4;
            badge.style.paddingBottom = 4;
            badge.style.paddingLeft = 10;
            badge.style.paddingRight = 10;
            badge.style.borderTopLeftRadius = 100;
            badge.style.borderTopRightRadius = 100;
            badge.style.borderBottomLeftRadius = 100;
            badge.style.borderBottomRightRadius = 100;
            header.Add(badge);

            return header;
        }

        private VisualElement BuildSidebar()
        {
            var sidebar = new VisualElement();
            sidebar.style.width = 220;
            sidebar.style.flexShrink = 0;
            sidebar.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.23f));
            SetStyleBorder(sidebar, new Color(0.2f, 0.25f, 0.33f), 1f);
            sidebar.style.borderTopLeftRadius = 12;
            sidebar.style.borderTopRightRadius = 12;
            sidebar.style.borderBottomLeftRadius = 12;
            sidebar.style.borderBottomRightRadius = 12;
            sidebar.style.paddingTop = 12;
            sidebar.style.paddingBottom = 12;
            sidebar.style.paddingLeft = 8;
            sidebar.style.paddingRight = 8;
            sidebar.style.marginRight = 10;

            _sidebarButtons.Clear();

            AddSidebarSection(sidebar, "OYUN & İÇERİK");
            AddSidebarNavButton(sidebar, 0, "🕹️ Oyun Kontrol");
            AddSidebarNavButton(sidebar, 1, "🎮 Seviye Stüdyosu");
            AddSidebarNavButton(sidebar, 2, "🎨 Garaj Stüdyosu");

            AddSidebarSection(sidebar, "DATA & EKONOMİ");
            AddSidebarNavButton(sidebar, 3, "📦 Data Yöneticisi");
            AddSidebarNavButton(sidebar, 4, "💰 Ekonomi & Isı Haritası");
            AddSidebarNavButton(sidebar, 5, "📺 Reklam Ayarları");

            AddSidebarSection(sidebar, "MÜHENDİSLİK & TEST");
            AddSidebarNavButton(sidebar, 6, "🧩 Toplu Çözücü");
            AddSidebarNavButton(sidebar, 7, "🔍 Sahne Tanılama");
            AddSidebarNavButton(sidebar, 8, "🔬 Nexus İzleyici");
            AddSidebarNavButton(sidebar, 9, "⚡ Performans");
            AddSidebarNavButton(sidebar, 10, "🛡️ Pre-Build Validator");

            return sidebar;
        }

        private void AddSidebarSection(VisualElement sidebar, string title)
        {
            var label = new Label(title);
            label.style.fontSize = 10;
            label.style.color = new StyleColor(new Color(0.39f, 0.45f, 0.55f));
            label.style.marginTop = 10;
            label.style.marginBottom = 4;
            label.style.paddingLeft = 6;
            sidebar.Add(label);
        }

        private void AddSidebarNavButton(VisualElement sidebar, int tabIdx, string text)
        {
            var btn = new Button(() => SelectTab(tabIdx)) { text = text };
            btn.style.backgroundColor = new StyleColor(Color.clear);
            btn.style.color = new StyleColor(new Color(0.58f, 0.64f, 0.72f));
            btn.style.fontSize = 11;
            btn.style.paddingTop = 8;
            btn.style.paddingBottom = 8;
            btn.style.paddingLeft = 10;
            btn.style.paddingRight = 10;
            btn.style.marginBottom = 2;
            SetStyleBorder(btn, Color.clear, 0f);
            btn.style.borderTopLeftRadius = 8;
            btn.style.borderTopRightRadius = 8;
            btn.style.borderBottomLeftRadius = 8;
            btn.style.borderBottomRightRadius = 8;

            _sidebarButtons.Add(btn);
            sidebar.Add(btn);
        }

        private static void SetStyleBorder(VisualElement elem, Color color, float width = 1f)
        {
            elem.style.borderTopColor = new StyleColor(color);
            elem.style.borderBottomColor = new StyleColor(color);
            elem.style.borderLeftColor = new StyleColor(color);
            elem.style.borderRightColor = new StyleColor(color);

            elem.style.borderTopWidth = width;
            elem.style.borderBottomWidth = width;
            elem.style.borderLeftWidth = width;
            elem.style.borderRightWidth = width;
        }

        private void SelectTab(int tabIdx)
        {
            _selectedTab = tabIdx;
            for (int i = 0; i < _sidebarButtons.Count; i++)
            {
                bool isActive = (i == tabIdx);
                _sidebarButtons[i].style.backgroundColor = isActive ? new StyleColor(new Color(0.23f, 0.51f, 0.96f)) : new StyleColor(Color.clear);
                _sidebarButtons[i].style.color = isActive ? new StyleColor(Color.white) : new StyleColor(new Color(0.58f, 0.64f, 0.72f));
            }

            RebuildContentPanel();
        }

        private void RebuildContentPanel()
        {
            if (_contentContainer == null) return;
            _contentContainer.Clear();

            switch (_selectedTab)
            {
                case 0: _contentContainer.Add(BuildGameControllerUIToolkitView()); break;
                case 1: _contentContainer.Add(BuildDiagnosticsUIToolkitView()); break;
                case 2: _contentContainer.Add(BuildLevelStudioUIToolkitView()); break;
                case 3: _contentContainer.Add(BuildBatchSolverUIToolkitView()); break;
                case 4: _contentContainer.Add(BuildDataManagerUIToolkitView()); break;
                case 5: _contentContainer.Add(BuildEconomyUIToolkitView()); break;
                case 6: _contentContainer.Add(BuildNexusUIToolkitView()); break;
                case 7: _contentContainer.Add(BuildPerformanceUIToolkitView()); break;
                case 8: _contentContainer.Add(BuildGarageUIToolkitView()); break;
                case 9: _contentContainer.Add(BuildAdsUIToolkitView()); break;
                case 10: _contentContainer.Add(BuildValidatorUIToolkitView()); break;
            }
        }

        private void SetBorderColor(VisualElement element, Color color)
        {
            element.style.borderTopColor = new StyleColor(color);
            element.style.borderBottomColor = new StyleColor(color);
            element.style.borderLeftColor = new StyleColor(color);
            element.style.borderRightColor = new StyleColor(color);
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

        private static Texture2D _cardBgTex;

        private void InitStyles()
        {
            if (_cardBgTex == null) _cardBgTex = MakeColorTexture(new Color(0.12f, 0.16f, 0.23f));

            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            _headerStyle.normal.textColor = new Color(0.97f, 0.98f, 0.99f);

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            _sectionHeaderStyle.normal.textColor = new Color(0.23f, 0.51f, 0.96f);

            _cardStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(14, 14, 12, 12) };
            _cardStyle.normal.background = _cardBgTex;

            _okBadgeStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.2f, 0.83f, 0.6f) }, fontStyle = FontStyle.Bold };
            _warnBadgeStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.96f, 0.62f, 0.04f) }, fontStyle = FontStyle.Bold };
            _errorBadgeStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.94f, 0.27f, 0.27f) }, fontStyle = FontStyle.Bold };

            _titleBannerStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(14, 14, 12, 12) };
            _titleBannerStyle.normal.background = _cardBgTex;

            _miniInfoStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            _miniInfoStyle.normal.textColor = new Color(0.58f, 0.64f, 0.72f);

            _sidebarHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(6, 0, 8, 4)
            };
            _sidebarHeaderStyle.normal.textColor = new Color(0.39f, 0.45f, 0.55f);

            _sidebarBtnStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 6, 6, 6),
                fontSize = 11
            };

            _sidebarActiveBtnStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 6, 6, 6),
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            _sidebarActiveBtnStyle.normal.textColor = Color.white;
            if (_activeBtnTex == null) _activeBtnTex = MakeColorTexture(new Color(0.23f, 0.51f, 0.96f));
            _sidebarActiveBtnStyle.normal.background = _activeBtnTex;
        }

        private void OnGUI()
        {
            InitStyles();
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
