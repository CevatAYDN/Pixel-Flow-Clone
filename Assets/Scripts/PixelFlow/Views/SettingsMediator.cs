using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using UnityEngine;

namespace PixelFlow.Views
{
    public class SettingsMediator : Mediator<SettingsView>
    {
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        protected override void OnBind()
        {
            LoggerService?.Log("[PixelFlow.SettingsMediator] Binding Settings UI...");

            View.OnMasterVolumeChanged += HandleMasterVolume;
            View.OnSfxVolumeChanged += HandleSfxVolume;
            View.OnMusicVolumeChanged += HandleMusicVolume;
            View.OnColorBlindChanged += HandleColorBlind;
            View.OnHapticsToggled += HandleHaptics;
            View.OnCloseClicked += HandleClose;

            if (GameStateModel != null)
            {
                GameStateModel.OnStateChanged += HandleStateChanged;
            }

            View.PopulateSettings(
                SettingsModel.MasterVolume,
                SettingsModel.SfxVolume,
                SettingsModel.MusicVolume,
                SettingsModel.CurrentColorBlindMode,
                !SettingsModel.HapticsDisabled);

            View.SetVisible(GameStateModel?.CurrentState == GameState.Paused);
            LoggerService?.Log("[PixelFlow.SettingsMediator] Settings UI initialized and ready.");
        }

        protected override void OnUnbind()
        {
            LoggerService?.Log("[PixelFlow.SettingsMediator] Unbinding Settings UI...");
            View.OnMasterVolumeChanged -= HandleMasterVolume;
            View.OnSfxVolumeChanged -= HandleSfxVolume;
            View.OnMusicVolumeChanged -= HandleMusicVolume;
            View.OnColorBlindChanged -= HandleColorBlind;
            View.OnHapticsToggled -= HandleHaptics;
            View.OnCloseClicked -= HandleClose;

            if (GameStateModel != null)
            {
                GameStateModel.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            LoggerService?.Log($"[PixelFlow.SettingsMediator] State changed -> {state}. Setting SettingsView visible: {state == GameState.Paused}");
            View?.SetVisible(state == GameState.Paused);
        }

        private void HandleMasterVolume(float v)
        {
            LoggerService?.Log($"[PixelFlow.SettingsMediator] Master Volume changed: {v:F2}");
            SettingsModel.SetMasterVolume(v);
        }

        private void HandleSfxVolume(float v)
        {
            LoggerService?.Log($"[PixelFlow.SettingsMediator] SFX Volume changed: {v:F2}");
            SettingsModel.SetSfxVolume(v);
        }

        private void HandleMusicVolume(float v)
        {
            LoggerService?.Log($"[PixelFlow.SettingsMediator] Music Volume changed: {v:F2}");
            SettingsModel.SetMusicVolume(v);
        }

        private void HandleColorBlind(ColorBlindMode mode)
        {
            LoggerService?.Log($"[PixelFlow.SettingsMediator] Colorblind mode changed: {mode}");
            SettingsModel.SetColorBlindMode(mode);
        }

        private void HandleHaptics(bool enabled)
        {
            LoggerService?.Log($"[PixelFlow.SettingsMediator] Haptics toggled: {enabled}");
            SettingsModel.SetHapticsDisabled(!enabled);
        }

        private void HandleClose()
        {
            LoggerService?.Log("[PixelFlow.SettingsMediator] Closing Settings panel and resuming game...");
            View.SetVisible(false);
            if (GameStateModel != null && GameStateModel.CurrentState == GameState.Paused)
            {
                GameStateModel.SetState(GameState.Playing);
            }
        }
    }
}
