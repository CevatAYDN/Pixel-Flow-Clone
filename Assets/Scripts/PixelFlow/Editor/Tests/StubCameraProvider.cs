using UnityEngine;
using PixelFlow.Services;

namespace PixelFlow.Editor.Tests
{
    /// <summary>
    /// Minimal no-op stub for <see cref="ICameraProvider"/>.
    /// Satisfies DI injection in VehicleSimulator without requiring
    /// a live Camera in EditMode tests. Returns a detached Camera object.
    /// </summary>
    public sealed class StubCameraProvider : ICameraProvider
    {
        private Camera _stubCam;
        public Camera MainCamera
        {
            get
            {
                if (_stubCam == null)
                {
                    var go = new GameObject("StubCamera");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    _stubCam = go.AddComponent<Camera>();
                }
                return _stubCam;
            }
        }
    }
}
