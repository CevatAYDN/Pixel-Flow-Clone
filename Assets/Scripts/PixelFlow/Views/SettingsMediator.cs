using Nexus.Core;
using PixelFlow.Models;
using UnityEngine;

namespace PixelFlow.Views
{
    public class SettingsMediator : Mediator<SettingsView>
    {
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }

        protected override void OnBind()
        {
            View.OnMasterVolumeChanged += HandleMasterVolume;
            View.OnSfxVolumeChanged += HandleSfxVolume;
            View.OnMusicVolumeChanged += HandleMusicVolume;
            View.OnColorBlindChanged += HandleColorBlind;
            View.OnHapticsToggled += HandleHaptics;
            View.OnCloseClicked += HandleClose;

            View.PopulateSettings(
                SettingsModel.MasterVolume,
                SettingsModel.SfxVolume,
                SettingsModel.MusicVolume,
                SettingsModel.CurrentColorBlindMode,
                !SettingsModel.HapticsDisabled);

            View.SetVisible(false);
        }

        protected override void OnUnbind()
        {
            View.OnMasterVolumeChanged -= HandleMasterVolume;
            View.OnSfxVolumeChanged -= HandleSfxVolume;
            View.OnMusicVolumeChanged -= HandleMusicVolume;
            View.OnColorBlindChanged -= HandleColorBlind;
            View.OnHapticsToggled -= HandleHaptics;
            View.OnCloseClicked -= HandleClose;
        }

        private void HandleMasterVolume(float v) => SettingsModel.SetMasterVolume(v);
        private void HandleSfxVolume(float v) => SettingsModel.SetSfxVolume(v);
        private void HandleMusicVolume(float v) => SettingsModel.SetMusicVolume(v);
        private void HandleColorBlind(ColorBlindMode mode) => SettingsModel.SetColorBlindMode(mode);
        private void HandleHaptics(bool enabled) => SettingsModel.SetHapticsDisabled(!enabled);
        private void HandleClose() => View.SetVisible(false);
    }
}
