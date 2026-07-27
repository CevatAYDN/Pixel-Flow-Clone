using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;

namespace PixelFlow.Commands
{
    /// <summary>
    /// Ses kanalı seviyesini değiştirir. MVCS §15.9 KURAL 5:
    /// SettingsMediator doğrudan Model'e yazmak yerine ChangeAudioVolumeSignal fırlatır.
    /// </summary>
    public class ChangeAudioVolumeCommand : ICommand<ChangeAudioVolumeSignal>, IResettable
    {
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        public void Execute(ChangeAudioVolumeSignal signal)
        {
            LoggerService?.Log($"[ChangeAudioVolumeCommand] {signal.Channel} volume -> {signal.Value:F2}");
            switch (signal.Channel)
            {
                case AudioChannel.Master:
                    SettingsModel.SetMasterVolume(signal.Value);
                    break;
                case AudioChannel.Sfx:
                    SettingsModel.SetSfxVolume(signal.Value);
                    break;
                case AudioChannel.Music:
                    SettingsModel.SetMusicVolume(signal.Value);
                    break;
            }
        }

        public void Reset() { }
    }
}
