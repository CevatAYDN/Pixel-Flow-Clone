using UnityEngine;
using System.Collections.Generic;
using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Data;

namespace PixelFlow.Views
{
    [Mediator(typeof(GridMediator))]
    public class GridView : View
    {
        public event System.Action<Vector2Int> OnGlobalPointerDown;
        public event System.Action<Vector2Int> OnGlobalPointerDrag;
        public event System.Action<Vector2Int> OnGlobalPointerUp;

        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Transform _gridContainer;

        private CellView[,] _cells;
        private List<CellView> _instantiatedCells = new List<CellView>();
        private Queue<CellView> _cellPool = new Queue<CellView>();
        public bool IsInitialized => _cells != null;

        private bool _isPointerDown;
        private bool _clickedOutside;
        private Vector2Int _lastGridPos = new Vector2Int(-1, -1);

        private void Update()
        {
            if (_cells == null) return;

            var pointer = UnityEngine.InputSystem.Pointer.current;
            if (pointer == null) return;

            bool isPressed = pointer.press.isPressed;
            Vector2 screenPos = pointer.position.ReadValue();

            if (isPressed)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
                    int gx = Mathf.RoundToInt(worldPos.x);
                    int gy = Mathf.RoundToInt(worldPos.y);

                    int width = _cells.GetLength(0);
                    int height = _cells.GetLength(1);

                    if (gx >= 0 && gx < width && gy >= 0 && gy < height)
                    {
                        Vector2Int currentGridPos = new Vector2Int(gx, gy);

                        if (!_isPointerDown)
                        {
                            _isPointerDown = true;
                            _clickedOutside = false;
                            _lastGridPos = currentGridPos;
                            OnGlobalPointerDown?.Invoke(currentGridPos);
                        }
                        else if (!_clickedOutside && currentGridPos != _lastGridPos)
                        {
                            Vector2Int tempPos = _lastGridPos;
                            while (tempPos != currentGridPos)
                            {
                                int dx = currentGridPos.x - tempPos.x;
                                int dy = currentGridPos.y - tempPos.y;

                                if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                                {
                                    tempPos.x += System.Math.Sign(dx);
                                }
                                else
                                {
                                    tempPos.y += System.Math.Sign(dy);
                                }

                                OnGlobalPointerDrag?.Invoke(tempPos);
                            }
                            _lastGridPos = currentGridPos;
                        }
                    }
                    else
                    {
                        if (!_isPointerDown)
                        {
                            _isPointerDown = true;
                            _clickedOutside = true;
                        }
                    }
                }
            }
            else
            {
                if (_isPointerDown)
                {
                    _isPointerDown = false;
                    if (!_clickedOutside)
                    {
                        OnGlobalPointerUp?.Invoke(_lastGridPos);
                    }
                    _clickedOutside = false;
                    _lastGridPos = new Vector2Int(-1, -1);
                }
            }
        }

        private void EnsurePool(int requiredSize)
        {
            int currentCount = _instantiatedCells.Count;
            if (currentCount < requiredSize)
            {
                int toCreate = requiredSize - currentCount;
                for (int i = 0; i < toCreate; i++)
                {
                    var cell = Instantiate(_cellPrefab, _gridContainer);
                    cell.gameObject.SetActive(false);
                    _cellPool.Enqueue(cell);
                    _instantiatedCells.Add(cell);
                }
                UnityEngine.Debug.Log($"[GridView] Instantiated {toCreate} new cells. Total cell pool size: {_instantiatedCells.Count}");
            }
        }

        public void InitializeGrid(int width, int height)
        {
            UnityEngine.Debug.Log($"[GridView] InitializeGrid called with {width}x{height}");

            // Deactivate and return all previously used cells to the pool
            if (_cells != null)
            {
                for (int x = 0; x < _cells.GetLength(0); x++)
                {
                    for (int y = 0; y < _cells.GetLength(1); y++)
                    {
                        if (_cells[x, y] != null)
                        {
                            _cells[x, y].gameObject.SetActive(false);
                            _cellPool.Enqueue(_cells[x, y]);
                        }
                    }
                }
            }

            // Clean up any path lines from previous level to prevent memory/asset leaks
            foreach (var kvp in _pathLines)
            {
                if (kvp.Value != null)
                {
                    if (kvp.Value.material != null)
                    {
                        Destroy(kvp.Value.material);
                    }
                    Destroy(kvp.Value.gameObject);
                }
            }
            _pathLines.Clear();

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

        private Dictionary<ColorType, LineRenderer> _pathLines = new Dictionary<ColorType, LineRenderer>();

        public void UpdateGridVisuals(CellData[,] gridData, int width, int height, AppTheme theme, Dictionary<ColorType, List<Vector2Int>> paths)
        {
            UnityEngine.Debug.Log($"[GridView] UpdateGridVisuals called.");
            if (_cells == null) return;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _cells[x, y].UpdateVisuals(gridData[x, y].Color, gridData[x, y].State, theme);
                }
            }
            UpdatePathVisuals(paths);
        }

        public void UpdatePathVisuals(Dictionary<ColorType, List<Vector2Int>> paths)
        {
            foreach (var lr in _pathLines.Values)
            {
                lr.gameObject.SetActive(false);
            }

            if (paths == null) return;

            foreach (var kvp in paths)
            {
                ColorType colorType = kvp.Key;
                List<Vector2Int> pathPositions = kvp.Value;

                if (pathPositions == null || pathPositions.Count < 2) continue;

                if (!_pathLines.TryGetValue(colorType, out var lineRenderer))
                {
                    GameObject lineObj = new GameObject($"PathLine_{colorType}");
                    lineObj.transform.SetParent(_gridContainer);
                    lineRenderer = lineObj.AddComponent<LineRenderer>();
                    
                    lineRenderer.startWidth = 0.35f;
                    lineRenderer.endWidth = 0.35f;
                    lineRenderer.numCornerVertices = 8;
                    lineRenderer.numCapVertices = 8;
                    lineRenderer.useWorldSpace = false;
                    lineRenderer.sortingOrder = 1;
                    
                    Shader spriteShader = Shader.Find("Sprites/Default");
                    Material mat = new Material(spriteShader != null ? spriteShader : Shader.Find("Unlit/Color"));
                    lineRenderer.material = mat;
                    
                    _pathLines[colorType] = lineRenderer;
                }

                lineRenderer.gameObject.SetActive(true);
                lineRenderer.positionCount = pathPositions.Count;
                
                Color pipeColor = CellView.GetColor(colorType);
                lineRenderer.startColor = pipeColor;
                lineRenderer.endColor = pipeColor;

                for (int i = 0; i < pathPositions.Count; i++)
                {
                    Vector2Int gridPos = pathPositions[i];
                    lineRenderer.SetPosition(i, new Vector3(gridPos.x, gridPos.y, -0.1f));
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var kvp in _pathLines)
            {
                if (kvp.Value != null)
                {
                    if (kvp.Value.material != null)
                    {
                        Destroy(kvp.Value.material);
                    }
                    Destroy(kvp.Value.gameObject);
                }
            }
            _pathLines.Clear();
        }
    }
}
