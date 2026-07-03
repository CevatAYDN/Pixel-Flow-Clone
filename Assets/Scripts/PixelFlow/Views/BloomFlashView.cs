using UnityEngine;
using UnityEngine.UI;
using Nexus.Core;
using PixelFlow.Signals;

namespace PixelFlow.Views
{
    [Mediator(typeof(BloomFlashMediator))]
    public class BloomFlashView : View
    {
        [SerializeField] private Image _flashImage;
        [SerializeField] private float _duration = 0.6f;

        public void Flash()
        {
            if (_flashImage == null) return;
            StartCoroutine(DoFlash());
        }

        private System.Collections.IEnumerator DoFlash()
        {
            float t = 0f;
            while (t < _duration)
            {
                t += Time.deltaTime;
                _flashImage.color = new Color(1f, 0.95f, 0.6f, Mathf.Lerp(0.7f, 0f, t / _duration));
                yield return null;
            }
        }
    }

    public class BloomFlashMediator : Mediator<BloomFlashView>
    {
        protected override void OnBind()
        {
            Subscribe<LevelCompletedSignal>(_ => View.Flash());
        }
    }
}
