using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Core;

namespace PixelFlow.Views
{
    [Mediator(typeof(HUDMediator))]
    public class HUDView : TickableView
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

        // Undo/Redo butonları
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _redoButton;

        // Tema değiştirme butonları (Dark/Light/Neon). Inspector'dan atanır;
        // biri null olsa bile UI çökmez (OnBind'de null-check var).
        [SerializeField] private Button _themeDarkButton;
        [SerializeField] private Button _themeLightButton;
        [SerializeField] private Button _themeNeonButton;

        // GDD §8: Pause butonu
        [SerializeField] private Button _pauseButton;

        // GDD §2.4: LevelFailed paneli
        [SerializeField] private GameObject _levelFailedPanel;
        [SerializeField] private TMP_Text _levelFailedText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _levelFailedContinueButton;

        // Color Jam 3D - Gold Coins & Power-Up UI
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private Button _garageButton;
        [SerializeField] private Button _rainbowRoadButton;
        [SerializeField] private Button _clearJamButton;

        public event Action OnGarageClicked;
        public event Action OnRainbowRoadClicked;
        public event Action OnClearJamClicked;

        public event Action OnHintClicked;
        public event Action OnNextLevelClicked;
        public event Action OnContinueClicked;
        public event Action OnUndoClicked;
        public event Action OnRedoClicked;
        public event Action OnThemeDarkClicked;
        public event Action OnThemeLightClicked;
        public event Action OnThemeNeonClicked;
        public event Action OnSimulateDebugPressed;
        public event Action OnCrisisViaductClicked;
        public event Action OnCrisisUndoClicked;
        public event Action OnPauseClicked;
        public event Action OnRetryClicked;
        public event Action OnLevelFailedContinueClicked;

        private Button _crisisViaductButton;
        private Button _crisisUndoButton;

        [Inject] public ILoggerService LoggerService { get; set; }

        private void Awake()
        {
            AutoWireUIReferences();
        }

        public void AutoWireUIReferences()
        {
            var texts = GetComponentsInChildren<TMP_Text>(true);
            var buttons = GetComponentsInChildren<Button>(true);

            foreach (var t in texts)
            {
                string name = t.gameObject.name.ToLower();
                if (_scoreText == null && (name.Contains("score") || name.Contains("puan"))) _scoreText = t;
                if (_timerText == null && (name.Contains("timer") || name.Contains("time") || name.Contains("sure"))) _timerText = t;
                if (_hintCountText == null && name.Contains("hint")) _hintCountText = t;
                if (_completionText == null && name.Contains("complet")) _completionText = t;
                if (_completionScoreText == null && name.Contains("finalscore")) _completionScoreText = t;
                if (_levelFailedText == null && name.Contains("failed")) _levelFailedText = t;
            }

            foreach (var b in buttons)
            {
                string name = b.gameObject.name.ToLower();
                if (_hintButton == null && name.Contains("hint")) _hintButton = b;
                if (_undoButton == null && name.Contains("undo")) _undoButton = b;
                if (_redoButton == null && name.Contains("redo")) _redoButton = b;
                if (_nextLevelButton == null && name.Contains("next")) _nextLevelButton = b;
                if (_continueButton == null && name.Contains("continue")) _continueButton = b;
                if (_pauseButton == null && name.Contains("pause")) _pauseButton = b;
                if (_retryButton == null && name.Contains("retry")) _retryButton = b;
                if (_themeDarkButton == null && name.Contains("dark")) _themeDarkButton = b;
                if (_themeLightButton == null && name.Contains("light")) _themeLightButton = b;
                if (_themeNeonButton == null && name.Contains("neon")) _themeNeonButton = b;
            }

            var transforms = GetComponentsInChildren<Transform>(true);
            foreach (var tr in transforms)
            {
                string name = tr.gameObject.name.ToLower();
                if (_completionPanel == null && (name.Contains("completion") || name.Contains("victory"))) _completionPanel = tr.gameObject;
                if (_levelFailedPanel == null && (name.Contains("fail") || name.Contains("gameover"))) _levelFailedPanel = tr.gameObject;
                if (_starsContainer == null && name.Contains("star")) _starsContainer = tr.gameObject;
            }
        }

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            AutoWireUIReferences();
            if (_hintButton != null)
                _hintButton.onClick.AddListener(() => OnHintClicked?.Invoke());
            if (_nextLevelButton != null)
                _nextLevelButton.onClick.AddListener(() => OnNextLevelClicked?.Invoke());
            if (_continueButton != null)
                _continueButton.onClick.AddListener(() => OnContinueClicked?.Invoke());
            if (_undoButton != null)
                _undoButton.onClick.AddListener(() => OnUndoClicked?.Invoke());
            if (_redoButton != null)
                _redoButton.onClick.AddListener(() => OnRedoClicked?.Invoke());
            if (_themeDarkButton != null)
                _themeDarkButton.onClick.AddListener(() => OnThemeDarkClicked?.Invoke());
            if (_themeLightButton != null)
                _themeLightButton.onClick.AddListener(() => OnThemeLightClicked?.Invoke());
            if (_themeNeonButton != null)
                _themeNeonButton.onClick.AddListener(() => OnThemeNeonClicked?.Invoke());

            // GDD §8: Pause butonu
            if (_pauseButton != null)
                _pauseButton.onClick.AddListener(() => OnPauseClicked?.Invoke());

            // GDD §2.4: LevelFailed paneli
            if (_retryButton != null)
                _retryButton.onClick.AddListener(() => OnRetryClicked?.Invoke());
            if (_levelFailedContinueButton != null)
                _levelFailedContinueButton.onClick.AddListener(() => OnLevelFailedContinueClicked?.Invoke());
            // Color Jam 3D - Gold Coins & Power-Up UI Listeners
            if (_garageButton != null)
                _garageButton.onClick.AddListener(() => OnGarageClicked?.Invoke());
            if (_rainbowRoadButton != null)
                _rainbowRoadButton.onClick.AddListener(() => OnRainbowRoadClicked?.Invoke());
            if (_clearJamButton != null)
                _clearJamButton.onClick.AddListener(() => OnClearJamClicked?.Invoke());

            if (_completionPanel != null)
                _completionPanel.SetActive(false);
            if (_levelFailedPanel != null)
                _levelFailedPanel.SetActive(false);
        }

        protected override void OnUnbind()
        {
            base.OnUnbind();
            if (_hintButton != null)
                _hintButton.onClick.RemoveAllListeners();
            if (_nextLevelButton != null)
                _nextLevelButton.onClick.RemoveAllListeners();
            if (_continueButton != null)
                _continueButton.onClick.RemoveAllListeners();
            if (_undoButton != null)
                _undoButton.onClick.RemoveAllListeners();
            if (_redoButton != null)
                _redoButton.onClick.RemoveAllListeners();
            if (_themeDarkButton != null)
                _themeDarkButton.onClick.RemoveAllListeners();
            if (_themeLightButton != null)
                _themeLightButton.onClick.RemoveAllListeners();
            if (_themeNeonButton != null)
                _themeNeonButton.onClick.RemoveAllListeners();
            if (_pauseButton != null)
                _pauseButton.onClick.RemoveAllListeners();
            if (_retryButton != null)
                _retryButton.onClick.RemoveAllListeners();
            if (_levelFailedContinueButton != null)
                _levelFailedContinueButton.onClick.RemoveAllListeners();

            if (_crisisViaductButton != null)
            {
                _crisisViaductButton.onClick.RemoveAllListeners();
                Destroy(_crisisViaductButton.gameObject);
                _crisisViaductButton = null;
            }
            if (_crisisUndoButton != null)
            {
                _crisisUndoButton.onClick.RemoveAllListeners();
                Destroy(_crisisUndoButton.gameObject);
                _crisisUndoButton = null;
            }
        }

        public void SetUndoInteractable(bool interactable)
        {
            if (_undoButton != null)
                _undoButton.interactable = interactable;
        }

        public void SetRedoInteractable(bool interactable)
        {
            if (_redoButton != null)
                _redoButton.interactable = interactable;
        }

        public void UpdateHintCount(int count, string format)
        {
            if (_hintCountText != null)
                _hintCountText.text = string.Format(format, count);
        }

        public void UpdateScore(int score, string format)
        {
            if (_scoreText != null)
                _scoreText.text = string.Format(format, score);
        }

        public void UpdateLevelTitle(int levelNumber, string format)
        {
            if (_levelTitleText != null)
                _levelTitleText.text = string.Format(format, levelNumber);
        }

        public void UpdateCoins(int coins)
        {
            if (_coinsText != null)
                _coinsText.text = coins.ToString("N0");
        }

        public void UpdateTimer(float elapsedTime)
        {
            if (_timerText != null)
            {
                int minutes = Mathf.FloorToInt(elapsedTime / 60f);
                int seconds = Mathf.FloorToInt(elapsedTime % 60f);
                _timerText.text = $"{minutes:00}:{seconds:00}";
            }
            if (_timerText != null)
            {
                _timerText.color = Color.white; // Simülasyondan çıkışta rengi sıfırla
            }
        }

        public void UpdateSimulationTimer(float remaining, string format)
        {
            if (_timerText != null)
            {
                _timerText.text = string.Format(format, remaining);
                _timerText.color = remaining > 3f ? Color.green : Color.Lerp(Color.red, Color.yellow, remaining / 3f);
            }
        }

        public void UpdateStars(int stars)
        {
            if (_starsContainer != null)
            {
                if (_star1 != null) _star1.SetActive(stars >= 1);
                if (_star2 != null) _star2.SetActive(stars >= 2);
                if (_star3 != null) _star3.SetActive(stars >= 3);
            }
        }

        public void ShowCompletion(int score, int stars, string title, string scoreFormat, string starsLabel)
        {
            if (_crisisViaductButton != null)
                _crisisViaductButton.gameObject.SetActive(false);
            if (_crisisUndoButton != null)
                _crisisUndoButton.gameObject.SetActive(false);

            if (_completionPanel == null)
            {
                _completionPanel = new GameObject("CompletionPanel");
                _completionPanel.transform.SetParent(transform, false);
                var rect = _completionPanel.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                var img = _completionPanel.AddComponent<Image>();
                img.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);

                if (_nextLevelButton == null)
                {
                    var btnObj = new GameObject("NextLevelButton");
                    btnObj.transform.SetParent(_completionPanel.transform, false);
                    var btnRect = btnObj.AddComponent<RectTransform>();
                    btnRect.sizeDelta = new Vector2(240, 60);
                    btnObj.AddComponent<Image>().color = new Color(0.1f, 0.7f, 0.3f);
                    _nextLevelButton = btnObj.AddComponent<Button>();
                    _nextLevelButton.onClick.AddListener(() => OnNextLevelClicked?.Invoke());

                    var btnTextObj = new GameObject("Text");
                    btnTextObj.transform.SetParent(btnObj.transform, false);
                    var tmp = btnTextObj.AddComponent<TextMeshProUGUI>();
                    tmp.text = "SONRAKİ SEVİYE";
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = Color.white;
                }
            }

            _completionPanel.SetActive(true);
            _completionPanel.transform.SetAsLastSibling();
            _completionPanel.transform.localScale = Vector3.zero;
            StartCoroutine(AnimateCompletion(score, stars, title, scoreFormat, starsLabel));

            if (_bloomFlashOverlay != null)
            {
                StartCoroutine(DoBloomFlash());
            }
        }

        private System.Collections.IEnumerator DoBloomFlash()
        {
            _bloomFlashOverlay.SetActive(true);
            var img = _bloomFlashOverlay.GetComponent<UnityEngine.UI.Image>();
            if (img == null) yield break;
            float duration = 0.6f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                img.color = new Color(1f, 0.95f, 0.6f, Mathf.Lerp(0.8f, 0f, t / duration));
                yield return null;
            }
            _bloomFlashOverlay.SetActive(false);
        }

        private System.Collections.IEnumerator AnimateCompletion(int score, int stars, string title, string scoreFormat, string starsLabel)
        {
            float duration = 0.5f;
            float elapsed = 0f;
            RectTransform panelRect = _completionPanel.GetComponent<RectTransform>();

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Min(elapsed / duration, 1f);
                float bounce = 1f + Mathf.Sin(t * Mathf.PI) * (1f - t) * 0.4f;
                if (panelRect != null) panelRect.localScale = Vector3.one * Mathf.Lerp(0f, bounce, t * 1.5f);
                yield return null;
            }

            if (panelRect != null) panelRect.localScale = Vector3.one;

            if (_completionText != null)
                _completionText.text = title;
            if (_completionScoreText != null)
                _completionScoreText.text = string.Format(scoreFormat, score);
            if (_completionStarsText != null)
                _completionStarsText.text = $"{starsLabel}: {new string('★', stars)}{new string('☆', 3 - stars)}";
            if (_nextLevelButton != null)
                _nextLevelButton.gameObject.SetActive(true);
            else
                LoggerService?.LogWarning("[HUDView] _nextLevelButton is null in Inspector!");
        }

        public void HideCompletion()
        {
            if (_completionPanel != null)
                _completionPanel.SetActive(false);
        }

        private void CreateCrisisButtonsIfNeeded(string viaductBtnText, string undoBtnText)
        {
            if (_nextLevelButton == null || _completionPanel == null) return;

            if (_crisisViaductButton == null)
            {
                _crisisViaductButton = Instantiate(_nextLevelButton, _completionPanel.transform);
                _crisisViaductButton.name = "CrisisViaductButton";
                
                RectTransform rt = _crisisViaductButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(-120f, -120f);
                    rt.sizeDelta = new Vector2(200f, 50f);
                }
                
                TMP_Text btnText = _crisisViaductButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = viaductBtnText;

                _crisisViaductButton.onClick.AddListener(() => OnCrisisViaductClicked?.Invoke());
            }
            else
            {
                TMP_Text btnText = _crisisViaductButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = viaductBtnText;
            }

            if (_crisisUndoButton == null)
            {
                _crisisUndoButton = Instantiate(_nextLevelButton, _completionPanel.transform);
                _crisisUndoButton.name = "CrisisUndoButton";
                
                RectTransform rt = _crisisUndoButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(120f, -120f);
                    rt.sizeDelta = new Vector2(200f, 50f);
                }
                
                TMP_Text btnText = _crisisUndoButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = undoBtnText;

                _crisisUndoButton.onClick.AddListener(() => OnCrisisUndoClicked?.Invoke());
            }
            else
            {
                TMP_Text btnText = _crisisUndoButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = undoBtnText;
            }
        }

        public void ShowCrisis(int availableViaducts, string title, string desc, string viaductLabelFormat, string viaductBtnText, string undoBtnText)
        {
            if (_completionPanel != null)
            {
                _completionPanel.SetActive(true);
                if (_completionText != null)
                    _completionText.text = title;
                if (_completionScoreText != null)
                    _completionScoreText.text = desc;
                if (_completionStarsText != null)
                {
                    _completionStarsText.text = string.Format(viaductLabelFormat, availableViaducts);
                    _completionStarsText.color = Color.white;
                }
                if (_nextLevelButton != null)
                    _nextLevelButton.gameObject.SetActive(false);

                CreateCrisisButtonsIfNeeded(viaductBtnText, undoBtnText);

                if (_crisisViaductButton != null)
                {
                    _crisisViaductButton.gameObject.SetActive(true);
                    _crisisViaductButton.interactable = availableViaducts > 0;
                }
                if (_crisisUndoButton != null)
                {
                    _crisisUndoButton.gameObject.SetActive(true);
                }
            }
        }

        public void HideCrisis()
        {
            if (_crisisViaductButton != null)
                _crisisViaductButton.gameObject.SetActive(false);
            if (_crisisUndoButton != null)
                _crisisUndoButton.gameObject.SetActive(false);

            if (_completionPanel != null)
            {
                _completionPanel.SetActive(false);
                if (_nextLevelButton != null)
                    _nextLevelButton.gameObject.SetActive(true);
            }
        }

        // GDD §2.4: LevelFailed paneli
        public void ShowLevelFailed(string title, string scoreFormat, string retryLabel, string hubLabel)
        {
            if (_levelFailedPanel != null)
            {
                _levelFailedPanel.SetActive(true);
                if (_levelFailedText != null)
                    _levelFailedText.text = title;
                if (_retryButton != null)
                {
                    var btnText = _retryButton.GetComponentInChildren<TMP_Text>();
                    if (btnText != null) btnText.text = retryLabel;
                }
                if (_levelFailedContinueButton != null)
                {
                    var btnText = _levelFailedContinueButton.GetComponentInChildren<TMP_Text>();
                    if (btnText != null) btnText.text = hubLabel;
                }
            }
        }

        public void HideLevelFailed()
        {
            if (_levelFailedPanel != null)
                _levelFailedPanel.SetActive(false);
        }

        public void SetPauseButtonVisible(bool visible)
        {
            if (_pauseButton != null)
                _pauseButton.gameObject.SetActive(visible);
        }

        public void ShowViaductLimitReached(string message)
        {
            if (_completionStarsText != null)
            {
                _completionStarsText.text = message;
                _completionStarsText.color = Color.red;
            }
        }

        public void ShowCrisisRetryExhausted(int retryCount)
        {
            if (_completionStarsText != null)
            {
                _completionStarsText.text = $"Retries exhausted ({retryCount})";
                _completionStarsText.color = Color.red;
            }
        }

        public void HighlightActiveTheme(PixelFlow.Models.AppTheme theme)
        {
            SetThemeButtonColor(_themeDarkButton, theme == PixelFlow.Models.AppTheme.Dark);
            SetThemeButtonColor(_themeLightButton, theme == PixelFlow.Models.AppTheme.Light);
            SetThemeButtonColor(_themeNeonButton, theme == PixelFlow.Models.AppTheme.Neon);
        }

        private static void SetThemeButtonColor(Button button, bool isActive)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image == null) return;
            image.color = isActive
                ? new Color(0.35f, 0.7f, 0.45f, 1f)   // aktif: açık yeşil
                : new Color(0.15f, 0.15f, 0.18f, 1f);  // pasif: koyu gri
        }

        protected override void OnTick(float deltaTime)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.sKey.wasPressedThisFrame)
                {
                    if (Debug.isDebugBuild)
                    {
                        OnSimulateDebugPressed?.Invoke();
                    }
                }

                if (_completionPanel != null && _completionPanel.activeSelf)
                {
                    if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.nKey.wasPressedThisFrame)
                    {
                        LoggerService?.Log("[HUDView] Next level keyboard shortcut triggered (Space/Enter/N).");
                        OnNextLevelClicked?.Invoke();
                    }
                }
            }
        }
    }
}