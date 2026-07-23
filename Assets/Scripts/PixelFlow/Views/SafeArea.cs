using UnityEngine;

namespace PixelFlow.Views
{
    /// <summary>
    /// GDD §12 & Core Rule: Portrait 9:16 mobil cihazlar için Notch/Dynamic Island Safe Area desteği.
    /// RectTransform alanını Screen.safeArea değerine göre dinamik ölçekler.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea = new Rect(0, 0, 0, 0);

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void OnEnable()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            Rect safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }
    }
}
