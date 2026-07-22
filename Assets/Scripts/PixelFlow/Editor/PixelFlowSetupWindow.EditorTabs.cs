#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Services;
using PixelFlow.Models;
using PixelFlow.Views;
using PixelFlow.Signals;
using System.Collections.Generic;
using System.Linq;

namespace PixelFlow.Editor
{
    partial class PixelFlowSetupWindow
    {
        // ═══════════════════════════════════════════════════
        // SEKME 2: SEVİYE STÜDYOSU
        // ═══════════════════════════════════════════════════

        private void DrawLevelStudioTab()
        {
            DrawCustomLevelCreator();
            DrawProceduralGenerator();
            DrawLevelDatabase();
        }

        private void DrawCustomLevelCreator()
        {
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
        }

        private void DrawProceduralGenerator()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🎲 Prosedürel Seviye Üreteci", _sectionHeaderStyle);
            GUILayout.Space(5);

            _procSelectedDifficulty = GUILayout.SelectionGrid(_procSelectedDifficulty, _procDifficultyNames.Split('|'), 5, GUILayout.Height(22));
            _procUseSeed = EditorGUILayout.Toggle("Sabit Tohum Kullan", _procUseSeed);
            if (_procUseSeed) _procSeed = EditorGUILayout.IntField("Tohum Değeri", _procSeed);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Tekli Üret", GUILayout.Height(28)))
            {
                GenerateProceduralLevel(_procSelectedDifficulty, _procUseSeed ? _procSeed : (int?)null, _newLevelIndex);
                RefreshData();
            }
            _procStartIndex = EditorGUILayout.IntField("Başlangıç", _procStartIndex, GUILayout.Width(70));
            _procBatchCount = EditorGUILayout.IntField("Adet", _procBatchCount, GUILayout.Width(60));
            if (GUILayout.Button("Toplu Üret", GUILayout.Height(28)))
            {
                GenerateProceduralBatch(_procSelectedDifficulty, _procUseSeed ? _procSeed : (int?)null, _procStartIndex, _procBatchCount);
                RefreshData();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawLevelDatabase()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label($"📁 Proje Seviye Kaydı ({_cachedLevels.Count} Seviye)", _sectionHeaderStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("3 Seviyeli Başlangıç Paketi Oluştur", GUILayout.Height(25))) { CreateThreeLevelPack(); RefreshData(); }
            if (GUILayout.Button("Faz 1+2 El Yapımı Paket (12 seviye)", GUILayout.Height(25))) { CreatePhase1And2HandCraftedPack(); RefreshData(); }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            if (_cachedLevels.Count == 0)
                GUILayout.Label("Bu projede hiç LevelData varlığı bulunamadı.", EditorStyles.miniLabel);
            else
            {
                DrawLevelTableHeader();
                for (int i = 0; i < _cachedLevels.Count; i++)
                {
                    if (_cachedLevels[i] != null)
                        DrawLevelTableRow(_cachedLevels[i], false);
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
            if (GUILayout.Button("TÜM Seviyelerde Toplu Çözücüyü Çalıştır", GUILayout.Height(32))) RunBatchSolver();
            if (GUILayout.Button("Eksik Çözümleri Otomatik Düzelt & Üret", GUILayout.Height(32))) AutoFixMissingSolutions();
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_batchSolveStatusMessage))
            {
                GUILayout.Space(6);
                EditorGUILayout.HelpBox(_batchSolveStatusMessage, MessageType.Info);
            }
            GUILayout.EndVertical();
            GUILayout.Space(8);

            DrawSolverResultsTable();
        }

        private void DrawSolverResultsTable()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label($"📊 Çözülebilirlik Denetim Durumu ({_cachedLevels.Count} Seviye)", _sectionHeaderStyle);
            GUILayout.Space(5);

            if (_cachedLevels.Count == 0) { GUILayout.EndVertical(); return; }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Seviye", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("Izgara", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("Çözülebilirlik", EditorStyles.boldLabel, GUILayout.Width(160));
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
                GUILayout.Label(isSolvable ? "✔ ÇÖZÜLEBİLİR" : "✖ ÇÖZÜLEMİYOR!", isSolvable ? _okBadgeStyle : _errorBadgeStyle, GUILayout.Width(160));
                int sc = lvl.solutions?.Count ?? 0;
                GUILayout.Label(sc > 0 ? $"{sc} renk çözüldü" : "Çözüm yok", GUILayout.Width(100));
                if (GUILayout.Button("İncele", GUILayout.Height(18), GUILayout.Width(60))) { Selection.activeObject = lvl; EditorGUIUtility.PingObject(lvl); }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        // SEKME 4: EKONOMİ & ISI HARİTASI
        // ═══════════════════════════════════════════════════

        private void DrawEconomyAnalyticsTab()
        {
            DrawDifficultyHeatmap();
            DrawEconomyTable();
        }

        private void DrawDifficultyHeatmap()
        {
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
        }

        private void DrawEconomyTable()
        {
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

            int[] storageCaps = { 1000, 2500, 5000, 10000, 25000, 50000, 100000, 200000, 500000, 1000000 };
            for (int lvl = 0; lvl < 10; lvl++)
            {
                int rateC = Mathf.RoundToInt(250f * Mathf.Pow(1.35f, lvl));
                int storageC = Mathf.RoundToInt(150f * Mathf.Pow(1.35f, lvl));
                int viaductC = Mathf.RoundToInt(500f * Mathf.Pow(1.35f, lvl));
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
        // SEKME 5: NEXUS DURUM İZLEYİCİ
        // ═══════════════════════════════════════════════════

        private void DrawNexusInspectorTab()
        {
            var root = Object.FindAnyObjectByType<Root>();
            bool initialized = root != null && root.IsInitialized && root.Context != null;

            DrawNexusRootStatus(root, initialized);
            if (!initialized) return;

            var container = root.Context.Container;
            DrawReactiveModels(container);
            DrawRegisteredServices(container);
            DrawActiveViews();
        }

        private void DrawNexusRootStatus(Root root, bool initialized)
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🔬 Nexus Root & Context Durumu", _sectionHeaderStyle);
            GUILayout.Space(5);
            DrawInfoRow("Root Nesnesi:", root != null ? $"✔ {root.gameObject.name}" : "✖ Bulunamadı");
            DrawInfoRow("Başlatıldı mı:", initialized ? "✔ Evet" : "✖ Hayır");
            if (root != null)
            {
                DrawInfoRow("Kapsam Etiketi:", root.Context?.ScopeTag ?? "(Boş)");
                DrawInfoRow("ContextData:", root.ContextData != null ? $"✔ {root.ContextData.name}" : "✖ Atanmamış");
            }
            GUILayout.EndVertical();
            GUILayout.Space(8);

            if (!initialized)
                EditorGUILayout.HelpBox("Nexus Root başlatılmamış. Canlı izleme için Play Mode'a girin.", MessageType.Warning);
        }

        private void DrawReactiveModels(NexusDI container)
        {
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
        }

        private void DrawRegisteredServices(NexusDI container)
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("⚙️ Kayıtlı Servisler", _sectionHeaderStyle);
            GUILayout.Space(5);

            DrawNexusResolveStatus<IPathService>(container, "IPathService (Yol)");
            DrawNexusResolveStatus<IGameHistoryService>(container, "IGameHistoryService (Geçmiş)");
            DrawNexusResolveStatus<IVehicleSimulator>(container, "IVehicleSimulator (Araç Sim.)");
            DrawNexusResolveStatus<IGameplayTimerService>(container, "IGameplayTimerService (Zaman)");
            DrawNexusResolveStatus<IObstacleService>(container, "IObstacleService (Engel)");
            DrawNexusResolveStatus<IDailyCrisisService>(container, "IDailyCrisisService (Kriz)");
            DrawNexusResolveStatus<ICrisisAdService>(container, "ICrisisAdService (Kriz Reklam)");
            DrawNexusResolveStatus<IHintService>(container, "IHintService (İpucu)");
            DrawNexusResolveStatus<ILevelProgressionService>(container, "ILevelProgressionService (Sev.İler.)");
            DrawNexusResolveStatus<IFeedbackService>(container, "IFeedbackService (Geri Bild.)");
            DrawNexusResolveStatus<ILoggerService>(container, "ILoggerService (Kayıtçı)");
            DrawNexusResolveStatus<IPlayerPrefsService>(container, "IPlayerPrefsService (Tercihler)");
            DrawNexusResolveStatus<ISignalBus>(container, "ISignalBus (Sinyal)");
            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawActiveViews()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("🖼️ Aktif View Bağlantıları", _sectionHeaderStyle);
            GUILayout.Space(5);

            var allViews = Object.FindObjectsByType<View>(FindObjectsInactive.Exclude);
            if (allViews.Length == 0)
                GUILayout.Label("Sahnede aktif View bulunamadı.", EditorStyles.miniLabel);
            else
            {
                foreach (var view in allViews)
                {
                    bool active = view.gameObject.activeInHierarchy;
                    Color c = active ? new Color(0.12f, 0.65f, 0.22f) : new Color(0.6f, 0.6f, 0.6f);
                    GUIStyle vs = new GUIStyle(EditorStyles.label) { normal = { textColor = c } };
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(active ? "●" : "○", vs, GUILayout.Width(15));
                    GUILayout.Label(view.GetType().Name, EditorStyles.boldLabel, GUILayout.Width(180));
                    GUILayout.Label($"({view.gameObject.name})", EditorStyles.miniLabel);
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════
        // SEKME 6: PERFORMANS & DENETİM
        // ═══════════════════════════════════════════════════

        private void DrawPerformanceTab()
        {
            bool isPlaying = Application.isPlaying;
            DrawRuntimeMetrics(isPlaying);
            DrawTimeScaleControls(isPlaying);
            DrawSceneStats();
            DrawAssetSummary();
        }

        private void DrawRuntimeMetrics(bool isPlaying)
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("📈 Çalışma Zamanı Performans Metrikleri", _sectionHeaderStyle);
            GUILayout.Space(5);

            if (isPlaying)
            {
                float fps = 1.0f / Time.unscaledDeltaTime;
                Color fpsColor = fps > 55 ? new Color(0.12f, 0.65f, 0.22f) : fps > 30 ? new Color(0.9f, 0.6f, 0.1f) : new Color(0.85f, 0.2f, 0.18f);
                GUIStyle fpsStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = fpsColor }, fontSize = 16 };
                GUILayout.BeginHorizontal();
                GUILayout.Label("FPS:", GUILayout.Width(110));
                GUILayout.Label($"{fps:F1}", fpsStyle);
                GUILayout.EndHorizontal();

                DrawInfoRow("Toplam Ayrılan Bellek:", $"{UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f):F1} MB");
                DrawInfoRow("Ayrılmış Bellek:", $"{UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f):F1} MB");
                DrawInfoRow("GC Toplama:", $"{System.GC.CollectionCount(0)}/{System.GC.CollectionCount(1)}/{System.GC.CollectionCount(2)} (Gen0/1/2)");
                DrawInfoRow("Zaman Ölçeği:", $"{Time.timeScale:F2}x");
                DrawInfoRow("Kare Sayısı:", $"{Time.frameCount:N0}");
            }
            else
                EditorGUILayout.HelpBox("Performans metrikleri yalnızca Play Mode'da görüntülenebilir.", MessageType.Info);

            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawTimeScaleControls(bool isPlaying)
        {
            if (!isPlaying) return;
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("⏱️ Zaman Ölçeği Kontrolü", _sectionHeaderStyle);
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("⏸ Durdur (0x)", GUILayout.Height(28))) Time.timeScale = 0f;
            if (GUILayout.Button("▶ Normal (1x)", GUILayout.Height(28))) Time.timeScale = 1f;
            if (GUILayout.Button("⏩ Hızlı (2x)", GUILayout.Height(28))) Time.timeScale = 2f;
            if (GUILayout.Button("⏩⏩ Çok Hızlı (5x)", GUILayout.Height(28))) Time.timeScale = 5f;
            if (GUILayout.Button("🐌 Ağır Çekim (0.25x)", GUILayout.Height(28))) Time.timeScale = 0.25f;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawSceneStats()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("📊 Sahne İstatistikleri", _sectionHeaderStyle);
            GUILayout.Space(5);

            var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
            DrawInfoRow("Toplam GameObject:", $"{allGOs.Length} (Aktif: {allGOs.Count(go => go.activeInHierarchy)})");
            DrawInfoRow("Renderer:", $"{Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude).Length}");
            DrawInfoRow("Collider2D:", $"{Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude).Length}");
            DrawInfoRow("Canvas:", $"{Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude).Length}");
            DrawInfoRow("AudioSource:", $"{Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude).Length}");

            if (Application.isPlaying)
            {
                var views = Object.FindObjectsByType<View>(FindObjectsInactive.Exclude);
                DrawInfoRow("Nexus View:", $"{views.Length} (Aktif: {views.Count(v => v.gameObject.activeInHierarchy)})");
            }
            GUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawAssetSummary()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("📁 Proje Varlık Özeti", _sectionHeaderStyle);
            GUILayout.Space(5);

            DrawInfoRow("LevelData Varlıkları:", $"{_cachedLevels.Count}");
            DrawInfoRow("Prefab Dosyaları:", AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }).Length.ToString());
            DrawInfoRow("Script Dosyaları:", AssetDatabase.FindAssets("t:Script", new[] { "Assets/Scripts" }).Length.ToString());
            DrawInfoRow("Material Dosyaları:", AssetDatabase.FindAssets("t:Material", new[] { "Assets" }).Length.ToString());
            GUILayout.EndVertical();
        }
    }
}
#endif
