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
        // SEKME 0: OYUN KONTROL MERKEZİ
        // ═══════════════════════════════════════════════════

        private void DrawGameControllerTab()
        {
            DrawGameStatusCard();
            DrawLiveGameControls();
            DrawSignalTriggerPanel();
            DrawVehicleStyleSelector();
            DrawQuickLevelLauncher();
            DrawBootstrapperConfig();
        }

        private void DrawGameStatusCard()
        {
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
            string bootName = Object.FindAnyObjectByType<GameBootstrapper>()?.initialLevel?.name;
            string currentLvlInfo = levelModel?.CurrentLevel != null
                ? $"Seviye {levelModel.CurrentLevel.levelIndex + 1} ({levelModel.CurrentLevel.name})"
                : bootName != null ? $"Başlangıç: {bootName}" : "Yok";

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
            if (GUILayout.Button(isPlaying ? "▶ Açık Seviyeyi Yeniden Yükle" : "▶ Oyunu Başlat (Seviye 1)", GUILayout.Height(32)))
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
            if (GUILayout.Button("🏆 Seviyeyi Tamamla (Kazan)", GUILayout.Height(32))) CompleteCurrentLevel();
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("🔁 Seviyeyi Yeniden Başlat", GUILayout.Height(32))) RestartCurrentLevel();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("💡 Bedava İpucu Ver", GUILayout.Height(28))) DispatchSignal(new RequestHintSignal());
            if (GUILayout.Button("↩️ Geri Al", GUILayout.Height(28))) DispatchSignal(new UndoSignal());
            if (GUILayout.Button("↪️ Yinele", GUILayout.Height(28))) DispatchSignal(new RedoSignal());
            if (GUILayout.Button("🧹 Temiz Seviye Yükle (Save Temizle)", GUILayout.Height(28)))
            {
                var prefs = GetPrefsService();
                prefs.DeleteKey("NT_PuzzleSave_");
                prefs.Save();
                var lvl1 = ResolveLevelByIndex(0);
                if (lvl1 != null) PlayLevel(lvl1);
                Debug.Log("[PixelFlow] Save file cleared! Clean LevelData reloaded.");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔓 Tüm Seviyeleri Aç", GUILayout.Height(28))) UnlockAllLevels();
            if (GUILayout.Button("🔒 İlerlemeyi Sıfırla", GUILayout.Height(28))) ResetProgress();
            if (GUILayout.Button("💾 Zorla Kaydet", GUILayout.Height(28))) ForceSaveGame();
            if (GUILayout.Button("🗑️ Tüm Kayıtları Sil", GUILayout.Height(28))) WipeSaveData();
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
                : PlayerPrefs.GetInt("VehicleStyle", 0);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Aktif Araç Modeli:", GUILayout.Width(140));
            GUI.backgroundColor = currentStyleInt == 0 ? new Color(0.2f, 0.7f, 1f) : Color.white;
            if (GUILayout.Button("🚘 Araba", GUILayout.Height(28))) SetVehicleStyle(VehicleStyle.Car);
            GUI.backgroundColor = currentStyleInt == 1 ? new Color(0.9f, 0.7f, 0.2f) : Color.white;
            if (GUILayout.Button("🚆 Tren / Ekspres", GUILayout.Height(28))) SetVehicleStyle(VehicleStyle.Train);
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
                EditorGUILayout.HelpBox("GameBootstrapper bulunamadı. Tanılama sekmesinden oluşturabilirsiniz.", MessageType.Warning);
            GUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        // SEKME 1: SAHNE TANILAMA
        // ═══════════════════════════════════════════════════

        private void DrawDiagnosticsTab()
        {
            DrawCoreDiagnostics();
            DrawExtendedViewDiagnostics();
            DrawEnvironmentDiagnostics();
            DrawHierarchySummary();
            DrawQuickFixPanel();
            DrawDevTools();
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

            DrawDiagnosticRow("DailyCrisisView (Günlük Kriz)", _dailyCrisisOk, SetupExtendedViews);
            DrawDiagnosticRow("ConfettiView (Kutlama Efekti)", _confettiOk, SetupExtendedViews);
            DrawDiagnosticRow("BloomFlashView (Işık Patlaması)", _bloomFlashOk, SetupExtendedViews);
            DrawDiagnosticRow("TutorialView (Eğitim Sistemi)", _tutorialOk, SetupExtendedViews);
            DrawDiagnosticRow("SettingsView (Ayarlar Paneli)", _settingsViewOk, SetupExtendedViews);

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
            if (GUILayout.Button("🚀 Tek Tıkla Tam Sahne Kurulumu", GUILayout.Height(35)))
            {
                GeneratePrefabs(); SetupScene(); SetupExtendedViews(); SetupGlobalVolume(); SetupCameraController();
                RefreshData();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (!allCoreOk && GUILayout.Button("Temel Bileşenleri Kur", GUILayout.Height(28))) { GeneratePrefabs(); SetupScene(); RefreshData(); }
            if (!allExtOk && GUILayout.Button("Genişletilmiş View'leri Kur", GUILayout.Height(28))) { SetupExtendedViews(); RefreshData(); }
            if (!allEnvOk && GUILayout.Button("Ortam Bileşenlerini Kur", GUILayout.Height(28))) { SetupGlobalVolume(); SetupCameraController(); RefreshData(); }
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
            if (GUILayout.Button("🗑️ PlayerPrefs & Kayıt Verilerini Temizle", GUILayout.Height(28))) WipeSaveData();
            if (GUILayout.Button("📂 Sahneyi Kaydet", GUILayout.Height(28)))
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
