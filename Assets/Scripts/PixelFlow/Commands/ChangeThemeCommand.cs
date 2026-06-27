using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Commands
{
    public struct ChangeThemeSignal 
    { 
        public AppTheme Theme;
    }

    public class ChangeThemeCommand : ICommand<ChangeThemeSignal>
    {
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }

        public void Execute(ChangeThemeSignal signal)
        {
            SettingsModel.SetTheme(signal.Theme);
            SignalBus.Fire(new ThemeChangedSignal());
        }
    }
}
