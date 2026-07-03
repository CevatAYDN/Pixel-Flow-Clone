using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using PixelFlow.Models;
using UnityEngine;

namespace PixelFlow.Services
{
    public interface IOverclockService
    {
        bool IsActive { get; }
        float RemainingSeconds { get; }
        void Activate();
        void Tick(float deltaTime);
    }

    /// <summary>
    /// GDD §6.1: Overclock 4 saat boyunca vergi üretimini ×2 yapar.
    /// Ödüllü reklam izlendiğinde RewardedAdCommand tarafından tetiklenir.
    /// </summary>
    public class OverclockService : IOverclockService, INexusService
    {
        [Inject] public ICityEconomyModel CityEconomyModel { get; set; }

        private const float DurationSeconds = 4f * 60f * 60f; // 4 saat
        private float _remaining;

        public bool IsActive => _remaining > 0f;
        public float RemainingSeconds => _remaining;

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }

        public void Activate()
        {
            _remaining = DurationSeconds;
        }

        public void Tick(float deltaTime)
        {
            if (_remaining > 0f)
            {
                _remaining -= deltaTime;
                if (_remaining < 0f) _remaining = 0f;
            }
        }

        /// <summary>
        /// CityEconomyModel.TaxRatePerSecond çağrılırken ×2 çarpanı olarak
        /// kullanılır. Aktif değilse 1.0 döner.
        /// </summary>
        public float Multiplier => IsActive ? 2f : 1f;
    }
}
