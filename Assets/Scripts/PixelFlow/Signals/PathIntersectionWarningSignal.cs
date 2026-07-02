using UnityEngine;

namespace PixelFlow.Signals
{
    /// <summary>
    /// İki veya daha fazla yol kesiştiğinde ve henüz viyadük yerleştirilmediğinde
    /// tetiklenen yumuşak uyarı (soft warning) sinyali.
    /// </summary>
    public struct PathIntersectionWarningSignal
    {
        public Vector2Int Position;
    }
}
