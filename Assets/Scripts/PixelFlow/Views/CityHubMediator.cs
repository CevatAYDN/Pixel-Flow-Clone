using Nexus.Core;
using PixelFlow.Commands;
using PixelFlow.Models;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Views
{
    public class CityHubMediator : Mediator<CityHubView>
    {
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public IProgressModel ProgressModel { get; set; }

        protected override void OnBind()
        {
            GameStateModel.OnStateChanged += HandleStateChanged;

            View.OnDistrictClicked += HandleDistrictClicked;

            RefreshHub();
        }

        protected override void OnUnbind()
        {
            GameStateModel.OnStateChanged -= HandleStateChanged;

            View.OnDistrictClicked -= HandleDistrictClicked;
        }

        private void HandleStateChanged(GameState state)
        {
            bool isHubActive = state == GameState.MainMenu;
            if (View != null && View.gameObject != null)
            {
                View.gameObject.SetActive(isHubActive);
            }
            if (isHubActive)
            {
                View.SetupCamera(true);
                RefreshHub();
            }
        }

        private void HandleDistrictClicked(int districtIndex)
        {
            if (GameStateModel.CurrentState != GameState.MainMenu) return;
            int requiredLevel = EnterDistrictCommand.DistrictToLevelIndex(districtIndex);
            if (requiredLevel < 0) return;
            if (requiredLevel > ProgressModel.UnlockedLevels - 1) return;
            SignalBus.Fire(new EnterDistrictSignal { DistrictIndex = districtIndex });
        }

        private void RefreshHub()
        {
            // District unlock seviyesi artık progress model'den
            int districtUnlockLevel = ProgressModel.UnlockedLevels / 10; // Her 10 level yeni district
            View.RefreshCityLayout(districtUnlockLevel, ProgressModel.UnlockedLevels);
            View.SetupCamera(GameStateModel.CurrentState == GameState.MainMenu);
        }
    }
}
