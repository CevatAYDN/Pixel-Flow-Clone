using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Views
{
    public class DailyCrisisMediator : Mediator<DailyCrisisView>
    {
        [Inject] public IDailyCrisisModel DailyCrisisModel { get; set; }
        [Inject] public IDailyCrisisService DailyCrisisService { get; set; }

        protected override void OnBind()
        {
            View.OnCloseClicked += HandleCloseClicked;
            View.OnStartCrisisClicked += HandleStartCrisisClicked;
            if (DailyCrisisModel != null)
            {
                DailyCrisisModel.OnDailyCrisisUpdated += RefreshUI;
                RefreshUI();
            }
        }

        protected override void OnUnbind()
        {
            View.OnCloseClicked -= HandleCloseClicked;
            View.OnStartCrisisClicked -= HandleStartCrisisClicked;
            if (DailyCrisisModel != null)
            {
                DailyCrisisModel.OnDailyCrisisUpdated -= RefreshUI;
            }
        }

        private void HandleCloseClicked()
        {
            View.Hide();
        }

        private void HandleStartCrisisClicked(int index)
        {
            if (DailyCrisisService == null) return;
            var crisisLevel = DailyCrisisService.GenerateDailyCrisisLevel(index);
            if (crisisLevel != null)
            {
                View.Hide();
                SignalBus?.Fire(new LoadLevelSignal { LevelToLoad = crisisLevel });
            }
        }

        private void RefreshUI()
        {
            if (DailyCrisisModel == null || View == null) return;
            View.UpdateInfo(
                DailyCrisisModel.StreakCount,
                DailyCrisisModel.BadgesEarned,
                DailyCrisisModel.IsCrisisCompleted(0),
                DailyCrisisModel.IsCrisisCompleted(1),
                DailyCrisisModel.IsCrisisCompleted(2)
            );
        }
    }
}
