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

        public event Action OnHintClicked;

        protected override void OnBind(IContext context)
        {
            if (_hintButton != null)
                _hintButton.onClick.AddListener(() => OnHintClicked?.Invoke());
        }

        protected override void OnUnbind()
        {
            if (_hintButton != null)
                _hintButton.onClick.RemoveAllListeners();
        }

        public void UpdateHintCount(int count)
        {
            if (_hintCountText != null)
                _hintCountText.text = count.ToString();
        }
    }
}
