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

        private readonly string[] _tabNames = {
            "🎮 Oyun", "🎮 Level", "🎨 Garaj",
            "📦 Data", "📊 Audit", "🛡️ Validator"
        };

        public static void OpenTab(int tabIndex)
        {
            var w = GetWindow<PixelFlowSetupWindow>("Color Jam 3D Studio");
            w.minSize = new Vector2(800, 600);
            w.SelectTab(tabIndex);
        }

        [MenuItem("Pixel Flow/COLOR JAM 3D Studio (v6)", false, 0)]
        public static void ShowWindow()
        {
            var w = GetWindow<PixelFlowSetupWindow>("Color Jam 3D Studio");
            w.minSize = new Vector2(800, 600);
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
            var h = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, backgroundColor = new Color(0.12f, 0.16f, 0.23f), paddingTop = 10, paddingBottom = 10, paddingLeft = 14, paddingRight = 14, marginBottom = 8, borderTopLeftRadius = 8, borderTopRightRadius = 8, borderBottomLeftRadius = 8, borderBottomRightRadius = 8 } };
            var title = new Label("COLOR JAM 3D — Master Studio");
            title.style.fontSize = 16; title.style.color = Color.white;
            h.Add(title);
            var badge = new Label("● Live");
            badge.style.backgroundColor = new Color(0f, 0.8f, 0.5f, 0.15f);
            badge.style.color = new Color(0.2f, 0.83f, 0.6f);
            badge.style.paddingLeft = badge.style.paddingRight = 10;
            badge.style.paddingTop = badge.style.paddingBottom = 4;
            badge.style.borderTopLeftRadius = badge.style.borderTopRightRadius = badge.style.borderBottomLeftRadius = badge.style.borderBottomRightRadius = 100;
            h.Add(badge);
            return h;
        }

        private VisualElement BuildSidebar()
        {
            var s = new VisualElement { style = { width = 180, flexShrink = 0, backgroundColor = new Color(0.12f, 0.16f, 0.23f), paddingLeft = 8, paddingRight = 8, paddingTop = 8, paddingBottom = 8, marginRight = 8, borderTopLeftRadius = 8, borderTopRightRadius = 8, borderBottomLeftRadius = 8, borderBottomRightRadius = 8 } };
            _navButtons.Clear();
            for (int i = 0; i < _tabNames.Length; i++)
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
            return s;
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
            switch (idx)
            {
                case 0: BuildGameControlTab(_content); break;
                case 1: BuildLevelTab(_content); break;
                case 2: BuildGarageTab(_content); break;
                case 3: BuildDataTab(_content); break;
                case 4: BuildAuditTab(_content); break;
                case 5: BuildValidatorTab(_content); break;
            }
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

        // ─── TAB 0: OYUN KONTROL ───
        private void BuildGameControlTab(VisualElement parent)
        {
            var playCard = Card("🎮 Oyun Kontrol");
            playCard.Add(Row("Editor Modu:", StatusLabel(!Application.isPlaying, "Editör", "Oynaniyor")));
            playCard.Add(Row("Oyun Durumu:", StatusLabel(Application.isPlaying, "Oynaniyor", "Durmus")));

            var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 6 } };
            if (Application.isPlaying)
            {
                btnRow.Add(MakeBtn("⏹ Durdur", () => EditorApplication.isPlaying = false));
                btnRow.Add(MakeBtn("Level Bitir", () => { FireSignal(new LevelCompletedSignal()); }));
            }
            else
                btnRow.Add(MakeBtn("▶ Play", () => EditorApplication.isPlaying = true));
            playCard.Add(btnRow);
            parent.Add(playCard);

            var bootCard = Card("⚡ Bootstrapper");
            var boot = FindAnyObjectByType<GameBootstrapper>(FindObjectsInactive.Include);
            if (boot != null)
            {
                bootCard.Add(Row("Active:", StatusLabel(true)));
                bootCard.Add(Row("Initial Level:", StatusLabel(boot.initialLevel != null, boot.initialLevel?.name ?? "Yok")));
                var so = new SerializedObject(boot);
                var lvlProp = so.FindProperty("initialLevel");
                var field = new PropertyField(lvlProp);
                field.style.marginTop = 4;
                bootCard.Add(field);
            }
            else bootCard.Add(InfoLabel("Bootstrapper sahnede bulunamadi."));
            parent.Add(bootCard);

            var diagCard = Card("🔍 Hizli Tanilama");
            int ok = 0, total = 0;
            void DiagRow(string name, bool okFlag) { diagCard.Add(Row(name, StatusLabel(okFlag))); total++; if (okFlag) ok++; }
            DiagRow("Root", FindAnyObjectByType<Root>(FindObjectsInactive.Include) != null);
            DiagRow("Canvas", FindAnyObjectByType<Canvas>(FindObjectsInactive.Include) != null);
            DiagRow("EventSystem", FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include) != null);
            DiagRow("GameBootstrapper", boot != null);
            DiagRow("GridView", FindAnyObjectByType<Views.GridView>(FindObjectsInactive.Include) != null);
            DiagRow("HUDView", FindAnyObjectByType<Views.HUDView>(FindObjectsInactive.Include) != null);
            DiagRow("MainMenuView", FindAnyObjectByType<Views.MainMenuView>(FindObjectsInactive.Include) != null);
            DiagRow("SettingsView", FindAnyObjectByType<Views.SettingsView>(FindObjectsInactive.Include) != null);
            diagCard.Add(Row($"Toplam: {ok}/{total}", StatusLabel(ok == total, "TAMAM", "EKSIK")));
            parent.Add(diagCard);
        }

        // ─── TAB 1: LEVEL ───
        private void BuildLevelTab(VisualElement parent)
        {
            var levels = AssetDatabase.FindAssets("t:LevelData")
                .Select(g => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(l => l != null).OrderBy(l => l.levelIndex).ToList();

            var card = Card($"🎮 Leveller ({levels.Count})");

            var toolbox = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6 } };
            toolbox.Add(MakeBtn("Toplu Coz", () => {
                int solved = 0;
                var solver = new RuntimePathSolver();
                foreach (var l in levels) { if (solver.Solve(l, out _)) solved++; }
                Debug.Log($"[PixelFlow] {levels.Count} levelden {solved} cozulebilir.");
            }));
            toolbox.Add(MakeBtn("Inspector'da Ac", () => {
                if (levels.Count > 0) { Selection.activeObject = levels[0]; EditorGUIUtility.PingObject(levels[0]); }
            }));
            card.Add(toolbox);

            foreach (var lvl in levels)
            {
                var r = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2, backgroundColor = new Color(0.08f, 0.11f, 0.18f), paddingLeft = 4, paddingRight = 4, paddingTop = 4, paddingBottom = 4 } };
                var info = new Label($"#{lvl.levelIndex}  {lvl.name}  ({lvl.width}x{lvl.height})");
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

        // ─── TAB 2: GARAJ ───
        private void BuildGarageTab(VisualElement parent)
        {
            var guids = AssetDatabase.FindAssets("t:VehicleSkinConfig");
            var card = Card($"🎨 Garaj — {guids.Length} Skin");

            card.Add(MakeBtn("+ Yeni Skin", CreateNewVehicleSkin));
            card.Add(MakeBtn("🍦 Standart Paket", CreateStandardSkinSuite));

            if (guids.Length == 0)
                card.Add(InfoLabel("Henuz skin yok. Yukaridaki butonu kullanarak olusturun."));
            else
            {
                foreach (var guid in guids)
                {
                    var skin = AssetDatabase.LoadAssetAtPath<VehicleSkinConfig>(AssetDatabase.GUIDToAssetPath(guid));
                    if (skin == null) continue;
                    var r = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2, paddingLeft = 4, paddingRight = 4, paddingTop = 4, paddingBottom = 4, backgroundColor = new Color(0.08f, 0.11f, 0.18f) } };
                    var info = new Label($"{skin.DisplayName}  (${skin.UnlockCoinCost})");
                    info.style.color = Color.white; info.style.fontSize = 11; info.style.flexGrow = 1;
                    r.Add(info);
                    r.Add(MakeBtn("Sec", () => { Selection.activeObject = skin; EditorGUIUtility.PingObject(skin); }));
                    card.Add(r);
                }
            }
            parent.Add(card);
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
            Make($"{dir}/VehicleSkin_IceCreamTruck.asset", "skin_icecream", "Dondurma Arabasi", ColorType.Yellow, 250, false);
            Make($"{dir}/VehicleSkin_MonsterTruck.asset", "skin_monstertruck", "Canavar Kamyon", ColorType.Red, 500, false);
            Make($"{dir}/VehicleSkin_GoldenBus.asset", "skin_goldenbus", "Altin Otobus", ColorType.Yellow, 1000, true);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("[PixelFlow] Standart skin paketi olusturuldu.");
        }

        // ─── TAB 3: DATA ───
        private void BuildDataTab(VisualElement parent)
        {
            var card = Card("📦 Config Asset'ler");
            var types = new (string path, string label)[]
            {
                ("Configs/GameConfig", "GameConfig"),
                ("Configs/StorageKeysConfig", "StorageKeysConfig"),
                ("Configs/ThemePalette", "ThemePalette"),
                ("Configs/ColorBlindPalette", "ColorBlindPalette"),
                ("Configs/VehicleMaterialConfig", "VehicleMaterialConfig"),
                ("Configs/VehicleVisualConfig", "VehicleVisualConfig"),
                ("Configs/EconomyConfig", "EconomyConfig"),
                ("Configs/LevelCatalog", "LevelCatalog"),
                ("Configs/PhaseConfig", "PhaseConfig"),
            };
            foreach (var (p, n) in types)
            {
                var asset = Resources.Load(p);
                var r = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
                var lbl = new Label($"{n}:");
                lbl.style.width = 180; lbl.style.color = new Color(0.78f, 0.82f, 0.85f); lbl.style.fontSize = 11;
                r.Add(lbl);
                r.Add(StatusLabel(asset != null));
                card.Add(r);
            }
            parent.Add(card);

            var saveCard = Card("💾 Kayit Yonetimi");
            saveCard.Add(MakeBtn("Save PlayerPrefs", () => PlayerPrefs.Save()));
            saveCard.Add(MakeBtn("Tum Seviyeleri Ac", () => { PlayerPrefs.SetInt("UnlockedLevels", 999); PlayerPrefs.Save(); }));
            saveCard.Add(MakeBtn("Progress Sifirla", () => { PlayerPrefs.SetInt("UnlockedLevels", 1); PlayerPrefs.Save(); }));
            saveCard.Add(MakeBtn("Tum Kaydi Sil", () => { PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); }));
            parent.Add(saveCard);
        }

        // ─── TAB 4: AUDIT ───
        private void BuildAuditTab(VisualElement parent)
        {
            var card = Card("🔍 Sahne Denetimi");

            void Diag(string name, System.Func<object> getVal)
            {
                var val = getVal();
                var r = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };
                var l = new Label(name); l.style.width = 200; l.style.color = new Color(0.78f, 0.82f, 0.85f); l.style.fontSize = 11;
                r.Add(l);
                var v = val is bool b ? StatusLabel(b) : new Label(val?.ToString() ?? "-") { style = { color = Color.white, fontSize = 11 } };
                r.Add(v);
                card.Add(r);
            }

            Diag("Nexus Root", () => FindAnyObjectByType<Root>(FindObjectsInactive.Include) != null);
            Diag("GameBootstrapper", () => FindAnyObjectByType<GameBootstrapper>(FindObjectsInactive.Include) != null);
            Diag("[PixelFlow] GO", () => GameObject.Find("[PixelFlow]") != null);
            Diag("Camera", () => Camera.main != null);
            Diag("Canvas", () => FindAnyObjectByType<Canvas>(FindObjectsInactive.Include) != null);
            Diag("EventSystem", () => FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include) != null);

            var root = FindAnyObjectByType<Root>(FindObjectsInactive.Include);
            Diag("Context Init", () => root?.IsInitialized ?? false);
            Diag("GridModel", () => root?.Context?.Container?.Resolve<IGridModel>() != null);
            Diag("StateModel", () => root?.Context?.Container?.Resolve<IGameStateModel>() != null);
            Diag("SignalBus", () => root?.Context?.Container?.Resolve<ISignalBus>() != null);
            Diag("ProgressModel", () => root?.Context?.Container?.Resolve<IProgressModel>() != null);

            var configs = Resources.FindObjectsOfTypeAll<GameConfig>();
            Diag("GameConfig asset", () => configs.Length > 0 ? configs[0].name : "Yok");

            parent.Add(card);
        }

        // ─── TAB 5: VALIDATOR ───
        private void BuildValidatorTab(VisualElement parent)
        {
            var card = Card("🛡️ Pre-Build Validator");

            card.Add(MakeBtn("🔍 Dogrulama Calistir", () => {
                if (PreBuildDataValidator.ValidateAllData(out var err))
                {
                    Debug.Log("[Validator] ✅ Tum kontroller gecti.");
                    card.Add(InfoLabel("✅ Tum kontroller gecti."));
                }
                else
                {
                    Debug.LogError($"[Validator] ❌ {err}");
                    card.Add(InfoLabel($"❌ {err}"));
                }
            }));
            parent.Add(card);
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
            public void SetupMinimalScene() { }
        }
    }
}
#endif
