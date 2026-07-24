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

        public event Action OnCloseClicked;
        public event Action<VehicleSkinConfig> OnBuySkinClicked;
        public event Action<VehicleSkinConfig> OnEquipSkinClicked;

        [Inject] public ILoggerService LoggerService { get; set; }

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            AutoWireUIReferences();
            if (_closeButton != null)
            {
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
            if (_closeButton == null) _closeButton = GetComponentInChildren<Button>(true);
            if (_coinsText == null) _coinsText = GetComponentInChildren<TMP_Text>(true);
            if (_skinContainer == null) _skinContainer = transform.Find("Container") ?? transform;
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
                _coinsText.text = $"{coins:N0} GOLD";
        }

        public void PopulateSkins(IReadOnlyList<VehicleSkinConfig> skins, Func<string, bool> isUnlocked, Func<ColorType, string, bool> isEquipped)
        {
            if (_skinContainer == null) return;
            LoggerService?.Log($"[PixelFlow.GarageView] Populating {skins?.Count ?? 0} vehicle skins in Garage UI...");

            // Clear existing children
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

                var itemObj = new GameObject($"SkinCard_{skin.SkinId}", typeof(RectTransform), typeof(Image), typeof(Button));
                itemObj.transform.SetParent(_skinContainer, false);

                var img = itemObj.GetComponent<Image>();
                img.color = equipped ? new Color(0.2f, 0.6f, 0.3f, 0.9f) : (unlocked ? new Color(0.2f, 0.2f, 0.3f, 0.9f) : new Color(0.12f, 0.12f, 0.15f, 0.9f));

                var btn = itemObj.GetComponent<Button>();
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

                var textObj = new GameObject("Label", typeof(RectTransform));
                textObj.transform.SetParent(itemObj.transform, false);
                var tmp = textObj.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 20;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;

                string statusStr = equipped ? "[KUŞANILDI]" : (unlocked ? "[KUŞAN]" : $"[{skin.UnlockCoinCost} GOLD]");
                tmp.text = $"{skin.DisplayName}\n{statusStr}";
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
    }
}
