using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Commands
{
    public class ChangeThemeCommand : ICommand<ChangeThemeSignal>, IResettable
    {
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        public void Execute(ChangeThemeSignal signal)
        {
            LoggerService?.Log($"[ChangeThemeCommand] Changing theme to: {signal.Theme}");
            SettingsModel.SetTheme(signal.Theme);
            SignalBus.Fire(new ThemeChangedSignal());
        }

        public void Reset()
        {
        }
    }
}
