using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using UnityEngine;

namespace PixelFlow.Services
{
    public interface IObstacleService
    {
        bool IsOneWay(Vector2Int cell, ColorType color, Vector2Int moveDir);
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

        private readonly Dictionary<Vector2Int, ObstacleType> _obstacles = new Dictionary<Vector2Int, ObstacleType>();
        private readonly Dictionary<Vector2Int, Vector2Int> _oneWayDirs = new Dictionary<Vector2Int, Vector2Int>();
        private readonly Dictionary<Vector2Int, bool> _ferryBlocked = new Dictionary<Vector2Int, bool>();
        private readonly Dictionary<Vector2Int, ColorType> _narrowPassOccupants = new Dictionary<Vector2Int, ColorType>();
        private float _ferryTimer;
        private const float FerryPeriod = 10f;

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose()
        {
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
            if (level?.obstacles == null) return;

            foreach (var obs in level.obstacles)
            {
                _obstacles[obs.position] = obs.type;
                switch (obs.type)
                {
                    case ObstacleType.OneWay:
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

        public void Tick(float deltaTime)
        {
            if (_ferryBlocked.Count == 0) return;
            _ferryTimer += deltaTime;
            if (_ferryTimer < FerryPeriod) return;
            _ferryTimer = 0f;
            // Tüm ferilerin blok durumunu ters çevir.
            var keys = new List<Vector2Int>(_ferryBlocked.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                _ferryBlocked[keys[i]] = !_ferryBlocked[keys[i]];
                if (GridModel != null && _ferryBlocked[keys[i]])
                {
                    // Bloklandığında hücreyi Obstacle yap, yoksa Empty.
                    if (keys[i].x >= 0 && keys[i].x < GridModel.Width && keys[i].y >= 0 && keys[i].y < GridModel.Height)
                    {
                        GridModel.Grid[keys[i].x, keys[i].y].State = CellState.Obstacle;
                    }
                }
                else
                {
                    if (GridModel != null && keys[i].x >= 0 && keys[i].x < GridModel.Width && keys[i].y >= 0 && keys[i].y < GridModel.Height)
                    {
                        var cell = GridModel.Grid[keys[i].x, keys[i].y];
                        if (cell.State == CellState.Obstacle)
                        {
                            cell.State = CellState.Empty;
                            cell.Color = ColorType.None;
                            cell.PathColors.Clear();
                        }
                    }
                }
            }
        }

        public bool IsOneWay(Vector2Int cell, ColorType color, Vector2Int moveDir)
        {
            if (!_oneWayDirs.TryGetValue(cell, out var allowedDir)) return false;
            if (allowedDir == Vector2Int.zero) return false;
            return moveDir != allowedDir && moveDir != Vector2Int.zero;
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
            return occupant == ColorType.None || occupant == color;
        }

        public void OnVehicleEnteredNarrowPass(Vector2Int cell, ColorType color)
        {
            if (!IsNarrowPass(cell)) return;
            _narrowPassOccupants[cell] = color;
        }

        public void OnVehicleLeftNarrowPass(Vector2Int cell, ColorType color)
        {
            if (!IsNarrowPass(cell)) return;
            if (_narrowPassOccupants.TryGetValue(cell, out var occ) && occ == color)
            {
                _narrowPassOccupants[cell] = ColorType.None;
            }
        }
    }
}
