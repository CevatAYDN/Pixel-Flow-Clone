using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Signals;
using UnityEngine;

namespace PixelFlow.Services
{
    public interface IObstacleService
    {
        bool IsOneWay(Vector2Int cell, Vector2Int moveDir);
        bool IsFerryBlocked(Vector2Int cell);
        bool IsNarrowPass(Vector2Int cell);
        bool CanVehicleEnterNarrowPass(Vector2Int cell, ColorType color);
        void OnVehicleEnteredNarrowPass(Vector2Int cell, ColorType color);
        void OnVehicleLeftNarrowPass(Vector2Int cell, ColorType color);
        Vector2Int GetOneWayDirection(Vector2Int cell);
        void Tick(float deltaTime);
        void InitializeFromLevel(LevelData level);
    }

    /// <summary>
    /// GDD §9.4: 6 engel türünün runtime davranışı.
    ///   - Construction/Lake/Park: CellState.Obstacle (statik, çizim engellenmiş).
    ///   - OneWay: tek yön ok işareti, ters yönde çizim yasak.
    ///   - Ferry: 10 saniyede yön değiştiren hareketli engel.
    ///   - NarrowPass: tek araç genişliği, sıralı geçiş.
    /// </summary>
    public class ObstacleService : IObstacleService, INexusService
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject, OptionalInject] public Data.GameConfig Config { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        private readonly Dictionary<Vector2Int, ObstacleType> _obstacles = new Dictionary<Vector2Int, ObstacleType>();
        private readonly Dictionary<Vector2Int, Vector2Int> _oneWayDirs = new Dictionary<Vector2Int, Vector2Int>();
        private readonly Dictionary<Vector2Int, bool> _ferryBlocked = new Dictionary<Vector2Int, bool>();
        private readonly Dictionary<Vector2Int, ColorType> _narrowPassOccupants = new Dictionary<Vector2Int, ColorType>();
        private float _ferryTimer;
        private float ConfigFerryPeriod => Config != null ? Config.FerryPeriod : 10f;

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose()
        {
            LoggerService?.Log("[PixelFlow.ObstacleService] Disposing and clearing static caches.");
            _obstacles.Clear();
            _oneWayDirs.Clear();
            _ferryBlocked.Clear();
            _narrowPassOccupants.Clear();
        }

        public void InitializeFromLevel(LevelData level)
        {
            _obstacles.Clear();
            _oneWayDirs.Clear();
            _ferryBlocked.Clear();
            _narrowPassOccupants.Clear();
            _ferryTimer = 0f;
            if (level == null)
            {
                LoggerService?.LogWarning("[PixelFlow.ObstacleService] InitializeFromLevel: level is null.");
                return;
            }

            // 1. Geleneksel engelleri yükle (GDD §9.4)
            if (level.obstacles != null)
            {
                foreach (var obs in level.obstacles)
                {
                    _obstacles[obs.position] = obs.type;
                    switch (obs.type)
                    {
                        case ObstacleType.OneWay:
                            // Geriye uyumluluk için
                            _oneWayDirs[obs.position] = Vector2Int.right;
                            break;
                        case ObstacleType.Ferry:
                            _ferryBlocked[obs.position] = false;
                            break;
                        case ObstacleType.NarrowPass:
                            _narrowPassOccupants[obs.position] = ColorType.None;
                            break;
                    }
                }
            }

            // 2. Yeni GDD OneWay hücrelerini yükle (GDD §2.7 — viyadüğe alternatif)
            if (level.oneWayCells != null)
            {
                foreach (var owc in level.oneWayCells)
                {
                    _obstacles[owc.position] = ObstacleType.OneWay;
                    _oneWayDirs[owc.position] = owc.allowedDirection != Vector2Int.zero ? owc.allowedDirection : Vector2Int.right;
                }
            }
            LoggerService?.Log($"[PixelFlow.ObstacleService] Initialized from Level {level.levelIndex + 1}. Mapped: obstacles={_obstacles.Count}, oneWays={_oneWayDirs.Count}, ferries={_ferryBlocked.Count}, narrowPasses={_narrowPassOccupants.Count}");
        }

        public void Tick(float deltaTime)
        {
            if (_ferryBlocked.Count == 0) return;
            _ferryTimer += deltaTime;
            if (_ferryTimer < ConfigFerryPeriod) return;
            _ferryTimer = 0f;
            // Tüm ferilerin blok durumunu ters çevir.
            var keys = new List<Vector2Int>(_ferryBlocked.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                _ferryBlocked[keys[i]] = !_ferryBlocked[keys[i]];
                LoggerService?.Log($"[PixelFlow.ObstacleService] Ferry at {keys[i]} block status toggled: {_ferryBlocked[keys[i]]}");
            }

            SignalBus?.Fire(new GridUpdatedSignal());
        }

        public bool IsOneWay(Vector2Int cell, Vector2Int moveDir)
        {
            if (!_oneWayDirs.TryGetValue(cell, out var allowedDir)) return false;
            if (allowedDir == Vector2Int.zero) return false;
            bool violated = moveDir != allowedDir && moveDir != Vector2Int.zero;
            if (violated)
            {
                LoggerService?.LogWarning($"[PixelFlow.ObstacleService] OneWay violation check at {cell}. Allowed: {allowedDir}, Trying: {moveDir}");
            }
            return violated;
        }

        public Vector2Int GetOneWayDirection(Vector2Int cell)
        {
            return _oneWayDirs.TryGetValue(cell, out var d) ? d : Vector2Int.right;
        }

        public bool IsFerryBlocked(Vector2Int cell)
        {
            return _ferryBlocked.TryGetValue(cell, out var blocked) && blocked;
        }

        public bool IsNarrowPass(Vector2Int cell)
        {
            return _obstacles.TryGetValue(cell, out var t) && t == ObstacleType.NarrowPass;
        }

        public bool CanVehicleEnterNarrowPass(Vector2Int cell, ColorType color)
        {
            if (!IsNarrowPass(cell)) return true;
            if (!_narrowPassOccupants.TryGetValue(cell, out var occupant)) return true;
            bool allowed = occupant == ColorType.None || occupant == color;
            if (!allowed)
            {
                LoggerService?.LogWarning($"[PixelFlow.ObstacleService] NarrowPass entry blocked at {cell} for color {color}. Occupant is {occupant}.");
            }
            return allowed;
        }

        public void OnVehicleEnteredNarrowPass(Vector2Int cell, ColorType color)
        {
            if (!IsNarrowPass(cell)) return;
            _narrowPassOccupants[cell] = color;
            LoggerService?.Log($"[PixelFlow.ObstacleService] NarrowPass at {cell} entered/locked by vehicle color {color}.");
        }

        public void OnVehicleLeftNarrowPass(Vector2Int cell, ColorType color)
        {
            if (!IsNarrowPass(cell)) return;
            if (_narrowPassOccupants.TryGetValue(cell, out var occ) && occ == color)
            {
                _narrowPassOccupants[cell] = ColorType.None;
                LoggerService?.Log($"[PixelFlow.ObstacleService] NarrowPass at {cell} released by vehicle color {color}.");
            }
        }
    }
}
