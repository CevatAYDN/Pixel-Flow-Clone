using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Nexus.Core;
using PixelFlow.Data;

namespace PixelFlow.Views
{
    [Mediator(typeof(GarageMediator))]
    public class GarageView : View
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Text _coinsText;
        [SerializeField] private Transform _skinContainer;

        public event Action OnCloseClicked;
        public event Action<VehicleSkinConfig> OnBuySkinClicked;
        public event Action<VehicleSkinConfig> OnEquipSkinClicked;

        private void Start()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
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
