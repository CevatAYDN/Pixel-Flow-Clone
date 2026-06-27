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

        public event Action OnHintClicked;
        public event Action OnNextLevelClicked;

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            if (_hintButton != null)
                _hintButton.onClick.AddListener(() => OnHintClicked?.Invoke());
            if (_nextLevelButton != null)
                _nextLevelButton.onClick.AddListener(() => OnNextLevelClicked?.Invoke());
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
        }

        public void UpdateHintCount(int count)
        {
            if (_hintCountText != null)
                _hintCountText.text = $"HINT ({count})";
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
