#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Views;
using PixelFlow.Data;
using PixelFlow.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using System.Linq;

namespace PixelFlow.Editor
{
    partial class PixelFlowSetupWindow
    {
        // ═══════════════════════════════════════════════════
        // SEKME 0-1
        // ═══════════════════════════════════════════════════

        private void DrawGameControllerTab()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawGameStatusCard();
            DrawLiveGameControls();
            DrawVehicleSimulatorControls();
            DrawFrameStepControl();
            DrawSignalTriggerPanel();
            DrawVehicleStyleSelector();
            DrawQuickLevelLauncher();
            DrawBootstrapperConfig();
            EditorGUILayout.EndScrollView();
        }

        private void DrawGameStatusCard()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("📊 Canlı Oyun Durumu İzleyici", _sectionHeaderStyle);
            GUILayout.Space(5);

            bool isPlaying = Application.isPlaying;
            string playStatus = isPlaying ? "▶ OYNANIYOR (Canlı)" : "⏸ DÜZENLEME MODU (Durmuş)";
            // GUIStyle cached in _okBadgeStyle/_warnBadgeStyle — inline color overrides for status

            GUILayout.BeginHorizontal();
            GUILayout.Label("Motor Durumu:", GUILayout.MinWidth(110));
            GUILayout.Label(playStatus, isPlaying ? _okBadgeStyle : _warnBadgeStyle);
            GUILayout.EndHorizontal();

            var stateModel = GetModel<IGameStateModel>();
            var levelModel = GetModel<ILevelModel>();
            var progressModel = GetModel<IProgressModel>();
            var sessionModel = GetModel<IGameSessionModel>();
            var hintModel = GetModel<IHintModel>();

            string currentState = stateModel != null ? stateModel.CurrentState.ToString() : "Başlatılmadı";
            string bootName = Object.FindAnyObjectByType<GameBootstrapper>()?.initialLevel?.name;
            string currentLvlInfo = levelModel?.CurrentLevel != null
                ? $"Seviye {levelModel.CurrentLevel.levelIndex + 1} ({levelModel.CurrentLevel.name})"
                : bootName != null ? $"Başlangıç: {bootName}" : "Yok";

            int unlockedLvl = progressModel != null ? progressModel.UnlockedLevels : (GetPrefsService()?.GetInt("NT_UnlockedLevels", 1) ?? PlayerPrefs.GetInt("NT_UnlockedLevels", 1));
            int hints = hintModel != null ? hintModel.HintsRemaining : -1;
            float elapsed = sessionModel != null ? sessionModel.ElapsedTime : 0f;

            DrawInfoRow("Oyun Durumu:", currentState);
            DrawInfoRow("Yüklü Seviye:", currentLvlInfo);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Açık Seviye:", GUILayout.MinWidth(110));
            GUILayout.Label($"Seviye {unlockedLvl + 1}", EditorStyles.boldLabel, GUILayout.MinWidth(100));
            if (hints >= 0)
            {
                GUILayout.Label("İpucu:", GUILayout.MinWidth(45));
                GUILayout.Label($"{hints}", EditorStyles.boldLabel);
            }
            GUILayout.EndHorizontal();

            if (isPlaying && elapsed > 0)
                DrawInfoRow("Geçen Süre:", $"{(int)(elapsed / 60)}dk {(int)(elapsed % 60)}sn");

            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawLiveGameControls()
        {
            bool isPlaying = Application.isPlaying;
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🎮 Canlı Oyun Kontrolleri", _sectionHeaderStyle);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = isPlaying ? new Color(0.2f, 0.7f, 1f) : new Color(0.2f, 0.8f, 0.3f);
            if (GUILayout.Button(isPlaying ? "▶ Açık Seviyeyi Yeniden Yükle" : "▶ Oyunu Başlat (Seviye 1)", GUILayout.MinHeight(32)))
            {
                if (!isPlaying) EditorApplication.isPlaying = true;
                else
                {
                    var pModel = GetModel<IProgressModel>();
                    var lvl = ResolveLevelByIndex(pModel?.UnlockedLevels ?? 0);
                    if (lvl != null) PlayLevel(lvl);
                }
            }
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.3f);
            if (GUILayout.Button("🏆 Seviyeyi Tamamla (Kazan)", GUILayout.MinHeight(32))) CompleteCurrentLevel();
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("🔁 Seviyeyi Yeniden Başlat", GUILayout.MinHeight(32))) RestartCurrentLevel();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("💡 Bedava İpucu Ver", GUILayout.MinHeight(28))) DispatchSignal(new RequestHintSignal());
            if (GUILayout.Button("↩️ Geri Al", GUILayout.MinHeight(28))) DispatchSignal(new UndoSignal());
            if (GUILayout.Button("↪️ Yinele", GUILayout.MinHeight(28))) DispatchSignal(new RedoSignal());
            if (GUILayout.Button("🧹 Temiz Seviye Yükle (Save Temizle)", GUILayout.MinHeight(28)))
            {
                var prefs = GetPrefsService();
                if (prefs != null) { prefs.DeleteKey("NT_PuzzleSave_"); prefs.Save(); }
                var lvl1 = ResolveLevelByIndex(0);
                if (lvl1 != null) PlayLevel(lvl1);
                Debug.Log("[PixelFlow] Save file cleared! Clean LevelData reloaded.");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔓 Tüm Seviyeleri Aç", GUILayout.MinHeight(28))) UnlockAllLevels();
            if (GUILayout.Button("🔒 İlerlemeyi Sıfırla", GUILayout.MinHeight(28))) ResetProgress();
            if (GUILayout.Button("💾 Zorla Kaydet", GUILayout.MinHeight(28))) ForceSaveGame();
            if (GUILayout.Button("🗑️ Tüm Kayıtları Sil", GUILayout.MinHeight(28))) WipeSaveData();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawVehicleSimulatorControls()
        {
            bool isPlaying = Application.isPlaying;
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🚗 VehicleSimulator Canlı Kontrol", _sectionHeaderStyle);
            GUILayout.Space(4);

            if (!isPlaying)
            {
                EditorGUILayout.HelpBox("Araç simülasyonu kontrolleri yalnızca Play Mode'da kullanılabilir.", MessageType.Info);
                GUILayout.EndVertical();
                return;
            }

            // Resolve GameConfig from Nexus container
            var root = Object.FindAnyObjectByType<Root>();
            GameConfig config = null;
            if (root?.IsInitialized == true && root.Context != null)
            {
                try { config = root.Context.Container.Resolve<GameConfig>(); }
                catch { /* GameConfig henüz Nexus konteynerine bağlı değil; repaint sırasında beklenen durum */ }
            }

            // Initialize cached config values as soon as config is resolved
            if (config != null)
            {
                if (_cachedBaseSpeed < 0f) _cachedBaseSpeed = config.VehicleSpeed;
                if (_cachedBaseSpawnInterval < 0f) _cachedBaseSpawnInterval = config.SpawnInterval;
            }

            // Speed Multiplier Slider
            GUILayout.BeginHorizontal();
            GUILayout.Label("🚀 Hız Çarpanı:", GUILayout.MinWidth(110));
            _vehicleSpeedMultiplier = GUILayout.HorizontalSlider(_vehicleSpeedMultiplier, 0.1f, 5.0f, GUILayout.MinWidth(120));
            GUILayout.Label($"{_vehicleSpeedMultiplier:F1}x", EditorStyles.boldLabel, GUILayout.MinWidth(40));

            if (config != null)
            {

                float effectiveSpeed = _cachedBaseSpeed * _vehicleSpeedMultiplier;
                GUILayout.Label($"({effectiveSpeed:F1} br/sn)", EditorStyles.miniLabel, GUILayout.MinWidth(70));

                // Apply speed to GameConfig for new spawns
                float newSpeed = _cachedBaseSpeed * _vehicleSpeedMultiplier;
                if (!Mathf.Approximately(config.VehicleSpeed, newSpeed))
                {
                    config.VehicleSpeed = newSpeed;
                    config.SpawnInterval = Mathf.Max(0.3f, _cachedBaseSpawnInterval / Mathf.Max(_vehicleSpeedMultiplier, 0.1f));
                    EditorUtility.SetDirty(config);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(110);
            GUILayout.Label("💡 Yeni spawn olan araçları etkiler, yoldakilere uygulanmaz.", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();

            // Spawn Toggle
            GUILayout.BeginHorizontal();
            GUILayout.Label("🐣 Araç Spawn'ı:", GUILayout.MinWidth(110));
            bool newSpawnState = GUILayout.Toggle(_vehicleSpawnEnabled, _vehicleSpawnEnabled ? "AÇIK" : "KAPALI", GUILayout.MinWidth(80));
            if (newSpawnState != _vehicleSpawnEnabled)
            {
                _vehicleSpawnEnabled = newSpawnState;
                if (config != null)
                {
                    config.SpawnInterval = _vehicleSpawnEnabled ? Mathf.Max(0.3f, 1.2f / _vehicleSpeedMultiplier) : 9999f;
                    EditorUtility.SetDirty(config);
                    Debug.Log($"[PixelFlow] Vehicle spawn {( _vehicleSpawnEnabled ? "AÇILDI" : "KAPANDI" )}");
                }
            }
            GUILayout.EndHorizontal();

            // Clear Vehicles Button
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🧹 Tüm Araçları Temizle", GUILayout.MinHeight(24)))
            {
                var sim = GetService<IVehicleSimulator>();
                sim?.ClearAllVehicles();
                Debug.Log("[PixelFlow] All vehicles cleared.");
            }
            if (GUILayout.Button("🔄 Simülasyonu Sıfırla", GUILayout.MinHeight(24)))
            {
                var sim = GetService<IVehicleSimulator>();
                sim?.StopSimulationPhase();
                sim?.ClearAllVehicles();
                Debug.Log("[PixelFlow] Simulation reset.");
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawFrameStepControl()
        {
            bool isPlaying = Application.isPlaying;
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("⏭️ Frame Step (Debug)", _sectionHeaderStyle);
            GUILayout.Space(4);

            if (!isPlaying)
            {
                EditorGUILayout.HelpBox("Frame Step yalnızca Play Mode'da kullanılabilir.", MessageType.Info);
                GUILayout.EndVertical();
                GUILayout.Space(8);
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("⏸ Önce Durdur", GUILayout.MinHeight(28)))
            {
                Time.timeScale = 0f;
                _frameStepQueued = false;
                Debug.Log("[PixelFlow] Time scale = 0x (Durduruldu)");
            }
            if (GUILayout.Button("⏭ Bir Frame İlerlet", GUILayout.MinHeight(28)))
            {
                EditorApplication.Step();
                Time.timeScale = 0f;
                _frameStepQueued = true;
                Debug.Log("[PixelFlow] Frame Step: 1 frame ilerletildi.");
            }
            if (GUILayout.Button("▶ 5 Frame İlerlet", GUILayout.MinHeight(28)))
            {
                for (int i = 0; i < 5; i++)
                    EditorApplication.Step();
                Debug.Log("[PixelFlow] Frame Step: 5 frame ilerletildi.");
            }
            if (GUILayout.Button("▶ Devam Et (1x)", GUILayout.MinHeight(28)))
            {
                Time.timeScale = 1f;
                _frameStepQueued = false;
                Debug.Log("[PixelFlow] Time scale = 1x (Devam)");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"⏱ Zaman Ölçeği: {Time.timeScale:F2}x", EditorStyles.boldLabel);
            GUILayout.Label(_frameStepQueued ? "🎯 Frame Step - Hazır" : "", _okBadgeStyle);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawSignalTriggerPanel()
        {
            GUILayout.BeginVertical(_cardStyle);
            _signalPanelOpen = EditorGUILayout.Foldout(_signalPanelOpen, "📡 Gelişmiş Sinyal Tetikleyici", true, EditorStyles.foldoutHeader);
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
        }

        private void DrawVehicleStyleSelector()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🚗 Araç Modeli ve Görsel Stili", _sectionHeaderStyle);
            GUILayout.Space(4);

            var settingsModel = GetModel<ISettingsModel>();
            int currentStyleInt = settingsModel != null
                ? (int)settingsModel.CurrentVehicleStyle
                : (GetPrefsService()?.GetInt("VehicleStyle", 0) ?? PlayerPrefs.GetInt("VehicleStyle", 0));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Aktif Araç Modeli:", GUILayout.MinWidth(140));
            GUI.backgroundColor = currentStyleInt == 0 ? new Color(0.2f, 0.7f, 1f) : Color.white;
            if (GUILayout.Button("🚘 Araba", GUILayout.MinHeight(28))) SetVehicleStyle(VehicleStyle.Car);
            GUI.backgroundColor = currentStyleInt == 1 ? new Color(0.9f, 0.7f, 0.2f) : Color.white;
            if (GUILayout.Button("🚆 Tren / Ekspres", GUILayout.MinHeight(28))) SetVehicleStyle(VehicleStyle.Train);
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawQuickLevelLauncher()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label($"⚡ Hızlı Seviye Başlatıcı ({_cachedLevels.Count} Seviye)", _sectionHeaderStyle);
            GUILayout.Label("Herhangi bir seviyeyi anında Play Mode'da başlatmak için 'Başlat'a tıklayın.", EditorStyles.miniLabel);
            GUILayout.Space(6);

            if (_cachedLevels.Count == 0)
            {
                EditorGUILayout.HelpBox("Hiç LevelData varlığı bulunamadı.", MessageType.Warning);
                if (GUILayout.Button("Faz 1+2 Seviye Paketi Oluştur (12 Seviye)", GUILayout.MinHeight(30)))
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
                    if (_cachedLevels[i] != null)
                        DrawLevelTableRow(_cachedLevels[i], true);
                }
            }
            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawBootstrapperConfig()
        {
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

                if (GUILayout.Button("Seviye 1'i Başlangıç Hedefi Olarak Ata", GUILayout.MinHeight(24)))
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
                EditorGUILayout.HelpBox("GameBootstrapper bulunamadı. Tanılama sekmesinden oluşturabilirsiniz.", MessageType.Warning);
            GUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        // SEKME 1: SAHNE TANILAMA
        // ═══════════════════════════════════════════════════

        private void DrawDiagnosticsTab()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawCoreDiagnostics();
            DrawExtendedViewDiagnostics();
            DrawEnvironmentDiagnostics();
            DrawHierarchySummary();
            DrawQuickFixPanel();
            DrawDevTools();
            EditorGUILayout.EndScrollView();
        }

        private void DrawCoreDiagnostics()
        {
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
        }

        private void DrawExtendedViewDiagnostics()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🖼️ Genişletilmiş View Bileşenleri", _sectionHeaderStyle);
            GUILayout.Space(5);

            DrawDiagnosticRow("DailyCrisisView (Günlük Kriz)", _dailyCrisisOk, SetupScene);
            DrawDiagnosticRow("ConfettiView (Kutlama Efekti)", _confettiOk, SetupScene);
            DrawDiagnosticRow("BloomFlashView (Işık Patlaması)", _bloomFlashOk, SetupScene);
            DrawDiagnosticRow("TutorialView (Eğitim Sistemi)", _tutorialOk, SetupScene);
            DrawDiagnosticRow("SettingsView (Ayarlar Paneli)", _settingsViewOk, SetupScene);

            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawEnvironmentDiagnostics()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🌐 Ortam & Kamera Bileşenleri", _sectionHeaderStyle);
            GUILayout.Space(5);

            DrawDiagnosticRow("Global Volume (Post-Processing)", _globalVolumeOk, SetupGlobalVolume);
            DrawDiagnosticRow("Kamera Kontrolcüsü", _cameraControllerOk, SetupCameraController);

            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawHierarchySummary()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("📋 Sahne Hiyerarşi Özeti", _sectionHeaderStyle);
            GUILayout.Space(5);

            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
            int activeCount = allObjects.Count(go => go.activeInHierarchy);
            int viewCount = Object.FindObjectsByType<View>(FindObjectsInactive.Exclude).Length;
            int rootCount = Object.FindObjectsByType<Root>(FindObjectsInactive.Exclude).Length;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Toplam Obje: {allObjects.Length}", GUILayout.MinWidth(160));
            GUILayout.Label($"Aktif: {activeCount}", GUILayout.MinWidth(100));
            GUILayout.Label($"Pasif: {allObjects.Length - activeCount}", GUILayout.MinWidth(100));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Nexus View: {viewCount}", GUILayout.MinWidth(160));
            GUILayout.Label($"Nexus Root: {rootCount}", GUILayout.MinWidth(100));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawQuickFixPanel()
        {
            bool allCoreOk = _prefabsOk && _cellWarningIconOk && _rootOk && _contextDataOk && _gridViewOk &&
                            _canvasOk && _hudOk && _eventSystemOk && _soundOk && _themeOk && _bootstrapperOk && _levelsOk;
            bool allExtOk = _dailyCrisisOk && _confettiOk && _bloomFlashOk && _tutorialOk && _settingsViewOk;
            bool allEnvOk = _globalVolumeOk && _cameraControllerOk;

            if (allCoreOk && allExtOk && allEnvOk)
            {
                EditorGUILayout.HelpBox("✔ Her şey mükemmel yapılandırılmış. Oynamaya hazır!", MessageType.Info);
                return;
            }

            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🔧 Hızlı Düzeltme Araçları", _sectionHeaderStyle);
            GUILayout.Space(5);

            GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
            if (GUILayout.Button("🚀 Tek Tıkla Tam Sahne Kurulumu", GUILayout.MinHeight(35)))
            {
                GeneratePrefabs(); SetupScene(); SetupGlobalVolume(); SetupCameraController();
                RefreshData();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (!allCoreOk && GUILayout.Button("Temel Bileşenleri Kur", GUILayout.MinHeight(28))) { GeneratePrefabs(); SetupScene(); RefreshData(); }
            if (!allExtOk && GUILayout.Button("Genişletilmiş View'leri Kur", GUILayout.MinHeight(28))) { SetupScene(); RefreshData(); }
            if (!allEnvOk && GUILayout.Button("Ortam Bileşenlerini Kur", GUILayout.MinHeight(28))) { SetupGlobalVolume(); SetupCameraController(); RefreshData(); }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawDevTools()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🛠️ Geliştirici Hızlı Araçları", _sectionHeaderStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🗑️ PlayerPrefs & Kayıt Verilerini Temizle", GUILayout.MinHeight(28))) WipeSaveData();
            if (GUILayout.Button("📂 Sahneyi Kaydet", GUILayout.MinHeight(28)))
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                Debug.Log("[PixelFlow] Açık sahneler kaydedildi.");
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }
}
#endif
