using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;

namespace PixelFlow.Views
{
    [Mediator(typeof(GarageMediator))]
    public class GarageView : View
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private Transform _skinContainer;
        [SerializeField] private Transform _stopSkinContainer;

        public event Action OnCloseClicked;
        public event Action<VehicleSkinConfig> OnBuySkinClicked;
        public event Action<VehicleSkinConfig> OnEquipSkinClicked;
        public event Action<StopSkinConfig> OnBuyStopSkinClicked;
        public event Action<StopSkinConfig> OnEquipStopSkinClicked;

        [Inject] public ILoggerService LoggerService { get; set; }

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            AutoWireUIReferences();
            if (_skinContainer == null)
            {
                var content = transform.Find("GarageCard/ScrollView/Viewport/Content");
                if (content == null) content = transform.Find("GarageCard/Container");
                if (content == null) content = transform.Find("Container");
                _skinContainer = content != null ? content : transform;
            }
            if (_closeButton != null)
            {
                ButtonJuice.AttachTo(_closeButton);
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() =>
                {
                    LoggerService?.Log("[PixelFlow.GarageView] Close button clicked.");
                    OnCloseClicked?.Invoke();
                });
            }

            LoggerService?.Log($"[PixelFlow.GarageView] AutoWire: panel={(bool)_panel}, closeButton={(bool)_closeButton}, " +
                $"coinsText={(bool)_coinsText}, skinContainer={(bool)_skinContainer}");
        }

        public void AutoWireUIReferences()
        {
            if (_panel == null) _panel = gameObject;

            // Search for existing close button
            if (_closeButton == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                foreach (var button in buttons)
                {
                    string name = button.gameObject.name.ToLowerInvariant();
                    if (name.Contains("close") || name.Contains("back") || name.Contains("kapat"))
                    {
                        _closeButton = button;
                        break;
                    }
                }
            }

            // Create a prominent top-right AAA close button if missing
            if (_closeButton == null)
            {
                var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
                closeGo.transform.SetParent(transform, false);
                var closeRect = closeGo.GetComponent<RectTransform>();
                closeRect.anchorMin = new Vector2(1f, 1f);
                closeRect.anchorMax = new Vector2(1f, 1f);
                closeRect.pivot = new Vector2(1f, 1f);
                closeRect.anchoredPosition = new Vector2(-20f, -20f);
                closeRect.sizeDelta = new Vector2(44f, 44f);

                var img = closeGo.GetComponent<Image>();
                img.color = new Color(0.94f, 0.27f, 0.27f); // Vibrant Red #EF4444

                var txtGo = new GameObject("Text", typeof(RectTransform));
                txtGo.transform.SetParent(closeGo.transform, false);
                var txt = txtGo.AddComponent<TextMeshProUGUI>();
                txt.text = "✕";
                txt.fontSize = 24;
                txt.fontStyle = FontStyles.Bold;
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.Center;
                var txtRect = txtGo.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.sizeDelta = Vector2.zero;

                _closeButton = closeGo.GetComponent<Button>();
            }

            if (_closeButton != null)
            {
                ButtonJuice.AttachTo(_closeButton);
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() =>
                {
                    LoggerService?.Log("[PixelFlow.GarageView] Close button clicked.");
                    OnCloseClicked?.Invoke();
                });
            }

            if (_coinsText == null)
            {
                var texts = GetComponentsInChildren<TMP_Text>(true);
                foreach (var text in texts)
                {
                    string name = text.gameObject.name.ToLowerInvariant();
                    if (name.Contains("coin") || name.Contains("gold"))
                    {
                        _coinsText = text;
                        break;
                    }
                }

                if (_coinsText == null) _coinsText = GetComponentInChildren<TMP_Text>(true);
            }

            if (_skinContainer == null)
            {
                var content = transform.Find("GarageCard/ScrollView/Viewport/Content");
                if (content == null) content = transform.Find("GarageCard/Container");
                if (content == null) content = transform.Find("Container");
                _skinContainer = content != null ? content : transform;
            }
            if (_stopSkinContainer == null) _stopSkinContainer = transform.Find("StopSkinContainer") ?? _skinContainer;
        }

        public void SetActive(bool active)
        {
            LoggerService?.Log($"[PixelFlow.GarageView] SetActive -> {active}");
            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = active ? 1f : 0f;
            cg.blocksRaycasts = active;
            cg.interactable = active;

            var canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = active;
        }

        public void UpdateCoins(int coins)
        {
            LoggerService?.Log($"[PixelFlow.GarageView] Updating UI Coins: {coins:N0}");
            if (_coinsText != null)
                _coinsText.text = $"{coins:N0}";
        }

        private static string SanitizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var sb = new System.Text.StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsHighSurrogate(c) || char.IsLowSurrogate(c)) continue;
                sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        private static Color GetColorFamilyBg(ColorType color)
        {
            return color switch
            {
                ColorType.Red => new Color(0.99f, 0.88f, 0.88f),
                ColorType.Blue => new Color(0.88f, 0.94f, 0.99f),
                ColorType.Green => new Color(0.88f, 0.99f, 0.92f),
                ColorType.Yellow => new Color(0.99f, 0.97f, 0.82f),
                ColorType.Purple => new Color(0.94f, 0.88f, 0.99f),
                _ => new Color(0.92f, 0.95f, 0.98f)
            };
        }

        private void EnsureLayoutGroup(Transform container)
        {
            if (container == null) return;
            if (container.parent != null && container.parent.GetComponent<RectMask2D>() == null)
            {
                container.parent.gameObject.AddComponent<RectMask2D>();
            }
            var grid = container.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = container.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(92, 100);
                grid.spacing = new Vector2(8, 8);
                grid.padding = new RectOffset(6, 6, 6, 6);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
            }
        }

        public void PopulateSkins(IReadOnlyList<VehicleSkinConfig> skins, Func<string, bool> isUnlocked, Func<ColorType, string, bool> isEquipped, Func<int, string> formatCoinCost, string equipLabel, string equippedLabel)
        {
            if (_skinContainer == null) return;
            EnsureLayoutGroup(_skinContainer);
            LoggerService?.Log($"[PixelFlow.GarageView] Populating {skins?.Count ?? 0} vehicle skins in Garage UI...");

            for (int i = _skinContainer.childCount - 1; i >= 0; i--)
            {
                var child = _skinContainer.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            if (skins == null) return;

            foreach (var skin in skins)
            {
                if (skin == null) continue;
                bool unlocked = isUnlocked != null && isUnlocked(skin.SkinId);
                bool equipped = isEquipped != null && isEquipped(skin.ColorFamily, skin.SkinId);

                // Main Rounded White Card (index.html style)
                var itemObj = new GameObject($"SkinCard_{skin.SkinId}", typeof(RectTransform), typeof(Image), typeof(Button));
                itemObj.transform.SetParent(_skinContainer, false);

                var img = itemObj.GetComponent<Image>();
                img.color = Color.white;

                var btn = itemObj.GetComponent<Button>();
                ButtonJuice.AttachTo(btn);
                var capturedSkin = skin;

                btn.onClick.AddListener(() =>
                {
                    if (unlocked)
                    {
                        LoggerService?.Log($"[PixelFlow.GarageView] Card clicked -> Equip {capturedSkin.DisplayName}");
                        TriggerEquipSkin(capturedSkin);
                    }
                    else
                    {
                        LoggerService?.Log($"[PixelFlow.GarageView] Card clicked -> Buy {capturedSkin.DisplayName} for {capturedSkin.UnlockCoinCost}");
                        TriggerBuySkin(capturedSkin);
                    }
                });

                // Top Preview Panel
                var prevObj = new GameObject("PreviewPanel", typeof(RectTransform), typeof(Image));
                prevObj.transform.SetParent(itemObj.transform, false);
                var prevImg = prevObj.GetComponent<Image>();
                prevImg.color = GetColorFamilyBg(skin.ColorFamily);
                var prevRect = prevObj.GetComponent<RectTransform>();
                prevRect.anchorMin = new Vector2(0.06f, 0.44f);
                prevRect.anchorMax = new Vector2(0.94f, 0.94f);
                prevRect.sizeDelta = Vector2.zero;

                // Skin Display Name
                var nameObj = new GameObject("SkinName", typeof(RectTransform));
                nameObj.transform.SetParent(itemObj.transform, false);
                var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
                nameTmp.fontSize = 11;
                nameTmp.fontStyle = FontStyles.Bold;
                nameTmp.alignment = TextAlignmentOptions.Center;
                nameTmp.color = new Color(0.06f, 0.09f, 0.16f); // #0F172A Dark Slate
                nameTmp.text = SanitizeText(skin.DisplayName);
                var nameRect = nameObj.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0.04f, 0.22f);
                nameRect.anchorMax = new Vector2(0.96f, 0.42f);
                nameRect.sizeDelta = Vector2.zero;

                // Status Badge Panel
                var badgeObj = new GameObject("StatusBadge", typeof(RectTransform), typeof(Image));
                badgeObj.transform.SetParent(itemObj.transform, false);
                var badgeImg = badgeObj.GetComponent<Image>();
                badgeImg.color = equipped ? new Color(0.92f, 0.99f, 0.95f) : (unlocked ? new Color(0.94f, 0.96f, 1f) : new Color(0.99f, 0.95f, 0.78f));
                var badgeRect = badgeObj.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(0.06f, 0.04f);
                badgeRect.anchorMax = new Vector2(0.94f, 0.20f);
                badgeRect.sizeDelta = Vector2.zero;

                var badgeTxtObj = new GameObject("Text", typeof(RectTransform));
                badgeTxtObj.transform.SetParent(badgeObj.transform, false);
                var badgeTmp = badgeTxtObj.AddComponent<TextMeshProUGUI>();
                badgeTmp.fontSize = 10;
                badgeTmp.fontStyle = FontStyles.Bold;
                badgeTmp.alignment = TextAlignmentOptions.Center;
                badgeTmp.color = equipped ? new Color(0.02f, 0.59f, 0.41f) : (unlocked ? new Color(0.14f, 0.38f, 0.92f) : new Color(0.7f, 0.35f, 0.05f));
                badgeTmp.text = equipped ? equippedLabel : (unlocked ? equipLabel : formatCoinCost(skin.UnlockCoinCost));
                var badgeTxtRect = badgeTxtObj.GetComponent<RectTransform>();
                badgeTxtRect.anchorMin = Vector2.zero;
                badgeTxtRect.anchorMax = Vector2.one;
                badgeTxtRect.sizeDelta = Vector2.zero;
            }
        }

        public void PopulateStopSkins(IReadOnlyList<StopSkinConfig> skins, Func<string, bool> isUnlocked, Func<ColorType, string, bool> isEquipped, Func<int, string> formatCoinCost, string equipLabel, string equippedLabel)
        {
            var container = _stopSkinContainer != null ? _stopSkinContainer : _skinContainer;
            if (container == null) return;
            EnsureLayoutGroup(container);
            LoggerService?.Log($"[PixelFlow.GarageView] Populating {skins?.Count ?? 0} stop skins in Garage UI...");

            if (_stopSkinContainer != null && _stopSkinContainer != _skinContainer)
            {
                for (int i = _stopSkinContainer.childCount - 1; i >= 0; i--)
                {
                    var child = _stopSkinContainer.GetChild(i).gameObject;
                    if (Application.isPlaying) Destroy(child);
                    else DestroyImmediate(child);
                }
            }

            if (skins == null) return;

            foreach (var skin in skins)
            {
                if (skin == null) continue;
                bool unlocked = isUnlocked != null && isUnlocked(skin.SkinId);
                bool equipped = isEquipped != null && isEquipped((ColorType)skin.ThemePalette, skin.SkinId);

                var itemObj = new GameObject($"StopSkinCard_{skin.SkinId}", typeof(RectTransform), typeof(Image), typeof(Button));
                itemObj.transform.SetParent(container, false);

                var img = itemObj.GetComponent<Image>();
                img.color = Color.white;

                var btn = itemObj.GetComponent<Button>();
                var capturedSkin = skin;

                btn.onClick.AddListener(() =>
                {
                    if (unlocked)
                    {
                        LoggerService?.Log($"[PixelFlow.GarageView] Card clicked -> Equip Stop {capturedSkin.DisplayName}");
                        TriggerEquipStopSkin(capturedSkin);
                    }
                    else
                    {
                        LoggerService?.Log($"[PixelFlow.GarageView] Card clicked -> Buy Stop {capturedSkin.DisplayName} for {capturedSkin.UnlockCoinCost}");
                        TriggerBuyStopSkin(capturedSkin);
                    }
                });

                var prevObj = new GameObject("PreviewPanel", typeof(RectTransform), typeof(Image));
                prevObj.transform.SetParent(itemObj.transform, false);
                var prevImg = prevObj.GetComponent<Image>();
                prevImg.color = GetColorFamilyBg((ColorType)skin.ThemePalette);
                var prevRect = prevObj.GetComponent<RectTransform>();
                prevRect.anchorMin = new Vector2(0.06f, 0.44f);
                prevRect.anchorMax = new Vector2(0.94f, 0.94f);
                prevRect.sizeDelta = Vector2.zero;

                var nameObj = new GameObject("SkinName", typeof(RectTransform));
                nameObj.transform.SetParent(itemObj.transform, false);
                var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
                nameTmp.fontSize = 11;
                nameTmp.fontStyle = FontStyles.Bold;
                nameTmp.alignment = TextAlignmentOptions.Center;
                nameTmp.color = new Color(0.06f, 0.09f, 0.16f);
                nameTmp.text = SanitizeText(skin.DisplayName);
                var nameRect = nameObj.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0.04f, 0.22f);
                nameRect.anchorMax = new Vector2(0.96f, 0.42f);
                nameRect.sizeDelta = Vector2.zero;

                var badgeObj = new GameObject("StatusBadge", typeof(RectTransform), typeof(Image));
                badgeObj.transform.SetParent(itemObj.transform, false);
                var badgeImg = badgeObj.GetComponent<Image>();
                badgeImg.color = equipped ? new Color(0.92f, 0.99f, 0.95f) : (unlocked ? new Color(0.94f, 0.96f, 1f) : new Color(0.99f, 0.95f, 0.78f));
                var badgeRect = badgeObj.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(0.06f, 0.04f);
                badgeRect.anchorMax = new Vector2(0.94f, 0.20f);
                badgeRect.sizeDelta = Vector2.zero;

                var badgeTxtObj = new GameObject("Text", typeof(RectTransform));
                badgeTxtObj.transform.SetParent(badgeObj.transform, false);
                var badgeTmp = badgeTxtObj.AddComponent<TextMeshProUGUI>();
                badgeTmp.fontSize = 10;
                badgeTmp.fontStyle = FontStyles.Bold;
                badgeTmp.alignment = TextAlignmentOptions.Center;
                badgeTmp.color = equipped ? new Color(0.02f, 0.59f, 0.41f) : (unlocked ? new Color(0.14f, 0.38f, 0.92f) : new Color(0.7f, 0.35f, 0.05f));
                badgeTmp.text = equipped ? equippedLabel : (unlocked ? equipLabel : formatCoinCost(skin.UnlockCoinCost));
                var badgeTxtRect = badgeTxtObj.GetComponent<RectTransform>();
                badgeTxtRect.anchorMin = Vector2.zero;
                badgeTxtRect.anchorMax = Vector2.one;
                badgeTxtRect.sizeDelta = Vector2.zero;
            }
        }

        public void TriggerBuySkin(VehicleSkinConfig skin)
        {
            LoggerService?.Log($"[PixelFlow.GarageView] TriggerBuySkin -> {skin?.SkinId}");
            OnBuySkinClicked?.Invoke(skin);
        }

        public void TriggerEquipSkin(VehicleSkinConfig skin)
        {
            LoggerService?.Log($"[PixelFlow.GarageView] TriggerEquipSkin -> {skin?.SkinId}");
            OnEquipSkinClicked?.Invoke(skin);
        }

        public void TriggerBuyStopSkin(StopSkinConfig skin)
        {
            LoggerService?.Log($"[PixelFlow.GarageView] TriggerBuyStopSkin -> {skin?.SkinId}");
            OnBuyStopSkinClicked?.Invoke(skin);
        }

        public void TriggerEquipStopSkin(StopSkinConfig skin)
        {
            LoggerService?.Log($"[PixelFlow.GarageView] TriggerEquipStopSkin -> {skin?.SkinId}");
            OnEquipStopSkinClicked?.Invoke(skin);
        }
    }
}
