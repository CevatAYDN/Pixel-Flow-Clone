using UnityEngine;
using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Data;

namespace PixelFlow.Views
{
    [Mediator(typeof(GridMediator))]
    public class GridView : View
    {
        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Transform _gridContainer;

        private CellView[,] _cells;
        public bool IsInitialized => _cells != null;

        public void InitializeGrid(int width, int height)
        {
            _cells = new CellView[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var cell = Instantiate(_cellPrefab, _gridContainer);
                    cell.transform.localPosition = new Vector3(x, y, 0);
                    cell.Setup(new Vector2Int(x, y));
                    _cells[x, y] = cell;
                }
            }
        }

        public void UpdateGridVisuals(CellData[,] gridData, int width, int height)
        {
            if (_cells == null) return;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _cells[x, y].UpdateVisuals(gridData[x, y].Color, gridData[x, y].State);
                }
            }
        }
    }
}
