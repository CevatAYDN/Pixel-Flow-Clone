using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Data;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;

namespace PixelFlow.Models
{
    public enum CellState { Empty, Node, Path, Bridge, Obstacle }

    public class CellData
    {
        public CellState State;
        public ColorType Color;
        /// <summary>
        /// Bitmask of path colors traversing this cell.
        /// Bit 0=Blue, 1=Red, 2=Yellow, 3=Green, 4=Purple.
        /// Replaces HashSet{ColorType} PathColors to eliminate per-cell GC allocations.
        /// </summary>
        public byte PathColorsMask;

        public bool HasPathColor(ColorType c) => (PathColorsMask & (1 << (int)c)) != 0;

        public void AddPathColor(ColorType c) => PathColorsMask |= (byte)(1 << (int)c);

        public void RemovePathColor(ColorType c) => PathColorsMask &= (byte)~(1 << (int)c);

        public int PathColorCount
        {
            get
            {
                int count = 0;
                byte b = PathColorsMask;
                while (b > 0) { count += b & 1; b >>= 1; }
                return count;
            }
        }

        public void ClearPathColors() => PathColorsMask = 0;

        /// <summary>
        /// Returns first set color (lowest bit), or None if empty.
        /// </summary>
        public ColorType FirstPathColor
        {
            get
            {
                if (PathColorsMask == 0) return ColorType.None;
                int i = 0;
                byte b = PathColorsMask;
                while ((b & 1) == 0) { b >>= 1; i++; }
                return (ColorType)i;
            }
        }

        /// <summary>
        /// Enumerate all path colors set in the bitmask.
        /// </summary>
        public IEnumerable<ColorType> GetPathColors()
        {
            for (int i = 0; i < 5; i++)
            {
                if ((PathColorsMask & (1 << i)) != 0)
                    yield return (ColorType)i;
            }
        }

        public bool HasViaduct;
        public ColorType UnderColor = ColorType.None;
        public ColorType OverColor = ColorType.None;
        public ObstacleType ObstacleType = ObstacleType.None;
    }

    public interface IGridModel
    {
        int Width { get; }
        int Height { get; }
        CellData[,] Grid { get; }
        Dictionary<ColorType, List<Vector2Int>> Paths { get; }
        HashSet<ColorType> LockedColors { get; }
        ObservableProperty<ColorType> ActiveColor { get; }
        ObservableProperty<Vector2Int> LastPosition { get; }
        ObservableProperty<Vector2Int> LastCrashPosition { get; }
        ObservableProperty<ColorType> CrashColorA { get; }
        ObservableProperty<ColorType> CrashColorB { get; }

        void Initialize(int width, int height);
        CellData GetCell(int x, int y);
        CellData GetCell(Vector2Int pos);
        void PlaceNodes(IEnumerable<GridNode> nodes);
        void PlaceObstacles(IEnumerable<ObstacleData> obstacles);
        void PlaceOneWays(IEnumerable<OneWayCell> oneWays);
        bool AllColorPairsConnected();
        bool IsGridFullyCovered();
    }

    /// <summary>
    /// Izgara state'i. Bildirim için ObservableProperty kullanılıyor,
    /// aynı zamanda SignalBus.Fire(GridUpdatedSignal) de destekleniyor.
    /// </summary>
    public class GridModel : IGridModel, IReactiveModel
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public CellData[,] Grid { get; private set; } = null!;
        public Dictionary<ColorType, List<Vector2Int>> Paths { get; private set; } = null!;
        public HashSet<ColorType> LockedColors { get; private set; } = null!;
        public ObservableProperty<ColorType> ActiveColor { get; } = new(ColorType.None);
        public ObservableProperty<Vector2Int> LastPosition { get; } = new(new Vector2Int(-1, -1));
        public ObservableProperty<Vector2Int> LastCrashPosition { get; } = new(new Vector2Int(-1, -1));
        public ObservableProperty<ColorType> CrashColorA { get; } = new(ColorType.None);
        public ObservableProperty<ColorType> CrashColorB { get; } = new(ColorType.None);

        // Pool for CellData reuse across grid reinitialization
        private CellData[] _cellDataPool;

        public void Initialize(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new System.ArgumentException($"Grid boyutu pozitif olmalı: {width}x{height}");

            Width = width;
            Height = height;
            Grid = new CellData[width, height];

            // Pool CellData objects to avoid repeated allocations on undo/redo/level load
            int totalCells = width * height;
            if (_cellDataPool == null || _cellDataPool.Length < totalCells)
            {
                _cellDataPool = new CellData[totalCells];
                for (int i = 0; i < totalCells; i++)
                    _cellDataPool[i] = new CellData();
            }

            int idx = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var cell = _cellDataPool[idx++];
                    cell.State = CellState.Empty;
                    cell.Color = ColorType.None;
                    cell.PathColorsMask = 0;
                    cell.HasViaduct = false;
                    cell.UnderColor = ColorType.None;
                    cell.OverColor = ColorType.None;
                    cell.ObstacleType = ObstacleType.None;
                    Grid[x, y] = cell;
                }
            }
            Paths = new Dictionary<ColorType, List<Vector2Int>>();
            LockedColors = new HashSet<ColorType>();
            ActiveColor.Value = ColorType.None;
            LastPosition.Value = new Vector2Int(-1, -1);
            LastCrashPosition.Value = new Vector2Int(-1, -1);
        }

        public ValueTask OnBind(CancellationToken ct) => default;

        public CellData GetCell(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                throw new System.ArgumentOutOfRangeException($"Cell ({x},{y}) out of bounds ({Width}x{Height})");
            return Grid[x, y];
        }

        public CellData GetCell(Vector2Int pos) => GetCell(pos.x, pos.y);

        public void PlaceNodes(IEnumerable<GridNode> nodes)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                if (node.position.x < 0 || node.position.x >= Width ||
                    node.position.y < 0 || node.position.y >= Height)
                    continue;
                var cell = Grid[node.position.x, node.position.y];
                cell.State = CellState.Node;
                cell.Color = node.color;
                cell.AddPathColor(node.color);
            }
        }

        public void PlaceObstacles(IEnumerable<ObstacleData> obstacles)
        {
            if (obstacles == null) return;
            foreach (var obs in obstacles)
            {
                if (obs.position.x < 0 || obs.position.x >= Width ||
                    obs.position.y < 0 || obs.position.y >= Height)
                    continue;
                var cell = Grid[obs.position.x, obs.position.y];
                cell.State = CellState.Obstacle;
                cell.ObstacleType = obs.type;
            }
        }

        public void PlaceOneWays(IEnumerable<OneWayCell> oneWays)
        {
            if (oneWays == null) return;
            foreach (var ow in oneWays)
            {
                if (ow.position.x < 0 || ow.position.x >= Width ||
                    ow.position.y < 0 || ow.position.y >= Height)
                    continue;
                var cell = Grid[ow.position.x, ow.position.y];
                cell.ObstacleType = ObstacleType.OneWay;
            }
        }

        public bool AllColorPairsConnected()
        {
            // Her renk için Path kaydı varsa ve path count >= 2 ise bağlı kabul et
            foreach (var kvp in Paths)
            {
                if (kvp.Value == null || kvp.Value.Count < 2)
                    return false;
            }
            return Paths.Count > 0;
        }

        public bool IsGridFullyCovered()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = Grid[x, y];
                    if (cell.State == CellState.Empty && cell.PathColorCount == 0)
                        return false;
                }
            }
            return true;
        }
    }
}