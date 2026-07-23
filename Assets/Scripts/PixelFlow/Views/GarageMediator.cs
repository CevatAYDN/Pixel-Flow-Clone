using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Views
{
    public class GarageMediator : Mediator<GarageView>
    {
        [Inject] public IInventoryModel InventoryModel { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        protected override void OnBind()
        {
            LoggerService?.Log("[PixelFlow.GarageMediator] Binding Garage UI...");
            if (View == null || InventoryModel == null) return;

            View.UpdateCoins(InventoryModel.Coins);
            InventoryModel.OnCoinsChanged += HandleCoinsChanged;

            View.OnCloseClicked += OnClose;
            View.OnBuySkinClicked += HandleBuySkin;
            View.OnEquipSkinClicked += HandleEquipSkin;

            LoggerService?.Log("[PixelFlow.GarageMediator] Garage UI successfully bound.");
        }

        protected override void OnUnbind()
        {
            LoggerService?.Log("[PixelFlow.GarageMediator] Unbinding Garage UI...");
            if (View != null)
            {
                View.OnCloseClicked -= OnClose;
                View.OnBuySkinClicked -= HandleBuySkin;
                View.OnEquipSkinClicked -= HandleEquipSkin;
            }

            if (InventoryModel != null)
            {
                InventoryModel.OnCoinsChanged -= HandleCoinsChanged;
            }
        }

        private void HandleCoinsChanged(int coins)
        {
            LoggerService?.Log($"[PixelFlow.GarageMediator] Coins updated in Garage UI: {coins:N0}");
            View?.UpdateCoins(coins);
        }

        private void HandleBuySkin(VehicleSkinConfig skin)
        {
            LoggerService?.Log($"[PixelFlow.GarageMediator] Purchase skin requested: {(skin != null ? skin.DisplayName : "Unknown")}");
        }

        private void HandleEquipSkin(VehicleSkinConfig skin)
        {
            LoggerService?.Log($"[PixelFlow.GarageMediator] Equip skin requested: {(skin != null ? skin.DisplayName : "Unknown")}");
        }

        private void OnClose()
        {
            LoggerService?.Log("[PixelFlow.GarageMediator] Closing Garage panel...");
            View?.SetActive(false);
        }
    }
}
