using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Nexus.Core;
using Nexus.Core.Services;

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

        [Inject] public ILoggerService LoggerService { get; set; }

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            AutoWireUIReferences();
            BindButtonListeners();
            LogBindingDiagnostics();
        }

        private void LogBindingDiagnostics()
        {
            LoggerService?.Log($"[PixelFlow.MainMenuView] UI Reference Diagnostics: " +
                $"titleText={(bool)_titleText}, coinText={(bool)_coinText}, " +
                $"garageCard={(bool)_garageCard}, vehName={(bool)_equippedVehicleNameText}, " +
                $"vehType={(bool)_equippedVehicleTypeText}, " +
                $"playButton={(bool)_playButton}, playBtnText={(bool)_playButtonText}, " +
                $"garageButton={(bool)_openGarageButton}, settingsButton={(bool)_settingsButton}");

            if (_playButton != null)
                LoggerService?.Log($"[PixelFlow.MainMenuView] PlayButton interactable={_playButton.interactable}, active={_playButton.gameObject.activeInHierarchy}");
            if (_openGarageButton != null)
                LoggerService?.Log($"[PixelFlow.MainMenuView] GarageButton interactable={_openGarageButton.interactable}, active={_openGarageButton.gameObject.activeInHierarchy}");
            if (_settingsButton != null)
                LoggerService?.Log($"[PixelFlow.MainMenuView] SettingsButton interactable={_settingsButton.interactable}, active={_settingsButton.gameObject.activeInHierarchy}");

            var canvas = GetComponent<Canvas>();
            var cg = GetComponent<CanvasGroup>();
            LoggerService?.Log($"[PixelFlow.MainMenuView] Canvas enabled={(canvas != null ? canvas.enabled.ToString() : "null")}, " +
                $"CanvasGroup alpha={(cg != null ? cg.alpha.ToString() : "null")}, " +
                $"blocksRaycasts={(cg != null ? cg.blocksRaycasts.ToString() : "null")}, " +
                $"interactable={(cg != null ? cg.interactable.ToString() : "null")}");

            var es = UnityEngine.EventSystems.EventSystem.current;
            LoggerService?.Log($"[PixelFlow.MainMenuView] EventSystem current={(bool)es}, " +
                $"inputModule={(es != null ? es.currentInputModule?.GetType().Name : "null")}");
        }

        private void BindButtonListeners()
        {
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
                _coinText.text = $"{coins:N0} GOLD";
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
