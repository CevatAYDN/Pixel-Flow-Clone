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
        public HashSet<ColorType> PathColors = new HashSet<ColorType>();
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

        public void Initialize(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new System.ArgumentException($"Grid boyutu pozitif olmalı: {width}x{height}");

            Width = width;
            Height = height;
            Grid = new CellData[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Grid[x, y] = new CellData { State = CellState.Empty, Color = ColorType.None };
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