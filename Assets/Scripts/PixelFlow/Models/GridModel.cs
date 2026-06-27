using System;
using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Data;

namespace PixelFlow.Models
{
    public enum CellState { Empty, Node, Path, Bridge }

    public class CellData
    {
        public CellState State;
        public ColorType Color;
    }

    public interface IGridModel
    {
        int Width { get; }
        int Height { get; }
        CellData[,] Grid { get; }
        Dictionary<ColorType, List<Vector2Int>> Paths { get; }
        HashSet<ColorType> LockedColors { get; }
        
        event Action OnGridUpdated;
        void Initialize(int width, int height);
        void UpdateGrid();
    }

    public class GridModel : IGridModel
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public CellData[,] Grid { get; private set; }
        public Dictionary<ColorType, List<Vector2Int>> Paths { get; private set; }
        public HashSet<ColorType> LockedColors { get; private set; }

        public event Action OnGridUpdated;

        public void Initialize(int width, int height)
        {
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
        }

        public void UpdateGrid()
        {
            OnGridUpdated?.Invoke();
        }
    }
}
