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
        public List<ColorType> PathColors = new List<ColorType>();
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
        ColorType ActiveColor { get; set; }
        Vector2Int LastPosition { get; set; }
        Vector2Int LastCrashPosition { get; set; }
        ColorType CrashColorA { get; set; }
        ColorType CrashColorB { get; set; }

        void Initialize(int width, int height);
    }

    /// <summary>
    /// Izgara state'i. Bildirim için doğrudan C# event kullanmak yerine
    /// SignalBus.Fire(GridUpdatedSignal) tercih edilir; bu sayede tüm MVCS akışı
    /// tek kanaldan (sinyal) geçer, debug/trace tutarlı olur ve state değişikliği
    /// Nerede olursa olsun tek bir yerde gözlemlenebilir.
    /// </summary>
    public class GridModel : IGridModel, IReactiveModel
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public CellData[,] Grid { get; private set; }
        public Dictionary<ColorType, List<Vector2Int>> Paths { get; private set; }
        public HashSet<ColorType> LockedColors { get; private set; }
        public ColorType ActiveColor { get; set; }
        public Vector2Int LastPosition { get; set; }
        public Vector2Int LastCrashPosition { get; set; } = new Vector2Int(-1, -1);
        public ColorType CrashColorA { get; set; }
        public ColorType CrashColorB { get; set; }

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
            ActiveColor = ColorType.None;
            LastPosition = new Vector2Int(-1, -1);
        }

        public ValueTask OnBind(CancellationToken ct) => default;
    }
}