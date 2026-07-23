using UnityEngine;
using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;

namespace PixelFlow.Views
{
    [Mediator(typeof(GridMediator))]
    public class GridView : TickableView
    {
        public event System.Action<Vector2Int> OnGlobalPointerDown;
        public event System.Action<Vector2Int> OnGlobalPointerDrag;
        public event System.Action<Vector2Int> OnGlobalPointerUp;

        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Transform _gridContainer;

        [Inject] public ICameraProvider CameraProvider { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject, OptionalInject] public PixelFlow.Data.GameConfig Config { get; set; }

        private Camera _cam;
        private CellView[,] _cells;
        private List<CellView> _instantiatedCells = new List<CellView>();
        private Queue<CellView> _cellPool = new Queue<CellView>();
        public bool IsInitialized => _cells != null;

        // Input state machine — GridInputService'te yönetilir
        private IGridInputService _inputService;
        private Vector2Int _lastDragPos = new Vector2Int(-1, -1);

        private float _targetZoom;

        private float ConfigMinZoom => Config != null ? Config.MinZoom : 8f;
        private float ConfigMaxZoom => Config != null ? Config.MaxZoom : 12f;

        // GPU glow pulse: CPU'daki sinüs hesaplaması kalktı, GlowPulse.shader _Time.y ile yönetiyor
        // Eskiden: her 3 frame'de 1 sin(Time.time * 6.5f) + LineRenderer.width set
        // Şimdi: 0 CPU, GPU _Time.y ile otomatik animasyon
        private static Shader _glowPulseShader;

        private void Awake()
        {
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
            _inputService = new GridInputService();
        }

        protected override void OnTick(float deltaTime)
        {
            // NOT: Glow pulse animasyonu GlowPulse.shader tarafından GPU'da yapılır
            // CPU: her 3 frame'de sin() hesaplaması + LineRenderer.width set → TAMAMEN KALDIRILDI

            if (_cells == null) return;

            // Tick cell animations on all cells in a fast 2D loop
            int cellW = _cells.GetLength(0);
            int cellH = _cells.GetLength(1);
            for (int x = 0; x < cellW; x++)
            {
                for (int y = 0; y < cellH; y++)
                {
                    _cells[x, y]?.TickAnimation(deltaTime);
                }
            }

            HandlePinchZoom();

            // Input state machine GridInputService'te yönetilir
            if (_cam == null) _cam = CameraProvider?.MainCamera;
            if (_cam == null)
            {
                NexusLog.Warn("GridView", "OnTick", "?", "Camera is NULL — input skipped");
                return;
            }

            // Pointer UI üzerindeyse (örn: Pause, Viaduct, Clear butonları) grid girdisini yoksay
            var es = UnityEngine.EventSystems.EventSystem.current;
            bool overUI = es != null && es.IsPointerOverGameObject();
            if (overUI)
            {
                return;
            }

            // Log EventSystem status periodically (every 60 ticks ~1 sec at 60fps)
            if (Time.frameCount % 60 == 0)
            {
                LoggerService?.Log($"[PixelFlow.GridView] EventSystem: current={(bool)es}, " +
                    $"inputModule={(es != null ? es.currentInputModule?.GetType().Name : "null")}, " +
                    $"overUI={overUI}, inputServiceActive={_inputService != null}");
            }

            var result = _inputService?.ProcessInput(_cam, _cells.GetLength(0), _cells.GetLength(1));

            if (result == null || !result.Value.HasEvent) return;

            var r = result.Value;
            if (r.IsDown)
            {
                _lastDragPos = r.GridPosition;
                OnGlobalPointerDown?.Invoke(r.GridPosition);
            }
            else if (r.IsDrag)
            {
                // Interpolate through intermediate cells for smooth line drawing
                var currentPos = r.GridPosition;
                var tempPos = _lastDragPos;
                if (tempPos.x >= 0)
                {
                    while (tempPos != currentPos)
                    {
                        int dx = currentPos.x - tempPos.x;
                        int dy = currentPos.y - tempPos.y;
                        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                            tempPos.x += System.Math.Sign(dx);
                        else
                            tempPos.y += System.Math.Sign(dy);
                        OnGlobalPointerDrag?.Invoke(tempPos);
                    }
                }
                _lastDragPos = currentPos;
            }
            else if (r.IsUp)
            {
                _lastDragPos = new Vector2Int(-1, -1);
                OnGlobalPointerUp?.Invoke(r.GridPosition);
            }
        }

        private void EnsurePool(int requiredSize)
        {
            if (_cellPrefab == null)
            {
                Debug.LogError("[GridView] _cellPrefab is not assigned! Cannot create cell pool.");
                return;
            }

            int currentCount = _instantiatedCells.Count;
            if (currentCount < requiredSize)
            {
                int toCreate = requiredSize - currentCount;
                for (int i = 0; i < toCreate; i++)
                {
                    if (_gridContainer == null)
                    {
                        _gridContainer = transform;
                    }

                    var cell = Instantiate(_cellPrefab, _gridContainer);
                    cell.gameObject.SetActive(false);
                    _cellPool.Enqueue(cell);
                    _instantiatedCells.Add(cell);
                }
                NexusLog.Info("GridView", "EnsurePool", "?", "Instantiated " + toCreate + " new cells. Total cell pool size: " + _instantiatedCells.Count);
            }
        }

        public void InitializeGrid(int width, int height)
        {
            NexusLog.Info("GridView", "InitializeGrid", "?", "InitializeGrid called with " + width + "x" + height);

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
        private Dictionary<ColorType, LineRenderer> _glowLines = new Dictionary<ColorType, LineRenderer>();
        private HashSet<ColorType> _previousPathColors = new HashSet<ColorType>();
        private static Shader _cachedSpriteShader;
        private static Gradient _rainbowGradient;
        private static bool _rainbowGradientInitialized;

        /// <summary>
        /// Rainbow Road hücrelerinden geçen path'ler için gökkuşağı gradient'i oluşturur.
        /// </summary>
        private static Gradient GetRainbowGradient()
        {
            if (_rainbowGradientInitialized)
                return _rainbowGradient;

            _rainbowGradient = new Gradient();
            _rainbowGradient.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.red, 0f / 5f),
                new GradientColorKey(Color.yellow, 1f / 5f),
                new GradientColorKey(Color.green, 2f / 5f),
                new GradientColorKey(Color.cyan, 3f / 5f),
                new GradientColorKey(Color.blue, 4f / 5f),
                new GradientColorKey(new Color(0.5f, 0f, 1f), 5f / 5f) // purple
            };
            _rainbowGradient.alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            _rainbowGradientInitialized = true;
            return _rainbowGradient;
        }

        /// <summary>
        /// Bir path'teki hücrelerden en az biri Rainbow Road işaretli mi?
        /// </summary>
        private static bool PathHasRainbowCell(List<Vector2Int> pathPositions, CellData[,] gridData)
        {
            if (gridData == null) return false;
            int gw = gridData.GetLength(0);
            int gh = gridData.GetLength(1);
            foreach (var pos in pathPositions)
            {
                if (pos.x >= 0 && pos.x < gw && pos.y >= 0 && pos.y < gh)
                {
                    if (gridData[pos.x, pos.y].IsRainbowRoad)
                        return true;
                }
            }
            return false;
        }

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

        public void UpdateDifferential(CellData[,] gridData, AppTheme theme, HashSet<Vector2Int> changedCells, Vector2Int crashPos = default, HashSet<Vector2Int> stateChangedCells = null)
        {
            if (_cells == null || changedCells == null) return;
            foreach (var pos in changedCells)
            {
                if (pos.x >= 0 && pos.x < _cells.GetLength(0) && pos.y >= 0 && pos.y < _cells.GetLength(1))
                {
                    _cells[pos.x, pos.y].UpdateVisuals(gridData[pos.x, pos.y], theme, crashPos);
                    
                    bool shouldBounce = stateChangedCells != null && stateChangedCells.Contains(pos);
                    if (shouldBounce)
                    {
                        _cells[pos.x, pos.y].TriggerBounceAnimation(0.95f, 0.12f);
                    }
                }
            }
        }

        public void TriggerJuicyBounce(Vector2Int position, float scale = 1.25f, float duration = 0.4f)
        {
            if (_cells == null) return;
            if (position.x >= 0 && position.x < _cells.GetLength(0) &&
                position.y >= 0 && position.y < _cells.GetLength(1))
            {
                _cells[position.x, position.y].TriggerBounceAnimation(scale, duration);
            }
        }

        public void UpdatePathVisuals(Dictionary<ColorType, List<Vector2Int>> paths, CellData[,] gridData = null, 
            Vector2Int crashPos = default, ColorType crashColorA = ColorType.None, ColorType crashColorB = ColorType.None)
        {
            if (paths == null) return;

            foreach (var prevColor in _previousPathColors)
            {
                if (!paths.ContainsKey(prevColor))
                {
                    if (_pathLines.TryGetValue(prevColor, out var oldLr))
                        oldLr.gameObject.SetActive(false);
                    if (_glowLines.TryGetValue(prevColor, out var oldGlow))
                        oldGlow.gameObject.SetActive(false);
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
                    if (_glowLines.TryGetValue(colorType, out var inactiveGlow))
                        inactiveGlow.gameObject.SetActive(false);
                    continue;
                }

                if (!_pathLines.TryGetValue(colorType, out var lineRenderer))
                {
                    GameObject lineObj = new GameObject($"PathLine_{colorType}");
                    lineObj.transform.SetParent(_gridContainer);
                    lineRenderer = lineObj.AddComponent<LineRenderer>();
                    
                    lineRenderer.startWidth = 0.2f;
                    lineRenderer.endWidth = 0.2f;
                    lineRenderer.numCornerVertices = 8;
                    lineRenderer.numCapVertices = 8;
                    lineRenderer.useWorldSpace = false;
                    lineRenderer.sortingOrder = 5;
                    
                    Shader spriteShader = _cachedSpriteShader ?? (_cachedSpriteShader = Shader.Find("Sprites/Default"));
                    Material mat = new Material(spriteShader != null ? spriteShader : Shader.Find("Unlit/Color"));
                    lineRenderer.material = mat;
                    
                    _pathLines[colorType] = lineRenderer;
                }

                if (!_glowLines.TryGetValue(colorType, out var glowRenderer))
                {
                    GameObject glowObj = new GameObject($"PathLineGlow_{colorType}");
                    glowObj.transform.SetParent(_gridContainer);
                    glowRenderer = glowObj.AddComponent<LineRenderer>();
                    
                    glowRenderer.startWidth = 0.55f;
                    glowRenderer.endWidth = 0.55f;
                    glowRenderer.numCornerVertices = 8;
                    glowRenderer.numCapVertices = 8;
                    glowRenderer.useWorldSpace = false;
                    glowRenderer.sortingOrder = 2;
                    
                    // GPU glow pulse: GlowPulse.shader _Time.y ile alpha animasyonu yapar
                    // Width sabit (0.55), alpha GPU'da pulse eder — CPU yükü 0
                    // Vertex alpha 0.55 × shader pulse 0.65±0.10 = final alpha 0.30-0.41
                    if (_glowPulseShader == null)
                        _glowPulseShader = Shader.Find("Hidden/PixelFlow/GlowPulse") ?? _cachedSpriteShader ?? Shader.Find("Sprites/Default");
                    Material mat = new Material(_glowPulseShader);
                    glowRenderer.material = mat;
                    
                    _glowLines[colorType] = glowRenderer;
                }

                lineRenderer.gameObject.SetActive(true);
                lineRenderer.positionCount = pathPositions.Count;

                glowRenderer.gameObject.SetActive(true);
                glowRenderer.positionCount = pathPositions.Count;
                
                bool hasRainbow = PathHasRainbowCell(pathPositions, gridData);
                Color pipeColor = CellView.GetColor(colorType);
                Color glowColor = new Color(pipeColor.r, pipeColor.g, pipeColor.b, 0.55f);

                bool isCrashColor = crashPos.x >= 0 && 
                    (colorType == crashColorA || colorType == crashColorB);

                if (hasRainbow && !isCrashColor)
                {
                    // Rainbow Road path'i: gradient ile gökkuşağı renkleri
                    lineRenderer.colorGradient = GetRainbowGradient();
                    glowRenderer.startColor = new Color(1f, 1f, 1f, 0.55f);
                    glowRenderer.endColor = new Color(1f, 1f, 1f, 0.55f);
                }
                else
                {
                    lineRenderer.startColor = pipeColor;
                    lineRenderer.endColor = pipeColor;
                    glowRenderer.startColor = glowColor;
                    glowRenderer.endColor = glowColor;
                }

                int gw = gridData != null ? gridData.GetLength(0) : 0;
                int gh = gridData != null ? gridData.GetLength(1) : 0;

                for (int i = 0; i < pathPositions.Count; i++)
                {
                    Vector2Int gridPos = pathPositions[i];
                    float z = -0.12f;
                    float zGlow = -0.11f;
                    if (gridPos.x >= 0 && gridPos.x < gw && gridPos.y >= 0 && gridPos.y < gh)
                    {
                        var cell = gridData[gridPos.x, gridPos.y];
                        if (cell.HasViaduct && cell.OverColor == colorType)
                        {
                            z = -0.32f;
                            zGlow = -0.31f;
                        }
                    }

                    if (isCrashColor && crashPos.x >= 0)
                    {
                        int dist = Mathf.Abs(gridPos.x - crashPos.x) + Mathf.Abs(gridPos.y - crashPos.y);
                        if (dist <= 2)
                        {
                            Color crashColor = Color.red;
                            lineRenderer.startColor = Color.Lerp(crashColor, pipeColor, dist / 3f);
                            lineRenderer.endColor = Color.Lerp(crashColor, pipeColor, dist / 3f);

                            Color crashGlow = new Color(1f, 0f, 0f, 0.35f);
                            glowRenderer.startColor = Color.Lerp(crashGlow, glowColor, dist / 3f);
                            glowRenderer.endColor = Color.Lerp(crashGlow, glowColor, dist / 3f);
                        }
                    }

                    lineRenderer.SetPosition(i, new Vector3(gridPos.x, gridPos.y, z));
                    glowRenderer.SetPosition(i, new Vector3(gridPos.x, gridPos.y, zGlow));
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
                        Destroy(kvp.Value.material);
                    Destroy(kvp.Value.gameObject);
                }
            }
            _pathLines.Clear();

            foreach (var kvp in _glowLines)
            {
                if (kvp.Value != null)
                {
                    if (kvp.Value.material != null)
                        Destroy(kvp.Value.material);
                    Destroy(kvp.Value.gameObject);
                }
            }
            _glowLines.Clear();
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

            float prevSqr = (prevPos0 - prevPos1).sqrMagnitude;
            float currSqr = (t0.screenPosition - t1.screenPosition).sqrMagnitude;

            if (prevSqr < 0.000001f) return; // 0.001²

            float zoomFactor = Mathf.Sqrt(prevSqr / currSqr);
            _targetZoom = Mathf.Clamp(_targetZoom * zoomFactor, ConfigMinZoom, ConfigMaxZoom);
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, 0.3f);
        }
    }
}
