using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;

namespace PixelFlow.Views
{
    /// <summary>
    /// DesignSystem/Mockups/index.html mimarisine uygun Ana Menü / Hub Mediator'ı.
    /// GameState.MainMenu aktifleştiğinde görünür olur, OYUNA BAŞLA butonuna tıklanınca
    /// LoadLevelSignal ve GameState.Playing geçişini tetikler.
    /// </summary>
    public class MainMenuMediator : Mediator<MainMenuView>
    {
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IProgressModel ProgressModel { get; set; }
        [Inject] public IInventoryModel InventoryModel { get; set; }
        [Inject] public ILevelProgressionService ProgressionService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        protected override void OnBind()
        {
            View.OnPlayClicked += HandlePlayClicked;
            View.OnGarageClicked += HandleGarageClicked;
            View.OnSettingsClicked += HandleSettingsClicked;

            GameStateModel.OnStateChanged += HandleStateChanged;

            RefreshHubUI();
            UpdateVisibility();
        }

        protected override void OnUnbind()
        {
            View.OnPlayClicked -= HandlePlayClicked;
            View.OnGarageClicked -= HandleGarageClicked;
            View.OnSettingsClicked -= HandleSettingsClicked;

            if (GameStateModel != null)
                GameStateModel.OnStateChanged -= HandleStateChanged;
        }

        private void HandlePlayClicked()
        {
            LoggerService?.Log("[PixelFlow.MainMenuMediator] 'OYUNA BAŞLA' clicked on Main Hub. Resolving unlocked level...");
            int currentUnlockedLevel = ProgressModel != null ? ProgressModel.UnlockedLevels : 1;
            int levelIndex = currentUnlockedLevel - 1;

            var levelToLoad = ProgressionService?.GetOrGenerateLevel(levelIndex);
            if (levelToLoad != null)
            {
                LoggerService?.Log($"[PixelFlow.MainMenuMediator] Firing LoadLevelSignal for Level {currentUnlockedLevel} ({levelToLoad.name})");
                SignalBus.Fire(new LoadLevelSignal { LevelToLoad = levelToLoad });
            }
            else
            {
                LoggerService?.LogError($"[PixelFlow.MainMenuMediator] Failed to resolve level data for level index {levelIndex}.");
            }

            LoggerService?.Log("[PixelFlow.MainMenuMediator] Transitioning GameState -> Playing...");
            GameStateModel?.SetState(GameState.Playing);
            LoggerService?.Log("[PixelFlow.MainMenuMediator] GameState set to Playing. MainMenuView hidden, HUDView active.");
        }

        private void HandleGarageClicked()
        {
            LoggerService?.Log("[MainMenuMediator] 'Garaj' button clicked from Hub.");
            // Garaj modali / paneli tetiklenir
        }

        private void HandleSettingsClicked()
        {
            LoggerService?.Log("[MainMenuMediator] 'Ayarlar' button clicked from Hub.");
        }

        private void HandleStateChanged(GameState state)
        {
            UpdateVisibility();
            if (state == GameState.MainMenu)
            {
                RefreshHubUI();
            }
        }

        private void RefreshHubUI()
        {
            int coins = InventoryModel != null ? InventoryModel.Coins : 1450;
            int levelNumber = ProgressModel != null ? ProgressModel.UnlockedLevels : 1;

            View.UpdateCoinBalance(coins);
            View.UpdatePlayButtonText(levelNumber);
            View.UpdateEquippedVehicle("Dondurma Arabası", "Kuşanılan Sarı Araç");
        }

        private void UpdateVisibility()
        {
            if (GameStateModel == null || View == null) return;
            bool isMainMenu = GameStateModel.CurrentState == GameState.MainMenu;
            View.SetVisible(isMainMenu);
        }
    }
}
