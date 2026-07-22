using Nexus.Core;
using Nexus.Core.Services;

namespace PixelFlow.Core
{
    /// <summary>
    /// Tüm View'lar için merkezi update base class'ı.
    /// Nexus ITickService'e otomatik kaydolur — her View kendi Update()'ini yazmak yerine
    /// OnTick(float deltaTime) override eder.
    /// 
    /// Avantajları:
    /// - 8 ayrı MonoBehaviour Update() → 1 TickService.OnTick çağrısı
    /// - Pause desteği (TickService.IsPaused ile otomatik durur)
    /// - SimulationUpdater GameObject'lerine gerek kalmaz
    /// - Tüm frame mantığı tek noktadan yönetilir
    /// </summary>
    public abstract class TickableView : View, ITickable
    {
        [Inject] public ITickService TickService { get; set; }

        protected override void OnBind(IContext context)
        {
            base.OnBind(context);
            TickService?.RegisterTickable(this);
        }

        protected override void OnUnbind()
        {
            TickService?.UnregisterTickable(this);
            base.OnUnbind();
        }

        void ITickable.Tick(float deltaTime) => OnTick(deltaTime);

        /// <summary>
        /// Update yerine bu metodu override et.
        /// deltaTime = Time.deltaTime (TickService tarafından sağlanır).
        /// </summary>
        protected virtual void OnTick(float deltaTime) { }
    }
}
