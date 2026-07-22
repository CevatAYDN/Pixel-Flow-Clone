using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Commands
{
    /// <summary>
    /// RequestInterstitialAdSignal → SDK adapter'ı (henüz yok) tarafından
    /// işlenecek. Placeholder olarak sadece log düşer.
    /// </summary>
    public class InterstitialAdCommand : ICommand<RequestInterstitialAdSignal>, IResettable
    {
        [Inject] public ILoggerService LoggerService { get; set; }

        public void Execute(RequestInterstitialAdSignal signal)
        {
            LoggerService?.Log("[InterstitialAdCommand] Interstitial ad requested (placeholder).");
        }

        public void Reset() { }
    }
}
