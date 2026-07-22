using UnityEngine;
using System.Collections.Generic;
using Nexus.Core;
using PixelFlow.Models;
using PixelFlow.Data;
using PixelFlow.Services;

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

        [Inject] public ICameraProvider CameraProvider { get; set; }

        private Camera _cam;
        private CellView[,] _cells;
        private List<CellView> _instantiatedCells = new List<CellView>();
        private Queue<CellView> _cellPool = new Queue<CellView>();
        public bool IsInitialized => _cells != null;

        private bool _isPointerDown;
        private bool _clickedOutside;
        private Vector2Int _lastGridPos = new Vector2Int(-1, -1);
        private int _activePointerId = -1;

        private float _targetZoom;
        private const float MinZoom = 8f;
        private const float MaxZoom = 12f;

        private void Awake()
        {
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        }

        private void Update()
        {
            if (_cells == null) return;

            HandlePinchZoom();

            var pointer = UnityEngine.InputSystem.Pointer.current;
            if (pointer == null) return;

            bool isPressed = pointer.press.isPressed;
            int pointerId = pointer.deviceId;
            Vector2 screenPos = pointer.position.ReadValue();

            if (isPressed)
            {
                if (_isPointerDown && pointerId != _activePointerId) return;

                if (_cam == null) _cam = CameraProvider?.MainCamera;
                var cam = _cam;
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
                            _activePointerId = pointerId;
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
                            _activePointerId = pointerId;
                            _clickedOutside = true;
                        }
                    }
                }
            }
            else
            {
                if (_isPointerDown && pointerId == _activePointerId)
                {
                    _isPointerDown = false;
                    _activePointerId = -1;
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
        private HashSet<ColorType> _previousPathColors = new HashSet<ColorType>();
        private static Shader _cachedSpriteShader;

        public void UpdateGridVisuals(CellData[,] gridData, int width, int height, AppTheme theme, Dictionary<ColorType, List<Vector2Int>> paths, Vector2Int crashPos = default)
        {
            if (_cells == null) return;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _cells[x, y].UpdateVisuals(gridData[x, y], theme, crashPos);
                }
            }
            UpdatePathVisuals(paths, gridData);
        }

        /// <summary>
        /// GDD §4.2: 3. renk reddedildiğinde ilgili hücrede kırmızı pulse animasyonu oynatır.
        /// GridMediator aracılığıyla çağrılır.
        /// </summary>
        public void TriggerThirdColorRejectionPulse(Vector2Int position)
        {
            if (_cells == null) return;
            if (position.x >= 0 && position.x < _cells.GetLength(0) &&
                position.y >= 0 && position.y < _cells.GetLength(1))
            {
                _cells[position.x, position.y].TriggerThirdColorRejectionPulse();
            }
        }

        public void UpdateDifferential(CellData[,] gridData, AppTheme theme, HashSet<Vector2Int> changedCells, Vector2Int crashPos = default)
        {
            if (_cells == null || changedCells == null) return;
            foreach (var pos in changedCells)
            {
                if (pos.x >= 0 && pos.x < _cells.GetLength(0) && pos.y >= 0 && pos.y < _cells.GetLength(1))
                {
                    _cells[pos.x, pos.y].UpdateVisuals(gridData[pos.x, pos.y], theme, crashPos);
                    _cells[pos.x, pos.y].TriggerBounceAnimation(0.95f, 0.12f);
                }
            }
        }

        public void UpdatePathVisuals(Dictionary<ColorType, List<Vector2Int>> paths, CellData[,] gridData = null, 
            Vector2Int crashPos = default, ColorType crashColorA = ColorType.None, ColorType crashColorB = ColorType.None)
        {
            if (paths == null) return;

            foreach (var prevColor in _previousPathColors)
            {
                if (!paths.ContainsKey(prevColor) && _pathLines.TryGetValue(prevColor, out var oldLr))
                {
                    oldLr.gameObject.SetActive(false);
                }
            }

            foreach (var kvp in paths)
            {
                ColorType colorType = kvp.Key;
                List<Vector2Int> pathPositions = kvp.Value;

                if (pathPositions == null || pathPositions.Count < 2)
                {
                    if (_pathLines.TryGetValue(colorType, out var inactiveLr))
                        inactiveLr.gameObject.SetActive(false);
                    continue;
                }

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
                    lineRenderer.sortingOrder = 0;
                    
                    Shader spriteShader = _cachedSpriteShader ?? (_cachedSpriteShader = Shader.Find("Sprites/Default"));
                    Material mat = new Material(spriteShader != null ? spriteShader : Shader.Find("Unlit/Color"));
                    lineRenderer.material = mat;
                    
                    _pathLines[colorType] = lineRenderer;
                }

                lineRenderer.gameObject.SetActive(true);
                lineRenderer.positionCount = pathPositions.Count;
                
                Color pipeColor = CellView.GetColor(colorType);

                bool isCrashColor = crashPos.x >= 0 && 
                    (colorType == crashColorA || colorType == crashColorB);

                lineRenderer.startColor = pipeColor;
                lineRenderer.endColor = pipeColor;

                int gw = gridData != null ? gridData.GetLength(0) : 0;
                int gh = gridData != null ? gridData.GetLength(1) : 0;

                for (int i = 0; i < pathPositions.Count; i++)
                {
                    Vector2Int gridPos = pathPositions[i];
                    float z = -0.05f;
                    if (gridPos.x >= 0 && gridPos.x < gw && gridPos.y >= 0 && gridPos.y < gh)
                    {
                        var cell = gridData[gridPos.x, gridPos.y];
                        if (cell.HasViaduct && cell.OverColor == colorType)
                        {
                            z = -0.35f;
                        }
                    }

                    if (isCrashColor && crashPos.x >= 0)
                    {
                        int dist = Mathf.Abs(gridPos.x - crashPos.x) + Mathf.Abs(gridPos.y - crashPos.y);
                        if (dist <= 2)
                        {
                            lineRenderer.startColor = Color.Lerp(Color.red, pipeColor, dist / 3f);
                            lineRenderer.endColor = Color.Lerp(Color.red, pipeColor, dist / 3f);
                        }
                    }

                    lineRenderer.SetPosition(i, new Vector3(gridPos.x, gridPos.y, z));
                }
            }

            _previousPathColors.Clear();
            foreach (var kvp in paths)
            {
                if (kvp.Value != null && kvp.Value.Count >= 2)
                    _previousPathColors.Add(kvp.Key);
            }
        }

        private void OnDestroy()
        {
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
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

        public Camera GetCachedCamera() => _cam ?? (_cam = CameraProvider?.MainCamera);

        public void CenterCamera(int width, int height)
        {
            if (_cam == null) _cam = CameraProvider?.MainCamera;
            var cam = _cam;
            if (cam == null) return;

            float cx = (width - 1) * 0.5f;
            float cy = (height - 1) * 0.5f;
            cam.transform.position = new Vector3(cx, cy, -10f);
            cam.orthographic = true;

            float aspect = cam.aspect;
            const float padding = 1f;
            float hSize = (height + padding) * 0.5f;
            float wSize = (width + padding) * 0.5f / aspect;

            if (aspect < 1f)
            {
                hSize += 1.5f;
            }

            cam.orthographicSize = Mathf.Max(hSize, wSize);
            _targetZoom = cam.orthographicSize;
        }

        private void HandlePinchZoom()
        {
            var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            if (touches.Count < 2) return;

            if (_cam == null) _cam = CameraProvider?.MainCamera;
            if (_cam == null || !_cam.orthographic) return;

            var t0 = touches[0];
            var t1 = touches[1];

            Vector2 prevPos0 = t0.screenPosition - t0.delta;
            Vector2 prevPos1 = t1.screenPosition - t1.delta;

            float prevDist = Vector2.Distance(prevPos0, prevPos1);
            float currDist = Vector2.Distance(t0.screenPosition, t1.screenPosition);

            if (prevDist < 0.001f) return;

            float zoomFactor = prevDist / currDist;
            _targetZoom = Mathf.Clamp(_targetZoom * zoomFactor, MinZoom, MaxZoom);
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, 0.3f);
        }
    }
}
