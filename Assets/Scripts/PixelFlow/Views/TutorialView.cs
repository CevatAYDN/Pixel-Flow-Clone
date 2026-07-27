using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelFlow.Core;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using Nexus.Core;
using Nexus.Core.Services;

namespace PixelFlow.Views
{
    [Mediator(typeof(TutorialMediator))]
    public class TutorialView : TickableView
    {
        [SerializeField] private GameObject _bubble;
        [SerializeField] private TMP_Text _bubbleText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject _fingerIndicator;
        [SerializeField] private RectTransform _fingerTarget;

        [Inject] public ILocalizationService LocalizationService { get; set; }

        private TutorialStep _currentStep;
        private float _showTime;
        private float _fingerTime;
        private Vector2 _fingerOrigin;
        private Vector2 _fingerTapTarget;

        public void ShowStep(TutorialStep step, string message, float autoHideSeconds = 0f)
        {
            _currentStep = step;
            if (_bubble != null) _bubble.SetActive(true);
            if (_bubbleText != null) _bubbleText.text = ResolveMessage(step, message);
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            _showTime = autoHideSeconds;
            StartFingerAnimation(step);
        }

        public void Hide()
        {
            if (_bubble != null) _bubble.SetActive(false);
            if (_fingerIndicator != null) _fingerIndicator.SetActive(false);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }

        private void StartFingerAnimation(TutorialStep step)
        {
            if (_fingerIndicator == null) return;
            _fingerIndicator.SetActive(true);
            _fingerTime = 0f;
            _fingerOrigin = ResolveFingerOrigin(step);
            _fingerTapTarget = _fingerOrigin + new Vector2(0f, -50f);
        }

        private string ResolveMessage(TutorialStep step, string fallbackMessage)
        {
            if (!string.IsNullOrWhiteSpace(fallbackMessage)) return fallbackMessage;
            string key = GetMessageKey(step);
            if (string.IsNullOrWhiteSpace(key) || LocalizationService == null) return string.Empty;
            string value = LocalizationService.GetString(key);
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }

        private string GetMessageKey(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.TouchAndDrag: return "tutorial_touch_drag";
                case TutorialStep.ColorMatch: return "tutorial_color_match";
                case TutorialStep.VehicleFlow: return "tutorial_vehicle_flow";
                case TutorialStep.LevelComplete: return "tutorial_level_complete";
                case TutorialStep.ReturnToHub: return "tutorial_return_to_hub";
                case TutorialStep.TaxCollect: return "tutorial_tax_collect";
                case TutorialStep.SecondColor: return "tutorial_second_color";
                case TutorialStep.CrashIntro: return "tutorial_crash_intro";
                case TutorialStep.ViaductIntro: return "tutorial_viaduct_intro";
                case TutorialStep.UndoIntro: return "tutorial_undo_intro";
                case TutorialStep.ObstacleIntro: return "tutorial_obstacle_intro";
                case TutorialStep.OneWayIntro: return "tutorial_one_way_intro";
                default: return string.Empty;
            }
        }

        private Vector2 ResolveFingerOrigin(TutorialStep step)
        {
            if (_fingerTarget != null)
            {
                Vector3 worldPos = _fingerTarget.TransformPoint(_fingerTarget.rect.center);
                return transform.InverseTransformPoint(worldPos);
            }
            switch (step)
            {
                case TutorialStep.TouchAndDrag:
                case TutorialStep.ColorMatch:
                    return new Vector2(0f, -100f);
                case TutorialStep.VehicleFlow:
                    return new Vector2(0f, -50f);
                case TutorialStep.LevelComplete:
                case TutorialStep.ReturnToHub:
                    return new Vector2(0f, 100f);
                case TutorialStep.CrashIntro:
                case TutorialStep.ViaductIntro:
                case TutorialStep.UndoIntro:
                    return new Vector2(-120f, -50f);
                default:
                    return Vector2.zero;
            }
        }

        protected override void OnTick(float deltaTime)
        {
            if (_currentStep == TutorialStep.None) return;
            if (_showTime > 0f)
            {
                _showTime -= deltaTime;
                if (_showTime <= 0f) Hide();
            }
            if (_fingerIndicator != null && _fingerIndicator.activeSelf)
            {
                _fingerTime += deltaTime;
                float cycle = Mathf.PingPong(_fingerTime * 1.5f, 1f);
                Vector2 pos = Vector2.Lerp(_fingerOrigin, _fingerTapTarget, cycle);
                if (_fingerTarget != null)
                {
                    _fingerIndicator.transform.position = transform.TransformPoint(pos);
                }
                else
                {
                    _fingerIndicator.transform.localPosition = pos;
                }
                float scale = 1f + Mathf.Sin(_fingerTime * 4f) * 0.08f;
                _fingerIndicator.transform.localScale = Vector3.one * scale;
            }
        }
    }

    public class TutorialMediator : Mediator<TutorialView>
    {
        [Inject] public ITutorialModel TutorialModel { get; set; }
        [Inject] public ITutorialDriver TutorialDriver { get; set; }

        protected override void OnBind()
        {
            TutorialModel.OnStepStarted += OnStepStarted;
            TutorialModel.OnStepCompleted += OnStepCompleted;
        }

        protected override void OnUnbind()
        {
            TutorialModel.OnStepStarted -= OnStepStarted;
            TutorialModel.OnStepCompleted -= OnStepCompleted;
        }

        private void OnStepStarted(TutorialStep step)
        {
            var (msg, autoHide) = GetStepInfo(step);
            View.ShowStep(step, msg, autoHide);
        }

        private void OnStepCompleted(TutorialStep step)
        {
            View.Hide();
            TutorialDriver.CompleteCurrentStep();
        }

        private static (string msg, float autoHide) GetStepInfo(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.TouchAndDrag:  return ("Parmağını sürükle", 4f);
                case TutorialStep.ColorMatch:    return ("Aynı renkleri bağla", 4f);
                case TutorialStep.VehicleFlow:   return ("Araçlar akıyor", 3f);
                case TutorialStep.LevelComplete: return ("Tebrikler!", 0f);
                case TutorialStep.ReturnToHub:   return ("Hub'a Dön", 0f);
                case TutorialStep.TaxCollect:    return ("Vergi topla", 4f);
                case TutorialStep.SecondColor:   return ("Yeni renk geldi", 4f);
                case TutorialStep.CrashIntro:    return ("[!] Kaza! Yolu değiştir", 0f);
                case TutorialStep.ViaductIntro:  return ("[Köprü] Viyadük ile çöz", 0f);
                case TutorialStep.UndoIntro:     return ("Geri al", 4f);
                case TutorialStep.ObstacleIntro: return ("[!] Engel var", 4f);
                case TutorialStep.OneWayIntro:   return ("[>] Tek yön", 4f);
                default: return ("", 3f);
            }
        }
    }
}
