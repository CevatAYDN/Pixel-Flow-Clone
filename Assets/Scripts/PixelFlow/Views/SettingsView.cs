using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PixelFlow.Models;
using Nexus.Core;
using Nexus.Core.Services;

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
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _masterVolumeLabel;
        [SerializeField] private TMP_Text _sfxVolumeLabel;
        [SerializeField] private TMP_Text _musicVolumeLabel;
        [SerializeField] private TMP_Text _colorBlindLabel;
        [SerializeField] private TMP_Text _hapticsLabel;

        public event Action OnCloseClicked;
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnSfxVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<ColorBlindMode> OnColorBlindChanged;
        public event Action<bool> OnHapticsToggled;

        [Inject] public ILoggerService LoggerService { get; set; }

        public void AutoWireUIReferences()
        {
            if (_settingsCanvas == null) _settingsCanvas = gameObject;
            if (_titleText == null || _masterVolumeLabel == null || _sfxVolumeLabel == null || _musicVolumeLabel == null || _colorBlindLabel == null || _hapticsLabel == null)
            {
                var texts = GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in texts)
                {
                    string name = t.gameObject.name.ToLower();
                    if (_titleText == null && name.Contains("title")) _titleText = t;
                    else if (_masterVolumeLabel == null && name.Contains("master")) _masterVolumeLabel = t;
                    else if (_sfxVolumeLabel == null && (name.Contains("sfx") || name.Contains("effect"))) _sfxVolumeLabel = t;
                    else if (_musicVolumeLabel == null && name.Contains("music")) _musicVolumeLabel = t;
                    else if (_colorBlindLabel == null && (name.Contains("colorblind") || name.Contains("renk"))) _colorBlindLabel = t;
                    else if (_hapticsLabel == null && (name.Contains("haptic") || name.Contains("titreşim") || name.Contains("vibration"))) _hapticsLabel = t;
                }
            }
            var sliders = GetComponentsInChildren<Slider>(true);
            foreach (var s in sliders)
            {
                string name = s.gameObject.name.ToLower();
                if (_masterVolumeSlider == null && name.Contains("master")) _masterVolumeSlider = s;
                if (_sfxVolumeSlider == null && name.Contains("sfx")) _sfxVolumeSlider = s;
                if (_musicVolumeSlider == null && name.Contains("music")) _musicVolumeSlider = s;
            }
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string name = b.gameObject.name.ToLower();
                if (_closeButton == null && (name.Contains("close") || name.Contains("back"))) _closeButton = b;
                if (_colorBlindNoneButton == null && name.Contains("none")) _colorBlindNoneButton = b;
                if (_colorBlindProtanButton == null && name.Contains("protan")) _colorBlindProtanButton = b;
                if (_colorBlindDeutanButton == null && name.Contains("deutan")) _colorBlindDeutanButton = b;
                if (_colorBlindTritanButton == null && name.Contains("tritan")) _colorBlindTritanButton = b;
            }
            if (_hapticsToggle == null) _hapticsToggle = GetComponentInChildren<Toggle>(true);

            LoggerService?.Log($"[PixelFlow.SettingsView] AutoWire: masterSlider={(bool)_masterVolumeSlider}, sfxSlider={(bool)_sfxVolumeSlider}, " +
                $"musicSlider={(bool)_musicVolumeSlider}, closeButton={(bool)_closeButton}, " +
                $"cbNone={(bool)_colorBlindNoneButton}, cbProtan={(bool)_colorBlindProtanButton}, " +
                $"cbDeutan={(bool)_colorBlindDeutanButton}, cbTritan={(bool)_colorBlindTritanButton}, " +
                $"hapticsToggle={(bool)_hapticsToggle}");
        }

        public void SetVisible(bool visible)
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = visible ? 1f : 0f;
            cg.blocksRaycasts = visible;
            cg.interactable = visible;

            var canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = visible;
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

        private void LogCanvasGroupDiagnostics()
        {
            var cg = GetComponent<CanvasGroup>();
            var canvas = GetComponent<Canvas>();
            LoggerService?.Log($"[PixelFlow.SettingsView] CanvasGroup: alpha={(cg != null ? cg.alpha.ToString("F2") : "null")}, " +
                $"blocksRaycasts={(cg != null ? cg.blocksRaycasts.ToString() : "null")}, interactable={(cg != null ? cg.interactable.ToString() : "null")}, " +
                $"canvasEnabled={(canvas != null ? canvas.enabled.ToString() : "null")}, " +
                $"activeInHierarchy={gameObject.activeInHierarchy}");

            var es = UnityEngine.EventSystems.EventSystem.current;
            LoggerService?.Log($"[PixelFlow.SettingsView] EventSystem: current={(bool)es}, " +
                $"inputModule={(es != null ? es.currentInputModule?.GetType().Name : "null")}");
        }

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            AutoWireUIReferences();
            LogCanvasGroupDiagnostics();

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
