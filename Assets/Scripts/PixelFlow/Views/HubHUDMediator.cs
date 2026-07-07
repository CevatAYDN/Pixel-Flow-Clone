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
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public IProgressModel ProgressModel { get; set; }
        [Inject] public ILevelProgressionService ProgressionService { get; set; }

        protected override void OnBind()
        {
            GameStateModel.OnStateChanged += HandleStateChanged;

            Subscribe<ProgressUpdatedSignal>(HandleProgressUpdated);
            Subscribe<EnterHubSignal>(HandleEnterHub);

            View.OnPlayLevelClicked += HandlePlayLevel;

            // Initial view update
            UpdateView();
        }

        protected override void OnUnbind()
        {
            GameStateModel.OnStateChanged -= HandleStateChanged;

            View.OnPlayLevelClicked -= HandlePlayLevel;
        }

        private void HandleProgressUpdated(ProgressUpdatedSignal signal)
        {
            UpdateView();
        }

        private void HandleEnterHub(EnterHubSignal signal)
        {
            UpdateView();
        }

        private void HandleStateChanged(GameState state)
        {
            UpdateView();
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

        private void UpdateView()
        {
            bool isHubActive = GameStateModel.CurrentState == GameState.MainMenu;
            View.SetVisible(isHubActive);

            if (isHubActive)
            {
                // Basit level progress UI
                View.UpdateLevelProgress(
                    ProgressModel.UnlockedLevels,
                    ProgressModel.UnlockedLevels + 1 // Next level
                );
            }
        }
    }
}
