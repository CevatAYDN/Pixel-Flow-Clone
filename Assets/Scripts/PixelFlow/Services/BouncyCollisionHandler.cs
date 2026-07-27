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
        /// game_plan.md §15.4.4: fizik parametreleri LevelData.bouncyPhysicsConfig'ten (data-driven) gelir.
        /// LevelData'da atanmamışsa global BouncyPhysicsConfigAsset kullanılır.
        /// </summary>
        public static void ApplyBouncyBounce(GameObject vehicleVisual, Vector3 bounceDirection, BouncyPhysicsConfigAsset physics)
        {
            if (vehicleVisual == null) return;

            // Fallback to global defaults if null
            float bounceForce = physics?.BounceForce ?? 4.5f;
            float bounceDamping = physics?.BounceDamping ?? 0.75f;
            float squishFactor = physics?.SquishFactor ?? 0.35f;

            // Simple bouncy animation: quick squash and stretch
            var bouncyComp = vehicleVisual.GetComponent<BouncyVisualEffect>();
            if (bouncyComp == null)
            {
                bouncyComp = vehicleVisual.AddComponent<BouncyVisualEffect>();
            }

            bouncyComp.TriggerBounce(bounceDirection, bounceForce, bounceDamping, squishFactor);
        }
    }

    public class BouncyVisualEffect : MonoBehaviour
    {
        private float _bounceTimer = 0f;
        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private float _bounceSpeed = 1f;
        private float _damping = 1f;
        private bool _isBouncing = false;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        public void TriggerBounce(Vector3 direction, float bounceForce, float bounceDamping, float squishFactor)
        {
            _originalScale = Vector3.one;
            // SquishFactor: yatay eksende ger, dikey eksende ez (squash & stretch)
            float squish = squishFactor;
            _targetScale = new Vector3(1f + squish, 1f - squish, 1f + squish);
            _bounceSpeed = bounceForce;   // animasyon hızı zıplama kuvvetinden
            _damping = bounceDamping;     // sönümleme genliği azaltır
            _bounceTimer = 0f;
            _isBouncing = true;
        }

        private void Update()
        {
            if (!_isBouncing) return;

            _bounceTimer += Time.deltaTime * _bounceSpeed;
            float t = Mathf.Sin(_bounceTimer * Mathf.PI) * _damping;

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
