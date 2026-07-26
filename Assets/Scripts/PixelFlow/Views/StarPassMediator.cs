using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Views
{
    public class StarPassMediator : Mediator<StarPassView>
    {
        [Inject] public IInventoryModel InventoryModel { get; set; }
        [Inject] public IProgressModel ProgressModel { get; set; }

        protected override void OnBind()
        {
            View.OnCloseClicked += HandleCloseClicked;
            View.OnClaimClicked += HandleClaimClicked;
            View.OnBuyPassClicked += HandleBuyPassClicked;
            RefreshView();
        }

        protected override void OnUnbind()
        {
            if (View != null)
            {
                View.OnCloseClicked -= HandleCloseClicked;
                View.OnClaimClicked -= HandleClaimClicked;
                View.OnBuyPassClicked -= HandleBuyPassClicked;
            }
        }

        private void HandleCloseClicked()
        {
            View.SetVisible(false);
        }

        private void HandleClaimClicked()
        {
            int currentCoins = InventoryModel?.Coins ?? 0;
            InventoryModel?.AddCoins(100);
            RefreshView();
        }

        private void HandleBuyPassClicked()
        {
            SignalBus?.Fire(new RequestInterstitialAdSignal());
            RefreshView();
        }

        private void RefreshView()
        {
            if (View == null) return;
            int level = ProgressModel?.UnlockedLevels ?? 1;
            int currentTier = Mathf.Clamp(level / 5, 1, 30);
            float progress = (level % 5) / 5f;
            View.UpdateProgress(currentTier, 30, progress, InventoryModel?.IsStarPassActive ?? false);
        }
    }
}
