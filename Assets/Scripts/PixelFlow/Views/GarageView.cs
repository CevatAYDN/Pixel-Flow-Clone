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
                _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());

            LoggerService?.Log($"[PixelFlow.GarageView] AutoWire: panel={(bool)_panel}, closeButton={(bool)_closeButton}, " +
                $"coinsText={(bool)_coinsText}, skinContainer={(bool)_skinContainer}");
            if (_closeButton != null)
                LoggerService?.Log($"[PixelFlow.GarageView] CloseButton interactable={_closeButton.interactable}, active={_closeButton.gameObject.activeInHierarchy}");

            var cg = GetComponent<CanvasGroup>();
            var canvas = GetComponent<Canvas>();
            LoggerService?.Log($"[PixelFlow.GarageView] CanvasGroup: alpha={(cg != null ? cg.alpha.ToString("F2") : "null")}, " +
                $"blocksRaycasts={(cg != null ? cg.blocksRaycasts.ToString() : "null")}, interactable={(cg != null ? cg.interactable.ToString() : "null")}");

            var es = UnityEngine.EventSystems.EventSystem.current;
            LoggerService?.Log($"[PixelFlow.GarageView] EventSystem: current={(bool)es}, " +
                $"inputModule={(es != null ? es.currentInputModule?.GetType().Name : "null")}");
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
            if (_coinsText != null)
                _coinsText.text = coins.ToString("N0");
        }

        public void TriggerBuySkin(VehicleSkinConfig skin)
        {
            OnBuySkinClicked?.Invoke(skin);
        }

        public void TriggerEquipSkin(VehicleSkinConfig skin)
        {
            OnEquipSkinClicked?.Invoke(skin);
        }
    }
}
