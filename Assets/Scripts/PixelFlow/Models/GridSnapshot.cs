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
        public IReadOnlyDictionary<ColorType, IReadOnlyList<Vector2Int>> Paths { get; }
        public HashSet<ColorType> LockedColors { get; }
        public ColorType ActiveColor { get; }
        public Vector2Int LastPosition { get; }

        private GridSnapshot(
            int width, int height,
            CellState[,] cellStates, ColorType[,] cellColors,
            IReadOnlyDictionary<ColorType, IReadOnlyList<Vector2Int>> paths,
            HashSet<ColorType> lockedColors,
            ColorType activeColor,
            Vector2Int lastPosition)
        {
            Width = width;
            Height = height;
            CellStates = cellStates;
            CellColors = cellColors;
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

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var cell = grid.Grid[x, y];
                    states[x, y] = cell.State;
                    colors[x, y] = cell.Color;
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
