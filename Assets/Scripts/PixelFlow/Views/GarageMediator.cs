using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Views
{
    public class GarageMediator : Mediator<GarageView>
    {
        [Inject] public IInventoryModel InventoryModel { get; set; }

        protected override void OnBind()
        {
            if (View == null || InventoryModel == null) return;

            View.UpdateCoins(InventoryModel.Coins);
            InventoryModel.OnCoinsChanged += View.UpdateCoins;

            View.OnCloseClicked += OnClose;
        }

        protected override void OnUnbind()
        {
            if (View != null)
            {
                View.OnCloseClicked -= OnClose;
            }

            if (InventoryModel != null)
            {
                InventoryModel.OnCoinsChanged -= View.UpdateCoins;
            }
        }

        private void OnClose()
        {
            View?.SetActive(false);
        }
    }
}
