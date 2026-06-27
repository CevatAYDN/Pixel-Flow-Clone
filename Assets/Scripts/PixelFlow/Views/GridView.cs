using UnityEngine;
using System.Collections.Generic;
using Nexus.Core;
using PixelFlow.Models;

namespace PixelFlow.Views
{
    [Mediator(typeof(GridMediator))]
    public class GridView : View
    {
        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Transform _gridContainer;

        private CellView[,] _cells;
        private Queue<CellView> _cellPool;
        private bool _poolInitialized;
        public bool IsInitialized => _cells != null;

        private void EnsurePool(int size)
        {
            if (_poolInitialized) return;
            _poolInitialized = true;
            _cellPool = new Queue<CellView>(size);
            for (int i = 0; i < size; i++)
            {
                var cell = Instantiate(_cellPrefab, _gridContainer);
                cell.gameObject.SetActive(false);
                _cellPool.Enqueue(cell);
            }
        }

        public void InitializeGrid(int width, int height)
        {
            EnsurePool(width * height);

            _cells = new CellView[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var cell = _cellPool.Dequeue();
                    cell.gameObject.SetActive(true);
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
