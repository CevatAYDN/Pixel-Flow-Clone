using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using PixelFlow.Models;
using Nexus.Core;

namespace PixelFlow.Services
{
    public interface ITaxCollectionService
    {
        void CollectNow();
    }

    /// <summary>
    /// Vergi birikimini yöneten servis. CityEconomyModel'den bağımsız
    /// olarak Unity Update döngüsünde çalışır ve vergi birikimini hesaplar.
    /// CityEconomyModel'in Unity GameObject yaratmasını engeller (MVCS ihlali).
    /// </summary>
    public class TaxCollectionService : ITaxCollectionService, INexusService
    {
        [Inject] public ICityEconomyModel CityEconomyModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }

        private SimulationUpdater _updater;
        private float _cachedTaxRate;
        private int _cachedMaxStorage;
        private float _flushTimer;
        private const float FlushInterval = 5f;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (Application.isPlaying)
            {
                GameObject updaterObj = new GameObject("[TaxAutoCollector]");
                updaterObj.hideFlags = HideFlags.DontSave;
                _updater = updaterObj.AddComponent<SimulationUpdater>();
                _updater.OnUpdate = Update;
            }

            return default;
        }

        public void OnDispose()
        {
            if (CityEconomyModel != null) CityEconomyModel.FlushAccumulated();
            if (_updater != null)
            {
                UnityEngine.Object.Destroy(_updater.gameObject);
                _updater = null;
            }
        }

        public void CollectNow()
        {
            CityEconomyModel.CollectTaxes();
            CityEconomyModel.FlushAccumulated();
        }

        private void Update()
        {
            if (GameStateModel == null || CityEconomyModel == null)
                return;

            float dt = Time.deltaTime;
            if (CityEconomyModel is CityEconomyModel modelImpl)
            {
                modelImpl.TickOverclock(dt);
            }

            _cachedTaxRate = CityEconomyModel.TaxRatePerSecond;
            _cachedMaxStorage = CityEconomyModel.MaxStorage;

            float accumulated = CityEconomyModel.GetAccumulatedTaxes();
            accumulated = Mathf.Min(accumulated + _cachedTaxRate * dt, _cachedMaxStorage);
            // Memory-only update (no PlayerPrefs write)
            CityEconomyModel.SetAccumulatedTaxes(accumulated, saveImmediately: false);

            // Throttled flush to PlayerPrefs every 5s
            _flushTimer += dt;
            if (_flushTimer >= FlushInterval)
            {
                _flushTimer = 0f;
                CityEconomyModel.FlushAccumulated();
            }
        }
    }

    /// <summary>
    /// MonoBehaviour olmadan Unity Update döngüsüne bağlanmak için
    /// hafif yardımcı bileşen. VehicleSimulator ile paylaşılır.
    /// </summary>
    public class SimulationUpdater : MonoBehaviour
    {
        public Action OnUpdate;
        private void Update() => OnUpdate?.Invoke();
    }
}
