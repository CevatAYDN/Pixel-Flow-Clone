using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Views
{
    [Mediator(typeof(ThemeHandlerMediator))]
    public class ThemeHandlerView : View
    {
    }

    public class ThemeHandlerMediator : Mediator<ThemeHandlerView>
    {
        [Inject] public ISettingsModel SettingsModel { get; set; }

        protected override void OnBind()
        {
            Subscribe<ThemeChangedSignal>(HandleThemeChanged);
            ApplyTheme(SettingsModel.CurrentTheme);
        }

        private void HandleThemeChanged(ThemeChangedSignal signal)
        {
            ApplyTheme(SettingsModel.CurrentTheme);
        }

        private void ApplyTheme(AppTheme theme)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            switch (theme)
            {
                case AppTheme.Dark:
                    cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
                    Debug.Log("[ThemeHandler] Applied Dark theme");
                    break;
                case AppTheme.Light:
                    cam.backgroundColor = new Color(0.85f, 0.85f, 0.88f);
                    Debug.Log("[ThemeHandler] Applied Light theme");
                    break;
                case AppTheme.Neon:
                    cam.backgroundColor = new Color(0.03f, 0.01f, 0.06f);
                    Debug.Log("[ThemeHandler] Applied Neon theme");
                    break;
            }
        }
    }
}
