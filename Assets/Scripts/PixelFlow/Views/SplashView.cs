using UnityEngine;
using System.Collections;
using Nexus.Core;

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

        private void Start()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

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
            OnSplashComplete?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
