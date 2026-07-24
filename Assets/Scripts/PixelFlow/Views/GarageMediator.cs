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
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }

        protected override void OnBind()
        {
            LoggerService?.Log("[PixelFlow.GarageMediator] Binding Garage UI...");
            if (View == null || InventoryModel == null) return;

            View.UpdateCoins(InventoryModel.Coins);
            InventoryModel.OnCoinsChanged += HandleCoinsChanged;

            View.OnCloseClicked += OnClose;
            View.OnBuySkinClicked += HandleBuySkin;
            View.OnEquipSkinClicked += HandleEquipSkin;

            Subscribe<ShowGarageSignal>(_ =>
            {
                LoggerService?.Log("[PixelFlow.GarageMediator] ShowGarageSignal received, opening panel.");
                View?.SetActive(true);
            });

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
            if (skin == null) return;
            LoggerService?.Log($"[PixelFlow.GarageMediator] Purchase skin requested: {skin.DisplayName} for {skin.UnlockCoinCost} coins");

            if (InventoryModel == null) return;

            if (InventoryModel.IsSkinUnlocked(skin.SkinId))
            {
                LoggerService?.Log($"[PixelFlow.GarageMediator] Skin {skin.SkinId} is already unlocked. Equipping.");
                InventoryModel.EquipSkin(skin.ColorFamily, skin.SkinId);
                return;
            }

            if (InventoryModel.TrySpendCoins(skin.UnlockCoinCost))
            {
                InventoryModel.UnlockSkin(skin.SkinId);
                InventoryModel.EquipSkin(skin.ColorFamily, skin.SkinId);
                LoggerService?.Log($"[PixelFlow.GarageMediator] Successfully purchased and equipped skin: {skin.SkinId}");
            }
            else
            {
                LoggerService?.LogWarning($"[PixelFlow.GarageMediator] Failed to purchase skin {skin.SkinId}: Not enough coins!");
            }
        }

        private void HandleEquipSkin(VehicleSkinConfig skin)
        {
            if (skin == null || InventoryModel == null) return;
            LoggerService?.Log($"[PixelFlow.GarageMediator] Equip skin requested: {skin.DisplayName}");

            if (InventoryModel.IsSkinUnlocked(skin.SkinId))
            {
                InventoryModel.EquipSkin(skin.ColorFamily, skin.SkinId);
                LoggerService?.Log($"[PixelFlow.GarageMediator] Successfully equipped skin: {skin.SkinId}");
            }
            else
            {
                LoggerService?.LogWarning($"[PixelFlow.GarageMediator] Cannot equip skin {skin.SkinId}: Skin is locked!");
            }
        }

        private void OnClose()
        {
            LoggerService?.Log("[PixelFlow.GarageMediator] Closing Garage panel...");
            View?.SetActive(false);
        }
    }
}
