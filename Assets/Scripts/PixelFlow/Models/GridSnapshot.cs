using System.Collections.Generic;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Models
{
    /// <summary>
    /// GridModel'in tam state'ini immutable olarak yakalar.
    /// Array pooling kullanır (Capacity ve altı aynı boyuttaki snapshot'larda 0 alloc).
    /// </summary>
    public readonly struct GridSnapshot
    {
        public int Width { get; }
        public int Height { get; }
        public CellState[,] CellStates { get; }
        public ColorType[,] CellColors { get; }
        public byte[,] CellPathColorMasks { get; }
        public bool[,] CellHasViaduct { get; }
        public ColorType[,] CellUnderColor { get; }
        public ColorType[,] CellOverColor { get; }
        public ObstacleType[,] CellObstacleTypes { get; }
        public IReadOnlyDictionary<ColorType, IReadOnlyList<Vector2Int>> Paths { get; }
        public HashSet<ColorType> LockedColors { get; }
        public ColorType ActiveColor { get; }
        public Vector2Int LastPosition { get; }

        private GridSnapshot(
            int width, int height,
            CellState[,] cellStates, ColorType[,] cellColors,
            byte[,] cellPathColorMasks, bool[,] cellHasViaduct,
            ColorType[,] cellUnderColor, ColorType[,] cellOverColor,
            ObstacleType[,] cellObstacleTypes,
            IReadOnlyDictionary<ColorType, IReadOnlyList<Vector2Int>> paths,
            HashSet<ColorType> lockedColors,
            ColorType activeColor,
            Vector2Int lastPosition)
        {
            Width = width;
            Height = height;
            CellStates = cellStates;
            CellColors = cellColors;
            CellPathColorMasks = cellPathColorMasks;
            CellHasViaduct = cellHasViaduct;
            CellUnderColor = cellUnderColor;
            CellOverColor = cellOverColor;
            CellObstacleTypes = cellObstacleTypes;
            Paths = paths;
            LockedColors = lockedColors;
            ActiveColor = activeColor;
            LastPosition = lastPosition;
        }

        // Thread-local array pool: reuses arrays when dimensions match
        private static CellState[,] _poolStates;
        private static ColorType[,] _poolColors;
        private static byte[,] _poolPathColorMasks;
        private static bool[,] _poolHasViaduct;
        private static ColorType[,] _poolUnderColors;
        private static ColorType[,] _poolOverColors;
        private static ObstacleType[,] _poolObstacleTypes;
        private static int _poolWidth;
        private static int _poolHeight;

        private static void EnsurePool(int w, int h)
        {
            if (_poolStates != null && _poolWidth == w && _poolHeight == h)
                return;
            _poolStates = new CellState[w, h];
            _poolColors = new ColorType[w, h];
            _poolPathColorMasks = new byte[w, h];
            _poolHasViaduct = new bool[w, h];
            _poolUnderColors = new ColorType[w, h];
            _poolOverColors = new ColorType[w, h];
            _poolObstacleTypes = new ObstacleType[w, h];
            _poolWidth = w;
            _poolHeight = h;
        }

        /// <summary>
        /// GridModel'den anlık snapshot alır. Deep-copy yapar, pooled array kullanır.
        /// </summary>
        public static GridSnapshot Capture(IGridModel grid)
        {
            int w = grid.Width;
            int h = grid.Height;

            EnsurePool(w, h);

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var cell = grid.Grid[x, y];
                    _poolStates[x, y] = cell.State;
                    _poolColors[x, y] = cell.Color;
                    _poolPathColorMasks[x, y] = cell.PathColorsMask;
                    _poolHasViaduct[x, y] = cell.HasViaduct;
                    _poolUnderColors[x, y] = cell.UnderColor;
                    _poolOverColors[x, y] = cell.OverColor;
                    _poolObstacleTypes[x, y] = cell.ObstacleType;
                }
            }

            var paths = new Dictionary<ColorType, IReadOnlyList<Vector2Int>>();
            foreach (var kvp in grid.Paths)
            {
                paths[kvp.Key] = new List<Vector2Int>(kvp.Value);
            }

            var locked = new HashSet<ColorType>(grid.LockedColors);

            return new GridSnapshot(
                w, h,
                _poolStates, _poolColors,
                _poolPathColorMasks, _poolHasViaduct, _poolUnderColors, _poolOverColors,
                _poolObstacleTypes,
                paths,
                locked,
                grid.ActiveColor.Value,
                grid.LastPosition.Value
            );
        }

        /// <summary>
        /// Bu snapshot'ı GridModel'e uygular. Tüm state'i overwrite eder.
        /// </summary>
        public void ApplyTo(IGridModel grid)
        {
            grid.Initialize(Width, Height);

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = grid.Grid[x, y];
                    cell.State = CellStates[x, y];
                    cell.Color = CellColors[x, y];
                    cell.PathColorsMask = CellPathColorMasks[x, y];
                    cell.HasViaduct = CellHasViaduct[x, y];
                    cell.UnderColor = CellUnderColor[x, y];
                    cell.OverColor = CellOverColor[x, y];
                    cell.ObstacleType = CellObstacleTypes[x, y];
                }
            }

            grid.Paths.Clear();
            foreach (var kvp in Paths)
            {
                grid.Paths[kvp.Key] = new List<Vector2Int>(kvp.Value);
            }

            grid.LockedColors.Clear();
            foreach (var c in LockedColors)
            {
                grid.LockedColors.Add(c);
            }

            grid.ActiveColor.Value = ActiveColor;
            grid.LastPosition.Value = LastPosition;
        }
    }
}
