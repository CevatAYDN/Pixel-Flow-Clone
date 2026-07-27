using PixelFlow.Data;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Views
{
    public class SettingsMediator : Mediator<SettingsView>
    {
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public ILocalizationService LocalizationService { get; set; }

        protected override void OnBind()
        {
            LoggerService?.Log("[PixelFlow.SettingsMediator] Binding Settings UI...");
            if (LocalizationService == null)
                throw new DataValidationException("SettingsMediator.OnBind: ILocalizationService not injected! Cannot localize UI.");
            if (SettingsModel == null)
                throw new DataValidationException("SettingsMediator.OnBind: ISettingsModel not injected! Cannot populate settings UI.");

            View.OnMasterVolumeChanged += HandleMasterVolume;
            View.OnSfxVolumeChanged += HandleSfxVolume;
            View.OnMusicVolumeChanged += HandleMusicVolume;
            View.OnColorBlindChanged += HandleColorBlind;
            View.OnHapticsToggled += HandleHaptics;
            View.OnLanguageSelected += HandleLanguageSelected;
            View.OnCloseClicked += HandleClose;

            if (GameStateModel != null)
            {
                GameStateModel.OnStateChanged += HandleStateChanged;
            }

            Subscribe<ShowSettingsSignal>(_ =>
            {
                LoggerService?.Log("[PixelFlow.SettingsMediator] ShowSettingsSignal received -> Opening Settings panel.");
                View?.SetVisible(true);
            });

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
            if (View != null)
            {
                View.OnMasterVolumeChanged -= HandleMasterVolume;
                View.OnSfxVolumeChanged -= HandleSfxVolume;
                View.OnMusicVolumeChanged -= HandleMusicVolume;
                View.OnColorBlindChanged -= HandleColorBlind;
                View.OnHapticsToggled -= HandleHaptics;
                View.OnLanguageSelected -= HandleLanguageSelected;
                View.OnCloseClicked -= HandleClose;
            }

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
            SignalBus.Fire(new ChangeAudioVolumeSignal { Channel = AudioChannel.Master, Value = v });
        }

        private void HandleSfxVolume(float v)
        {
            LoggerService?.Log($"[PixelFlow.SettingsMediator] SFX Volume changed: {v:F2}");
            SignalBus.Fire(new ChangeAudioVolumeSignal { Channel = AudioChannel.Sfx, Value = v });
        }

        private void HandleMusicVolume(float v)
        {
            LoggerService?.Log($"[PixelFlow.SettingsMediator] Music Volume changed: {v:F2}");
            SignalBus.Fire(new ChangeAudioVolumeSignal { Channel = AudioChannel.Music, Value = v });
        }

        private void HandleColorBlind(ColorBlindMode mode)
        {
            LoggerService?.Log($"[PixelFlow.SettingsMediator] Colorblind mode changed: {mode}");
            SignalBus.Fire(new ChangeColorBlindModeSignal { Mode = mode });
        }

        private void HandleHaptics(bool enabled)
        {
            LoggerService?.Log($"[PixelFlow.SettingsMediator] Haptics toggled: {enabled}");
            SignalBus.Fire(new ToggleHapticsSignal { Disabled = !enabled });
        }

        private void HandleLanguageSelected(string langCode)
        {
            LoggerService?.Log($"[PixelFlow.SettingsMediator] Language changed -> {langCode}");
            LocalizationService?.SetLanguage(langCode);
        }

        private void HandleClose()
        {
            LoggerService?.Log("[PixelFlow.SettingsMediator] Closing Settings panel...");
            View?.SetVisible(false);
            if (GameStateModel != null && GameStateModel.CurrentState == GameState.Paused)
            {
                var targetState = GameStateModel.PreviousState != GameState.Paused ? GameStateModel.PreviousState : GameState.Playing;
                LoggerService?.Log($"[PixelFlow.SettingsMediator] Restoring PreviousState: {targetState}");
                GameStateModel.SetState(targetState);
            }
        }
    }
}
