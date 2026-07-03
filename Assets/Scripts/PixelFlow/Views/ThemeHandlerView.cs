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
                    cam.backgroundColor = new Color(0.043f, 0.059f, 0.098f);
                    RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.4f);
                    Debug.Log("[ThemeHandler] Applied Dark theme");
                    break;
                case AppTheme.Light:
                    cam.backgroundColor = new Color(0.92f, 0.92f, 0.94f);
                    RenderSettings.ambientLight = new Color(0.8f, 0.8f, 0.85f);
                    Debug.Log("[ThemeHandler] Applied Light theme");
                    break;
                case AppTheme.Neon:
                    cam.backgroundColor = new Color(0.03f, 0.01f, 0.06f);
                    RenderSettings.ambientLight = new Color(0.6f, 0.2f, 0.8f);
                    Debug.Log("[ThemeHandler] Applied Neon theme");
                    break;
            }
        }
    }
}
