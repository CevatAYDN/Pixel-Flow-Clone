using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Views
{
    public class HubHUDMediator : Mediator<HubHUDView>
    {
        [Inject] public ICityEconomyModel CityEconomyModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public IProgressModel ProgressModel { get; set; }
        [Inject] public ILevelProgressionService ProgressionService { get; set; }
        [Inject] public ITaxCollectionService TaxCollectionService { get; set; }

        protected override void OnBind()
        {
            CityEconomyModel.OnCoinsChanged += HandleCoinsChanged;
            CityEconomyModel.OnEconomyUpdated += HandleEconomyUpdated;
            GameStateModel.OnStateChanged += HandleStateChanged;

            Subscribe<ProgressUpdatedSignal>(HandleProgressUpdated);
            Subscribe<EnterHubSignal>(HandleEnterHub);

            View.OnCollectTaxesClicked += HandleCollectTaxes;
            View.OnPlayLevelClicked += HandlePlayLevel;
            View.OnUpgradeClicked += HandleUpgrade;

            // Initial view update
            UpdateView();
        }

        protected override void OnUnbind()
        {
            CityEconomyModel.OnCoinsChanged -= HandleCoinsChanged;
            CityEconomyModel.OnEconomyUpdated -= HandleEconomyUpdated;
            GameStateModel.OnStateChanged -= HandleStateChanged;

            View.OnCollectTaxesClicked -= HandleCollectTaxes;
            View.OnPlayLevelClicked -= HandlePlayLevel;
            View.OnUpgradeClicked -= HandleUpgrade;
        }

        private void HandleProgressUpdated(ProgressUpdatedSignal signal)
        {
            UpdateView();
        }

        private void HandleEnterHub(EnterHubSignal signal)
        {
            UpdateView();
        }

        private void HandleCoinsChanged(int coins)
        {
            UpdateView();
        }

        private void HandleEconomyUpdated()
        {
            UpdateView();
        }

        private void HandleStateChanged(GameState state)
        {
            UpdateView();
        }

        private void HandleCollectTaxes()
        {
            TaxCollectionService.CollectNow();
        }

        private void HandlePlayLevel()
        {
            if (GameStateModel != null && GameStateModel.CurrentState != GameState.MainMenu)
            {
                Debug.LogWarning($"[HubHUDMediator] HandlePlayLevel ignored: current state is {GameStateModel.CurrentState}, expected MainMenu.");
                return;
            }

            // Load current level from progression service
            int levelIndex = ProgressModel.UnlockedLevels;
            LevelData level = ProgressionService.GetOrGenerateLevel(levelIndex);
            if (level != null)
            {
                SignalBus.Fire(new LoadLevelSignal { LevelToLoad = level });
            }
            else
            {
                // Fallback to Resources/Levels/Level1 if generated is null
                var any = Resources.Load<LevelData>("Levels/Level1");
                if (any != null)
                {
                    SignalBus.Fire(new LoadLevelSignal { LevelToLoad = any });
                }
            }
        }

        private void HandleUpgrade(UpgradeType type)
        {
            SignalBus.Fire(new UpgradeSignal { Type = type });
        }

        public void HandleOverclockRequested()
        {
            if (CityEconomyModel is CityEconomyModel model)
            {
                model.TriggerOverclock(14400f); // 4 hours Rush Hour per GDD §6.1
            }
            SignalBus.Fire(new RequestRewardedAdSignal { Type = RewardedAdType.Overclock });
        }

        private void UpdateView()
        {
            bool isHubActive = GameStateModel.CurrentState == GameState.MainMenu;
            View.SetVisible(isHubActive);

            if (isHubActive)
            {
                // Calculate current taxes to show accumulation progress
                // Since taxes are stored as a float in CityEconomyModel and cast to int, we can pass them
                // We'll read the fields and cost details
                int storageCost = CityEconomyModel.GetUpgradeCost(UpgradeType.Storage);
                int rateCost = CityEconomyModel.GetUpgradeCost(UpgradeType.Rate);
                int viaductCost = CityEconomyModel.GetUpgradeCost(UpgradeType.Viaduct);
                int offlineCost = CityEconomyModel.GetUpgradeCost(UpgradeType.Offline);
                int districtCost = CityEconomyModel.GetUpgradeCost(UpgradeType.District);

                // Use simple reflection or direct values since we have them in CityEconomyModel
                // Read from accumulated taxes
                float accTaxes = CityEconomyModel.GetAccumulatedTaxes();

                View.UpdateUI(
                    CityEconomyModel.Coins,
                    CityEconomyModel.TaxRatePerSecond,
                    CityEconomyModel.MaxStorage,
                    Mathf.FloorToInt(accTaxes),
                    CityEconomyModel.StorageUpgradeLevel,
                    CityEconomyModel.RateUpgradeLevel,
                    CityEconomyModel.ViaductUpgradeLevel,
                    CityEconomyModel.OfflineUpgradeLevel,
                    CityEconomyModel.DistrictUnlockLevel,
                    storageCost,
                    rateCost,
                    viaductCost,
                    offlineCost,
                    districtCost
                );
            }
        }
    }
}
