using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Commands
{
    /// <summary>
    /// Haptik geri bildirimi açar/kapatır. MVCS §15.9 KURAL 5:
    /// SettingsMediator doğrudan Model'e yazmak yerine ToggleHapticsSignal fırlatır.
    /// </summary>
    public class ToggleHapticsCommand : ICommand<ToggleHapticsSignal>, IResettable
    {
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        public void Execute(ToggleHapticsSignal signal)
        {
            LoggerService?.Log($"[ToggleHapticsCommand] Haptics disabled -> {signal.Disabled}");
            SettingsModel.SetHapticsDisabled(signal.Disabled);
        }

        public void Reset() { }
    }
}
