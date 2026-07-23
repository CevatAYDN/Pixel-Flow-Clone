using UnityEngine;
using PixelFlow.Data;
using PixelFlow.Models;

namespace PixelFlow.Services
{
    /// <summary>
    /// Color Jam 3D - Bouncy Collision & Rubber Reaction Handler.
    /// Araçlar kesiştiğinde komik bir kauçuk zıplaması (squash & stretch) yaptırır
    /// ve oyunu durdurmadan akıcı bir şekilde geri itme uygular.
    /// </summary>
    public static class BouncyCollisionHandler
    {
        /// <summary>
        /// Kaza anında araç görseline elastik zıplama efekti uygular.
        /// </summary>
        public static void ApplyBouncyBounce(GameObject vehicleVisual, Vector3 bounceDirection)
        {
            if (vehicleVisual == null) return;

            // Simple bouncy animation: quick squash and stretch
            var bouncyComp = vehicleVisual.GetComponent<BouncyVisualEffect>();
            if (bouncyComp == null)
            {
                bouncyComp = vehicleVisual.AddComponent<BouncyVisualEffect>();
            }

            bouncyComp.TriggerBounce(bounceDirection);
        }
    }

    public class BouncyVisualEffect : MonoBehaviour
    {
        private float _bounceTimer = 0f;
        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private bool _isBouncing = false;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        public void TriggerBounce(Vector3 direction)
        {
            _originalScale = Vector3.one;
            _targetScale = new Vector3(1.3f, 0.7f, 1.3f); // Squash
            _bounceTimer = 0f;
            _isBouncing = true;
        }

        private void Update()
        {
            if (!_isBouncing) return;

            _bounceTimer += Time.deltaTime * 10f; // Fast bounce
            float t = Mathf.Sin(_bounceTimer * Mathf.PI);

            if (_bounceTimer >= 1f)
            {
                transform.localScale = _originalScale;
                _isBouncing = false;
            }
            else
            {
                transform.localScale = Vector3.Lerp(_originalScale, _targetScale, t);
            }
        }
    }
}
