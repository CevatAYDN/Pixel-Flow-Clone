using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace PixelFlow.Views
{
    /// <summary>
    /// Tactile Button Juice (Game Feel): Adds micro-scale animation (punch press)
    /// and subtle haptic feedback to any UI button on click.
    /// </summary>
    public class ButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float _pressedScale = 0.92f;
        [SerializeField] private float _animationSpeed = 16f;

        private Vector3 _originalScale;
        private Coroutine _scaleCoroutine;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        private void OnEnable()
        {
            transform.localScale = _originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            AnimateScale(_originalScale * _pressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateScale(_originalScale);
        }

        private void AnimateScale(Vector3 targetScale)
        {
            if (_scaleCoroutine != null)
                StopCoroutine(_scaleCoroutine);
            if (gameObject.activeInHierarchy)
                _scaleCoroutine = StartCoroutine(ScaleRoutine(targetScale));
            else
                transform.localScale = targetScale;
        }

        private IEnumerator ScaleRoutine(Vector3 targetScale)
        {
            while (Vector3.Distance(transform.localScale, targetScale) > 0.005f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * _animationSpeed);
                yield return null;
            }
            transform.localScale = targetScale;
            _scaleCoroutine = null;
        }

        public static void AttachTo(UnityEngine.UI.Button button)
        {
            if (button != null && button.gameObject.GetComponent<ButtonJuice>() == null)
            {
                button.gameObject.AddComponent<ButtonJuice>();
            }
        }
    }
}
