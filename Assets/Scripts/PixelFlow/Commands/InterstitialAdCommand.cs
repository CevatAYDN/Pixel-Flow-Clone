using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Commands
{
    /// <summary>
    /// RequestInterstitialAdSignal → SDK adapter'ı (henüz yok) tarafından
    /// işlenecek. Placeholder olarak sadece log düşer.
    /// </summary>
    public class InterstitialAdCommand : ICommand<RequestInterstitialAdSignal>, IResettable
    {
        public void Execute(RequestInterstitialAdSignal signal)
        {
            Debug.Log("[InterstitialAdCommand] Interstitial ad requested (placeholder).");
        }

        public void Reset() { }
    }
}
