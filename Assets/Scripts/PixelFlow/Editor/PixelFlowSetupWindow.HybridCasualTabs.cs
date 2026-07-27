#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PixelFlow.Data;
using PixelFlow.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PixelFlow.Editor
{
    public partial class PixelFlowSetupWindow : EditorWindow
    {
        // ─── TAB 8: GARAJ & SKİN STÜDYOSU ───
        private void BuildGarageTab(VisualElement parent)
        {
            var vehicleGuids = AssetDatabase.FindAssets("t:VehicleSkinConfig");
            var stopGuids = AssetDatabase.FindAssets("t:StopSkinConfig");

            var card = Card($"🎨 Garaj — {vehicleGuids.Length} Araç / {stopGuids.Length} Durak Skin");

            card.Add(MakeBtn("+ Yeni Skin", CreateNewVehicleSkin));
            card.Add(MakeBtn("🍦 Standart Paket", CreateStandardSkinSuite));
            card.Add(MakeBtn("+ Yeni Durak Skin", CreateNewStopSkin));
            card.Add(MakeBtn("🏙 Durak Paketi", CreateStandardStopSkinSuite));

            card.Add(Row("Araç Skin Sayısı:", new Label(vehicleGuids.Length.ToString()) { style = { color = Color.white, fontSize = 11 } }));
            card.Add(Row("Durak Skin Sayısı:", new Label(stopGuids.Length.ToString()) { style = { color = Color.white, fontSize = 11 } }));

            var vehicleSection = Card("🚗 Araç Skinleri");
            if (vehicleGuids.Length == 0)
            {
                vehicleSection.Add(InfoLabel("Henüz araç skin yok. Yukarıdaki butonu kullanarak oluşturun."));
            }
            else
            {
                foreach (var guid in vehicleGuids)
                {
                    var skin = AssetDatabase.LoadAssetAtPath<VehicleSkinConfig>(AssetDatabase.GUIDToAssetPath(guid));
                    if (skin == null) continue;
                    var r = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2, paddingLeft = 4, paddingRight = 4, paddingTop = 4, paddingBottom = 4, backgroundColor = new Color(0.08f, 0.11f, 0.18f) } };
                    var info = new Label($"{skin.DisplayName}  (${skin.UnlockCoinCost})");
                    info.style.color = Color.white; info.style.fontSize = 11; info.style.flexGrow = 1;
                    r.Add(info);
                    r.Add(MakeBtn("Seç", () => { Selection.activeObject = skin; EditorGUIUtility.PingObject(skin); }));
                    vehicleSection.Add(r);
                }
            }

            var stopSection = Card("🛑 Durak Skinleri");
            if (stopGuids.Length == 0)
            {
                stopSection.Add(InfoLabel("Henüz durak skin yok. Yukarıdaki butonu kullanarak oluşturun."));
            }
            else
            {
                foreach (var guid in stopGuids)
                {
                    var skin = AssetDatabase.LoadAssetAtPath<StopSkinConfig>(AssetDatabase.GUIDToAssetPath(guid));
                    if (skin == null) continue;
                    var r = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2, paddingLeft = 4, paddingRight = 4, paddingTop = 4, paddingBottom = 4, backgroundColor = new Color(0.08f, 0.11f, 0.18f) } };
                    var info = new Label($"{skin.DisplayName}  (${skin.UnlockCoinCost})");
                    info.style.color = Color.white; info.style.fontSize = 11; info.style.flexGrow = 1;
                    r.Add(info);
                    r.Add(MakeBtn("Seç", () => { Selection.activeObject = skin; EditorGUIUtility.PingObject(skin); }));
                    stopSection.Add(r);
                }
            }

            parent.Add(card);
            parent.Add(vehicleSection);
            parent.Add(stopSection);
        }

        // ─── TAB 9: REKLAM & MONETIZATION ───
        private void BuildAdMonetizationTab(VisualElement parent)
        {
            var card = Card("📺 Reklam & Monetization (MAX / UMP)");
            var gameConfig = Resources.Load<GameConfig>("Configs/GameConfig");
            if (gameConfig != null)
            {
                card.Add(Row("First Ad Level:", new Label(gameConfig.FirstAdLevel.ToString()) { style = { color = Color.white, fontSize = 11 } }));
                card.Add(Row("Interstitial Frequency:", new Label(gameConfig.InterstitialFrequency.ToString()) { style = { color = Color.white, fontSize = 11 } }));
                card.Add(Row("Rewarded Undo Limit:", new Label(gameConfig.RewardedUndoLimit.ToString()) { style = { color = Color.white, fontSize = 11 } }));
                card.Add(Row("Rewarded Coin Reward:", new Label(gameConfig.RewardedAdCoinReward.ToString()) { style = { color = Color.white, fontSize = 11 } }));
                card.Add(Row("Rewarded Hint Reward:", new Label(gameConfig.RewardedAdHintReward.ToString()) { style = { color = Color.white, fontSize = 11 } }));
                card.Add(Row("Interstitial Placement:", new Label(gameConfig.InterstitialPlacementId) { style = { color = Color.white, fontSize = 11 } }));
                card.Add(Row("Rewarded Placement:", new Label(gameConfig.RewardedPlacementId) { style = { color = Color.white, fontSize = 11 } }));
                card.Add(Row("Banner Placement:", new Label(gameConfig.BannerPlacementId) { style = { color = Color.white, fontSize = 11 } }));
                card.Add(Row("In-App Review Seviyeleri:", new Label($"{string.Join(", ", gameConfig.InAppReviewTriggerLevels)}") { style = { color = Color.white, fontSize = 11 } }));
                card.Add(MakeBtn("GameConfig Inspector'da Aç", () => { Selection.activeObject = gameConfig; EditorGUIUtility.PingObject(gameConfig); }));
            }
            else
            {
                card.Add(InfoLabel("GameConfig asset Resources/Configs/GameConfig bulunamadı."));
            }
            parent.Add(card);
        }

        // ─── TAB 10: PRE-BUILD VALIDATOR ───
        private void BuildValidatorTab(VisualElement parent)
        {
            var card = Card("🛡️ Pre-Build Validator");
            var resultLabel = new Label("") { style = { color = Color.white, fontSize = 11, marginTop = 4 } };
            card.Add(resultLabel);

            card.Add(MakeBtn("🧪 Full Level Audit", RunFullLevelAudit));

            card.Add(MakeBtn("🔍 Doğrulama Çalıştır", () => {
                if (PreBuildDataValidator.ValidateAllData(out var err))
                {
                    Debug.Log("[Validator] ✅ Tüm kontroller geçti.");
                    resultLabel.text = "✅ Tüm kontroller geçti.";
                    resultLabel.style.color = Color.green;
                }
                else
                {
                    Debug.LogError($"[Validator] ❌ {err}");
                    resultLabel.text = $"❌ {err}";
                    resultLabel.style.color = Color.red;
                }
            }));
            parent.Add(card);
        }

        private void BuildToolsTab(VisualElement parent)
        {
            var card = Card("🧰 Araç Merkezi");
            card.Add(InfoLabel("Dağınık yardımcı pencereler yerine bu merkezden aç."));

            card.Add(MakeBtn("🧩 Tüm Config Asset'ler", DataManagerController.CreateAllConfigAssets));
            card.Add(MakeBtn("🔄 Level Catalog Yenile", DataManagerController.RegenerateLevelCatalog));
            card.Add(MakeBtn("🩹 Eksik Level Referansları", DataManagerController.FixMissingLevelReferences));
            card.Add(MakeBtn("📦 Veri Yöneticisi", EditorDataManager.ShowWindow));
            card.Add(MakeBtn("⚙️ Config Validator", ConfigValidator.ShowWindow));
            card.Add(MakeBtn("🚧 Level Generator", GenerateLevels.ShowWindow));
            card.Add(MakeBtn("🧩 LevelCatalog Düzeltici", () => LevelCatalogFixer.FixProceduralEntries()));
            card.Add(MakeBtn("🧬 Eksik Referans Onar", FixMissingScriptRefs.FixMissingRefs));
            card.Add(MakeBtn("🧱 UI Prefab Üret", UIPrefabCreator.CreateAllUIPrefabs));
            card.Add(MakeBtn("🎵 Eksik Audio Oluştur", AudioClipGenerator.GenerateAllAudioClips));
            card.Add(MakeBtn("🧩 Phase Asset Üret", PhaseAssetGenerator.GeneratePhaseAssets));
            card.Add(MakeBtn("🔤 Emoji Font Kurulumu", PixelFlow.EditorTools.PixelFlowEmojiFontSetup.SetupEmojiFallback));
            card.Add(MakeBtn("🔗 Auto-Reference Views", AutoReferenceEditor.AutoReferenceAllViewsInScene));

            parent.Add(card);
        }

        private void RunFullLevelAudit()
        {
            var levels = AssetDatabase.FindAssets("t:LevelData")
                .Select(g => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(l => l != null)
                .OrderBy(l => l.levelIndex)
                .ToList();

            var solver = new RuntimePathSolver();
            var validator = new LevelValidator(solver);
            int validCount = 0;
            int solvableCount = 0;
            int issueCount = 0;

            foreach (var level in levels)
            {
                var result = validator.Validate(level);
                if (result.IsValid) validCount++;
                if (result.IsSolvable) solvableCount++;
                issueCount += result.Issues.Count;
            }

            Debug.Log($"[PixelFlowSetupWindow] Full Level Audit: {levels.Count} level, {validCount} valid, {solvableCount} solvable, {issueCount} issues.");
        }

        private void CreateNewStopSkin()
        {
            string dir = "Assets/Resources/Configs/Skins";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var skin = CreateInstance<StopSkinConfig>();
            skin.SkinId = $"stop_skin_{System.DateTime.Now.Ticks}";
            skin.DisplayName = "Yeni Durak Skin";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/StopSkin_New.asset");
            AssetDatabase.CreateAsset(skin, path); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Selection.activeObject = skin; EditorGUIUtility.PingObject(skin);
        }

        private void CreateStandardStopSkinSuite()
        {
            string dir = "Assets/Resources/Configs/Skins";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            void Make(string path, string id, string name, int palette, int cost, bool ad)
            {
                if (AssetDatabase.LoadAssetAtPath<StopSkinConfig>(path) != null) return;
                var s = CreateInstance<StopSkinConfig>();
                s.SkinId = id; s.DisplayName = name; s.ThemePalette = palette; s.UnlockCoinCost = cost; s.RequiresRewardedAd = ad;
                AssetDatabase.CreateAsset(s, path);
            }

            Make($"{dir}/StopSkin_PastelPark.asset", "stop_skin_pastelpark", "Pastel Park", 1, 800, false);
            Make($"{dir}/StopSkin_NeonCity.asset", "stop_skin_neoncity", "Neon City", 2, 1000, false);
            Make($"{dir}/StopSkin_CandyLand.asset", "stop_skin_candyland", "Candy Land", 1, 1200, true);
            Make($"{dir}/StopSkin_SpaceStation.asset", "stop_skin_spacestation", "Space Station", 2, 1500, true);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("[PixelFlow] Standart stop skin paketi oluşturuldu.");
        }
    }
}
#endif
