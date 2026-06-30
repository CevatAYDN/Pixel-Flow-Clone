using UnityEngine;
using UnityEngine.UI;
using System;
using Nexus.Core;

namespace PixelFlow.Views
{
    [Mediator(typeof(HUDMediator))]
    public class HUDView : View
    {
        [SerializeField] private Button _hintButton;
        [SerializeField] private Text _hintCountText;
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _timerText;
        [SerializeField] private GameObject _starsContainer;
        [SerializeField] private GameObject _star1;
        [SerializeField] private GameObject _star2;
        [SerializeField] private GameObject _star3;
        [SerializeField] private GameObject _completionPanel;
        [SerializeField] private Text _completionText;
        [SerializeField] private Text _completionScoreText;
        [SerializeField] private Text _completionStarsText;
        [SerializeField] private Button _nextLevelButton;

        // Undo/Redo butonları
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _redoButton;

        // Tema değiştirme butonları (Dark/Light/Neon). Inspector'dan atanır;
        // biri null olsa bile UI çökmez (OnBind'de null-check var).
        [SerializeField] private Button _themeDarkButton;
        [SerializeField] private Button _themeLightButton;
        [SerializeField] private Button _themeNeonButton;

        public event Action OnHintClicked;
        public event Action OnNextLevelClicked;
        public event Action OnUndoClicked;
        public event Action OnRedoClicked;
        public event Action OnThemeDarkClicked;
        public event Action OnThemeLightClicked;
        public event Action OnThemeNeonClicked;

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            if (_hintButton != null)
                _hintButton.onClick.AddListener(() => OnHintClicked?.Invoke());
            if (_nextLevelButton != null)
                _nextLevelButton.onClick.AddListener(() => OnNextLevelClicked?.Invoke());
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

            if (_completionPanel != null)
                _completionPanel.SetActive(false);
        }

        protected override void OnUnbind()
        {
            base.OnUnbind();
            if (_hintButton != null)
                _hintButton.onClick.RemoveAllListeners();
            if (_nextLevelButton != null)
                _nextLevelButton.onClick.RemoveAllListeners();
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

        public void UpdateHintCount(int count)
        {
            if (_hintCountText != null)
                _hintCountText.text = $"HINT ({count})";
        }

        public void UpdateScore(int score)
        {
            if (_scoreText != null)
                _scoreText.text = $"SKOR: {score}";
        }

        public void UpdateTimer(float elapsedTime)
        {
            if (_timerText != null)
            {
                int minutes = Mathf.FloorToInt(elapsedTime / 60f);
                int seconds = Mathf.FloorToInt(elapsedTime % 60f);
                _timerText.text = $"{minutes:00}:{seconds:00}";
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

        public void ShowCompletion(int score, int stars)
        {
            if (_completionPanel != null)
            {
                _completionPanel.SetActive(true);
                if (_completionText != null)
                    _completionText.text = "Tebrikler! Seviye Tamamland\u0131!";
                if (_completionScoreText != null)
                    _completionScoreText.text = $"Skor: {score}";
                if (_completionStarsText != null)
                    _completionStarsText.text = $"Y\u0131ld\u0131z: {new string('\u2605', stars)}{new string('\u2606', 3 - stars)}";
            }
        }

        public void HideCompletion()
        {
            if (_completionPanel != null)
                _completionPanel.SetActive(false);
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

    }
}