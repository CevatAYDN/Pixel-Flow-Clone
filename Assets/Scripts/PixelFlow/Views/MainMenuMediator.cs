using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;
using PixelFlow.Data;

namespace PixelFlow.Views
{
    /// <summary>
    /// DesignSystem/Mockups/index.html mimarisine uygun Ana Menü / Hub Mediator'ı.
    /// GameState.MainMenu aktifleştiğinde görünür olur, OYUNA BAŞLA butonuna tıklanınca
    /// LoadLevelSignal ve GameState.Playing geçişini tetikler.
    /// Kayıtlı oyun varsa restore edilir, yoksa yeni level yüklenir.
    /// </summary>
    public class MainMenuMediator : Mediator<MainMenuView>
    {
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IProgressModel ProgressModel { get; set; }
        [Inject] public IInventoryModel InventoryModel { get; set; }
        [Inject] public ILevelProgressionService ProgressionService { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public IPlayerPrefsService PlayerPrefsService { get; set; }
        [Inject] public IObstacleService ObstacleService { get; set; }
        [Inject] public ITutorialDriver TutorialDriver { get; set; }

        protected override void OnBind()
        {
            View.OnPlayClicked += HandlePlayClicked;
            View.OnGarageClicked += HandleGarageClicked;
            View.OnLevelSelectClicked += HandleLevelSelectClicked;
            View.OnSettingsClicked += HandleSettingsClicked;

            GameStateModel.OnStateChanged += HandleStateChanged;

            RefreshHubUI();
            UpdateVisibility();
        }

        protected override void OnUnbind()
        {
            View.OnPlayClicked -= HandlePlayClicked;
            View.OnGarageClicked -= HandleGarageClicked;
            View.OnLevelSelectClicked -= HandleLevelSelectClicked;
            View.OnSettingsClicked -= HandleSettingsClicked;

            if (GameStateModel != null)
                GameStateModel.OnStateChanged -= HandleStateChanged;
        }

        private void HandlePlayClicked()
        {
            LoggerService?.Log("[PixelFlow.MainMenuMediator] 'OYUNA BAŞLA' clicked. Checking for saved game...");

            // Kayıtlı oyun varsa restore et
            if (TryRestoreSavedGame())
            {
                LoggerService?.Log("[PixelFlow.MainMenuMediator] Saved game restored. Transitioning to Playing.");
                return;
            }

            // Save yok — yeni level yükle
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
        }

        private bool TryRestoreSavedGame()
        {
            if (!GridStateSerializer.HasSavedGame(PlayerPrefsService))
                return false;

            var saved = GridStateSerializer.Load(PlayerPrefsService);
            if (saved == null || saved.cells == null || saved.cells.Count == 0)
                return false;
            if (saved.paths == null || saved.paths.Count == 0)
            {
                GridStateSerializer.ClearSave(PlayerPrefsService);
                return false;
            }

            var level = ProgressionService?.GetOrGenerateLevel(saved.levelIndex);
            if (level == null)
                return false;

            if (!GridStateSerializer.IsSaveDataValidForLevel(saved, level))
            {
                GridStateSerializer.ClearSave(PlayerPrefsService);
                return false;
            }

            LoggerService?.Log($"[PixelFlow.MainMenuMediator] Restoring saved game: Level {saved.levelIndex + 1}");
            LevelModel.SetLevel(level);
            GridStateSerializer.ApplyToGrid(saved, GridModel);
            GridStateSerializer.EnsureInitialNodesOnGrid(level, GridModel);
            GameSessionModel.ApplySave(saved.availableViaducts, saved.maxViaducts,
                saved.elapsedTime, saved.score, saved.stars, saved.levelIndex, saved.targetFlowScore);

            ObstacleService?.InitializeFromLevel(level);
            TutorialDriver?.OnLevelLoaded(level.levelIndex);

            SignalBus.Fire(new GridUpdatedSignal());
            GameStateModel.SetState(GameState.Playing);
            return true;
        }

        private void HandleGarageClicked()
        {
            LoggerService?.Log("[MainMenuMediator] 'Garaj' button clicked from Hub.");
        }

        private void HandleLevelSelectClicked()
        {
            LoggerService?.Log("[MainMenuMediator] 'Seviye Seçimi' button clicked from Hub. Transitioning -> LevelSelect.");
            GameStateModel?.SetState(GameState.LevelSelect);
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
            LoggerService?.Log($"[PixelFlow.MainMenuMediator] UpdateVisibility: state={GameStateModel.CurrentState}, isMainMenu={isMainMenu}");
            View.SetVisible(isMainMenu);
        }
    }
}
