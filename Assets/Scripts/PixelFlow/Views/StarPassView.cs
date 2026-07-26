using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Nexus.Core;

namespace PixelFlow.Views
{
    [Mediator(typeof(StarPassMediator))]
    public class StarPassView : View
    {
        [SerializeField] private GameObject _panelContainer;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _tierProgressText;
        [SerializeField] private Slider _tierProgressBar;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _claimRewardButton;
        [SerializeField] private Button _buyPassButton;
        [SerializeField] private RectTransform _rewardTrackContainer;

        public event Action OnCloseClicked;
        public event Action OnClaimClicked;
        public event Action OnBuyPassClicked;

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            if (_closeButton != null)
            {
                ButtonJuice.AttachTo(_closeButton);
                _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
            }
            if (_claimRewardButton != null)
            {
                ButtonJuice.AttachTo(_claimRewardButton);
                _claimRewardButton.onClick.AddListener(() => OnClaimClicked?.Invoke());
            }
            if (_buyPassButton != null)
            {
                ButtonJuice.AttachTo(_buyPassButton);
                _buyPassButton.onClick.AddListener(() => OnBuyPassClicked?.Invoke());
            }
        }

        protected override void OnUnbind()
        {
            base.OnUnbind();
            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
            if (_claimRewardButton != null) _claimRewardButton.onClick.RemoveAllListeners();
            if (_buyPassButton != null) _buyPassButton.onClick.RemoveAllListeners();
        }

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

        public void UpdateProgress(int currentTier, int maxTier, float progress, bool isPremiumUnlocked)
        {
            if (_tierProgressText != null)
                _tierProgressText.text = $"Tier {currentTier} / {maxTier}";
            if (_tierProgressBar != null)
                _tierProgressBar.value = progress;
            if (_buyPassButton != null)
                _buyPassButton.gameObject.SetActive(!isPremiumUnlocked);
        }
    }
}
