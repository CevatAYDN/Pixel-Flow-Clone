using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nexus.Core;
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

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            AutoWireUIReferences();
            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
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
            if (_panel != null)
                _panel.SetActive(active);
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
