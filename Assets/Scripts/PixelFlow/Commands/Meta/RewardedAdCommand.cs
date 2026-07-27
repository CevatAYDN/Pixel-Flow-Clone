using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Signals;

namespace PixelFlow.Commands
{
    /// <summary>
    /// Rewarded ad isteklerini merkezî olarak işler.
    /// Plan §9.4: rewarded placement'lar GamePlan ile uyumlu olarak burada yönlendirilir.
    /// </summary>
    public class RewardedAdCommand : ICommand, IResettable
    {
        [Inject] public ILoggerService Logger { get; set; }
        [Inject] public IFeedbackService FeedbackService { get; set; }

        public void Execute()
        {
            Logger?.Log("[PixelFlow] Rewarded ad request received.");
            FeedbackService?.Play(FeedbackPreset.LightClick);
        }

        public void Reset()
        {
            Logger = null;
            FeedbackService = null;
        }
    }
}
