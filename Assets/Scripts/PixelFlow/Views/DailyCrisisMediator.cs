using Nexus.Core;
using Nexus.Core.Services;
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
        [Inject] public ILoggerService LoggerService { get; set; }

        protected override void OnBind()
        {
            LoggerService?.Log("[PixelFlow.DailyCrisisMediator] Binding Daily Crisis UI...");
            View.OnCloseClicked += HandleCloseClicked;
            View.OnStartCrisisClicked += HandleStartCrisisClicked;
            if (DailyCrisisModel != null)
            {
                DailyCrisisModel.OnDailyCrisisUpdated += RefreshUI;
                RefreshUI();
            }
            LoggerService?.Log("[PixelFlow.DailyCrisisMediator] Daily Crisis UI bound successfully.");
        }

        protected override void OnUnbind()
        {
            LoggerService?.Log("[PixelFlow.DailyCrisisMediator] Unbinding Daily Crisis UI...");
            View.OnCloseClicked -= HandleCloseClicked;
            View.OnStartCrisisClicked -= HandleStartCrisisClicked;
            if (DailyCrisisModel != null)
            {
                DailyCrisisModel.OnDailyCrisisUpdated -= RefreshUI;
            }
        }

        private void HandleCloseClicked()
        {
            LoggerService?.Log("[PixelFlow.DailyCrisisMediator] Closing Daily Crisis modal...");
            View.Hide();
        }

        private void HandleStartCrisisClicked(int index)
        {
            LoggerService?.Log($"[PixelFlow.DailyCrisisMediator] Starting Daily Crisis level at index: {index}");
            if (DailyCrisisService == null) return;
            var crisisLevel = DailyCrisisService.GenerateDailyCrisisLevel(index);
            if (crisisLevel != null)
            {
                LoggerService?.Log($"[PixelFlow.DailyCrisisMediator] Firing LoadLevelSignal for Daily Crisis Level '{crisisLevel.name}'");
                View.Hide();
                SignalBus?.Fire(new LoadLevelSignal { LevelToLoad = crisisLevel });
            }
            else
            {
                LoggerService?.LogError($"[PixelFlow.DailyCrisisMediator] Failed to generate Daily Crisis Level at index: {index}");
            }
        }

        private void RefreshUI()
        {
            if (DailyCrisisModel == null || View == null) return;
            LoggerService?.Log($"[PixelFlow.DailyCrisisMediator] Refreshing Daily Crisis UI (Streak: {DailyCrisisModel.StreakCount}, Badges: {DailyCrisisModel.BadgesEarned})");
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
