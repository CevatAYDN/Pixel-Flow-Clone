using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Commands
{
    public struct ChangeThemeSignal 
    { 
        public AppTheme Theme;
    }

    // Kayıt: GameContextLifecycle.OnConfigure'da fluent API ile yapılıyor
    // (builder.BindSignal<ChangeThemeSignal>().To<ChangeThemeCommand>()).
    // Attribute tabanlı otomatik keşif bu projede kapalı; çift kayıt riskini önlemek
    // ve AOT uyumluluğunu korumak için tek kaynak burası.
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
            // Do not nullify injected properties to prevent null-ref risks on framework reuse
        }
    }
}
