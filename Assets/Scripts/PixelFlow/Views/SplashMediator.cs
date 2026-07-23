using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Views
{
    public class SplashMediator : Mediator<SplashView>
    {
        [Inject] public ILoggerService LoggerService { get; set; }

        protected override void OnBind()
        {
            LoggerService?.Log("[PixelFlow.SplashMediator] Binding Splash Screen UI...");
            if (View != null)
            {
                View.OnSplashComplete += HandleSplashComplete;
            }
        }

        protected override void OnUnbind()
        {
            LoggerService?.Log("[PixelFlow.SplashMediator] Unbinding Splash Screen UI...");
            if (View != null)
            {
                View.OnSplashComplete -= HandleSplashComplete;
            }
        }

        private void HandleSplashComplete()
        {
            LoggerService?.Log("[PixelFlow.SplashMediator] Splash screen animation complete. Hiding splash view...");
            View?.SetVisible(false);
        }
    }
}
