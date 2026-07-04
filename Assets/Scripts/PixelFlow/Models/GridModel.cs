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
    }
}