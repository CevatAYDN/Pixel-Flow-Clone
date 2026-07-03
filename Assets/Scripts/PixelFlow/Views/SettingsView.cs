using System;
using UnityEngine;
using UnityEngine.UI;
using PixelFlow.Models;
using Nexus.Core;

namespace PixelFlow.Views
{
    /// <summary>
    /// GDD §11: Ayar menüsü (Bento-Glass). Volume slider'ları, renk körlüğü
    /// modu, haptik on/off ve dil placeholder'ı. Hub açıkken gösterilir.
    /// [Mediator] attribute ile otomatik bağlanır.
    /// </summary>
    [Mediator(typeof(SettingsMediator))]
    public class SettingsView : View
    {
        [SerializeField] private GameObject _settingsCanvas;
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Button _colorBlindNoneButton;
        [SerializeField] private Button _colorBlindProtanButton;
        [SerializeField] private Button _colorBlindDeutanButton;
        [SerializeField] private Button _colorBlindTritanButton;
        [SerializeField] private Toggle _hapticsToggle;
        [SerializeField] private Button _closeButton;

        public event Action OnCloseClicked;
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnSfxVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<ColorBlindMode> OnColorBlindChanged;
        public event Action<bool> OnHapticsToggled;

        public void SetVisible(bool visible)
        {
            if (_settingsCanvas != null) _settingsCanvas.SetActive(visible);
        }

        public void PopulateSettings(float master, float sfx, float music, ColorBlindMode cb, bool haptics)
        {
            if (_masterVolumeSlider != null) _masterVolumeSlider.value = master;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.value = sfx;
            if (_musicVolumeSlider != null) _musicVolumeSlider.value = music;
            if (_hapticsToggle != null) _hapticsToggle.isOn = haptics;
            UpdateColorBlindButtons(cb);
        }

        private void UpdateColorBlindButtons(ColorBlindMode mode)
        {
            SetButtonActive(_colorBlindNoneButton, mode == ColorBlindMode.None);
            SetButtonActive(_colorBlindProtanButton, mode == ColorBlindMode.Protanopia);
            SetButtonActive(_colorBlindDeutanButton, mode == ColorBlindMode.Deuteranopia);
            SetButtonActive(_colorBlindTritanButton, mode == ColorBlindMode.Tritanopia);
        }

        private static void SetButtonActive(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? new Color(0.2f, 0.6f, 1f) : new Color(0.2f, 0.2f, 0.25f);
        }

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);

            if (_masterVolumeSlider != null) _masterVolumeSlider.onValueChanged.AddListener(v => OnMasterVolumeChanged?.Invoke(v));
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.onValueChanged.AddListener(v => OnSfxVolumeChanged?.Invoke(v));
            if (_musicVolumeSlider != null) _musicVolumeSlider.onValueChanged.AddListener(v => OnMusicVolumeChanged?.Invoke(v));
            if (_hapticsToggle != null) _hapticsToggle.onValueChanged.AddListener(v => OnHapticsToggled?.Invoke(v));
            if (_closeButton != null) _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
            if (_colorBlindNoneButton != null) _colorBlindNoneButton.onClick.AddListener(() => OnColorBlindChanged?.Invoke(ColorBlindMode.None));
            if (_colorBlindProtanButton != null) _colorBlindProtanButton.onClick.AddListener(() => OnColorBlindChanged?.Invoke(ColorBlindMode.Protanopia));
            if (_colorBlindDeutanButton != null) _colorBlindDeutanButton.onClick.AddListener(() => OnColorBlindChanged?.Invoke(ColorBlindMode.Deuteranopia));
            if (_colorBlindTritanButton != null) _colorBlindTritanButton.onClick.AddListener(() => OnColorBlindChanged?.Invoke(ColorBlindMode.Tritanopia));
        }

        protected override void OnUnbind()
        {
            base.OnUnbind();
            if (_masterVolumeSlider != null) _masterVolumeSlider.onValueChanged.RemoveAllListeners();
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            if (_musicVolumeSlider != null) _musicVolumeSlider.onValueChanged.RemoveAllListeners();
            if (_hapticsToggle != null) _hapticsToggle.onValueChanged.RemoveAllListeners();
            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
            if (_colorBlindNoneButton != null) _colorBlindNoneButton.onClick.RemoveAllListeners();
            if (_colorBlindProtanButton != null) _colorBlindProtanButton.onClick.RemoveAllListeners();
            if (_colorBlindDeutanButton != null) _colorBlindDeutanButton.onClick.RemoveAllListeners();
            if (_colorBlindTritanButton != null) _colorBlindTritanButton.onClick.RemoveAllListeners();
        }
    }
}
