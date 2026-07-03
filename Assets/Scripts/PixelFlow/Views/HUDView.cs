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
        public event Action OnSimulateDebugPressed;
        public event Action OnCrisisViaductClicked;
        public event Action OnCrisisUndoClicked;

        private Button _crisisViaductButton;
        private Button _crisisUndoButton;

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

        public void UpdateSimulationTimer(float remaining)
        {
            if (_timerText != null)
            {
                _timerText.text = $"Simülasyon: {remaining:F1}s";
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

        public void ShowCompletion(int score, int stars)
        {
            if (_crisisViaductButton != null)
                _crisisViaductButton.gameObject.SetActive(false);
            if (_crisisUndoButton != null)
                _crisisUndoButton.gameObject.SetActive(false);

            if (_completionPanel != null)
            {
                _completionPanel.SetActive(true);
                _completionPanel.transform.localScale = Vector3.zero;
                StartCoroutine(AnimateCompletion(score, stars));
            }
            else
            {
                Debug.LogWarning("[HUDView] _completionPanel is null in Inspector! Cannot show level completed panel.");
            }
        }

        private System.Collections.IEnumerator AnimateCompletion(int score, int stars)
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
                _completionText.text = "Tebrikler! Seviye Tamamlandı!";
            if (_completionScoreText != null)
                _completionScoreText.text = $"Skor: {score}";
            if (_completionStarsText != null)
                _completionStarsText.text = $"Yıldız: {new string('★', stars)}{new string('☆', 3 - stars)}";
            if (_nextLevelButton != null)
                _nextLevelButton.gameObject.SetActive(true);
            else
                Debug.LogWarning("[HUDView] _nextLevelButton is null in Inspector!");
        }

        public void HideCompletion()
        {
            if (_completionPanel != null)
                _completionPanel.SetActive(false);
        }

        private void CreateCrisisButtonsIfNeeded()
        {
            if (_nextLevelButton == null || _completionPanel == null) return;

            if (_crisisViaductButton == null)
            {
                _crisisViaductButton = Instantiate(_nextLevelButton, _completionPanel.transform);
                _crisisViaductButton.name = "CrisisViaductButton";
                
                // Konumu ayarla (Sol taraf)
                RectTransform rt = _crisisViaductButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(-120f, -120f);
                    rt.sizeDelta = new Vector2(200f, 50f);
                }
                
                Text btnText = _crisisViaductButton.GetComponentInChildren<Text>();
                if (btnText != null) btnText.text = "Viyadük Kullan";

                _crisisViaductButton.onClick.AddListener(() => OnCrisisViaductClicked?.Invoke());
            }

            if (_crisisUndoButton == null)
            {
                _crisisUndoButton = Instantiate(_nextLevelButton, _completionPanel.transform);
                _crisisUndoButton.name = "CrisisUndoButton";
                
                // Konumu ayarla (Sağ taraf)
                RectTransform rt = _crisisUndoButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(120f, -120f);
                    rt.sizeDelta = new Vector2(200f, 50f);
                }
                
                Text btnText = _crisisUndoButton.GetComponentInChildren<Text>();
                if (btnText != null) btnText.text = "Geri Al / Geri Dön";

                _crisisUndoButton.onClick.AddListener(() => OnCrisisUndoClicked?.Invoke());
            }
        }

        public void ShowCrisis(int availableViaducts)
        {
            if (_completionPanel != null)
            {
                _completionPanel.SetActive(true);
                if (_completionText != null)
                    _completionText.text = "TRAFİK KRİZİ! 🚨";
                if (_completionScoreText != null)
                    _completionScoreText.text = "Çarpışmayı çözmek için viyadük köprüsü yerleştirin!";
                if (_completionStarsText != null)
                    _completionStarsText.text = $"Kalan Viyadük: {availableViaducts}";
                if (_nextLevelButton != null)
                    _nextLevelButton.gameObject.SetActive(false);

                CreateCrisisButtonsIfNeeded();

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

        public void ShowViaductLimitReached()
        {
            if (_completionStarsText != null)
            {
                _completionStarsText.text = "Viyadük hakkınız bitti!";
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

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                // S tuşu ile simülasyonu manuel başlat/durdur (Test amaçlı debug)
                if (keyboard.sKey.wasPressedThisFrame)
                {
                    OnSimulateDebugPressed?.Invoke();
                }

                if (_completionPanel != null && _completionPanel.activeSelf)
                {
                    if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.nKey.wasPressedThisFrame)
                    {
                        Debug.Log("[HUDView] Next level keyboard shortcut triggered (Space/Enter/N).");
                        OnNextLevelClicked?.Invoke();
                    }
                }
            }
        }
    }
}