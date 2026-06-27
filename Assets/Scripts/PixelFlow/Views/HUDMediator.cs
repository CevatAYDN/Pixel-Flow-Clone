using UnityEngine;
using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Data;

namespace PixelFlow.Views
{
    public class HUDMediator : Mediator<HUDView>
    {
        [Inject] public IHintModel HintModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }

        protected override void OnBind()
        {
            View.OnHintClicked += HandleHintClicked;
            View.OnNextLevelClicked += HandleNextLevelClicked;
            HintModel.OnHintCountChanged += HandleHintCountChanged;
            View.HideCompletion();

            View.UpdateHintCount(HintModel.HintsRemaining);
            Subscribe<LevelCompletedSignal>(HandleLevelCompleted);
            Subscribe<LoadLevelSignal>(HandleLoadLevel);
        }

        private void HandleLoadLevel(LoadLevelSignal signal)
        {
            View.HideCompletion();
        }

        protected override void OnUnbind()
        {
            View.OnHintClicked -= HandleHintClicked;
            View.OnNextLevelClicked -= HandleNextLevelClicked;
            HintModel.OnHintCountChanged -= HandleHintCountChanged;
        }

        private void HandleHintClicked()
        {
            SignalBus.Fire(new RequestHintSignal());
        }

        private void HandleNextLevelClicked()
        {
            var pack = Resources.Load<LevelPack>("Levels/MainLevelPack");
            if (pack != null && LevelModel.CurrentLevel != null)
            {
                int currentIndex = pack.levels.FindIndex(l => l.levelIndex == LevelModel.CurrentLevel.levelIndex);
                int nextIndex = currentIndex + 1;
                if (nextIndex < pack.levels.Count)
                {
                    SignalBus.Fire(new LoadLevelSignal { LevelToLoad = pack.levels[nextIndex] });
                }
                else
                {
                    // Loop back to Level 1
                    SignalBus.Fire(new LoadLevelSignal { LevelToLoad = pack.levels[0] });
                }
            }
        }

        private void HandleHintCountChanged(int count)
        {
            View.UpdateHintCount(count);
        }

        private void HandleLevelCompleted(LevelCompletedSignal signal)
        {
            View.ShowCompletion();
        }
    }
}
