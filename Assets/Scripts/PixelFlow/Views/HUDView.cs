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

        public event Action OnHintClicked;

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            if (_hintButton != null)
                _hintButton.onClick.AddListener(() => OnHintClicked?.Invoke());
            if (_completionPanel != null)
                _completionPanel.SetActive(false);
        }

        protected override void OnUnbind()
        {
            base.OnUnbind();
            if (_hintButton != null)
                _hintButton.onClick.RemoveAllListeners();
        }

        public void UpdateHintCount(int count)
        {
            if (_hintCountText != null)
                _hintCountText.text = count.ToString();
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
