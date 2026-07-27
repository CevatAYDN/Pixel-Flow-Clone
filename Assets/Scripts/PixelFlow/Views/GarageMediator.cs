using System;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using PixelFlow.Services;
using UnityEngine;
using System.Collections.Generic;

namespace PixelFlow.Views
{
    public class GarageMediator : Mediator<GarageView>
    {
        [Inject] public IInventoryModel InventoryModel { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }
        [Inject] public ISkinCatalogService SkinCatalog { get; set; }
        [Inject] public ILocalizationService LocalizationService { get; set; }

        protected override void OnBind()
        {
            LoggerService?.Log("[PixelFlow.GarageMediator] Binding Garage UI with explicit logging...");
            if (View == null || InventoryModel == null) return;

            if (SkinCatalog == null)
                throw new DataValidationException("GarageMediator.OnBind: ISkinCatalogService not injected! Cannot load skins.");
            if (LocalizationService == null)
                throw new DataValidationException("GarageMediator.OnBind: ILocalizationService not injected! Cannot localize UI.");

            View.UpdateCoins(InventoryModel.Coins);
            InventoryModel.OnCoinsChanged += HandleCoinsChanged;
            InventoryModel.OnSkinUnlocked += HandleSkinUnlocked;
            InventoryModel.OnSkinEquipped += HandleSkinEquipped;
            InventoryModel.OnStopSkinUnlocked += HandleStopSkinUnlocked;
            InventoryModel.OnStopSkinEquipped += HandleStopSkinEquipped;

            View.OnCloseClicked += OnClose;
            View.OnBuySkinClicked += HandleBuySkin;
            View.OnEquipSkinClicked += HandleEquipSkin;
            View.OnBuyStopSkinClicked += HandleBuyStopSkin;
            View.OnEquipStopSkinClicked += HandleEquipStopSkin;

            Subscribe<ShowGarageSignal>(_ =>
            {
                LoggerService?.Log("[PixelFlow.GarageMediator] ShowGarageSignal received -> Opening Garage panel.");
                View?.SetActive(true);
                RefreshSkinsList();
                RefreshStopSkinsList();
            });

            LoggerService?.Log("[PixelFlow.GarageMediator] Garage UI bound and ready.");
        }

        protected override void OnUnbind()
        {
            LoggerService?.Log("[PixelFlow.GarageMediator] Unbinding Garage UI...");
            if (View != null)
            {
                View.OnCloseClicked -= OnClose;
                View.OnBuySkinClicked -= HandleBuySkin;
                View.OnEquipSkinClicked -= HandleEquipSkin;
                View.OnBuyStopSkinClicked -= HandleBuyStopSkin;
                View.OnEquipStopSkinClicked -= HandleEquipStopSkin;
            }

            if (InventoryModel != null)
            {
                InventoryModel.OnCoinsChanged -= HandleCoinsChanged;
                InventoryModel.OnSkinUnlocked -= HandleSkinUnlocked;
                InventoryModel.OnSkinEquipped -= HandleSkinEquipped;
                InventoryModel.OnStopSkinUnlocked -= HandleStopSkinUnlocked;
                InventoryModel.OnStopSkinEquipped -= HandleStopSkinEquipped;
            }
        }

        private void HandleCoinsChanged(int coins)
        {
            LoggerService?.Log($"[PixelFlow.GarageMediator] Coins updated: {coins:N0}");
            View?.UpdateCoins(coins);
            RefreshSkinsList();
        }

        private void HandleSkinUnlocked(string skinId)
        {
            LoggerService?.Log($"[PixelFlow.GarageMediator] Skin unlocked notification: {skinId}");
            RefreshSkinsList();
        }

        private void HandleSkinEquipped(ColorType color, string skinId)
        {
            LoggerService?.Log($"[PixelFlow.GarageMediator] Skin equipped notification: {color} -> {skinId}");
            RefreshSkinsList();
        }

        private void HandleStopSkinUnlocked(string skinId)
        {
            LoggerService?.Log($"[PixelFlow.GarageMediator] Stop skin unlocked notification: {skinId}");
            RefreshStopSkinsList();
        }

        private void HandleStopSkinEquipped(ColorType color, string skinId)
        {
            LoggerService?.Log($"[PixelFlow.GarageMediator] Stop skin equipped notification: {color} -> {skinId}");
            RefreshStopSkinsList();
        }

        private void RefreshSkinsList()
        {
            if (View == null || InventoryModel == null || SkinCatalog == null) return;
            if (LocalizationService == null)
                throw new DataValidationException("GarageMediator.RefreshSkinsList: ILocalizationService is null.");
            
            LoggerService?.Log("[PixelFlow.GarageMediator] Refreshing Garage skins list...");
            
            string equipLabel = LocalizationService.GetString("garage_equip_label");
            string equippedLabel = LocalizationService.GetString("garage_equipped_label");
            Func<int, string> formatCost = cost => LocalizationService.GetString("garage_cost_format").Replace("{0}", cost.ToString());
            
            View.PopulateSkins(
                SkinCatalog.AllVehicleSkins,
                id => InventoryModel.IsSkinUnlocked(id),
                (color, id) => InventoryModel.GetEquippedSkin(color) == id,
                formatCost,
                equipLabel,
                equippedLabel
            );
        }

        private void RefreshStopSkinsList()
        {
            if (View == null || InventoryModel == null || SkinCatalog == null) return;
            if (LocalizationService == null)
                throw new DataValidationException("GarageMediator.RefreshStopSkinsList: ILocalizationService is null.");
            
            LoggerService?.Log("[PixelFlow.GarageMediator] Refreshing Garage stop skins list...");
            
            string equipLabel = LocalizationService.GetString("garage_equip_label");
            string equippedLabel = LocalizationService.GetString("garage_equipped_label");
            Func<int, string> formatCost = cost => LocalizationService.GetString("garage_cost_format").Replace("{0}", cost.ToString());
            
            View.PopulateStopSkins(
                SkinCatalog.AllStopSkins,
                id => InventoryModel.IsStopSkinUnlocked(id),
                (color, id) => InventoryModel.GetEquippedStopSkin(color) == id,
                formatCost,
                equipLabel,
                equippedLabel
            );
        }

        private void HandleBuySkin(VehicleSkinConfig skin)
        {
            if (skin == null) return;
            LoggerService?.Log($"[PixelFlow.GarageMediator] Purchase skin requested: {skin.DisplayName} for {skin.UnlockCoinCost} coins");

            if (InventoryModel == null) return;

            if (InventoryModel.IsSkinUnlocked(skin.SkinId))
            {
                LoggerService?.Log($"[PixelFlow.GarageMediator] Skin {skin.SkinId} is already unlocked. Equipping directly.");
                InventoryModel.EquipSkin(skin.ColorFamily, skin.SkinId);
                RefreshSkinsList();
                return;
            }

            // Fire SkinUnlockedSignal to trigger the command
            SignalBus?.Fire(new SkinUnlockedSignal { SkinId = skin.SkinId, IsPurchase = true });
        }

        private void HandleEquipSkin(VehicleSkinConfig skin)
        {
            if (skin == null || InventoryModel == null) return;
            LoggerService?.Log($"[PixelFlow.GarageMediator] Equip skin requested: {skin.DisplayName}");

            if (InventoryModel.IsSkinUnlocked(skin.SkinId))
            {
                InventoryModel.EquipSkin(skin.ColorFamily, skin.SkinId);
                LoggerService?.Log($"[PixelFlow.GarageMediator] ✔ Successfully equipped skin: {skin.SkinId}");
                RefreshSkinsList();
            }
            else
            {
                LoggerService?.LogWarning($"[PixelFlow.GarageMediator] ✖ Cannot equip skin {skin.SkinId}: Skin is locked!");
            }
        }

        private void HandleBuyStopSkin(StopSkinConfig skin)
        {
            if (skin == null || InventoryModel == null) return;
            LoggerService?.Log($"[PixelFlow.GarageMediator] Purchase stop skin requested: {skin.DisplayName} for {skin.UnlockCoinCost} coins");

            if (InventoryModel.IsStopSkinUnlocked(skin.SkinId))
            {
                LoggerService?.Log($"[PixelFlow.GarageMediator] Stop skin {skin.SkinId} is already unlocked. Equipping directly.");
                InventoryModel.EquipStopSkin((ColorType)skin.ThemePalette, skin.SkinId);
                RefreshStopSkinsList();
                return;
            }

            SignalBus?.Fire(new StopSkinUnlockedSignal { SkinId = skin.SkinId, IsPurchase = true });
        }

        private void HandleEquipStopSkin(StopSkinConfig skin)
        {
            if (skin == null || InventoryModel == null) return;
            LoggerService?.Log($"[PixelFlow.GarageMediator] Equip stop skin requested: {skin.DisplayName}");

            if (InventoryModel.IsStopSkinUnlocked(skin.SkinId))
            {
                InventoryModel.EquipStopSkin((ColorType)skin.ThemePalette, skin.SkinId);
                LoggerService?.Log($"[PixelFlow.GarageMediator] ✔ Successfully equipped stop skin: {skin.SkinId}");
                RefreshStopSkinsList();
            }
            else
            {
                LoggerService?.LogWarning($"[PixelFlow.GarageMediator] ✖ Cannot equip stop skin {skin.SkinId}: Skin is locked!");
            }
        }

        private void OnClose()
        {
            LoggerService?.Log("[PixelFlow.GarageMediator] Closing Garage panel.");
            View?.SetActive(false);
        }
    }
}
