using UnityEngine;

namespace PixelFlow.Services
{
    /// <summary>
    /// MonoBehaviour olmadan Unity Update döngüsüne bağlanmak için
    /// hafif yardımcı bileşen. Multiple services tarafından paylaşılabilir.
    /// </summary>
    public class SimulationUpdater : MonoBehaviour
    {
        public System.Action OnUpdate;
        private void Update() => OnUpdate?.Invoke();
    }
}
