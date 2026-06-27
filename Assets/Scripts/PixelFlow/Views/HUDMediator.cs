using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Views
{
    public class HUDMediator : Mediator<HUDView>
    {
        [Inject] public IHintModel HintModel { get; set; }

        protected override void OnBind()
        {
            View.OnHintClicked += HandleHintClicked;
            HintModel.OnHintCountChanged += HandleHintCountChanged;
            View.HideCompletion();

            View.UpdateHintCount(HintModel.HintsRemaining);
            Subscribe<LevelCompletedSignal>(HandleLevelCompleted);
        }

        protected override void OnUnbind()
        {
            View.OnHintClicked -= HandleHintClicked;
            HintModel.OnHintCountChanged -= HandleHintCountChanged;
        }

        private void HandleHintClicked()
        {
            SignalBus.Fire(new RequestHintSignal());
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
