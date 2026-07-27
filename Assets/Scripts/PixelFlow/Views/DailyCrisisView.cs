using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Nexus.Core;
using Nexus.Core.Services;

namespace PixelFlow.Views
{
    [Mediator(typeof(DailyCrisisMediator))]
    public class DailyCrisisView : View
    {
        [SerializeField] private GameObject _panelContainer;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _streakText;
        [SerializeField] private TMP_Text _badgesText;
        [SerializeField] private Button _closeButton;

        [SerializeField] private Button _easyButton;
        [SerializeField] private Button _mediumButton;
        [SerializeField] private Button _hardButton;

        [SerializeField] private TMP_Text _easyStatusText;
        [SerializeField] private TMP_Text _mediumStatusText;
        [SerializeField] private TMP_Text _hardStatusText;

        public event Action OnCloseClicked;
        public event Action<int> OnStartCrisisClicked;

        [Inject] public ILocalizationService LocalizationService { get; set; }

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            if (_closeButton != null) _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
            if (_easyButton != null) _easyButton.onClick.AddListener(() => OnStartCrisisClicked?.Invoke(0));
            if (_mediumButton != null) _mediumButton.onClick.AddListener(() => OnStartCrisisClicked?.Invoke(1));
            if (_hardButton != null) _hardButton.onClick.AddListener(() => OnStartCrisisClicked?.Invoke(2));
        }

        protected override void OnUnbind()
        {
            base.OnUnbind();
            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
            if (_easyButton != null) _easyButton.onClick.RemoveAllListeners();
            if (_mediumButton != null) _mediumButton.onClick.RemoveAllListeners();
            if (_hardButton != null) _hardButton.onClick.RemoveAllListeners();
        }

        private void Awake() { } // UI init only; bindings moved to OnBind

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

        public void Show() => SetVisible(true);
        public void Hide() => SetVisible(false);

        public void UpdateInfo(int streak, int badges, bool easyCompleted, bool mediumCompleted, bool hardCompleted)
        {
            if (_streakText != null) _streakText.text = streak.ToString();
            if (_badgesText != null) _badgesText.text = badges.ToString();

            if (_easyStatusText != null) _easyStatusText.text = easyCompleted ? string.Empty : ResolveStatusLabel("daily_crisis_status_easy", "0");
            if (_mediumStatusText != null) _mediumStatusText.text = mediumCompleted ? string.Empty : ResolveStatusLabel("daily_crisis_status_medium", "1");
            if (_hardStatusText != null) _hardStatusText.text = hardCompleted ? string.Empty : ResolveStatusLabel("daily_crisis_status_hard", "2");

            if (_easyButton != null) _easyButton.interactable = !easyCompleted;
            if (_mediumButton != null) _mediumButton.interactable = !mediumCompleted;
            if (_hardButton != null) _hardButton.interactable = !hardCompleted;
        }

        private string ResolveStatusLabel(string key, string fallback)
        {
            if (LocalizationService == null) return fallback;
            string value = LocalizationService.GetString(key);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
