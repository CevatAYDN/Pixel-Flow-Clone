using UnityEngine;

namespace PixelFlow.Data
{
    /// <summary>
    /// Bouncy Physics konfigürasyonu - game_plan.md §2.1.A ve §15.4.4'e göre.
    /// Seviye bazlı zıplama parametreleri (BounceForce, BounceDamping, SquishFactor).
    /// LevelData.bouncyPhysics artık bu asset'i referans alacak.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BouncyPhysicsConfig",
        menuName = "PixelFlow/Bouncy Physics Config")]
    public class BouncyPhysicsConfigAsset : ScriptableObject
    {
        [Header("=== Global Defaults (LevelData override edebilir) ===")]
        [Tooltip("Zıplama kuvveti (g-force/impulse). Varsayılan: 4.5")]
        [Range(1f, 20f)]
        public float BounceForce = 4.5f;

        [Tooltip("Zayıflama / sönümleme katsayısı. Varsayılan: 0.75")]
        [Range(0.1f, 1.0f)]
        public float BounceDamping = 0.75f;

        [Tooltip("Esneklik / ezilme-büzülme şiddeti. Varsayılan: 0.35")]
        [Range(0.05f, 1.0f)]
        public float SquishFactor = 0.35f;
    }
}