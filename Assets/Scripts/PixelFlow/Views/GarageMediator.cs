using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;
using UnityEngine;
using System.Collections.Generic;

namespace PixelFlow.Views
{
    public class GarageMediator : Mediator<GarageView>
    {
        [Inject] public IInventoryModel InventoryModel { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }

        private readonly List<VehicleSkinConfig> _availableSkins = new List<VehicleSkinConfig>();

        protected override void OnBind()
        {
            LoggerService?.Log("[PixelFlow.GarageMediator] Binding Garage UI with explicit logging...");
            if (View == null || InventoryModel == null) return;

            InitDefaultSkins();

            View.UpdateCoins(InventoryModel.Coins);
            InventoryModel.OnCoinsChanged += HandleCoinsChanged;
            InventoryModel.OnSkinUnlocked += HandleSkinUnlocked;
            InventoryModel.OnSkinEquipped += HandleSkinEquipped;

            View.OnCloseClicked += OnClose;
            View.OnBuySkinClicked += HandleBuySkin;
            View.OnEquipSkinClicked += HandleEquipSkin;

            Subscribe<ShowGarageSignal>(_ =>
            {
                LoggerService?.Log("[PixelFlow.GarageMediator] ShowGarageSignal received -> Opening Garage panel.");
                View?.SetActive(true);
                RefreshSkinsList();
            });

            LoggerService?.Log("[PixelFlow.GarageMediator] Garage UI bound and ready.");
        }

        private void InitDefaultSkins()
        {
            _availableSkins.Clear();

            // Try loading ScriptableObject assets from Resources/Configs/Skins
            var loadedSkins = Resources.LoadAll<VehicleSkinConfig>("Configs/Skins");
            if (loadedSkins != null && loadedSkins.Length > 0)
            {
                _availableSkins.AddRange(loadedSkins);
                LoggerService?.Log($"[PixelFlow.GarageMediator] Loaded {_availableSkins.Count} VehicleSkinConfig assets from Resources.");
                return;
            }

            // Fallback default skins for each color family (Red, Blue, Green, Yellow, Purple)
            string[] names = { "Varsayılan Otobüs", "Spor Taksi", "Trafik Polisi", "Kırmızı Yarışçı", "Mor Minibüs" };
            ColorType[] colors = { ColorType.Red, ColorType.Blue, ColorType.Green, ColorType.Yellow, ColorType.Purple };

            for (int i = 0; i < names.Length; i++)
            {
                var skin = ScriptableObject.CreateInstance<VehicleSkinConfig>();
                skin.SkinId = i == 0 ? "skin_default" : $"skin_{colors[i].ToString().ToLower()}";
                skin.DisplayName = names[i];
                skin.ColorFamily = colors[i];
                skin.UnlockCoinCost = i * 200;
                _availableSkins.Add(skin);
            }
            LoggerService?.Log($"[PixelFlow.GarageMediator] Initialized {_availableSkins.Count} fallback vehicle skin configs for shop.");
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
                InventoryModel.OnSkinUnlocked -= HandleSkinUnlocked;
                InventoryModel.OnSkinEquipped -= HandleSkinEquipped;
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

        private void RefreshSkinsList()
        {
            if (View == null || InventoryModel == null) return;
            LoggerService?.Log("[PixelFlow.GarageMediator] Refreshing Garage skins list...");
            View.PopulateSkins(
                _availableSkins,
                id => InventoryModel.IsSkinUnlocked(id),
                (color, id) => InventoryModel.GetEquippedSkin(color) == id
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

            if (InventoryModel.TrySpendCoins(skin.UnlockCoinCost))
            {
                InventoryModel.UnlockSkin(skin.SkinId);
                InventoryModel.EquipSkin(skin.ColorFamily, skin.SkinId);
                LoggerService?.Log($"[PixelFlow.GarageMediator] ✔ Successfully purchased and equipped skin: {skin.SkinId}");
                RefreshSkinsList();
            }
            else
            {
                LoggerService?.LogWarning($"[PixelFlow.GarageMediator] ✖ Failed to purchase skin {skin.SkinId}: Insufficient coins! (Cost: {skin.UnlockCoinCost}, Available: {InventoryModel.Coins})");
            }
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

        private void OnClose()
        {
            LoggerService?.Log("[PixelFlow.GarageMediator] Closing Garage panel.");
            View?.SetActive(false);
        }
    }
}
