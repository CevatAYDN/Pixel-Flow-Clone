using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Commands
{
    public struct ChangeThemeSignal 
    { 
        public AppTheme Theme;
    }

    public class ChangeThemeCommand : ICommand<ChangeThemeSignal>, IResettable
    {
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }

        public void Execute(ChangeThemeSignal signal)
        {
            UnityEngine.Debug.Log($"[ChangeThemeCommand] Changing theme to: {signal.Theme}");
            SettingsModel.SetTheme(signal.Theme);
            SignalBus.Fire(new ThemeChangedSignal());
        }

        public void Reset()
        {
            SettingsModel = null;
            SignalBus = null;
        }
    }
}
