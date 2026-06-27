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
        [SerializeField] private GameObject _completionPanel;
        [SerializeField] private Text _completionText;
        [SerializeField] private Button _nextLevelButton;

        // Tema değiştirme butonları (Dark/Light/Neon). Inspector'dan atanır;
        // biri null olsa bile UI çökmez (OnBind'de null-check var).
        [SerializeField] private Button _themeDarkButton;
        [SerializeField] private Button _themeLightButton;
        [SerializeField] private Button _themeNeonButton;

        public event Action OnHintClicked;
        public event Action OnNextLevelClicked;
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
            if (_themeDarkButton != null)
                _themeDarkButton.onClick.RemoveAllListeners();
            if (_themeLightButton != null)
                _themeLightButton.onClick.RemoveAllListeners();
            if (_themeNeonButton != null)
                _themeNeonButton.onClick.RemoveAllListeners();
        }

        public void UpdateHintCount(int count)
        {
            if (_hintCountText != null)
                _hintCountText.text = $"HINT ({count})";
        }

        /// <summary>
        /// Mevcut aktif temayı vurgular. Tema butonlarından birinin Image bileşenini
        /// highlight etmek için kullanılır; herhangi bir buton null ise güvenli no-op.
        /// </summary>
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

        public void ShowCompletion()
        {
            if (_completionPanel != null)
            {
                _completionPanel.SetActive(true);
                if (_completionText != null)
                    _completionText.text = "Tebrikler! Seviye Tamamland\u0131!";
            }
        }

        public void HideCompletion()
        {
            if (_completionPanel != null)
                _completionPanel.SetActive(false);
        }
    }
}