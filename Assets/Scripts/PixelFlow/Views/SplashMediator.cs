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
            // Splash tamamlandı. GameBootstrapper.Start() EnterPlaying()'i çağıracak.
            // Herhangi bir signal'a gerek yok — bootstrapper event-driven.
        }
    }
}
