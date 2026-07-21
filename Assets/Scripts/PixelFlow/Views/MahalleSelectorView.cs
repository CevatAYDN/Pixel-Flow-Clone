using System;
using UnityEngine;
using UnityEngine.UI;
using PixelFlow.Models;
using PixelFlow.Signals;
using Nexus.Core;

namespace PixelFlow.Views
{
    [Mediator(typeof(MahalleSelectorMediator))]
    public class MahalleSelectorView : View
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _district0Button;
        [SerializeField] private Button _district1Button;
        [SerializeField] private Button _district2Button;
        [SerializeField] private Button _district3Button;
        [SerializeField] private Button _district4Button;
        [SerializeField] private Button _district5Button;
        [SerializeField] private Button _closeButton;

        public event Action<int> OnDistrictSelected;
        public event Action OnCloseClicked;

        public void SetVisible(bool visible)
        {
            if (_panel != null) _panel.SetActive(visible);
        }

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            if (_district0Button != null) _district0Button.onClick.AddListener(() => OnDistrictSelected?.Invoke(0));
            if (_district1Button != null) _district1Button.onClick.AddListener(() => OnDistrictSelected?.Invoke(1));
            if (_district2Button != null) _district2Button.onClick.AddListener(() => OnDistrictSelected?.Invoke(2));
            if (_district3Button != null) _district3Button.onClick.AddListener(() => OnDistrictSelected?.Invoke(3));
            if (_district4Button != null) _district4Button.onClick.AddListener(() => OnDistrictSelected?.Invoke(4));
            if (_district5Button != null) _district5Button.onClick.AddListener(() => OnDistrictSelected?.Invoke(5));
            if (_closeButton != null) _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
        }

        protected override void OnUnbind()
        {
            base.OnUnbind();
            if (_district0Button != null) _district0Button.onClick.RemoveAllListeners();
            if (_district1Button != null) _district1Button.onClick.RemoveAllListeners();
            if (_district2Button != null) _district2Button.onClick.RemoveAllListeners();
            if (_district3Button != null) _district3Button.onClick.RemoveAllListeners();
            if (_district4Button != null) _district4Button.onClick.RemoveAllListeners();
            if (_district5Button != null) _district5Button.onClick.RemoveAllListeners();
            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
        }
    }

    public class MahalleSelectorMediator : Mediator<MahalleSelectorView>
    {
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IProgressModel ProgressModel { get; set; }

        protected override void OnBind()
        {
            View.OnDistrictSelected += HandleDistrict;
            View.OnCloseClicked += HandleClose;
            View.SetVisible(false);
        }

        protected override void OnUnbind()
        {
            View.OnDistrictSelected -= HandleDistrict;
            View.OnCloseClicked -= HandleClose;
        }

        private void HandleDistrict(int idx)
        {
            int requiredLevel = PixelFlow.Commands.EnterDistrictCommand.DistrictToLevelIndex(idx);
            if (requiredLevel < 0) return;
            if (requiredLevel > ProgressModel.UnlockedLevels - 1) return;

            SignalBus.Fire(new PixelFlow.Signals.EnterDistrictSignal { DistrictIndex = idx });
            View.SetVisible(false);
        }

        private void HandleClose() => View.SetVisible(false);
    }
}
