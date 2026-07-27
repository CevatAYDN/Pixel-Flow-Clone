using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Commands
{
    /// <summary>
    /// Renk körlüğü modunu değiştirir. MVCS §15.9 KURAL 5:
    /// SettingsMediator doğrudan Model'e yazmak yerine ChangeColorBlindModeSignal fırlatır.
    /// </summary>
    public class ChangeColorBlindModeCommand : ICommand<ChangeColorBlindModeSignal>, IResettable
    {
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        public void Execute(ChangeColorBlindModeSignal signal)
        {
            LoggerService?.Log($"[ChangeColorBlindModeCommand] Colorblind mode -> {signal.Mode}");
            SettingsModel.SetColorBlindMode(signal.Mode);
        }

        public void Reset() { }
    }
}
