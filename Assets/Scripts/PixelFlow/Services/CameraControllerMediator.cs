using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Services
{
    /// <summary>
    /// CameraController için Mediator. State değişikliklerini dinler ve
    /// kamera transition'larını tetikler. Sensör mantığı View tarafında
    /// (sahne referansı), DI enjeksiyonu burada yapılır.
    /// </summary>
    public class CameraControllerMediator : Mediator<CameraController>
    {
        [Inject] public IGameStateModel GameStateModel { get; set; }

        protected override void OnBind()
        {
            View.GameStateModel = GameStateModel;
            View.MediatorOnBind();
        }

        protected override void OnUnbind()
        {
            View.MediatorOnUnbind();
        }
    }
}
