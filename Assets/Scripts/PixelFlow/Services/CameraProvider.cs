using UnityEngine;
using Nexus.Core;
using System.Threading;
using System.Threading.Tasks;

namespace PixelFlow.Services
{
    /// <summary>
    /// ICameraProvider implementasyonu. İlk erişimde Camera.main'i cache'ler.
    /// GameContextLifecycle'da BindService ile kaydedilir, tüm View/Service'ler
    /// [Inject] ICameraProvider ile alır.
    /// </summary>
    public class CameraProvider : ICameraProvider, INexusService
    {
        private Camera _cached;

        public Camera MainCamera
        {
            get
            {
                if (_cached == null)
                    _cached = Camera.main;
                return _cached;
            }
        }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { _cached = null; }
    }
}
