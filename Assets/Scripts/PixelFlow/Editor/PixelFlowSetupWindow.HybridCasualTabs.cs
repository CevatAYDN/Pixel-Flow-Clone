#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PixelFlow.Data;

namespace PixelFlow.Editor
{
    public partial class PixelFlowSetupWindow : EditorWindow
    {
        // ─── TAB 8: GARAJ & SKİN STÜDYOSU ───
        private void BuildGarageTab(VisualElement parent)
        {
            var guids = AssetDatabase.FindAssets("t:VehicleSkinConfig");
            var card = Card($"🎨 Garaj — {guids.Length} Skin");

            card.Add(MakeBtn("+ Yeni Skin", CreateNewVehicleSkin));
            card.Add(MakeBtn("🍦 Standart Paket", CreateStandardSkinSuite));

            if (guids.Length == 0)
                card.Add(InfoLabel("Henüz skin yok. Yukarıdaki butonu kullanarak oluşturun."));
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
                    r.Add(MakeBtn("Seç", () => { Selection.activeObject = skin; EditorGUIUtility.PingObject(skin); }));
                    card.Add(r);
                }
            }
            parent.Add(card);
        }

        // ─── TAB 9: REKLAM & MONETIZATION ───
        private void BuildAdMonetizationTab(VisualElement parent)
        {
            var card = Card("📺 Reklam & Monetization (MAX / UMP)");
            var gameConfig = Resources.Load<GameConfig>("Configs/GameConfig");
            if (gameConfig != null)
            {
                card.Add(Row("Interstitial Seviye Barajı:", new Label(gameConfig.InterstitialFrequency.ToString()) { style = { color = Color.white, fontSize = 11 } }));
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

            card.Add(MakeBtn("🔍 Doğrulama Çalıştır", () => {
                if (PreBuildDataValidator.ValidateAllData(out var err))
                {
                    Debug.Log("[Validator] ✅ Tüm kontroller geçti.");
                    card.Add(InfoLabel("✅ Tüm kontroller geçti."));
                }
                else
                {
                    Debug.LogError($"[Validator] ❌ {err}");
                    card.Add(InfoLabel($"❌ {err}"));
                }
            }));
            parent.Add(card);
        }
    }
}
#endif
