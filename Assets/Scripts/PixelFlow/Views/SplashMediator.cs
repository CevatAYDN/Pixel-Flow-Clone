using Nexus.Core;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Views
{
    public class SplashMediator : Mediator<SplashView>
    {
        protected override void OnBind()
        {
            if (View != null)
            {
                View.OnSplashComplete += HandleSplashComplete;
            }
        }

        protected override void OnUnbind()
        {
            if (View != null)
            {
                View.OnSplashComplete -= HandleSplashComplete;
            }
        }

        private void HandleSplashComplete()
        {
            SignalBus?.Fire(new EnterHubSignal());
        }
    }
}
