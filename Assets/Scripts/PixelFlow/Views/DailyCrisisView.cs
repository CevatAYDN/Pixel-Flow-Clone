using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlow.Views
{
    public class DailyCrisisView : MonoBehaviour
    {
        [SerializeField] private GameObject _panelContainer;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _streakText;
        [SerializeField] private Text _badgesText;
        [SerializeField] private Button _closeButton;

        [SerializeField] private Button _easyButton;
        [SerializeField] private Button _mediumButton;
        [SerializeField] private Button _hardButton;

        [SerializeField] private Text _easyStatusText;
        [SerializeField] private Text _mediumStatusText;
        [SerializeField] private Text _hardStatusText;

        public event Action OnCloseClicked;
        public event Action<int> OnStartCrisisClicked;

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
            if (_easyButton != null) _easyButton.onClick.AddListener(() => OnStartCrisisClicked?.Invoke(0));
            if (_mediumButton != null) _mediumButton.onClick.AddListener(() => OnStartCrisisClicked?.Invoke(1));
            if (_hardButton != null) _hardButton.onClick.AddListener(() => OnStartCrisisClicked?.Invoke(2));
        }

        public void Show()
        {
            if (_panelContainer != null) _panelContainer.SetActive(true);
            else gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_panelContainer != null) _panelContainer.SetActive(false);
            else gameObject.SetActive(false);
        }

        public void UpdateInfo(int streak, int badges, bool easyCompleted, bool mediumCompleted, bool hardCompleted)
        {
            if (_streakText != null) _streakText.text = $"Streak: {streak} Days 🔥";
            if (_badgesText != null) _badgesText.text = $"Badges: {badges} 🎖";

            if (_easyStatusText != null) _easyStatusText.text = easyCompleted ? "✔ Completed" : "Start (Easy)";
            if (_mediumStatusText != null) _mediumStatusText.text = mediumCompleted ? "✔ Completed" : "Start (Medium)";
            if (_hardStatusText != null) _hardStatusText.text = hardCompleted ? "✔ Completed" : "Start (Hard)";

            if (_easyButton != null) _easyButton.interactable = !easyCompleted;
            if (_mediumButton != null) _mediumButton.interactable = !mediumCompleted;
            if (_hardButton != null) _hardButton.interactable = !hardCompleted;
        }
    }
}
