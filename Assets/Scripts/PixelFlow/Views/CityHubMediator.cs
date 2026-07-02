using Nexus.Core;
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

        protected override void OnBind()
        {
            CityEconomyModel.OnEconomyUpdated += HandleEconomyUpdated;
            GameStateModel.OnStateChanged += HandleStateChanged;

            View.OnCollectTaxesClicked += HandleCollectTaxes;
            View.OnUpgradeClicked += HandleUpgrade;

            // İlk yerleşim kurulumu
            RefreshHub();
        }

        protected override void OnUnbind()
        {
            CityEconomyModel.OnEconomyUpdated -= HandleEconomyUpdated;
            GameStateModel.OnStateChanged -= HandleStateChanged;

            View.OnCollectTaxesClicked -= HandleCollectTaxes;
            View.OnUpgradeClicked -= HandleUpgrade;
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
        }

        private void HandleUpgrade(UpgradeType type)
        {
            CityEconomyModel.PurchaseUpgrade(type);
        }

        private void RefreshHub()
        {
            View.RefreshCityLayout(CityEconomyModel.DistrictUnlockLevel, CityEconomyModel.CompletedLevelsCount);
            View.SetupCamera(GameStateModel.CurrentState == GameState.MainMenu);
        }
    }
}
