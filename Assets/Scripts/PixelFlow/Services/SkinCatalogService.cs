using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;

namespace PixelFlow.Services
{
    /// <summary>
    /// game_plan.md §2.2 + §15.9: Skin katalog servisi, Resources.Load runtime ihlalini
    /// ortadan kaldırır. GameContextLifecycle.OnConfigure'da skin listesi toplanıp
    /// BindInstance ile inject edilir; tüketiciler (SkinUnlockCommand, StopSkinUnlockCommand,
    /// DailyLoginStreakService, GarageView) SkinId → config lookup için bu servisi çağırır.
    /// </summary>
    public interface ISkinCatalogService
    {
        VehicleSkinConfig GetVehicleSkinById(string skinId);
        StopSkinConfig GetStopSkinById(string skinId);
        IReadOnlyList<VehicleSkinConfig> AllVehicleSkins { get; }
        IReadOnlyList<StopSkinConfig> AllStopSkins { get; }
    }

    public class SkinCatalogService : ISkinCatalogService, INexusService
    {
        [Inject, OptionalInject] public ILoggerService LoggerService { get; set; }

        private readonly Dictionary<string, VehicleSkinConfig> _vehicleSkins
            = new Dictionary<string, VehicleSkinConfig>();
        private readonly Dictionary<string, StopSkinConfig> _stopSkins
            = new Dictionary<string, StopSkinConfig>();

        public IReadOnlyList<VehicleSkinConfig> AllVehicleSkins => _vehicleSkins.Values.ToList();
        public IReadOnlyList<StopSkinConfig> AllStopSkins => _stopSkins.Values.ToList();

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }

        public void RegisterVehicleSkins(IEnumerable<VehicleSkinConfig> skins)
        {
            if (skins == null) return;
            int count = 0;
            foreach (var s in skins)
            {
                if (s == null || string.IsNullOrEmpty(s.SkinId)) continue;
                _vehicleSkins[s.SkinId] = s;
                count++;
            }
            LoggerService?.Log($"[SkinCatalogService] Registered {count} vehicle skins.");
        }

        public void RegisterStopSkins(IEnumerable<StopSkinConfig> skins)
        {
            if (skins == null) return;
            int count = 0;
            foreach (var s in skins)
            {
                if (s == null || string.IsNullOrEmpty(s.SkinId)) continue;
                _stopSkins[s.SkinId] = s;
                count++;
            }
            LoggerService?.Log($"[SkinCatalogService] Registered {count} stop skins.");
        }

        public VehicleSkinConfig GetVehicleSkinById(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return null;
            _vehicleSkins.TryGetValue(skinId, out var skin);
            return skin;
        }

        public StopSkinConfig GetStopSkinById(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return null;
            _stopSkins.TryGetValue(skinId, out var skin);
            return skin;
        }
    }
}
