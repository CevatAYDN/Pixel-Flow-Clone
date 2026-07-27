using System;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlow.Views
{
    [Mediator(typeof(HUDMediator))]
    public class HUDView : View
    {
        [SerializeField] private Button _hintButton;
        [SerializeField] private TMP_Text _hintCountText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _levelTitleText;
        [SerializeField] private GameObject _starsContainer;
        [SerializeField] private GameObject _star1;
        [SerializeField] private GameObject _star2;
        [SerializeField] private GameObject _star3;
        [SerializeField] private GameObject _completionPanel;
        [SerializeField] private TMP_Text _completionText;
        [SerializeField] private TMP_Text _completionScoreText;
        [SerializeField] private TMP_Text _completionStarsText;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private GameObject _bloomFlashOverlay;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _redoButton;
        [SerializeField] private Button _themeDarkButton;
        [SerializeField] private Button _themeLightButton;
        [SerializeField] private Button _themeNeonButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private GameObject _levelFailedPanel;
        [SerializeField] private TMP_Text _levelFailedText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _levelFailedContinueButton;
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private Button _garageButton;
        [SerializeField] private Button _rainbowRoadButton;
        [SerializeField] private TMP_Text _rainbowRoadCountText;
        [SerializeField] private Button _clearJamButton;
        [SerializeField] private TMP_Text _clearJamCountText;
        [SerializeField] private Button _viaductButton;
        [SerializeField] private TMP_Text _viaductCountText;
        [SerializeField] private GameObject _crashToast;
        [SerializeField] private TMP_Text _crashToastText;
        [SerializeField] private float _crashToastDuration = 1.5f;

        [SerializeField] private GameObject _crisisPanel;
        [SerializeField] private TMP_Text _crisisTitleText;
        [SerializeField] private TMP_Text _crisisDescriptionText;
        [SerializeField] private TMP_Text _crisisViaductCountText;
        [SerializeField] private Button _crisisViaductButton;
        [SerializeField] private Button _crisisUndoButton;

        public event Action OnGarageClicked;
        public event Action OnRainbowRoadClicked;
        public event Action OnClearJamClicked;
        public event Action OnViaductClicked;
        public event Action OnHintClicked;
        public event Action OnNextLevelClicked;
        public event Action OnContinueClicked;
        public event Action OnUndoClicked;
        public event Action OnRedoClicked;
        public event Action OnThemeDarkClicked;
        public event Action OnThemeLightClicked;
        public event Action OnThemeNeonClicked;
#pragma warning disable CS0067
        public event Action OnSimulateDebugPressed;
#pragma warning restore CS0067
        public event Action OnCrisisViaductClicked;
        public event Action OnCrisisUndoClicked;
        public event Action OnPauseClicked;
        public event Action OnRetryClicked;
        public event Action OnLevelFailedContinueClicked;

        private Coroutine _crashToastCoroutine;

        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject, OptionalInject] public ThemePaletteAsset ThemePalette { get; set; }
        [Inject, OptionalInject] public ISettingsModel SettingsModel { get; set; }
        [Inject, OptionalInject] public ITickService TickService { get; set; }

        private readonly Color _goldPillBg = new Color(0.99f, 0.95f, 0.78f, 1f);
        private readonly Color _goldText = new Color(0.71f, 0.33f, 0.04f, 1f);
        private readonly Color _emeraldGreen = new Color(0.06f, 0.72f, 0.51f, 1f);
        private readonly Color _indigoBlue = new Color(0.31f, 0.27f, 0.90f, 1f);
        private readonly Color _darkSlate = new Color(0.20f, 0.25f, 0.33f, 1f);
        private readonly Color _slateBg = new Color(0.85f, 0.87f, 0.92f, 0.5f);
        private readonly Color _whiteGlass = new Color(1f, 1f, 1f, 0.95f);

        public void SetupHUD()
        {
            AutoWireUIReferences();
            BindHUDButtons();
            ApplyDesignTokens();
        }

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            SetupHUD();
            HideCompletion();
            HideLevelFailed();
            HideCrisis();
        }

        protected override void OnUnbind()
        {
            base.OnUnbind();
        }

        public void AutoWireUIReferences()
        {
            if (_completionPanel == null) _completionPanel = FindObject("completion");
            if (_levelFailedPanel == null) _levelFailedPanel = FindObject("failed");
            if (_crashToast == null) _crashToast = FindObject("toast");
            if (_crisisPanel == null) _crisisPanel = FindObject("crisis");
            if (_starsContainer == null) _starsContainer = FindObject("stars");

            var compTr = _completionPanel != null ? _completionPanel.transform : null;
            var failTr = _levelFailedPanel != null ? _levelFailedPanel.transform : null;
            var crisisTr = _crisisPanel != null ? _crisisPanel.transform : null;

            if (_hintButton == null) _hintButton = FindButton("hint");
            if (_nextLevelButton == null) _nextLevelButton = FindButton("next", compTr) ?? FindButton("next");
            if (_continueButton == null) _continueButton = FindButton("continue", compTr) ?? FindButton("continue");
            if (_undoButton == null) _undoButton = FindButton("undo");
            if (_redoButton == null) _redoButton = FindButton("redo");
            if (_themeDarkButton == null) _themeDarkButton = FindButton("dark");
            if (_themeLightButton == null) _themeLightButton = FindButton("light");
            if (_themeNeonButton == null) _themeNeonButton = FindButton("neon");
            if (_pauseButton == null) _pauseButton = FindButton("pause");
            if (_retryButton == null) _retryButton = FindButton("retry", failTr) ?? FindButton("retry");
            if (_levelFailedContinueButton == null) _levelFailedContinueButton = FindButton("continue", failTr) ?? FindButton("hub", failTr);
            if (_garageButton == null) _garageButton = FindButton("garage");
            if (_rainbowRoadButton == null) _rainbowRoadButton = FindButton("rainbow");
            if (_clearJamButton == null) _clearJamButton = FindButton("clear");
            if (_viaductButton == null) _viaductButton = FindButton("viaduct");
            if (_crisisViaductButton == null) _crisisViaductButton = FindButton("viaduct", crisisTr);
            if (_crisisUndoButton == null) _crisisUndoButton = FindButton("undo", crisisTr);

            if (_hintCountText == null) _hintCountText = FindText("hint");
            if (_scoreText == null) _scoreText = FindText("hudscore") ?? FindText("score");
            if (_timerText == null) _timerText = FindText("timer");
            if (_levelTitleText == null) _levelTitleText = FindText("title");
            if (_completionText == null) _completionText = FindText("completion") ?? FindText("title", compTr);
            if (_completionScoreText == null) _completionScoreText = FindText("score", compTr) ?? FindText("completionscore");
            if (_completionStarsText == null) _completionStarsText = FindText("stars", compTr) ?? FindText("completionstars");
            if (_levelFailedText == null) _levelFailedText = FindText("failed") ?? FindText("title", failTr);
            if (_coinsText == null) _coinsText = FindText("coin");
            if (_rainbowRoadCountText == null) _rainbowRoadCountText = FindText("rainbow");
            if (_clearJamCountText == null) _clearJamCountText = FindText("clear");
            if (_viaductCountText == null) _viaductCountText = FindText("viaduct");
            if (_crashToastText == null) _crashToastText = FindText("toast");
            if (_crisisTitleText == null) _crisisTitleText = FindText("crisistitle") ?? FindText("title", crisisTr);
            if (_crisisDescriptionText == null) _crisisDescriptionText = FindText("desc", crisisTr);
            if (_crisisViaductCountText == null) _crisisViaductCountText = FindText("count", crisisTr) ?? FindText("viaduct", crisisTr);

            LoggerService?.Log($"[PixelFlow.HUDView] AutoWire: hintBtn={(bool)_hintButton}, undoBtn={(bool)_undoButton}, redoBtn={(bool)_redoButton}, nextLvlBtn={(bool)_nextLevelButton}, continueBtn={(bool)_continueButton}, pauseBtn={(bool)_pauseButton}, retryBtn={(bool)_retryButton}, garageBtn={(bool)_garageButton}, rainbowBtn={(bool)_rainbowRoadButton}, clearJamBtn={(bool)_clearJamButton}, themeDark={(bool)_themeDarkButton}, themeLight={(bool)_themeLightButton}, themeNeon={(bool)_themeNeonButton}, completionPanel={(bool)_completionPanel}, levelFailedPanel={(bool)_levelFailedPanel}, starsContainer={(bool)_starsContainer}");
        }

        public void BindHUDButtons()
        {
            BindButton(_hintButton, () => OnHintClicked?.Invoke());
            BindButton(_nextLevelButton, () => OnNextLevelClicked?.Invoke());
            BindButton(_continueButton, () => OnContinueClicked?.Invoke());
            BindButton(_undoButton, () => OnUndoClicked?.Invoke());
            BindButton(_redoButton, () => OnRedoClicked?.Invoke());
            BindButton(_themeDarkButton, () => OnThemeDarkClicked?.Invoke());
            BindButton(_themeLightButton, () => OnThemeLightClicked?.Invoke());
            BindButton(_themeNeonButton, () => OnThemeNeonClicked?.Invoke());
            BindButton(_pauseButton, () => OnPauseClicked?.Invoke());
            BindButton(_retryButton, () => OnRetryClicked?.Invoke());
            BindButton(_levelFailedContinueButton, () => OnLevelFailedContinueClicked?.Invoke());
            BindButton(_garageButton, () => OnGarageClicked?.Invoke());
            BindButton(_rainbowRoadButton, () => OnRainbowRoadClicked?.Invoke());
            BindButton(_clearJamButton, () => OnClearJamClicked?.Invoke());
            BindButton(_viaductButton, () => OnViaductClicked?.Invoke());
            BindButton(_crisisViaductButton, () => OnCrisisViaductClicked?.Invoke());
            BindButton(_crisisUndoButton, () => OnCrisisUndoClicked?.Invoke());

            var debugBtn = FindButton("debug") ?? FindButton("simulate");
            if (debugBtn != null) BindButton(debugBtn, () => OnSimulateDebugPressed?.Invoke());
        }

        private static void BindButton(Button button, Action onClick)
        {
            if (button == null || onClick == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }

        public void UpdateHintCount(int count, string format)
        {
            if (_hintCountText != null) _hintCountText.text = string.Format(format, count);
        }

        public void UpdateScore(int score, string format)
        {
            if (_scoreText != null) _scoreText.text = string.Format(format, score);
        }

        public void UpdateTimer(float time)
        {
            if (_timerText != null) _timerText.text = time.ToString("F1");
        }

        public void UpdateSimulationTimer(float remaining, string format)
        {
            if (_timerText != null) _timerText.text = string.Format(format, remaining);
        }

        public void UpdateStars(int stars)
        {
            SetStarActive(_star1, stars >= 1);
            SetStarActive(_star2, stars >= 2);
            SetStarActive(_star3, stars >= 3);
            if (_completionStarsText != null) _completionStarsText.text = new string('★', Mathf.Clamp(stars, 0, 3));
        }

        public void UpdateLevelTitle(int levelNumber, string format)
        {
            if (_levelTitleText != null) _levelTitleText.text = string.Format(format, levelNumber);
        }

        public void UpdateViaductCount(int count)
        {
            if (_viaductCountText != null) _viaductCountText.text = count.ToString();
            if (_crisisViaductCountText != null) _crisisViaductCountText.text = count.ToString();
        }

        public void UpdateRainbowRoadCount(int count)
        {
            if (_rainbowRoadCountText != null) _rainbowRoadCountText.text = count.ToString();
        }

        public void UpdateClearJamCount(int count)
        {
            if (_clearJamCountText != null) _clearJamCountText.text = count.ToString();
        }

        public void HighlightActiveTheme(AppTheme theme)
        {
            SetThemeButtonColor(_themeDarkButton, theme == AppTheme.Dark);
            SetThemeButtonColor(_themeLightButton, theme == AppTheme.Light);
            SetThemeButtonColor(_themeNeonButton, theme == AppTheme.Neon);
        }

        public void SetUndoInteractable(bool interactable)
        {
            if (_undoButton != null) _undoButton.interactable = interactable;
        }

        public void SetRedoInteractable(bool interactable)
        {
            if (_redoButton != null) _redoButton.interactable = interactable;
        }

        public void SetPowerUpButtonsInteractable(bool interactable)
        {
            if (_rainbowRoadButton != null) _rainbowRoadButton.interactable = interactable;
            if (_clearJamButton != null) _clearJamButton.interactable = interactable;
            if (_viaductButton != null) _viaductButton.interactable = interactable;
        }

        public void ShowCompletion(int score, int stars, string title, string scoreFormat, string starsLabel)
        {
            SetPanelVisible(_completionPanel, true);
            if (_completionText != null) _completionText.text = title;
            if (_completionScoreText != null) _completionScoreText.text = string.Format(scoreFormat, score);
            if (_completionStarsText != null) _completionStarsText.text = string.Format(starsLabel, stars);
        }

        public void HideCompletion() => SetPanelVisible(_completionPanel, false);

        public void ShowLevelFailed(string title, string scoreFormat, string retryLabel, string hubLabel)
        {
            SetPanelVisible(_levelFailedPanel, true);
            if (_levelFailedText != null) _levelFailedText.text = title;
            if (_levelFailedContinueButton != null)
            {
                var txt = _levelFailedContinueButton.GetComponentInChildren<TMP_Text>(true);
                if (txt != null) txt.text = hubLabel;
            }
            if (_retryButton != null)
            {
                var txt = _retryButton.GetComponentInChildren<TMP_Text>(true);
                if (txt != null) txt.text = retryLabel;
            }
        }

        public void HideLevelFailed() => SetPanelVisible(_levelFailedPanel, false);

        public void ShowCrisis(int viaductCount, string title, string desc, string viaductFormat, string viaductBtn, string undoBtn)
        {
            SetPanelVisible(_crisisPanel, true);
            if (_crisisTitleText != null) _crisisTitleText.text = title;
            if (_crisisDescriptionText != null) _crisisDescriptionText.text = desc;
            if (_crisisViaductCountText != null) _crisisViaductCountText.text = string.Format(viaductFormat, viaductCount);
            if (_crisisViaductButton != null)
            {
                var txt = _crisisViaductButton.GetComponentInChildren<TMP_Text>(true);
                if (txt != null) txt.text = viaductBtn;
            }
            if (_crisisUndoButton != null)
            {
                var txt = _crisisUndoButton.GetComponentInChildren<TMP_Text>(true);
                if (txt != null) txt.text = undoBtn;
            }
        }

        public void HideCrisis() => SetPanelVisible(_crisisPanel, false);

        public void ShowCrashToast(string message)
        {
            if (_crashToastText != null) _crashToastText.text = message;
            SetPanelVisible(_crashToast, true);
            if (_crashToastCoroutine != null) StopCoroutine(_crashToastCoroutine);
            _crashToastCoroutine = StartCoroutine(HideToastAfterDelay());
        }

        public void ShowViaductLimitReached(string message)
        {
            ShowCrashToast(message);
        }

        public void ShowCrisisRetryExhausted(int retryCount)
        {
            ShowCrashToast($"Retry exhausted: {retryCount}");
        }

        private System.Collections.IEnumerator HideToastAfterDelay()
        {
            yield return new WaitForSeconds(_crashToastDuration);
            SetPanelVisible(_crashToast, false);
            _crashToastCoroutine = null;
        }

        private void ApplyDesignTokens()
        {
            var theme = SettingsModel != null ? SettingsModel.CurrentTheme : AppTheme.Dark;
            if (ThemePalette != null)
            {
                var colors = ThemePalette.GetThemeColors(theme);
                if (_levelTitleText != null)
                    _levelTitleText.color = colors.CameraBackground;
                if (_hintButton != null && _hintButton.GetComponent<Image>() != null)
                    _hintButton.GetComponent<Image>().color = _goldPillBg;
            }

            HighlightActiveTheme(theme);
        }

        private void UpdateTimerColor(float remaining)
        {
            if (_timerText == null) return;
            _timerText.color = remaining > 3f ? Color.white : Color.Lerp(Color.red, Color.yellow, remaining / 3f);
        }

        private static void SetThemeButtonColor(Button button, bool isActive)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image == null) return;
            image.color = isActive ? new Color(0.35f, 0.7f, 0.45f, 1f) : new Color(0.15f, 0.15f, 0.18f, 1f);
        }

        private static void SetPanelVisible(GameObject panel, bool visible)
        {
            if (panel == null) return;
            panel.SetActive(visible);
        }

        private static void SetStarActive(GameObject star, bool active)
        {
            if (star != null) star.SetActive(active);
        }

        private Button FindButton(string token, Transform root = null)
        {
            var searchRoot = root != null ? root : transform;
            var buttons = searchRoot.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                string name = button.gameObject.name.ToLowerInvariant();
                if (name.Contains(token)) return button;
            }
            return null;
        }

        private TMP_Text FindText(string token, Transform root = null)
        {
            var searchRoot = root != null ? root : transform;
            var texts = searchRoot.GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in texts)
            {
                string name = text.gameObject.name.ToLowerInvariant();
                if (name.Contains(token)) return text;
            }
            return null;
        }

        private GameObject FindObject(string token, Transform root = null)
        {
            var searchRoot = root != null ? root : transform;
            var transforms = searchRoot.GetComponentsInChildren<Transform>(true);
            foreach (var tr in transforms)
            {
                string name = tr.gameObject.name.ToLowerInvariant();
                if (name.Contains(token)) return tr.gameObject;
            }
            return null;
        }
    }
}
