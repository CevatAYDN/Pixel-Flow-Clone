#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Button = UnityEngine.UIElements.Button;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace PixelFlow.Editor
{
    public partial class PixelFlowSetupWindow : EditorWindow
    {
        private int _selectedTab;
        private VisualElement _content;
        private List<Button> _navButtons = new();
        private Vector2 _scrollPos;

        private static readonly (string Title, int StartIndex, int Count)[] SidebarGroups =
        {
            ("Başlangıç", 0, 2),
            ("Üretim", 2, 3),
            ("Veri ve Denge", 5, 3),
            ("Yayın ve Kontrol", 8, 3),
            ("Araçlar", 11, 1),
        };

        public static void OpenTab(int tabIndex)
        {
            var w = GetWindow<PixelFlowSetupWindow>("Color Jam 3D Studio");
            w.minSize = new Vector2(800, 600);
            w.SelectTab(Mathf.Clamp(tabIndex, 0, w._tabNames.Length - 1));
        }

        [MenuItem("Pixel Flow/COLOR JAM 3D Studio (v6)", false, 0)]
        public static void ShowWindow()
        {
            var w = GetWindow<PixelFlowSetupWindow>("Color Jam 3D Studio");
            w.minSize = new Vector2(800, 600);
        }

        public static void QuickSetupScene()
        {
            var window = GetWindow<PixelFlowSetupWindow>("Color Jam 3D Studio");
            window.SetupScene();
        }

        public static void QuickAutoReference()
        {
            AutoReferenceEditor.AutoReferenceAllViewsInScene();
        }

        public static void QuickGenerateAudio()
        {
            AudioClipGenerator.GenerateAllAudioClips();
        }

        private void CreateGUI()
        {
            rootVisualElement.style.backgroundColor = new Color(0.06f, 0.09f, 0.16f);
            rootVisualElement.style.paddingLeft = 8; rootVisualElement.style.paddingRight = 8; rootVisualElement.style.paddingTop = 8; rootVisualElement.style.paddingBottom = 8;

            rootVisualElement.Add(BuildHeader());
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            row.Add(BuildSidebar());
            _content = new ScrollView { style = { flexGrow = 1, paddingLeft = 12, paddingRight = 12 } };
            row.Add(_content);
            rootVisualElement.Add(row);
            SelectTab(0);
        }

        private VisualElement BuildHeader()
        {
            var h = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    backgroundColor = new Color(0.12f, 0.16f, 0.23f),
                    paddingTop = 12,
                    paddingBottom = 12,
                    paddingLeft = 14,
                    paddingRight = 14,
                    marginBottom = 8,
                    borderTopLeftRadius = 8,
                    borderTopRightRadius = 8,
                    borderBottomLeftRadius = 8,
                    borderBottomRightRadius = 8
                }
            };

            var titleRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
            var titleBlock = new VisualElement { style = { flexDirection = FlexDirection.Column, flexGrow = 1 } };

            var title = new Label("COLOR JAM 3D CONTROL HUB");
            title.style.fontSize = 16;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleBlock.Add(title);

            var subtitle = new Label("Oyunu kur, sahneyi denetle, seviyeleri üret, veriyi düzelt, yayına hazırla.");
            subtitle.style.fontSize = 11;
            subtitle.style.color = new Color(0.58f, 0.64f, 0.72f);
            subtitle.style.marginTop = 2;
            subtitle.style.whiteSpace = WhiteSpace.Normal;
            titleBlock.Add(subtitle);

            titleRow.Add(titleBlock);

            var liveBadge = new Label(Application.isPlaying ? "PLAY MODE" : "EDIT MODE");
            liveBadge.style.backgroundColor = Application.isPlaying ? new Color(0f, 0.8f, 0.5f, 0.15f) : new Color(0.23f, 0.51f, 0.96f, 0.15f);
            liveBadge.style.color = Application.isPlaying ? new Color(0.2f, 0.83f, 0.6f) : new Color(0.4f, 0.7f, 1f);
            liveBadge.style.paddingLeft = liveBadge.style.paddingRight = 10;
            liveBadge.style.paddingTop = liveBadge.style.paddingBottom = 4;
            liveBadge.style.marginLeft = 12;
            liveBadge.style.borderTopLeftRadius = liveBadge.style.borderTopRightRadius = liveBadge.style.borderBottomLeftRadius = liveBadge.style.borderBottomRightRadius = 100;
            titleRow.Add(liveBadge);

            h.Add(titleRow);

            var metricsRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginTop = 10 } };
            metricsRow.Add(BuildMetricChip("Root", FindAnyObjectByType<Root>(FindObjectsInactive.Include) != null ? "Açık" : "Yok", new Color(0.3f, 0.8f, 1f)));
            metricsRow.Add(BuildMetricChip("Bootstrapper", FindAnyObjectByType<GameBootstrapper>(FindObjectsInactive.Include) != null ? "Açık" : "Yok", new Color(0.4f, 1f, 0.4f)));
            metricsRow.Add(BuildMetricChip("Leveller", AssetDatabase.FindAssets("t:LevelData").Length.ToString(), new Color(1f, 0.85f, 0.3f)));
            metricsRow.Add(BuildMetricChip("Skin", AssetDatabase.FindAssets("t:VehicleSkinConfig").Length.ToString(), new Color(0.8f, 0.6f, 0.9f)));
            h.Add(metricsRow);

            var quickActions = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginTop = 10 } };
            quickActions.Add(MakeBtn("▶ Oyna", () => EditorApplication.isPlaying = true));
            quickActions.Add(MakeBtn("🎬 Sahneyi Kur", SetupScene));
            quickActions.Add(MakeBtn("🎮 Seviye Stüdyosu", PixelFlowLevelStudioWindow.ShowWindow));
            quickActions.Add(MakeBtn("🔬 Nexus Dashboard", () => EditorApplication.ExecuteMenuItem("Window/Nexus/Dashboard %#n")));
            quickActions.Add(MakeBtn("🛡 Doğrula", () => PreBuildDataValidator.ValidateAllData(out _)));
            h.Add(quickActions);

            return h;
        }

        private VisualElement BuildSidebar()
        {
            var s = new VisualElement { style = { width = 220, flexShrink = 0, backgroundColor = new Color(0.12f, 0.16f, 0.23f), paddingLeft = 8, paddingRight = 8, paddingTop = 8, paddingBottom = 8, marginRight = 8, borderTopLeftRadius = 8, borderTopRightRadius = 8, borderBottomLeftRadius = 8, borderBottomRightRadius = 8 } };
            _navButtons.Clear();
            foreach (var group in SidebarGroups)
            {
                s.Add(BuildSidebarGroupHeader(group.Title));

                for (int i = group.StartIndex; i < group.StartIndex + group.Count; i++)
                {
                    var idx = i;
                    var btn = new Button(() => SelectTab(idx)) { text = _tabNames[i] };
                    btn.style.backgroundColor = Color.clear;
                    btn.style.color = new Color(0.58f, 0.64f, 0.72f);
                    btn.style.fontSize = 12;
                    btn.style.paddingTop = btn.style.paddingBottom = 8;
                    btn.style.paddingLeft = btn.style.paddingRight = 10;
                    btn.style.marginBottom = 2;
                    btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 6;
                    btn.style.unityFontStyleAndWeight = FontStyle.Bold;
                    _navButtons.Add(btn);
                    s.Add(btn);
                }
            }
            return s;
        }

        private VisualElement BuildSidebarGroupHeader(string text)
        {
            var header = new VisualElement { style = { marginTop = 4, marginBottom = 4, paddingTop = 4, paddingBottom = 2, paddingLeft = 4 } };
            var label = new Label(text);
            label.style.fontSize = 10;
            label.style.color = new Color(0.42f, 0.5f, 0.58f);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(label);
            return header;
        }

        private VisualElement BuildMetricChip(string label, string value, Color accent)
        {
            var chip = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    backgroundColor = new Color(0.08f, 0.11f, 0.18f),
                    borderTopLeftRadius = 999,
                    borderTopRightRadius = 999,
                    borderBottomLeftRadius = 999,
                    borderBottomRightRadius = 999,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 4,
                    paddingBottom = 4,
                    marginRight = 6,
                    marginTop = 4
                }
            };

            var dot = new VisualElement();
            dot.style.width = 8;
            dot.style.height = 8;
            dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius = dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 999;
            dot.style.backgroundColor = accent;
            dot.style.marginRight = 6;
            chip.Add(dot);

            var txt = new Label($"{label}: {value}");
            txt.style.fontSize = 10;
            txt.style.color = Color.white;
            txt.style.unityFontStyleAndWeight = FontStyle.Bold;
            chip.Add(txt);
            return chip;
        }

        internal void SelectTab(int idx)
        {
            _selectedTab = idx;
            for (int i = 0; i < _navButtons.Count; i++)
            {
                _navButtons[i].style.backgroundColor = i == idx ? new Color(0.23f, 0.51f, 0.96f) : Color.clear;
                _navButtons[i].style.color = i == idx ? Color.white : new Color(0.58f, 0.64f, 0.72f);
            }
            _content.Clear();
            RenderSelectedTabContent(idx);
        }

        // ─── TAB 3: TOPLU ÇÖZÜCÜ ───
        private void BuildBatchSolverTab(VisualElement parent)
        {
            var card = Card("🧩 Toplu Çözücü (RuntimePathSolver & Solver Validation)");
            var levels = AssetDatabase.FindAssets("t:LevelData")
                .Select(g => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(l => l != null).OrderBy(l => l.levelIndex).ToList();

            card.Add(MakeBtn($"Tüm {levels.Count} Seviyeyi Çöz ve Raporla", () => {
                int solved = 0;
                var solver = new RuntimePathSolver();
                foreach (var l in levels) { if (solver.Solve(l, out _)) solved++; }
                Debug.Log($"[PixelFlow Toplu Çözücü] {levels.Count} seviyeden {solved} tanesi çözülebilir (%{(float)solved/levels.Count*100:F1}).");
            }));
            parent.Add(card);
        }

        // ─── HELPER: card wrapper ───
        private VisualElement Card(string title)
        {
            var c = new VisualElement { style = { backgroundColor = new Color(0.1f, 0.13f, 0.2f), paddingLeft = 12, paddingRight = 12, paddingTop = 12, paddingBottom = 12, marginBottom = 8, borderTopLeftRadius = 6, borderTopRightRadius = 6, borderBottomLeftRadius = 6, borderBottomRightRadius = 6 } };
            if (!string.IsNullOrEmpty(title))
            {
                var l = new Label(title);
                l.style.fontSize = 13; l.style.color = new Color(0.23f, 0.51f, 0.96f); l.style.unityFontStyleAndWeight = FontStyle.Bold; l.style.marginBottom = 6;
                c.Add(l);
            }
            return c;
        }

        private Button MakeBtn(string text, System.Action action)
        {
            var b = new Button(action) { text = text };
            b.style.fontSize = 11; b.style.paddingTop = b.style.paddingBottom = 6; b.style.paddingLeft = b.style.paddingRight = 10;
            return b;
        }

        private Label StatusLabel(bool ok, string okText = "OK", string failText = "EKSIK")
        {
            var l = new Label(ok ? $"✔ {okText}" : $"✘ {failText}");
            l.style.color = ok ? new Color(0.2f, 0.83f, 0.6f) : new Color(0.94f, 0.27f, 0.27f);
            l.style.fontSize = 11; l.style.unityFontStyleAndWeight = FontStyle.Bold;
            return l;
        }

        private Label InfoLabel(string text)
        {
            var l = new Label(text);
            l.style.color = new Color(0.58f, 0.64f, 0.72f); l.style.fontSize = 11; l.style.marginBottom = 4;
            return l;
        }

        private VisualElement Row(string label, VisualElement value)
        {
            var r = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 3 } };
            var l = new Label(label);
            l.style.width = 160; l.style.color = new Color(0.58f, 0.64f, 0.72f); l.style.fontSize = 11;
            r.Add(l); r.Add(value); return r;
        }

        // ─── TAB 2: SEVİYE STÜDYOSU ───
        private void BuildLevelTab(VisualElement parent)
        {
            var levels = AssetDatabase.FindAssets("t:LevelData")
                .Select(g => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(l => l != null).OrderBy(l => l.levelIndex).ToList();

            var card = Card($"🎮 Seviye Stüdyosu (Toplam {levels.Count} Seviye Asset'i)");

            var toolbox = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6 } };
            int nextLevelNum = levels.Count > 0 ? levels.Max(l => l.levelIndex) + 2 : 1;
            toolbox.Add(MakeBtn($"Tek Seviye Ekle (Level {nextLevelNum})", () => GenerateSingleNextLevel()));
            toolbox.Add(MakeBtn("150 Seviye Paketi Üret", () => GenerateMissingLevels(150)));
            toolbox.Add(MakeBtn("LevelCatalog Yenile", () => RegenerateLevelCatalog(150)));
            toolbox.Add(MakeBtn("Inspector'da Aç", () => {
                if (levels.Count > 0) { Selection.activeObject = levels[0]; EditorGUIUtility.PingObject(levels[0]); }
            }));
            card.Add(toolbox);

            foreach (var lvl in levels)
            {
                var r = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2, backgroundColor = new Color(0.08f, 0.11f, 0.18f), paddingLeft = 4, paddingRight = 4, paddingTop = 4, paddingBottom = 4 } };
                var info = new Label($"#{lvl.levelIndex}  {lvl.name}  ({lvl.width}x{lvl.height}, Nodes:{lvl.initialNodes?.Count ?? 0}, Obstacles:{lvl.obstacles?.Count ?? 0})");
                info.style.color = new Color(0.78f, 0.82f, 0.85f); info.style.fontSize = 11;
                info.style.flexGrow = 1;
                r.Add(info);

                var playBtn = MakeBtn("▶", () => PlayLevel(lvl));
                playBtn.style.width = 30;
                r.Add(playBtn);
                var selBtn = MakeBtn("Seç", () => { Selection.activeObject = lvl; EditorGUIUtility.PingObject(lvl); });
                selBtn.style.width = 40;
                r.Add(selBtn);
                card.Add(r);
            }
            parent.Add(card);
        }

        public static LevelData GenerateSingleNextLevel()
        {
            string outputPath = "Assets/Resources/Levels";
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

            var files = Directory.GetFiles(outputPath, "Level*.asset")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToList();

            int maxIndex = 0;
            foreach (var f in files)
            {
                string numStr = f.Replace("Level", "").Replace("Data_", "");
                if (int.TryParse(numStr, out int idx))
                {
                    if (idx > maxIndex) maxIndex = idx;
                }
            }

            int nextLevelNumber = maxIndex + 1;
            int nextLevelIndex = nextLevelNumber - 1;

            var phaseConfig = Resources.Load<PhaseConfigAsset>("Configs/PhaseConfig");
            if (phaseConfig == null)
            {
                Debug.LogError("[PixelFlow] PhaseConfig bulunamadı! Önce Phase Asset Generator çalıştırın.");
                return null;
            }

            var solver = new RuntimePathSolver();
            var progression = new LevelProgressionService(solver, phaseConfig);
            var generator = new ProceduralLevelGenerator(solver);

            var param = progression.GetDifficultyForLevel(nextLevelIndex);
            var level = generator.Generate(param);

            if (level != null)
            {
                level.levelIndex = nextLevelIndex;
                level.name = $"Level{nextLevelNumber}";
                string filePath = $"{outputPath}/Level{nextLevelNumber}.asset";
                AssetDatabase.CreateAsset(level, filePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                RegenerateLevelCatalog(Mathf.Max(150, nextLevelNumber));
                Debug.Log($"[PixelFlow] Tekli seviye başarıyla oluşturuldu: Seviye #{nextLevelIndex} ({level.name}) -> {filePath}");
                Selection.activeObject = level;
                EditorGUIUtility.PingObject(level);
                return level;
            }
            else
            {
                Debug.LogError($"[PixelFlow] Seviye #{nextLevelIndex} ({nextLevelNumber}) oluşturulamadı.");
                return null;
            }
        }

        public static void GenerateMissingLevels(int targetCount = 150)
        {
            string outputPath = "Assets/Resources/Levels";
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

            var phaseConfig = Resources.Load<PhaseConfigAsset>("Configs/PhaseConfig");
            if (phaseConfig == null)
            {
                Debug.LogError("[PixelFlow] PhaseConfig bulunamadı! Önce Phase Asset Generator çalıştırın.");
                return;
            }

            var solver = new RuntimePathSolver();
            var progression = new LevelProgressionService(solver, phaseConfig);
            var generator = new ProceduralLevelGenerator(solver);
            int generated = 0;

            for (int i = 1; i <= targetCount; i++)
            {
                string filePath = $"{outputPath}/Level{i}.asset";
                if (File.Exists(filePath)) continue;

                var param = progression.GetDifficultyForLevel(i - 1);
                var level = generator.Generate(param);
                if (level != null)
                {
                    level.levelIndex = i - 1;
                    level.name = $"Level{i}";
                    AssetDatabase.CreateAsset(level, filePath);
                    generated++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RegenerateLevelCatalog(targetCount);
            Debug.Log($"[PixelFlow] Toplam {generated} yeni seviye üretildi ve kataloğa eklendi.");
        }

        public static void RegenerateLevelCatalog(int targetCount = 150)
        {
            string catalogPath = "Assets/Resources/Configs/LevelCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogAsset>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            string levelsDir = "Assets/Resources/Levels";
            if (!Directory.Exists(levelsDir)) Directory.CreateDirectory(levelsDir);

            var files = Directory.GetFiles(levelsDir, "Level*.asset")
                .OrderBy(f => int.TryParse(Path.GetFileNameWithoutExtension(f).Replace("Level", "").Replace("Data_", ""), out int idx) ? idx : 999)
                .ToList();

            catalog.Levels.Clear();
            var phaseConfig = Resources.Load<PhaseConfigAsset>("Configs/PhaseConfig");
            var progression = phaseConfig != null ? new LevelProgressionService(new RuntimePathSolver(), phaseConfig, catalog) : null;

            foreach (var file in files)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(file);
                if (level != null)
                {
                    // Auto-fix node isSource flags if invalid
                    if (level.initialNodes != null && level.initialNodes.Count > 0)
                    {
                        var colorSeen = new HashSet<ColorType>();
                        bool isDirty = false;
                        for (int n = 0; n < level.initialNodes.Count; n++)
                        {
                            var node = level.initialNodes[n];
                            if (node.color == ColorType.None) continue;
                            if (colorSeen.Add(node.color))
                            {
                                if (!node.isSource)
                                {
                                    node.isSource = true;
                                    level.initialNodes[n] = node;
                                    isDirty = true;
                                }
                            }
                            else
                            {
                                if (node.isSource)
                                {
                                    node.isSource = false;
                                    level.initialNodes[n] = node;
                                    isDirty = true;
                                }
                            }
                        }
                        if (isDirty)
                        {
                            EditorUtility.SetDirty(level);
                        }
                    }

                    catalog.Levels.Add(new LevelCatalogAsset.LevelCatalogEntry
                    {
                        LevelIndex = level.levelIndex,
                        AuthoredLevel = level,
                        UseProceduralFallback = false
                    });
                }
            }

            // Auto-repair any null AuthoredLevel entries in catalog
            for (int i = 0; i < catalog.Levels.Count; i++)
            {
                var entry = catalog.Levels[i];
                if (entry != null && !entry.UseProceduralFallback && entry.AuthoredLevel == null)
                {
                    entry.UseProceduralFallback = true;
                    entry.ProceduralDifficulty = progression != null ? progression.GetDifficultyForLevel(entry.LevelIndex) : DifficultyParams.Easy;
                    catalog.Levels[i] = entry;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PixelFlow] LevelCatalog yenilendi. Toplam {catalog.Levels.Count} somut seviye kaydı hazır.");
        }

        private void CreateNewVehicleSkin()
        {
            string dir = "Assets/Resources/Configs/Skins";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var skin = CreateInstance<VehicleSkinConfig>();
            skin.SkinId = $"skin_{System.DateTime.Now.Ticks}";
            skin.DisplayName = "Yeni Skin";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/VehicleSkin_New.asset");
            AssetDatabase.CreateAsset(skin, path); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Selection.activeObject = skin; EditorGUIUtility.PingObject(skin);
        }

        private void CreateStandardSkinSuite()
        {
            string dir = "Assets/Resources/Configs/Skins";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            void Make(string path, string id, string name, ColorType color, int cost, bool ad)
            {
                if (AssetDatabase.LoadAssetAtPath<VehicleSkinConfig>(path) != null) return;
                var s = CreateInstance<VehicleSkinConfig>();
                s.SkinId = id; s.DisplayName = name; s.ColorFamily = color; s.UnlockCoinCost = cost; s.RequiresRewardedAd = ad;
                AssetDatabase.CreateAsset(s, path);
            }
            Make($"{dir}/VehicleSkin_IceCreamTruck.asset", "skin_icecream", "Dondurma Arabası", ColorType.Yellow, 250, false);
            Make($"{dir}/VehicleSkin_MonsterTruck.asset", "skin_monstertruck", "Canavar Kamyon", ColorType.Red, 500, false);
            Make($"{dir}/VehicleSkin_GoldenBus.asset", "skin_goldenbus", "Altın Otobüs", ColorType.Yellow, 1000, true);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("[PixelFlow] Standart skin paketi oluşturuldu.");
        }

        // ─── YARDIMCILAR ───
        private void PlayLevel(LevelData level)
        {
            if (level == null) return;
            if (Application.isPlaying)
                FireSignal(new LoadLevelSignal { LevelToLoad = level });
            else
            {
                var boot = FindAnyObjectByType<GameBootstrapper>(FindObjectsInactive.Include);
                if (boot == null)
                {
                    var sceneSetup = new SceneSetupHelper();
                    sceneSetup.SetupMinimalScene();
                    boot = FindAnyObjectByType<GameBootstrapper>(FindObjectsInactive.Include);
                }
                if (boot != null) { Undo.RecordObject(boot, "Set Level"); boot.initialLevel = level; EditorUtility.SetDirty(boot); }
                EditorApplication.isPlaying = true;
            }
        }

        private void FireSignal<T>(T signal) where T : struct
        {
            var bus = FindAnyObjectByType<Root>(FindObjectsInactive.Include)?.Context?.Container?.Resolve<ISignalBus>();
            if (bus != null) bus.Fire(signal);
            else Debug.LogWarning($"[PixelFlow] SignalBus not found for {typeof(T).Name}");
        }

        private class SceneSetupHelper
        {
            public void SetupMinimalScene()
            {
                var window = EditorWindow.GetWindow<PixelFlowSetupWindow>("Color Jam 3D Studio");
                window.minSize = new Vector2(800, 600);
                window.SetupScene();
            }
        }
    }
}
#endif
