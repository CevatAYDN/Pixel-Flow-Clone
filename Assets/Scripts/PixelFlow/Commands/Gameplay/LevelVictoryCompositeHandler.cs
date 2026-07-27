using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Signals;
using PixelFlow.Services;

namespace PixelFlow.Commands
{
    [CompositeSignalHandler(typeof(LevelCompletedSignal), typeof(CheckWinConditionSignal))]
    public class LevelVictoryCompositeHandler : ICommand, IResettable
    {
        [Inject] public IFeedbackService FeedbackService { get; set; }
        [Inject] public ILoggerService Logger { get; set; }

        public void Execute()
        {
            Logger?.Log("[PixelFlow] Composite trigger satisfied: LevelCompletedSignal + CheckWinConditionSignal!");
            FeedbackService?.Play(FeedbackPreset.SuccessFanfare);
        }

        public void Reset()
        {
            FeedbackService = null;
            Logger = null;
        }
    }
}
