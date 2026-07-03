using System.Collections.Generic;
using PixelFlow.Data;
using UnityEngine;

namespace PixelFlow.Models
{
    /// <summary>
    /// GridModel'in tam state'ini immutable olarak yakalar.
    /// Değer tipleri kopyalanır, referans tipler deep-copy yapılır.
    /// </summary>
    public readonly struct GridSnapshot
    {
        public int Width { get; }
        public int Height { get; }
        public CellState[,] CellStates { get; }
        public ColorType[,] CellColors { get; }
        public HashSet<ColorType>[,] CellPathColors { get; }
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
            HashSet<ColorType>[,] cellPathColors, bool[,] cellHasViaduct,
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
            CellPathColors = cellPathColors;
            CellHasViaduct = cellHasViaduct;
            CellUnderColor = cellUnderColor;
            CellOverColor = cellOverColor;
            CellObstacleTypes = cellObstacleTypes;
            Paths = paths;
            LockedColors = lockedColors;
            ActiveColor = activeColor;
            LastPosition = lastPosition;
        }

        /// <summary>
        /// GridModel'den anlık snapshot alır. Deep-copy yapar.
        /// </summary>
        public static GridSnapshot Capture(IGridModel grid)
        {
            int w = grid.Width;
            int h = grid.Height;

            var states = new CellState[w, h];
            var colors = new ColorType[w, h];
            var pathColors = new HashSet<ColorType>[w, h];
            var hasViaduct = new bool[w, h];
            var underColors = new ColorType[w, h];
            var overColors = new ColorType[w, h];
            var obstacleTypes = new ObstacleType[w, h];

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var cell = grid.Grid[x, y];
                    states[x, y] = cell.State;
                    colors[x, y] = cell.Color;
                    pathColors[x, y] = new HashSet<ColorType>(cell.PathColors);
                    hasViaduct[x, y] = cell.HasViaduct;
                    underColors[x, y] = cell.UnderColor;
                    overColors[x, y] = cell.OverColor;
                    obstacleTypes[x, y] = cell.ObstacleType;
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
                states, colors,
                pathColors, hasViaduct, underColors, overColors,
                obstacleTypes,
                paths,
                locked,
                grid.ActiveColor,
                grid.LastPosition
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
                    cell.PathColors = new HashSet<ColorType>(CellPathColors[x, y]);
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

            grid.ActiveColor = ActiveColor;
            grid.LastPosition = LastPosition;
        }
    }
}
