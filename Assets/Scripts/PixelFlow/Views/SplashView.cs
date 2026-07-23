using UnityEngine;
using System.Collections;
using Nexus.Core;
using Nexus.Core.Services;

namespace PixelFlow.Views
{
    [Mediator(typeof(SplashMediator))]
    public class SplashView : View
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _displayDuration = 2f;
        [SerializeField] private float _fadeDuration = 0.5f;

        public event System.Action OnSplashComplete;

        public bool IsComplete { get; private set; }

        [Inject] public ILoggerService LoggerService { get; set; }

        private void Start()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            LoggerService?.Log($"[PixelFlow.SplashView] Starting splash animation: displayDuration={_displayDuration}s, fadeDuration={_fadeDuration}s");
            StartCoroutine(PlaySplash());
        }

        private IEnumerator PlaySplash()
        {
            _canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / _fadeDuration);
                yield return null;
            }

            yield return new WaitForSeconds(_displayDuration);

            elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _fadeDuration);
                yield return null;
            }

            IsComplete = true;
            LoggerService?.Log("[PixelFlow.SplashView] Splash animation complete. Mediator will handle hiding.");
            OnSplashComplete?.Invoke();
            // SetVisible(false) is handled by SplashMediator.HandleSplashComplete
            // to avoid double-call with the mediator's own SetVisible(false).
        }

        public void SetVisible(bool visible)
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
                _canvasGroup.interactable = visible;
            }
            LoggerService?.Log($"[PixelFlow.SplashView] SetVisible({visible}): " +
                $"cgAlpha={(_canvasGroup != null ? _canvasGroup.alpha.ToString() : "null")}, " +
                $"blocksRaycasts={(_canvasGroup != null ? _canvasGroup.blocksRaycasts.ToString() : "null")}");
        }
    }
}
