using UnityEngine;

namespace PixelFlow.Services
{
    /// <summary>
    /// Camera.main yerine DI ile enjekte edilebilir kamera sağlayıcı.
    /// Data-driven: kamera referansı kod yerine DI tarafından yönetilir.
    /// </summary>
    public interface ICameraProvider
    {
        Camera MainCamera { get; }
    }
}
