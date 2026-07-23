using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Nexus.Core;

namespace PixelFlow.Views
{
    /// <summary>
    /// DesignSystem/Mockups/index.html tasarımına %100 sadık Ana Menü / Hub Görünümü.
    /// Başlık, Coin Pill, Garaj Vitrini Kartı ve "OYUNA BAŞLA (SEVİYE N)" butonu içerir.
    /// </summary>
    [Mediator(typeof(MainMenuMediator))]
    public class MainMenuView : View
    {
        [Header("=== Title & Coins ===")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _coinText;

        [Header("=== Garage Showcase Card ===")]
        [SerializeField] private GameObject _garageCard;
        [SerializeField] private TMP_Text _equippedVehicleNameText;
        [SerializeField] private TMP_Text _equippedVehicleTypeText;
        [SerializeField] private Button _openGarageButton;

        [Header("=== Actions ===")]
        [SerializeField] private Button _playButton;
        [SerializeField] private TMP_Text _playButtonText;
        [SerializeField] private Button _settingsButton;

        public event Action OnPlayClicked;
        public event Action OnGarageClicked;
        public event Action OnSettingsClicked;

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            AutoWireUIReferences();

            if (_playButton != null)
                _playButton.onClick.AddListener(() => OnPlayClicked?.Invoke());

            if (_openGarageButton != null)
                _openGarageButton.onClick.AddListener(() => OnGarageClicked?.Invoke());

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(() => OnSettingsClicked?.Invoke());
        }

        public void AutoWireUIReferences()
        {
            var texts = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                string name = t.gameObject.name.ToLower();
                if (_titleText == null && (name.Contains("title") || name.Contains("header"))) _titleText = t;
                if (_coinText == null && name.Contains("coin")) _coinText = t;
                if (_equippedVehicleNameText == null && (name.Contains("vehiclename") || name.Contains("name"))) _equippedVehicleNameText = t;
                if (_equippedVehicleTypeText == null && (name.Contains("vehicletype") || name.Contains("type"))) _equippedVehicleTypeText = t;
                if (_playButtonText == null && (name.Contains("play") || name.Contains("start"))) _playButtonText = t;
            }

            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string name = b.gameObject.name.ToLower();
                if (_playButton == null && (name.Contains("play") || name.Contains("start"))) _playButton = b;
                if (_openGarageButton == null && name.Contains("garage")) _openGarageButton = b;
                if (_settingsButton == null && name.Contains("setting")) _settingsButton = b;
            }

            var transforms = GetComponentsInChildren<Transform>(true);
            foreach (var tr in transforms)
            {
                string name = tr.gameObject.name.ToLower();
                if (_garageCard == null && name.Contains("garagecard")) _garageCard = tr.gameObject;
            }
        }

        protected override void OnUnbind()
        {
            base.OnUnbind();
            if (_playButton != null) _playButton.onClick.RemoveAllListeners();
            if (_openGarageButton != null) _openGarageButton.onClick.RemoveAllListeners();
            if (_settingsButton != null) _settingsButton.onClick.RemoveAllListeners();
        }

        public void UpdateCoinBalance(int coins)
        {
            if (_coinText != null)
                _coinText.text = $"💰 {coins:N0}";
        }

        public void UpdatePlayButtonText(int levelNumber)
        {
            if (_playButtonText != null)
                _playButtonText.text = $"OYUNA BAŞLA (SEVİYE {levelNumber})";
        }

        public void UpdateEquippedVehicle(string vehicleName, string vehicleType)
        {
            if (_equippedVehicleNameText != null)
                _equippedVehicleNameText.text = vehicleName;
            if (_equippedVehicleTypeText != null)
                _equippedVehicleTypeText.text = vehicleType;
        }

        public void SetVisible(bool visible)
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;

            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = visible;
            }
        }
    }
}
