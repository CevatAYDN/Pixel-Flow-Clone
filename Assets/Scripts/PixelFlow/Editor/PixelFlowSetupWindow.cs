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
using System.Reflection;
using UnityEngine.Rendering;

namespace PixelFlow.Editor
{
    public class PixelFlowSetupWindow : EditorWindow
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
        // Yeni View tanılama
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
        private string _procDifficultyNames = "Kolay|Orta|Zor|Uzman|Usta";
        private int _procSelectedDifficulty = 0;

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
            "🕹️ Oyun Kontrol",
            "🔍 Sahne Tanılama",
            "🎮 Seviye Stüdyosu",
            "🧩 Toplu Çözücü",
            "💰 Ekonomi & Isı Haritası",
            "🔬 Nexus İzleyici",
            "⚡ Performans"
        };

        // ─── Çözücü Önbelleği ───
        private Dictionary<LevelData, bool> _solvabilityCache = new Dictionary<LevelData, bool>();
        private string _batchSolveStatusMessage = "";

        // ─── Sinyal Paneli Durumu ───
        private bool _signalPanelOpen = false;

        // ─── Performans İzleyici ───
        private readonly List<string> _signalLog = new List<string>();
        private const int MaxSignalLogEntries = 20;

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

        // ═══════════════════════════════════════════════════
        // TANıLAMA SİSTEMİ
        // ═══════════════════════════════════════════════════

        private void RunDiagnostics()
        {
            // Prefab kontrolü
            var cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CellView.prefab");
            _prefabsOk = cellPrefab != null;
            if (_prefabsOk)
            {
                var cellView = cellPrefab.GetComponent<CellView>();
                if (cellView != null)
                {
                    SerializedObject cellSo = new SerializedObject(cellView);
                    var warnProp = cellSo.FindProperty("_warningRenderer");
                    _cellWarningIconOk = warnProp != null && warnProp.objectReferenceValue != null;
                }
                else
                {
                    _cellWarningIconOk = false;
                }
            }
            else
            {
                _cellWarningIconOk = false;
            }

            // Root & Context
            var root = Object.FindAnyObjectByType<Root>(FindObjectsInactive.Include);
            _rootOk = root != null;
            _contextDataOk = _rootOk && root.ContextData != null;

            // GridView
            var grid = Object.FindAnyObjectByType<GridView>(FindObjectsInactive.Include);
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

            // Canvas
            _canvasOk = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include) != null;

            // HUDView
            var hud = Object.FindAnyObjectByType<HUDView>(FindObjectsInactive.Include);
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

            // EventSystem
            var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
            _eventSystemOk = es != null && es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() != null;

            // Ses & Tema & Bootstrapper
            _soundOk = Object.FindAnyObjectByType<SoundHandlerView>(FindObjectsInactive.Include) != null;
            _themeOk = Object.FindAnyObjectByType<ThemeHandlerView>(FindObjectsInactive.Include) != null;

            var boot = Object.FindAnyObjectByType<GameBootstrapper>(FindObjectsInactive.Include);
            _bootstrapperOk = boot != null && boot.initialLevel != null;
            _levelsOk = _cachedLevels.Count > 0 && boot != null && boot.initialLevel != null;

            // Yeni View tanılamaları (Pasif nesneleri de kapsar)
            _dailyCrisisOk = Object.FindAnyObjectByType<DailyCrisisView>(FindObjectsInactive.Include) != null;
            _confettiOk = Object.FindAnyObjectByType<ConfettiView>(FindObjectsInactive.Include) != null;
            _bloomFlashOk = Object.FindAnyObjectByType<BloomFlashView>(FindObjectsInactive.Include) != null;
            _tutorialOk = Object.FindAnyObjectByType<TutorialView>(FindObjectsInactive.Include) != null;
            _settingsViewOk = Object.FindAnyObjectByType<SettingsView>(FindObjectsInactive.Include) != null;

            // Global Volume & Kamera
            _globalVolumeOk = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include).Any(go => go.name.Contains("Volume") || go.GetComponent("Volume") != null);
            var mainCam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            _cameraControllerOk = mainCam != null && mainCam.GetComponent<CameraController>() != null;
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

        // ═══════════════════════════════════════════════════
        // ANA GUI
        // ═══════════════════════════════════════════════════

        private void OnGUI()
        {
            InitStyles();

            // Başlık Bandı
            GUILayout.BeginVertical(_titleBannerStyle);
            GUILayout.Label("PIXEL FLOW KONTROL MERKEZİ", _headerStyle);
            GUILayout.Label("Canlı Oyun Yönetimi • Sahne Kurulumu • Seviye Stüdyosu • Nexus İzleyici", _miniInfoStyle);
            GUILayout.EndVertical();

            GUILayout.Space(4);

            // Sekme Navigasyonu
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, GUILayout.Height(26));
            GUILayout.Space(6);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            switch (_selectedTab)
            {
                case 0: DrawGameControllerTab(); break;
                case 1: DrawDiagnosticsTab(); break;
                case 2: DrawLevelStudioTab(); break;
                case 3: DrawBatchSolverTab(); break;
                case 4: DrawEconomyAnalyticsTab(); break;
                case 5: DrawNexusInspectorTab(); break;
                case 6: DrawPerformanceTab(); break;
            }

            GUILayout.EndScrollView();
        }

        // ═══════════════════════════════════════════════════
        // SEKME 0: OYUN KONTROL MERKEZİ
        // ═══════════════════════════════════════════════════

        private void DrawGameControllerTab()
        {
            // 1. Canlı Çalışma Durumu Kartı
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("📊 Canlı Oyun Durumu İzleyici", _sectionHeaderStyle);
            GUILayout.Space(5);

            bool isPlaying = Application.isPlaying;
            string playStatus = isPlaying ? "▶ OYNANIYOR (Canlı)" : "⏸ DÜZENLEME MODU (Durmuş)";
            Color statusColor = isPlaying ? new Color(0.2f, 0.8f, 0.3f) : new Color(0.9f, 0.6f, 0.1f);
            GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = statusColor } };

            DrawInfoRow("Motor Durumu:", playStatus, statusStyle);

            var stateModel = GetModel<IGameStateModel>();
            var levelModel = GetModel<ILevelModel>();
            var progressModel = GetModel<IProgressModel>();
            var sessionModel = GetModel<IGameSessionModel>();
            var hintModel = GetModel<IHintModel>();

            string currentState = stateModel != null ? stateModel.CurrentState.ToString() : "Başlatılmadı";
            string currentLvlInfo = levelModel != null && levelModel.CurrentLevel != null
                ? $"Seviye {levelModel.CurrentLevel.levelIndex + 1} ({levelModel.CurrentLevel.name})"
                : (Object.FindAnyObjectByType<GameBootstrapper>()?.initialLevel != null
                    ? $"Başlangıç: {Object.FindAnyObjectByType<GameBootstrapper>().initialLevel.name}"
                    : "Yok");

            int unlockedLvl = progressModel != null ? progressModel.UnlockedLevels : PlayerPrefs.GetInt("NT_UnlockedLevels", 1);
            int hints = hintModel != null ? hintModel.HintsRemaining : -1;
            float elapsed = sessionModel != null ? sessionModel.ElapsedTime : 0f;

            DrawInfoRow("Oyun Durumu:", currentState);
            DrawInfoRow("Yüklü Seviye:", currentLvlInfo);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Açık Seviye:", GUILayout.Width(110));
            GUILayout.Label($"Seviye {unlockedLvl + 1}", EditorStyles.boldLabel, GUILayout.Width(100));
            if (hints >= 0)
            {
                GUILayout.Label("İpucu:", GUILayout.Width(45));
                GUILayout.Label($"{hints}", EditorStyles.boldLabel);
            }
            GUILayout.EndHorizontal();

            if (isPlaying && elapsed > 0)
            {
                DrawInfoRow("Geçen Süre:", $"{(int)(elapsed / 60)}dk {(int)(elapsed % 60)}sn");
            }

            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 2. Canlı Oyun Kontrolleri
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🎮 Canlı Oyun Kontrolleri", _sectionHeaderStyle);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = isPlaying ? new Color(0.2f, 0.7f, 1f) : new Color(0.2f, 0.8f, 0.3f);
            if (GUILayout.Button(isPlaying ? "▶ Açık Seviyeyi Yeniden Yükle" : "▶ Oyunu Başlat (Seviye 1)", GUILayout.Height(32)))
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
            if (GUILayout.Button("🏆 Seviyeyi Tamamla (Kazan)", GUILayout.Height(32)))
            {
                CompleteCurrentLevel();
            }
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("🔁 Seviyeyi Yeniden Başlat", GUILayout.Height(32)))
            {
                RestartCurrentLevel();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("💡 Bedava İpucu Ver", GUILayout.Height(28)))
            {
                DispatchSignal(new RequestHintSignal());
            }
            if (GUILayout.Button("↩️ Geri Al", GUILayout.Height(28)))
            {
                DispatchSignal(new UndoSignal());
            }
            if (GUILayout.Button("↪️ Yinele", GUILayout.Height(28)))
            {
                DispatchSignal(new RedoSignal());
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔓 Tüm Seviyeleri Aç", GUILayout.Height(28)))
            {
                UnlockAllLevels();
            }
            if (GUILayout.Button("🔒 İlerlemeyi Sıfırla", GUILayout.Height(28)))
            {
                ResetProgress();
            }
            if (GUILayout.Button("💾 Zorla Kaydet", GUILayout.Height(28)))
            {
                ForceSaveGame();
            }
            if (GUILayout.Button("🗑️ Tüm Kayıtları Sil", GUILayout.Height(28)))
            {
                WipeSaveData();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 3. Sinyal Tetikleyici Paneli
            GUILayout.BeginVertical(_cardStyle);
            _signalPanelOpen = EditorGUILayout.Foldout(_signalPanelOpen, "📡 Gelişmiş Sinyal Tetikleyici (Tüm Sinyaller)", true, EditorStyles.foldoutHeader);
            if (_signalPanelOpen)
            {
                GUILayout.Space(4);
                EditorGUILayout.HelpBox("Play Mode'da tüm oyun sinyallerini manuel olarak ateşleyin.", MessageType.Info);
                GUILayout.Space(4);

                GUILayout.Label("Oyun Akışı Sinyalleri:", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                DrawSignalButton("LevelCompleted", () => DispatchSignal(new LevelCompletedSignal()));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                DrawSignalButton("CheckWin", () => DispatchSignal(new CheckWinConditionSignal()));
                DrawSignalButton("GridUpdated", () => DispatchSignal(new GridUpdatedSignal()));
                DrawSignalButton("ThemeChanged", () => DispatchSignal(new ThemeChangedSignal()));
                DrawSignalButton("TimerTick", () => DispatchSignal(new TimerTickSignal()));
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                GUILayout.Label("Giriş & Oynanış Sinyalleri:", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                DrawSignalButton("Undo", () => DispatchSignal(new UndoSignal()));
                DrawSignalButton("Redo", () => DispatchSignal(new RedoSignal()));
                DrawSignalButton("Hint", () => DispatchSignal(new RequestHintSignal()));
                DrawSignalButton("ViaductExhausted", () => DispatchSignal(new ViaductExhaustedSignal()));
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                GUILayout.Label("Ekonomi & Reklam Sinyalleri:", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                DrawSignalButton("RewardedAd", () => DispatchSignal(new RequestRewardedAdSignal()));
                DrawSignalButton("InterstitialAd", () => DispatchSignal(new RequestInterstitialAdSignal()));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 4. Araç Modeli Seçici
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🚗 Araç Modeli ve Görsel Stili", _sectionHeaderStyle);
            GUILayout.Space(4);

            var settingsModel = GetModel<ISettingsModel>();
            int currentStyleInt = settingsModel != null
                ? (int)settingsModel.CurrentVehicleStyle
                : PlayerPrefs.GetInt("VehicleStyle", 0);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Aktif Araç Modeli:", GUILayout.Width(140));
            GUI.backgroundColor = currentStyleInt == 0 ? new Color(0.2f, 0.7f, 1f) : Color.white;
            if (GUILayout.Button("🚘 Araba", GUILayout.Height(28)))
            {
                SetVehicleStyle(VehicleStyle.Car);
            }
            GUI.backgroundColor = currentStyleInt == 1 ? new Color(0.9f, 0.7f, 0.2f) : Color.white;
            if (GUILayout.Button("🚆 Tren / Ekspres", GUILayout.Height(28)))
            {
                SetVehicleStyle(VehicleStyle.Train);
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 5. Hızlı Seviye Başlatıcı
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label($"⚡ Hızlı Seviye Başlatıcı ({_cachedLevels.Count} Seviye)", _sectionHeaderStyle);
            GUILayout.Label("Herhangi bir seviyeyi anında Play Mode'da başlatmak için 'Başlat' düğmesine tıklayın.", EditorStyles.miniLabel);
            GUILayout.Space(6);

            if (_cachedLevels.Count == 0)
            {
                EditorGUILayout.HelpBox("Hiç LevelData varlığı bulunamadı. Aşağıya tıklayarak varsayılan seviyeleri oluşturun.", MessageType.Warning);
                if (GUILayout.Button("Faz 1+2 Seviye Paketi Oluştur (12 Seviye)", GUILayout.Height(30)))
                {
                    CreatePhase1And2HandCraftedPack();
                    RefreshData();
                }
            }
            else
            {
                DrawLevelTableHeader();
                for (int i = 0; i < _cachedLevels.Count; i++)
                {
                    var lvl = _cachedLevels[i];
                    if (lvl == null) continue;
                    DrawLevelTableRow(lvl, true);
                }
            }
            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 6. Bootstrapper Hedef Yapılandırma
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🎯 Bootstrapper Hedef Yapılandırması", _sectionHeaderStyle);
            GUILayout.Space(5);

            var bootstrapper = Object.FindAnyObjectByType<GameBootstrapper>();
            if (bootstrapper != null)
            {
                EditorGUI.BeginChangeCheck();
                var newInitial = (LevelData)EditorGUILayout.ObjectField("Başlangıç Seviyesi", bootstrapper.initialLevel, typeof(LevelData), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(bootstrapper, "Başlangıç Seviyesi Değiştir");
                    bootstrapper.initialLevel = newInitial;
                    EditorUtility.SetDirty(bootstrapper);
                }

                if (GUILayout.Button("Seviye 1'i Başlangıç Hedefi Olarak Ata", GUILayout.Height(24)))
                {
                    var lvl1 = ResolveLevelByIndex(0);
                    if (lvl1 != null)
                    {
                        Undo.RecordObject(bootstrapper, "Seviye 1 Ata");
                        bootstrapper.initialLevel = lvl1;
                        EditorUtility.SetDirty(bootstrapper);
                        Debug.Log($"[PixelFlow] {lvl1.name} GameBootstrapper başlangıç seviyesi olarak atandı.");
                        RefreshData();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Aktif sahnede GameBootstrapper bileşeni bulunamadı. Tanılama sekmesinden otomatik oluşturabilirsiniz.", MessageType.Warning);
            }
            GUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        // SEKME 1: SAHNE TANILAMA
        // ═══════════════════════════════════════════════════

        private void DrawDiagnosticsTab()
        {
            // 1. Temel Sahne Sağlığı
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🏗️ Temel Sahne Bileşenleri", _sectionHeaderStyle);
            GUILayout.Space(5);

            DrawDiagnosticRow("Temel Prefab'lar (CellView)", _prefabsOk, GeneratePrefabs);
            DrawDiagnosticRow("CellView Uyarı İkonu Renderer", _cellWarningIconOk, FixCellViewWarningIcon);
            DrawDiagnosticRow("Sahne Root Context", _rootOk, SetupScene);
            DrawDiagnosticRow("Context Data Yapılandırması", _contextDataOk, SetupScene);
            DrawDiagnosticRow("GridView Bileşeni & Düzeni", _gridViewOk, SetupScene);
            DrawDiagnosticRow("Canvas UI Sarmalayıcı", _canvasOk, SetupScene);
            DrawDiagnosticRow("HUDView Kontrol Paneli", _hudOk, SetupScene);
            DrawDiagnosticRow("EventSystem (Input System)", _eventSystemOk, SetupScene);
            DrawDiagnosticRow("Ses Sistemi İşleyici", _soundOk, SetupScene);
            DrawDiagnosticRow("Renk Teması İşleyici", _themeOk, SetupScene);
            DrawDiagnosticRow("Oyun Yaşam Döngüsü Başlatıcı", _bootstrapperOk, SetupScene);
            DrawDiagnosticRow("Seviye Veri Kaydı & Başlangıç Seviyesi", _levelsOk, SetupScene);

            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 2. Genişletilmiş View Tanılamaları
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🖼️ Genişletilmiş View Bileşenleri", _sectionHeaderStyle);
            GUILayout.Space(5);

            DrawDiagnosticRow("DailyCrisisView (Günlük Kriz)", _dailyCrisisOk, SetupExtendedViews);
            DrawDiagnosticRow("ConfettiView (Kutlama Efekti)", _confettiOk, SetupExtendedViews);
            DrawDiagnosticRow("BloomFlashView (Işık Patlaması)", _bloomFlashOk, SetupExtendedViews);
            DrawDiagnosticRow("TutorialView (Eğitim Sistemi)", _tutorialOk, SetupExtendedViews);
            DrawDiagnosticRow("SettingsView (Ayarlar Paneli)", _settingsViewOk, SetupExtendedViews);

            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 3. Ortam Bileşenleri
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🌐 Ortam & Kamera Bileşenleri", _sectionHeaderStyle);
            GUILayout.Space(5);

            DrawDiagnosticRow("Global Volume (Post-Processing)", _globalVolumeOk, SetupGlobalVolume);
            DrawDiagnosticRow("Kamera Kontrolcüsü", _cameraControllerOk, SetupCameraController);

            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 4. Sahne Hiyerarşi Özeti
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("📋 Sahne Hiyerarşi Özeti", _sectionHeaderStyle);
            GUILayout.Space(5);

            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
            int activeCount = allObjects.Count(go => go.activeInHierarchy);
            int viewCount = Object.FindObjectsByType<View>(FindObjectsInactive.Exclude).Length;
            int rootCount = Object.FindObjectsByType<Root>(FindObjectsInactive.Exclude).Length;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Toplam Obje: {allObjects.Length}", GUILayout.Width(160));
            GUILayout.Label($"Aktif: {activeCount}", GUILayout.Width(100));
            GUILayout.Label($"Pasif: {allObjects.Length - activeCount}", GUILayout.Width(100));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Nexus View: {viewCount}", GUILayout.Width(160));
            GUILayout.Label($"Nexus Root: {rootCount}", GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 5. Toplu İşlem Butonları
            bool allCoreOk = _prefabsOk && _cellWarningIconOk && _rootOk && _contextDataOk && _gridViewOk &&
                            _canvasOk && _hudOk && _eventSystemOk && _soundOk && _themeOk && _bootstrapperOk && _levelsOk;
            bool allExtOk = _dailyCrisisOk && _confettiOk && _bloomFlashOk &&
                           _tutorialOk && _settingsViewOk;
            bool allEnvOk = _globalVolumeOk && _cameraControllerOk;

            if (allCoreOk && allExtOk && allEnvOk)
            {
                EditorGUILayout.HelpBox("✔ Her şey mükemmel yapılandırılmış. Oynamaya hazır!", MessageType.Info);
            }
            else
            {
                GUILayout.BeginVertical(_cardStyle);
                GUILayout.Label("🔧 Hızlı Düzeltme Araçları", _sectionHeaderStyle);
                GUILayout.Space(5);

                GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
                if (GUILayout.Button("🚀 Tek Tıkla Tam Sahne Kurulumu (Temel + Genişletilmiş + Ortam)", GUILayout.Height(35)))
                {
                    GeneratePrefabs();
                    SetupScene();
                    SetupExtendedViews();
                    SetupGlobalVolume();
                    SetupCameraController();
                    RefreshData();
                }
                GUI.backgroundColor = Color.white;

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (!allCoreOk && GUILayout.Button("Temel Bileşenleri Kur", GUILayout.Height(28)))
                {
                    GeneratePrefabs();
                    SetupScene();
                    RefreshData();
                }
                if (!allExtOk && GUILayout.Button("Genişletilmiş View'leri Kur", GUILayout.Height(28)))
                {
                    SetupExtendedViews();
                    RefreshData();
                }
                if (!allEnvOk && GUILayout.Button("Ortam Bileşenlerini Kur", GUILayout.Height(28)))
                {
                    SetupGlobalVolume();
                    SetupCameraController();
                    RefreshData();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.Space(8);

            // 6. Geliştirici Hızlı Araçlar
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🛠️ Geliştirici Hızlı Araçları", _sectionHeaderStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🗑️ PlayerPrefs & Kayıt Verilerini Temizle", GUILayout.Height(28)))
            {
                WipeSaveData();
            }
            if (GUILayout.Button("📂 Sahneyi Kaydet", GUILayout.Height(28)))
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                Debug.Log("[PixelFlow] Açık sahneler kaydedildi.");
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        // SEKME 2: SEVİYE STÜDYOSU
        // ═══════════════════════════════════════════════════

        private void DrawLevelStudioTab()
        {
            // 1. Özel Seviye Oluşturma
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🎨 Özel Seviye Varlığı Oluştur", _sectionHeaderStyle);
            GUILayout.Space(5);

            _newLevelIndex = EditorGUILayout.IntField("Yeni Seviye İndeksi", _newLevelIndex);
            _newWidth = EditorGUILayout.IntSlider("Izgara Genişliği", _newWidth, 3, 10);
            _newHeight = EditorGUILayout.IntSlider("Izgara Yüksekliği", _newHeight, 3, 10);

            GUILayout.Space(8);
            if (GUILayout.Button("Boş Seviye Varlığı Oluştur", GUILayout.Height(28)))
            {
                CreateCustomLevel(_newLevelIndex, _newWidth, _newHeight);
                RefreshData();
            }
            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 2. Prosedürel Seviye Üretimi
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🎲 Prosedürel Seviye Üreteci", _sectionHeaderStyle);
            GUILayout.Space(5);

            _procSelectedDifficulty = GUILayout.SelectionGrid(
                _procSelectedDifficulty,
                _procDifficultyNames.Split('|'),
                5, GUILayout.Height(22));

            _procUseSeed = EditorGUILayout.Toggle("Sabit Tohum Kullan", _procUseSeed);
            if (_procUseSeed)
                _procSeed = EditorGUILayout.IntField("Tohum Değeri", _procSeed);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Tekli Üret", GUILayout.Height(28)))
            {
                GenerateProceduralLevel(_procSelectedDifficulty, _procUseSeed ? _procSeed : (int?)null, _newLevelIndex);
                RefreshData();
            }
            _procStartIndex = EditorGUILayout.IntField("Başlangıç İndeksi", _procStartIndex, GUILayout.Width(80));
            _procBatchCount = EditorGUILayout.IntField("Adet", _procBatchCount, GUILayout.Width(60));
            if (GUILayout.Button("Toplu Üret", GUILayout.Height(28)))
            {
                GenerateProceduralBatch(_procSelectedDifficulty, _procUseSeed ? _procSeed : (int?)null,
                    _procStartIndex, _procBatchCount);
                RefreshData();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 3. Seviye Veritabanı Yöneticisi
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label($"📁 Proje Seviye Kaydı ({_cachedLevels.Count} Seviye)", _sectionHeaderStyle);
            GUILayout.Space(5);

            if (_cachedLevels.Count == 0)
            {
                GUILayout.Label("Bu projede hiç LevelData varlığı bulunamadı.", EditorStyles.miniLabel);
                GUILayout.Space(5);
                if (GUILayout.Button("3 Seviyeli Başlangıç Paketi Oluştur", GUILayout.Height(25)))
                {
                    CreateThreeLevelPack();
                    RefreshData();
                }
                GUILayout.Space(3);
                if (GUILayout.Button("Faz 1+2 El Yapımı Paket Oluştur (12 seviye)", GUILayout.Height(25)))
                {
                    CreatePhase1And2HandCraftedPack();
                    RefreshData();
                }
            }
            else
            {
                DrawLevelTableHeader();
                for (int i = 0; i < _cachedLevels.Count; i++)
                {
                    var lvl = _cachedLevels[i];
                    if (lvl == null) continue;
                    DrawLevelTableRow(lvl, false);
                }
            }
            GUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        // SEKME 3: TOPLU ÇÖZÜCÜ
        // ═══════════════════════════════════════════════════

        private void DrawBatchSolverTab()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🧪 Toplu Otomatik Çözücü & Seviye Bütünlük Denetçisi", _sectionHeaderStyle);
            GUILayout.Label("Projedeki tüm seviyelerin matematiksel olarak çözülebilirliğini doğrulayın.", EditorStyles.miniLabel);
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("TÜM Seviyelerde Toplu Çözücüyü Çalıştır", GUILayout.Height(32)))
            {
                RunBatchSolver();
            }
            if (GUILayout.Button("Eksik Çözümleri Otomatik Düzelt & Üret", GUILayout.Height(32)))
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
            GUILayout.Space(8);

            // Çözücü Sonuç Tablosu
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label($"📊 Çözülebilirlik Denetim Durumu ({_cachedLevels.Count} Seviye)", _sectionHeaderStyle);
            GUILayout.Space(5);

            if (_cachedLevels.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Seviye", EditorStyles.boldLabel, GUILayout.Width(60));
                GUILayout.Label("Izgara", EditorStyles.boldLabel, GUILayout.Width(60));
                GUILayout.Label("Çözülebilirlik Durumu", EditorStyles.boldLabel, GUILayout.Width(160));
                GUILayout.Label("Çözüm Sayısı", EditorStyles.boldLabel, GUILayout.Width(100));
                GUILayout.Label("İşlem", EditorStyles.boldLabel);
                GUILayout.EndHorizontal();

                var solver = new RuntimePathSolver();

                foreach (var lvl in _cachedLevels)
                {
                    if (lvl == null) continue;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Svye {lvl.levelIndex}", GUILayout.Width(60));
                    GUILayout.Label($"{lvl.width}x{lvl.height}", GUILayout.Width(60));

                    if (!_solvabilityCache.TryGetValue(lvl, out bool isSolvable))
                    {
                        isSolvable = solver.Solve(lvl, out _);
                        _solvabilityCache[lvl] = isSolvable;
                    }

                    if (isSolvable)
                    {
                        GUILayout.Label("✔ ÇÖZÜLEBİLİR", _okBadgeStyle, GUILayout.Width(160));
                    }
                    else
                    {
                        GUILayout.Label("✖ ÇÖZÜLEMİYOR!", _errorBadgeStyle, GUILayout.Width(160));
                    }

                    int solutionCount = lvl.solutions != null ? lvl.solutions.Count : 0;
                    string solLabel = solutionCount > 0 ? $"{solutionCount} renk çözüldü" : "Kayıtlı çözüm yok";
                    GUILayout.Label(solLabel, GUILayout.Width(100));

                    if (GUILayout.Button("İncele", GUILayout.Height(18), GUILayout.Width(60)))
                    {
                        Selection.activeObject = lvl;
                        EditorGUIUtility.PingObject(lvl);
                    }

                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        // SEKME 4: EKONOMİ & ISI HARİTASI
        // ═══════════════════════════════════════════════════

        private void DrawEconomyAnalyticsTab()
        {
            // Zorluk Isı Haritası
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🌡️ Seviye Karmaşıklık & Zorluk Isı Haritası", _sectionHeaderStyle);
            GUILayout.Label("Izgara alanı, düğüm sayısı ve köprü yoğunluğuna göre hesaplanan skor.", EditorStyles.miniLabel);
            GUILayout.Space(8);

            if (_cachedLevels.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Seviye", EditorStyles.boldLabel, GUILayout.Width(60));
                GUILayout.Label("Alan", EditorStyles.boldLabel, GUILayout.Width(70));
                GUILayout.Label("Karmaşıklık Skoru", EditorStyles.boldLabel, GUILayout.Width(120));
                GUILayout.Label("Zorluk Seviyesi", EditorStyles.boldLabel, GUILayout.Width(110));
                GUILayout.Label("Kapsama Kuralı", EditorStyles.boldLabel);
                GUILayout.EndHorizontal();

                foreach (var lvl in _cachedLevels)
                {
                    if (lvl == null) continue;
                    int score = CalculateComplexityScore(lvl);
                    string tierName = GetDifficultyTierName(score);
                    Color tierColor = GetDifficultyTierColor(score);

                    GUIStyle tierStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = tierColor } };

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Svye {lvl.levelIndex}", GUILayout.Width(60));
                    GUILayout.Label($"{lvl.width}x{lvl.height} ({lvl.width * lvl.height})", GUILayout.Width(70));
                    GUILayout.Label($"{score} puan", GUILayout.Width(120));
                    GUILayout.Label(tierName, tierStyle, GUILayout.Width(110));
                    GUILayout.Label(lvl.requireFullGridCoverage ? "Tam Izgara (%100)" : "Esnek Bağlantı");
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
            GUILayout.Space(8);

            // Ekonomi Simülasyonu
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("💹 Boşta Ekonomi Bilanço Tablosu (Kademe 1-10 Maliyet Projeksiyonu)", _sectionHeaderStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Kademe", EditorStyles.boldLabel, GUILayout.Width(55));
            GUILayout.Label("Depo Kapasitesi", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label("Oran Maliyeti", EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("Depo Maliyeti", EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("Viyadük Maliyeti", EditorStyles.boldLabel);
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
                GUILayout.Label($"K{lvl + 1}", GUILayout.Width(55));
                GUILayout.Label($"{storageCaps[lvl]:N0}", GUILayout.Width(100));
                GUILayout.Label($"{rateC:N0} ₺", GUILayout.Width(90));
                GUILayout.Label($"{storageC:N0} ₺", GUILayout.Width(90));
                GUILayout.Label($"{viaductC:N0} ₺");
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        // SEKME 5: NEXUS DURUM İZLEYİCİ (YENİ)
        // ═══════════════════════════════════════════════════

        private void DrawNexusInspectorTab()
        {
            var root = Object.FindAnyObjectByType<Root>();
            bool initialized = root != null && root.IsInitialized && root.Context != null;

            // 1. Nexus Root Durumu
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🔬 Nexus Root & Context Durumu", _sectionHeaderStyle);
            GUILayout.Space(5);

            DrawInfoRow("Root Nesnesi:", root != null ? $"✔ {root.gameObject.name}" : "✖ Bulunamadı");
            DrawInfoRow("Başlatıldı mı:", initialized ? "✔ Evet" : "✖ Hayır");
            if (root != null)
            {
                string scope = root.Context != null && !string.IsNullOrEmpty(root.Context.ScopeTag) ? root.Context.ScopeTag : "(Boş)";
                DrawInfoRow("Kapsam Etiketi:", scope);
                DrawInfoRow("ContextData:", root.ContextData != null ? $"✔ {root.ContextData.name}" : "✖ Atanmamış");
            }

            GUILayout.EndVertical();
            GUILayout.Space(8);

            if (!initialized)
            {
                EditorGUILayout.HelpBox("Nexus Root başlatılmamış. Canlı izleme için Play Mode'a girin.", MessageType.Warning);
                return;
            }

            var container = root.Context.Container;

            // 2. Kayıtlı Reactive Modeller
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("📦 Kayıtlı Reactive Modeller", _sectionHeaderStyle);
            GUILayout.Space(5);

            DrawNexusResolveStatus<IGridModel>(container, "IGridModel (Izgara)");
            DrawNexusResolveStatus<ILevelModel>(container, "ILevelModel (Seviye)");
            DrawNexusResolveStatus<IProgressModel>(container, "IProgressModel (İlerleme)");
            DrawNexusResolveStatus<IGameStateModel>(container, "IGameStateModel (Oyun Durumu)");
            DrawNexusResolveStatus<IGameSessionModel>(container, "IGameSessionModel (Oturum)");
            DrawNexusResolveStatus<IHintModel>(container, "IHintModel (İpucu)");
            DrawNexusResolveStatus<ISettingsModel>(container, "ISettingsModel (Ayarlar)");
            DrawNexusResolveStatus<ISoundModel>(container, "ISoundModel (Ses)");
            DrawNexusResolveStatus<ITutorialModel>(container, "ITutorialModel (Eğitim)");
            DrawNexusResolveStatus<IDailyCrisisModel>(container, "IDailyCrisisModel (Günlük Kriz)");

            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 3. Kayıtlı Servisler
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("⚙️ Kayıtlı Servisler", _sectionHeaderStyle);
            GUILayout.Space(5);

            DrawNexusResolveStatus<IPathService>(container, "IPathService (Yol)");
            DrawNexusResolveStatus<IGameHistoryService>(container, "IGameHistoryService (Geçmiş)");
            DrawNexusResolveStatus<IVehicleSimulator>(container, "IVehicleSimulator (Araç Simülatörü)");
            DrawNexusResolveStatus<IGameplayTimerService>(container, "IGameplayTimerService (Zamanlayıcı)");
            DrawNexusResolveStatus<IObstacleService>(container, "IObstacleService (Engel)");
            DrawNexusResolveStatus<IDailyCrisisService>(container, "IDailyCrisisService (Kriz)");
            DrawNexusResolveStatus<ICrisisAdService>(container, "ICrisisAdService (Kriz Reklam)");
            DrawNexusResolveStatus<IHintService>(container, "IHintService (İpucu)");
            DrawNexusResolveStatus<ILevelProgressionService>(container, "ILevelProgressionService (Seviye İlerleme)");
            DrawNexusResolveStatus<IFeedbackService>(container, "IFeedbackService (Geri Bildirim)");
            DrawNexusResolveStatus<ILoggerService>(container, "ILoggerService (Kayıtçı)");
            DrawNexusResolveStatus<IPlayerPrefsService>(container, "IPlayerPrefsService (Tercihler)");
            DrawNexusResolveStatus<ISignalBus>(container, "ISignalBus (Sinyal Veriyolu)");

            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 4. View Bağlantı Durumları
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🖼️ Aktif View Bağlantıları", _sectionHeaderStyle);
            GUILayout.Space(5);

            var allViews = Object.FindObjectsByType<View>(FindObjectsInactive.Exclude);
            if (allViews.Length == 0)
            {
                GUILayout.Label("Sahnede aktif View bulunamadı.", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var view in allViews)
                {
                    string viewName = view.GetType().Name;
                    string objName = view.gameObject.name;
                    bool active = view.gameObject.activeInHierarchy;
                    Color c = active ? new Color(0.12f, 0.65f, 0.22f) : new Color(0.6f, 0.6f, 0.6f);
                    GUIStyle vs = new GUIStyle(EditorStyles.label) { normal = { textColor = c } };

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(active ? "●" : "○", vs, GUILayout.Width(15));
                    GUILayout.Label(viewName, EditorStyles.boldLabel, GUILayout.Width(180));
                    GUILayout.Label($"({objName})", EditorStyles.miniLabel);
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        // SEKME 6: PERFORMANS & DENETİM (YENİ)
        // ═══════════════════════════════════════════════════

        private void DrawPerformanceTab()
        {
            bool isPlaying = Application.isPlaying;

            // 1. Çalışma Zamanı Performans Metrikleri
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("📈 Çalışma Zamanı Performans Metrikleri", _sectionHeaderStyle);
            GUILayout.Space(5);

            if (isPlaying)
            {
                float fps = 1.0f / Time.unscaledDeltaTime;
                Color fpsColor = fps > 55 ? new Color(0.12f, 0.65f, 0.22f) :
                                 fps > 30 ? new Color(0.9f, 0.6f, 0.1f) :
                                 new Color(0.85f, 0.2f, 0.18f);
                GUIStyle fpsStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = fpsColor }, fontSize = 16 };

                GUILayout.BeginHorizontal();
                GUILayout.Label("FPS:", GUILayout.Width(110));
                GUILayout.Label($"{fps:F1}", fpsStyle);
                GUILayout.EndHorizontal();

                long totalMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                long reservedMemory = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
                long unusedMemory = UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong();

                DrawInfoRow("Toplam Ayrılan Bellek:", $"{totalMemory / (1024f * 1024f):F1} MB");
                DrawInfoRow("Ayrılmış Bellek:", $"{reservedMemory / (1024f * 1024f):F1} MB");
                DrawInfoRow("Kullanılmayan Bellek:", $"{unusedMemory / (1024f * 1024f):F1} MB");
                DrawInfoRow("GC Toplama Sayısı:", $"{System.GC.CollectionCount(0)} (Gen0), {System.GC.CollectionCount(1)} (Gen1), {System.GC.CollectionCount(2)} (Gen2)");
                DrawInfoRow("Zaman Ölçeği:", $"{Time.timeScale:F2}x");
                DrawInfoRow("Kare Sayısı:", $"{Time.frameCount:N0}");
            }
            else
            {
                EditorGUILayout.HelpBox("Performans metrikleri yalnızca Play Mode'da görüntülenebilir.", MessageType.Info);
            }

            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 2. Zaman Ölçeği Kontrolü
            if (isPlaying)
            {
                GUILayout.BeginVertical(_cardStyle);
                GUILayout.Label("⏱️ Zaman Ölçeği Kontrolü", _sectionHeaderStyle);
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("⏸ Durdur (0x)", GUILayout.Height(28)))
                    Time.timeScale = 0f;
                if (GUILayout.Button("▶ Normal (1x)", GUILayout.Height(28)))
                    Time.timeScale = 1f;
                if (GUILayout.Button("⏩ Hızlı (2x)", GUILayout.Height(28)))
                    Time.timeScale = 2f;
                if (GUILayout.Button("⏩⏩ Çok Hızlı (5x)", GUILayout.Height(28)))
                    Time.timeScale = 5f;
                if (GUILayout.Button("🐌 Ağır Çekim (0.25x)", GUILayout.Height(28)))
                    Time.timeScale = 0.25f;
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.Space(8);
            }

            // 3. Sahne İstatistikleri
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("📊 Sahne İstatistikleri", _sectionHeaderStyle);
            GUILayout.Space(5);

            var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
            var allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            var allColliders = Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude);
            var allCanvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
            var allAudioSources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude);

            int activeGOs = allGOs.Count(go => go.activeInHierarchy);
            int activeRenderers = allRenderers.Count(r => r.enabled && r.gameObject.activeInHierarchy);

            DrawInfoRow("Toplam GameObject:", $"{allGOs.Length} (Aktif: {activeGOs})");
            DrawInfoRow("Renderer Bileşeni:", $"{allRenderers.Length} (Aktif: {activeRenderers})");
            DrawInfoRow("Collider2D Bileşeni:", $"{allColliders.Length}");
            DrawInfoRow("Canvas Bileşeni:", $"{allCanvas.Length}");
            DrawInfoRow("AudioSource Bileşeni:", $"{allAudioSources.Length}");

            if (isPlaying)
            {
                var views = Object.FindObjectsByType<View>(FindObjectsInactive.Exclude);
                int activeViews = views.Count(v => v.gameObject.activeInHierarchy);
                DrawInfoRow("Nexus View:", $"{views.Length} (Aktif: {activeViews})");
            }

            GUILayout.EndVertical();
            GUILayout.Space(8);

            // 4. Proje Varlık Özeti
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("📁 Proje Varlık Özeti", _sectionHeaderStyle);
            GUILayout.Space(5);

            int levelCount = _cachedLevels.Count;
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
            string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { "Assets/Scripts" });
            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });

            DrawInfoRow("LevelData Varlıkları:", $"{levelCount}");
            DrawInfoRow("Prefab Dosyaları:", $"{prefabGuids.Length}");
            DrawInfoRow("Script Dosyaları:", $"{scriptGuids.Length}");
            DrawInfoRow("Material Dosyaları:", $"{materialGuids.Length}");

            GUILayout.EndVertical();
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
                if (bootstrapper == null)
                {
                    SetupScene();
                    bootstrapper = Object.FindAnyObjectByType<GameBootstrapper>();
                }
                if (bootstrapper != null)
                {
                    Undo.RecordObject(bootstrapper, "Başlangıç Seviyesi Ayarla");
                    bootstrapper.initialLevel = level;
                    EditorUtility.SetDirty(bootstrapper);
                }
                EditorApplication.isPlaying = true;
                Debug.Log($"[PixelFlow] {level.name} GameBootstrapper'a atandı ve Play Mode başlatıldı.");
            }
        }

        private void CompleteCurrentLevel()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PixelFlow] Kazanma simülasyonu için Play Mode gereklidir.");
                return;
            }
            var stateModel = GetModel<IGameStateModel>();
            if (stateModel != null)
            {
                stateModel.SetState(GameState.LevelCompleted);
                DispatchSignal(new LevelCompletedSignal());
                Debug.Log("[PixelFlow] Seviye tamamlandı sinyali ateşlendi.");
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
            Debug.Log($"[PixelFlow] Tüm {maxCount} seviye açıldı.");
        }

        private void ResetProgress()
        {
            PlayerPrefs.SetInt("UnlockedLevels", 1);
            PlayerPrefs.SetInt("NT_UnlockedLevels", 1);
            PlayerPrefs.DeleteKey("NT_PuzzleSave_");
            PlayerPrefs.Save();
            Debug.Log("[PixelFlow] İlerleme Seviye 1'e sıfırlandı.");
        }

        private void ForceSaveGame()
        {
            var grid = GetModel<IGridModel>();
            var session = GetModel<IGameSessionModel>();
            var level = GetModel<ILevelModel>();
            if (grid != null && session != null && level != null && level.CurrentLevel != null)
            {
                GridStateSerializer.Save(grid, session, level);
                Debug.Log("[PixelFlow] Oyun durumu zorla kaydedildi.");
            }
        }

        private void WipeSaveData()
        {
            if (EditorUtility.DisplayDialog("Kayıt & PlayerPrefs Temizle",
                "Tüm kaydedilmiş ilerleme ve oyuncu tercihlerini silmek istediğinizden emin misiniz?", "Evet", "Hayır"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("[PixelFlow] PlayerPrefs tamamen temizlendi.");
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
                    LogSignal(typeof(TSignal).Name);
                    return;
                }
            }
            Debug.LogWarning("[PixelFlow] Nexus Root başlatılmamış veya sinyal veriyolu bulunamadı.");
        }

        private void LogSignal(string signalName)
        {
            string entry = $"[{System.DateTime.Now:HH:mm:ss}] {signalName}";
            _signalLog.Insert(0, entry);
            if (_signalLog.Count > MaxSignalLogEntries)
                _signalLog.RemoveAt(_signalLog.Count - 1);
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

        private T GetService<T>() where T : class
        {
            return GetModel<T>();
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

        // ─── Çözücü ───

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

            _batchSolveStatusMessage = $"Toplu çözücü tamamlandı: {solvableCount} / {_cachedLevels.Count} seviye çözülebilir.";
            Debug.Log($"[PixelFlow] {_batchSolveStatusMessage}");
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
                    Undo.RecordObject(lvl, "Seviye Otomatik Çözüm");
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
            _batchSolveStatusMessage = $"{fixedCount} LevelData varlığına çözüm başarıyla yazıldı.";
            Debug.Log($"[PixelFlow] {_batchSolveStatusMessage}");
            RunBatchSolver();
        }

        // ─── Zorluk Hesaplayıcılar ───

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

            if (status)
            {
                GUILayout.Label("[TAMAM]", _okBadgeStyle, GUILayout.Width(70));
            }
            else
            {
                GUILayout.Label("[EKSİK]", _errorBadgeStyle, GUILayout.Width(70));
                if (GUILayout.Button("Düzelt", GUILayout.Height(18), GUILayout.Width(60)))
                {
                    fixAction.Invoke();
                    RefreshData();
                }
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
            bool canFire = Application.isPlaying;
            GUI.enabled = canFire;
            if (GUILayout.Button(label, GUILayout.Height(22)))
            {
                action?.Invoke();
            }
            GUI.enabled = true;
        }

        private void DrawLevelTableHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Svye", EditorStyles.boldLabel, GUILayout.Width(35));
            GUILayout.Label("İsim", EditorStyles.boldLabel, GUILayout.Width(130));
            GUILayout.Label("Izgara", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("Düğüm", EditorStyles.boldLabel, GUILayout.Width(45));
            GUILayout.Label("Köprü", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("İşlemler", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
        }

        private void DrawLevelTableRow(LevelData lvl, bool showLaunch)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label((lvl.levelIndex + 1).ToString(), GUILayout.Width(35));
            GUILayout.Label(lvl.name, GUILayout.Width(130));
            GUILayout.Label($"{lvl.width}x{lvl.height}", GUILayout.Width(50));
            GUILayout.Label(lvl.initialNodes != null ? lvl.initialNodes.Count.ToString() : "0", GUILayout.Width(45));
            GUILayout.Label(lvl.bridgePositions != null ? lvl.bridgePositions.Count.ToString() : "0", GUILayout.Width(50));

            if (showLaunch)
            {
                GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
                if (GUILayout.Button($"▶ Başlat", GUILayout.Height(20), GUILayout.Width(60)))
                {
                    PlayLevel(lvl);
                }
                GUI.backgroundColor = Color.white;
            }

            if (GUILayout.Button("Seç", GUILayout.Height(18), GUILayout.Width(40)))
            {
                Selection.activeObject = lvl;
                EditorGUIUtility.PingObject(lvl);
            }
            if (GUILayout.Button("Düzenle", GUILayout.Height(18), GUILayout.Width(55)))
            {
                Selection.activeObject = lvl;
                EditorGUIUtility.PingObject(lvl);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawNexusResolveStatus<T>(NexusDI container, string label) where T : class
        {
            GUILayout.BeginHorizontal();
            try
            {
                var instance = container.Resolve<T>();
                if (instance != null)
                {
                    GUILayout.Label("✔", _okBadgeStyle, GUILayout.Width(20));
                    GUILayout.Label(label, GUILayout.Width(280));
                    GUILayout.Label(instance.GetType().Name, EditorStyles.miniLabel);
                }
                else
                {
                    GUILayout.Label("✖", _errorBadgeStyle, GUILayout.Width(20));
                    GUILayout.Label(label, GUILayout.Width(280));
                    GUILayout.Label("null döndü", EditorStyles.miniLabel);
                }
            }
            catch
            {
                GUILayout.Label("⚠", _errorBadgeStyle, GUILayout.Width(20));
                GUILayout.Label(label, GUILayout.Width(280));
                GUILayout.Label("kayıtlı değil", EditorStyles.miniLabel);
            }
            GUILayout.EndHorizontal();
        }

        // ─── Stil Başlatma ───

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
                    _headerStyle.normal.textColor = new Color(0.05f, 0.25f, 0.5f);
            }

            if (_sectionHeaderStyle == null)
            {
                _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13
                };
                if (EditorGUIUtility.isProSkin)
                    _sectionHeaderStyle.normal.textColor = new Color(0.6f, 0.85f, 1f);
                else
                    _sectionHeaderStyle.normal.textColor = new Color(0.1f, 0.35f, 0.6f);
            }

            if (_cardStyle == null)
            {
                _cardStyle = new GUIStyle(GUI.skin.box);
                _cardStyle.padding = new RectOffset(10, 10, 10, 10);
                _cardStyle.margin = new RectOffset(6, 6, 4, 4);
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

            if (_warnBadgeStyle == null)
            {
                _warnBadgeStyle = new GUIStyle(EditorStyles.label);
                _warnBadgeStyle.normal.textColor = new Color(0.9f, 0.6f, 0.1f);
                _warnBadgeStyle.fontStyle = FontStyle.Bold;
            }

            if (_errorBadgeStyle == null)
            {
                _errorBadgeStyle = new GUIStyle(EditorStyles.label);
                _errorBadgeStyle.normal.textColor = new Color(0.85f, 0.2f, 0.18f);
                _errorBadgeStyle.fontStyle = FontStyle.Bold;
            }

            if (_miniInfoStyle == null)
            {
                _miniInfoStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }
        }

        // ═══════════════════════════════════════════════════
        // SAHNE KURULUMU & PREFAB ÜRETİMİ
        // ═══════════════════════════════════════════════════

        private void GeneratePrefabs()
        {
            Debug.Log("[PixelFlow] Temel Prefab'lar oluşturuluyor...");
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

                GameObject warningObj = new GameObject("Warning");
                warningObj.transform.SetParent(cellObj.transform);
                var warningRenderer = warningObj.AddComponent<SpriteRenderer>();

                SerializedObject so = new SerializedObject(cellView);
                so.FindProperty("_bgRenderer").objectReferenceValue = bgRenderer;
                so.FindProperty("_dotRenderer").objectReferenceValue = dotRenderer;
                so.FindProperty("_bridgeRenderer").objectReferenceValue = bridgeRenderer;
                so.FindProperty("_warningRenderer").objectReferenceValue = warningRenderer;
                so.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(cellObj, cellPrefabPath);
                DestroyImmediate(cellObj);
                Debug.Log("[PixelFlow] CellView prefab'ı WarningIcon ile oluşturuldu: " + cellPrefabPath);
            }
        }

        private void FixCellViewWarningIcon()
        {
            string cellPrefabPath = "Assets/Prefabs/CellView.prefab";
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(cellPrefabPath);
            if (prefabAsset != null)
            {
                var instance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
                if (instance != null)
                {
                    var cellView = instance.GetComponent<CellView>();
                    var warningTrans = instance.transform.Find("Warning");
                    SpriteRenderer warningRenderer = null;
                    if (warningTrans == null)
                    {
                        var warningObj = new GameObject("Warning");
                        warningObj.transform.SetParent(instance.transform);
                        warningRenderer = warningObj.AddComponent<SpriteRenderer>();
                    }
                    else
                    {
                        warningRenderer = warningTrans.GetComponent<SpriteRenderer>() ?? warningTrans.gameObject.AddComponent<SpriteRenderer>();
                    }

                    SerializedObject so = new SerializedObject(cellView);
                    so.Update();
                    so.FindProperty("_warningRenderer").objectReferenceValue = warningRenderer;
                    so.ApplyModifiedProperties();

                    PrefabUtility.SaveAsPrefabAsset(instance, cellPrefabPath);
                    DestroyImmediate(instance);
                    Debug.Log("[PixelFlow] CellView.prefab'ta WarningIcon SpriteRenderer referansı otomatik düzeltildi.");
                }
            }
            else
            {
                GeneratePrefabs();
            }
        }

        private void SetupScene()
        {
            Debug.Log("[PixelFlow] Sahne kurulumu başlatılıyor...");

            // 0. Seviyelerin var olduğundan emin ol
            RefreshLevelsCache();
            if (_cachedLevels.Count == 0)
            {
                CreatePhase1And2HandCraftedPack();
                RefreshLevelsCache();
            }

            // Root hierarchy: [PixelFlow]
            GameObject rootObj = GameObject.Find("[PixelFlow]");
            if (rootObj == null)
            {
                rootObj = new GameObject("[PixelFlow]");
                Undo.RegisterCreatedObjectUndo(rootObj, "[PixelFlow] Root oluştur");
            }

            // Kategori parent'ları — EnsureChild ile bul/oluştur
            Transform contextParent = EnsureChild(rootObj.transform, "_Context");
            Transform cameraParent = EnsureChild(rootObj.transform, "_Camera");
            Transform uiParent = EnsureChild(rootObj.transform, "_UI");
            Transform gridParent = EnsureChild(rootObj.transform, "_Grid");
            Transform servicesParent = EnsureChild(rootObj.transform, "_Services");
            Transform bootParent = EnsureChild(rootObj.transform, "_Bootstrapper");

            // 1. Context kurulumu → _Context altına
            Root context = Object.FindAnyObjectByType<Root>();
            if (context == null)
            {
                GameObject contextObj = new GameObject("NexusRoot");
                context = contextObj.AddComponent<Root>();
                contextObj.AddComponent<GameContextLifecycle>();
                contextObj.transform.SetParent(contextParent);
                Undo.RegisterCreatedObjectUndo(contextObj, "Context Oluştur");
            }
            else
            {
                context.transform.SetParent(contextParent);
            }

            if (context != null && context.ContextData == null)
            {
                string settingsFolder = "Assets/Settings";
                if (!AssetDatabase.IsValidFolder(settingsFolder))
                    AssetDatabase.CreateFolder("Assets", "Settings");

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

            // 2. GridView kurulumu → _Grid altına
            GridView gridView = Object.FindAnyObjectByType<GridView>();
            GameObject gridObj;
            if (gridView == null)
            {
                gridObj = new GameObject("GridView");
                gridView = gridObj.AddComponent<GridView>();
                gridObj.transform.SetParent(gridParent);
                Undo.RegisterCreatedObjectUndo(gridObj, "GridView Oluştur");
            }
            else
            {
                gridObj = gridView.gameObject;
                gridObj.transform.SetParent(gridParent);
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

            // 3. UI kurulumu (Canvas & HUDView) → _UI altına
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            GameObject canvasObj;
            if (canvas == null)
            {
                canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                canvasObj.transform.SetParent(uiParent);
                Undo.RegisterCreatedObjectUndo(canvasObj, "Canvas Oluştur");
            }
            else
            {
                canvasObj = canvas.gameObject;
                canvasObj.transform.SetParent(uiParent);
            }

            var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                eventSystemObj.transform.SetParent(uiParent);
                Undo.RegisterCreatedObjectUndo(eventSystemObj, "EventSystem Oluştur");
            }
            else
            {
                eventSystem.transform.SetParent(uiParent);
                var standaloneModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (standaloneModule != null)
                {
                    Undo.DestroyObjectImmediate(standaloneModule);
                    eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }

            // HUDView
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

            // Hint Button
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

            Transform hintTextTransform = hintBtnObj.transform.Find("HintCountText");
            GameObject hintTextObj = hintTextTransform != null ? hintTextTransform.gameObject : new GameObject("HintCountText", typeof(RectTransform));
            hintTextObj.transform.SetParent(hintBtnObj.transform, false);
            Text hintText = hintTextObj.GetComponent<Text>() ?? hintTextObj.AddComponent<Text>();
            hintText.text = "İPUCU (3)";
            hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            hintText.fontSize = 18;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = Color.white;
            RectTransform textRect = hintTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // Completion Panel
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

            // 4. SoundHandlerView → _Services
            SoundHandlerView soundView = Object.FindAnyObjectByType<SoundHandlerView>();
            if (soundView == null)
            {
                GameObject soundObj = new GameObject("SoundHandlerView");
                soundView = soundObj.AddComponent<SoundHandlerView>();
                soundObj.transform.SetParent(servicesParent);
                Undo.RegisterCreatedObjectUndo(soundObj, "SoundHandlerView Oluştur");
            }
            else
            {
                soundView.transform.SetParent(servicesParent);
            }

            // 5. ThemeHandlerView → _Services
            ThemeHandlerView themeView = Object.FindAnyObjectByType<ThemeHandlerView>();
            if (themeView == null)
            {
                GameObject themeObj = new GameObject("ThemeHandlerView");
                themeView = themeObj.AddComponent<ThemeHandlerView>();
                themeObj.transform.SetParent(servicesParent);
                Undo.RegisterCreatedObjectUndo(themeObj, "ThemeHandlerView Oluştur");
            }
            else
            {
                themeView.transform.SetParent(servicesParent);
            }

            // 6. GameBootstrapper → _Bootstrapper
            GameBootstrapper bootstrapper = Object.FindAnyObjectByType<GameBootstrapper>();
            if (bootstrapper == null)
            {
                GameObject bootObj = new GameObject("GameBootstrapper");
                bootstrapper = bootObj.AddComponent<GameBootstrapper>();
                bootObj.transform.SetParent(bootParent);
                Undo.RegisterCreatedObjectUndo(bootObj, "GameBootstrapper Oluştur");
            }
            else
            {
                bootstrapper.transform.SetParent(bootParent);
            }

            // 7. SplashView → Canvas altına (_UI > Canvas)
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
            else
            {
                splashView.transform.SetParent(canvasObj.transform, false);
            }

            // 8. Direct puzzle boot: Bootstrapper handles Playing → Playing state transition

            // Bootstrapper referansları
            bootstrapper.initialLevel = ResolveLevelByIndex(0);
            if (bootstrapper.nexusRoot == null)
            {
                bootstrapper.nexusRoot = context;
            }
            EditorUtility.SetDirty(bootstrapper);

            // 9. Ana Kamera kurulumu → _Camera altına
            Camera mainCam = Camera.main;
            if (mainCam == null)
                mainCam = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);

            if (mainCam != null)
            {
                mainCam.gameObject.name = "MainCamera";
                mainCam.transform.SetParent(cameraParent);
                mainCam.orthographic = true;
                mainCam.orthographicSize = 5;
                if (mainCam.GetComponent<CameraController>() == null)
                    mainCam.gameObject.AddComponent<CameraController>();
                EditorUtility.SetDirty(mainCam.gameObject);
            }
            else
            {
                GameObject camObj = new GameObject("MainCamera");
                camObj.transform.SetParent(cameraParent);
                mainCam = camObj.AddComponent<Camera>();
                mainCam.orthographic = true;
                mainCam.orthographicSize = 5;
                camObj.AddComponent<CameraController>();
                Undo.RegisterCreatedObjectUndo(camObj, "MainCamera Oluştur");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[PixelFlow] Sahne kurulumu başarıyla tamamlandı.");
        }

        // ─── Yardımcı: Parent altında child bul veya oluştur ───
        private Transform EnsureChild(Transform parent, string childName)
        {
            var existing = parent.Find(childName);
            if (existing != null) return existing;
            var go = new GameObject(childName);
            go.transform.SetParent(parent);
            Undo.RegisterCreatedObjectUndo(go, $"Parent '{childName}' oluştur");
            return go.transform;
        }

        private void SetupExtendedViews()
        {
            Debug.Log("[PixelFlow] Genişletilmiş View bileşenleri kuruluyor...");

            // Kök hierarchy'yi bul
            GameObject rootObj = GameObject.Find("[PixelFlow]");
            Transform servicesParent = rootObj != null ? EnsureChild(rootObj.transform, "_Services") : null;

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            GameObject canvasObj = canvas != null ? canvas.gameObject : null;

            // Servis View'leri → _Services altına
            CreateServiceViewIfMissing<DailyCrisisView>("DailyCrisisView", servicesParent);
            CreateServiceViewIfMissing<ConfettiView>("ConfettiView", servicesParent);
            CreateServiceViewIfMissing<BloomFlashView>("BloomFlashView", servicesParent);

            // UI tabanlı View'ler (Canvas altına)
            if (canvasObj != null)
            {
                CreateUIViewIfMissing<TutorialView>("TutorialView", canvasObj);
                CreateUIViewIfMissing<SettingsView>("SettingsView", canvasObj);
            }
            else
            {
                Debug.LogWarning("[PixelFlow] Canvas bulunamadı. Önce temel sahne kurulumunu çalıştırın.");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[PixelFlow] Genişletilmiş View'ler kurulumu tamamlandı.");
        }

        private void CreateServiceViewIfMissing<T>(string name, Transform parent) where T : Component
        {
            var existing = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (existing == null)
            {
                var obj = new GameObject(name);
                obj.AddComponent<T>();
                if (parent != null) obj.transform.SetParent(parent);
                Undo.RegisterCreatedObjectUndo(obj, $"{name} Oluştur");
                Debug.Log($"[PixelFlow] {name} oluşturuldu.");
            }
            else
            {
                if (parent != null) existing.transform.SetParent(parent);
            }
        }

        private void CreateUIViewIfMissing<T>(string name, GameObject parent) where T : Component
        {
            var existing = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (existing == null)
            {
                var obj = new GameObject(name, typeof(RectTransform));
                obj.transform.SetParent(parent.transform, false);
                obj.AddComponent<T>();
                var rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                obj.SetActive(false); // UI Panelleri varsayılan olarak kapalı durur
                Undo.RegisterCreatedObjectUndo(obj, $"{name} Oluştur");
                Debug.Log($"[PixelFlow] {name} UI bileşeni Canvas altında başarıyla oluşturuldu.");
            }
            else
            {
                Debug.Log($"[PixelFlow] {name} zaten sahnede mevcut ({existing.gameObject.name}).");
            }
        }

        private void SetupGlobalVolume()
        {
            if (!_globalVolumeOk)
            {
                var obj = new GameObject("Global Volume");
                Undo.RegisterCreatedObjectUndo(obj, "Global Volume Oluştur");
                Debug.Log("[PixelFlow] Global Volume nesnesi oluşturuldu.");
            }
        }

        private void SetupCameraController()
        {
            var targetCam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            if (targetCam != null)
            {
                targetCam.orthographic = true;
                targetCam.orthographicSize = 5;
                if (targetCam.GetComponent<CameraController>() == null)
                {
                    targetCam.gameObject.AddComponent<CameraController>();
                    Undo.RegisterCreatedObjectUndo(targetCam.gameObject, "CameraController Ekle");
                    EditorUtility.SetDirty(targetCam);
                    Debug.Log($"[PixelFlow] CameraController bileşeni '{targetCam.gameObject.name}' nesnesine eklendi.");
                }
                else
                {
                    Debug.Log($"[PixelFlow] CameraController zaten '{targetCam.gameObject.name}' üzerinde mevcut.");
                }
            }
            else
            {
                Debug.LogWarning("[PixelFlow] Sahnede Kamera bulunamadı. Lütfen bir Camera nesnesi ekleyin.");
            }
        }

        // ═══════════════════════════════════════════════════
        // SEVİYE OLUŞTURMA
        // ═══════════════════════════════════════════════════

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

            Debug.Log($"[PixelFlow] Boş seviye indeks {index} ({w}x{h}) oluşturuldu: {path}");
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void CreateThreeLevelPack()
        {
            string folder = "Assets/Resources/Levels";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // Seviye 1
            LevelData lvl1 = ScriptableObject.CreateInstance<LevelData>();
            lvl1.levelIndex = 0;
            lvl1.width = 5;
            lvl1.height = 5;
            lvl1.flowScoreThreshold = 5;
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

            // Seviye 2
            LevelData lvl2 = ScriptableObject.CreateInstance<LevelData>();
            lvl2.levelIndex = 1;
            lvl2.width = 5;
            lvl2.height = 5;
            lvl2.flowScoreThreshold = 5;
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

            // Seviye 3
            LevelData lvl3 = ScriptableObject.CreateInstance<LevelData>();
            lvl3.levelIndex = 2;
            lvl3.width = 5;
            lvl3.height = 5;
            lvl3.flowScoreThreshold = 5;
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

            // Seviye Paketi
            LevelPack pack = ScriptableObject.CreateInstance<LevelPack>();
            pack.packName = "5x5 Başlangıç Paketi";
            pack.levels = new System.Collections.Generic.List<LevelData> { lvl1, lvl2, lvl3 };
            AssetDatabase.CreateAsset(pack, $"{folder}/MainLevelPack.asset");

            AssetDatabase.SaveAssets();
            Debug.Log("[PixelFlow] Seviye 1, Seviye 2, Seviye 3 ve MainLevelPack.asset başarıyla oluşturuldu.");
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
                    Debug.LogWarning($"[PixelFlow] Seviye {idx} üretilemedi ({param.gridWidth}x{param.gridHeight}/{param.colorCount}r). Farklı tohum ile yeniden deneniyor...");
                    generator = new Services.ProceduralLevelGenerator(solver, seed: 1000 + idx * 17);
                    level = generator.Generate(param, maxAttempts: 100);
                }
                if (level == null) continue;

                level.levelIndex = idx;
                level.flowScoreThreshold = Mathf.Clamp(param.colorCount * 5, 5, 10);

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
            pack.packName = "Neon Transit Faz 1+2 (12 Seviye)";
            pack.levels = new System.Collections.Generic.List<LevelData>(allLevels);
            string packPath = $"{folder}/MainLevelPack.asset";
            AssetDatabase.DeleteAsset(packPath);
            AssetDatabase.CreateAsset(pack, packPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[PixelFlow] {allLevels.Length} el yapımı tarzda seviye üretildi (Faz 1+2).");
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
                Debug.LogError($"[PixelFlow] Zorluk {difficultyIndex} ile seviye üretilemedi.");
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

            Debug.Log($"[PixelFlow] Prosedürel seviye {levelIndex} ({param.gridWidth}x{param.gridHeight}, {param.colorCount} renk, {param.bridgeCount} köprü) oluşturuldu: {path}");
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
            Debug.Log($"[PixelFlow] {successCount}/{count} prosedürel seviye üretildi (zorluk {difficultyIndex}, başlangıç={startIndex}).");
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

        private void SetVehicleStyle(VehicleStyle style)
        {
            var settings = GetModel<ISettingsModel>();
            if (settings != null)
            {
                settings.SetVehicleStyle(style);
            }
            else
            {
                PlayerPrefs.SetInt("VehicleStyle", (int)style);
                PlayerPrefs.Save();
            }
            var sim = GetService<IVehicleSimulator>();
            if (sim != null)
            {
                sim.ClearAllVehicles();
            }
            Debug.Log($"[PixelFlow] Araç stili değiştirildi: {style}");
        }
    }
}
#endif
