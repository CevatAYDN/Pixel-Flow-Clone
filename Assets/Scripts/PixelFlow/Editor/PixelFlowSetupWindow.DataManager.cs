#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PixelFlow.Data;
using System.IO;

namespace PixelFlow.Editor
{
    public partial class PixelFlowSetupWindow : EditorWindow
    {
        // ─── TAB 4: DATA YÖNETİCİSİ ───
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
                ("Configs/DifficultyFormulaConfig", "DifficultyFormulaConfig"),
                ("Configs/DefaultSkinIdsConfig", "DefaultSkinIdsConfig"),
                ("Configs/BouncyPhysicsConfig", "BouncyPhysicsConfig"),
                ("Configs/StarCriteriaConfig", "StarCriteriaConfig"),
                ("Configs/RushHourConfig", "RushHourConfig"),
            };
            foreach (var (p, n) in types)
            {
                var asset = Resources.Load(p);
                var r = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
                var lbl = new Label($"{n}:");
                lbl.style.width = 200; lbl.style.color = new Color(0.78f, 0.82f, 0.85f); lbl.style.fontSize = 11;
                r.Add(lbl);
                r.Add(StatusLabel(asset != null));
                if (asset != null)
                {
                    r.Add(MakeBtn("Seç", () => { Selection.activeObject = asset; EditorGUIUtility.PingObject(asset); }));
                }
                card.Add(r);
            }
            parent.Add(card);

            var saveCard = Card("💾 Kayıt Yönetimi");
            saveCard.Add(MakeBtn("Save PlayerPrefs", () => PlayerPrefs.Save()));
            saveCard.Add(MakeBtn("Tüm Seviyeleri Aç", () => { PlayerPrefs.SetInt("UnlockedLevels", 999); PlayerPrefs.Save(); }));
            saveCard.Add(MakeBtn("Progress Sıfırla", () => { PlayerPrefs.SetInt("UnlockedLevels", 1); PlayerPrefs.Save(); }));
            saveCard.Add(MakeBtn("Tüm Kaydı Sil", () => { PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); }));
            parent.Add(saveCard);
        }

        // ─── TAB 5: EKONOMİ & ISI HARİTASI ───
        private void BuildEconomyHeatmapTab(VisualElement parent)
        {
            var card = Card("💰 Ekonomi & Isı Haritası (Balance Curve)");
            var gameConfig = Resources.Load<GameConfig>("Configs/GameConfig");
            var economyConfig = Resources.Load<EconomyConfigAsset>("Configs/EconomyConfig");
            if (gameConfig != null)
            {
                card.Add(Row("Ödüllü Reklam Coin Ödülü:", new Label(gameConfig.RewardedAdCoinReward.ToString()) { style = { color = Color.white, fontSize = 11 } }));
                card.Add(Row("Günlük Sandık Ödülü:", new Label($"{gameConfig.DailyChestCoins} Coin") { style = { color = Color.white, fontSize = 11 } }));
            }
            if (economyConfig != null)
            {
                card.Add(Row("Hücre Taban Skoru:", new Label($"{economyConfig.BaseScorePerCell}") { style = { color = Color.white, fontSize = 11 } }));
                card.Add(Row("3-Yıldız Maks Viyadük:", new Label($"{economyConfig.ThreeStarsMaxViaducts}") { style = { color = Color.white, fontSize = 11 } }));
                card.Add(Row("2-Yıldız Maks Viyadük:", new Label($"{economyConfig.TwoStarsMaxViaducts}") { style = { color = Color.white, fontSize = 11 } }));
                card.Add(MakeBtn("Ekonomi Inspector'da Aç", () => { Selection.activeObject = economyConfig; EditorGUIUtility.PingObject(economyConfig); }));
            }
            else
            {
                card.Add(InfoLabel("EconomyConfig asset Resources/Configs/EconomyConfig konumunda bulunamadı."));
            }
            parent.Add(card);
        }
    }
}
#endif
