using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Signals;
using PixelFlow.Services;
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
        [Inject] public ICameraProvider CameraProvider { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public ThemePaletteAsset ThemePalette { get; set; }

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
            Camera cam = CameraProvider?.MainCamera;
            if (cam == null) return;

            var colors = ThemePalette.GetThemeColors(theme);
            cam.backgroundColor = colors.CameraBackground;
            RenderSettings.ambientLight = colors.AmbientLight;
            LoggerService?.Log($"[ThemeHandler] Applied {theme} theme");
        }
    }
}
