using Nexus.Core;
using PixelFlow.Commands;
using PixelFlow.Models;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Views
{
    public class CityHubMediator : Mediator<CityHubView>
    {
        [Inject] public ICityEconomyModel CityEconomyModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public IProgressModel ProgressModel { get; set; }

        protected override void OnBind()
        {
            CityEconomyModel.OnEconomyUpdated += HandleEconomyUpdated;
            GameStateModel.OnStateChanged += HandleStateChanged;

            View.OnCollectTaxesClicked += HandleCollectTaxes;
            View.OnUpgradeClicked += HandleUpgrade;
            View.OnDistrictClicked += HandleDistrictClicked;

            RefreshHub();
        }

        protected override void OnUnbind()
        {
            CityEconomyModel.OnEconomyUpdated -= HandleEconomyUpdated;
            GameStateModel.OnStateChanged -= HandleStateChanged;

            View.OnCollectTaxesClicked -= HandleCollectTaxes;
            View.OnUpgradeClicked -= HandleUpgrade;
            View.OnDistrictClicked -= HandleDistrictClicked;
        }

        private void HandleEconomyUpdated()
        {
            RefreshHub();
        }

        private void HandleStateChanged(GameState state)
        {
            View.SetupCamera(state == GameState.MainMenu);
            if (state == GameState.MainMenu)
            {
                RefreshHub();
            }
        }

        private void HandleCollectTaxes()
        {
            CityEconomyModel.CollectTaxes();
            SignalBus.Fire(new CoinCollectionSignal { Amount = 0 });
        }

        private void HandleUpgrade(UpgradeType type)
        {
            CityEconomyModel.PurchaseUpgrade(type);
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
            View.RefreshCityLayout(CityEconomyModel.DistrictUnlockLevel, CityEconomyModel.CompletedLevelsCount);
            View.SetupCamera(GameStateModel.CurrentState == GameState.MainMenu);
        }
    }
}
