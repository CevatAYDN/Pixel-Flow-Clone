using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using PixelFlow.Models;
using PixelFlow.Data;
using PixelFlow.Signals;
using PixelFlow.Views;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Services;

namespace PixelFlow.Services
{
    public interface IVehicleSimulator
    {
        void StartSimulationPhase();
        void StopSimulationPhase();
        void ClearAllVehicles();
        void Tick(float deltaTime);
    }

    /// <summary>
    /// Araç simülasyonu. ITickable implement eder — TickService'e kaydolur.
    /// Artık SimulationUpdater kullanmaz, doğrudan ITickService üzerinden tick alır.
    /// </summary>

    /// <summary>
    /// Araç simülasyonunun çekirdek mantığı: spawn, movement, collision detection, timer.
    /// Görsel üretim VehicleVisualFactory'e, veri modeli VehicleInstance'e ayrıldı.
    /// </summary>
    public class VehicleSimulator : IVehicleSimulator, ITickable, INexusService
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public IHintModel HintModel { get; set; }
        [Inject] public IHapticService HapticService { get; set; }
        [Inject] public IAudioService AudioService { get; set; }
        [Inject] public IObstacleService ObstacleService { get; set; }
        [Inject] public ISettingsModel SettingsModel { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }
        [Inject] public ICameraProvider CamProvider { get; set; }
        [Inject] public ICrisisAdService CrisisAdService { get; set; }
        [Inject] public IGridViewProvider GridViewProvider { get; set; }
        [Inject, OptionalInject] public Data.GameConfig Config { get; set; }
        [Inject, OptionalInject] public ITickService TickService { get; set; }
        [Inject, OptionalInject] public DefaultSkinIdsConfigAsset DefaultSkinConfig { get; set; }
        [Inject, OptionalInject] public BouncyPhysicsConfigAsset BouncyPhysicsConfig { get; set; }
        [Inject, OptionalInject] public IInventoryModel InventoryModel { get; set; }

        private static readonly ColorType[] AllColors;

        static VehicleSimulator()
        {
            var values = System.Enum.GetValues(typeof(ColorType));
            AllColors = new ColorType[values.Length];
            for (int i = 0; i < values.Length; i++)
                AllColors[i] = (ColorType)values.GetValue(i);
        }

        private readonly Dictionary<ColorType, (Vector2Int, Vector2Int)> _cachedEndpoints = new Dictionary<ColorType, (Vector2Int, Vector2Int)>();

        // Cached node positions per level to avoid iterating initialNodes every tick
        private readonly Dictionary<int, Dictionary<ColorType, (Vector2Int, Vector2Int)>> _levelNodeCache = new Dictionary<int, Dictionary<ColorType, (Vector2Int, Vector2Int)>>();
        
        private Transform _vehicleContainer;
        private Transform _cachedGridView;
        private CameraController _cachedCameraController;
        private VehicleMovementService _movementService;

        private float _fixedTimeStep;
        private float _vehicleSpeed;
        private float _spawnInterval;
        private int _spawnCheckInterval;
        private float _maxSimulationSafetyDuration;
        private float _viaductOverZOffset;
        private float _viaductUnderZOffset;
        private float _normalZOffset;
        private float _collisionDistance;
        private float _speedVariationRange;
        
        private float _fixedAccumulator;
        private float _simulationPhaseTimer;
        private int _spawnSkipCounter;
        
        // Grid-based spatial partitioning collision detection — List pool for GC alloc reduction
        private readonly Dictionary<Vector2Int, List<VehicleInstance>> _cellOccupancy = new Dictionary<Vector2Int, List<VehicleInstance>>();
        private readonly List<List<VehicleInstance>> _occupancyListPool = new List<List<VehicleInstance>>();
        
        // PERFORMANCE: Her frame AllColors döngüsünden kaçınmak için aktif renk cache'i
        private readonly HashSet<ColorType> _activeConnectedColors = new HashSet<ColorType>();
        private int _activeColorCacheCounter = 0;
        private const int ActiveColorRefreshInterval = 15; // 15 frame'de bir yenile (~250ms @60fps). Keep in sync with GameConfig.SpawnCheckInterval

        // Active vehicles and spawn timers
        private readonly List<VehicleInstance> _activeVehicles = new List<VehicleInstance>();
        private readonly Dictionary<ColorType, float> _spawnTimers = new Dictionary<ColorType, float>();
        
        private ISignalSubscription _undoSubscription;
        private ISignalSubscription _redoSubscription;
        private ISignalSubscription _levelFailedSubscription;

        private void CacheConfigValues()
        {
            if (Config == null)
                throw new DataValidationException("[VehicleSimulator] GameConfig is not injected. Bind GameConfig in GameContextLifecycle. Zero-Silent-Fallback policy forbids magic number defaults.");

            _fixedTimeStep = Config.FixedTimeStep > 0f ? Config.FixedTimeStep : (1f / 60f);
            _vehicleSpeed = Config.VehicleSpeed;
            _spawnInterval = Config.SpawnInterval;
            _spawnCheckInterval = Config.SpawnCheckInterval;
            _maxSimulationSafetyDuration = Config.MaxSimulationSafetyDuration;
            _viaductOverZOffset = Config.ViaductOverZOffset;
            _viaductUnderZOffset = Config.ViaductUnderZOffset;
            _normalZOffset = Config.NormalZOffset;
            _collisionDistance = Config.CollisionDistance;
            _speedVariationRange = Config.SpeedVariationRange;
        }

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            CacheConfigValues();
            _movementService = new VehicleMovementService(
                GridModel, GameStateModel, GameSessionModel,
                SignalBus, AudioService, ObstacleService, Config);

            if (Application.isPlaying)
            {
                TickService?.RegisterTickable(this);

                _vehicleContainer = new GameObject("[Vehicles]").transform;
                _vehicleContainer.gameObject.hideFlags = HideFlags.DontSave;

                var gridTransform = GridViewProvider?.GridTransform;
                if (gridTransform != null)
                {
                    _vehicleContainer.SetParent(gridTransform, false);
                }

                _cachedCameraController = CamProvider?.MainCamera?.GetComponent<CameraController>();
            }

            if (GameStateModel != null)
                GameStateModel.OnStateChanged += HandleStateChanged;

            _undoSubscription = SignalBus != null ? SignalBus.Subscribe<UndoSignal>(sig => ClearAllVehicles()) : null;
            _redoSubscription = SignalBus != null ? SignalBus.Subscribe<RedoSignal>(sig => ClearAllVehicles()) : null;

            _levelFailedSubscription = SignalBus != null ? SignalBus.Subscribe<LevelFailedSignal>(sig =>
            {
                ClearAllVehicles();
                GameStateModel.SetState(GameState.LevelFailed);
            }) : null;

            return default;
        }

        private void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(obj);
            else
                UnityEngine.Object.DestroyImmediate(obj);
        }

        public void OnDispose()
        {
            TickService?.UnregisterTickable(this);
            if (_vehicleContainer != null)
            {
                SafeDestroy(_vehicleContainer.gameObject);
            }
            if (GameStateModel != null)
                GameStateModel.OnStateChanged -= HandleStateChanged;
            _undoSubscription?.Dispose();
            _redoSubscription?.Dispose();
            _levelFailedSubscription?.Dispose();
            ClearAllVehicles();
        }

        private void HandleStateChanged(GameState state)
        {
            LoggerService?.Log($"[PixelFlow.VehicleSimulator] HandleStateChanged: {GameStateModel.PreviousState} -> {state}");
            
            // Eğer duraklatılmış (Paused) durumdan Simulating durumuna geçiyorsak (viyadük yerleştirildi), araçları temizleme!
            if (GameStateModel.PreviousState == GameState.Paused && state == GameState.Simulating)
            {
                LoggerService?.Log("[PixelFlow.VehicleSimulator] Resuming simulation from Paused state. Preserving existing vehicles.");
                return;
            }

            if (state == GameState.Playing)
            {
                LoggerService?.Log("[PixelFlow.VehicleSimulator] GameState set to Playing. Resetting simulation timer and clearing vehicles.");
                _simulationPhaseTimer = 0f;
                _cachedEndpoints.Clear();
                ClearAllVehicles();  // ClearAllVehicles içinde InvalidateSplineCache() çağrılır
            }
            else if (state == GameState.Simulating)
            {
                _simulationPhaseTimer = 0f;
                _cachedEndpoints.Clear();
                _movementService?.InvalidateSplineCache();
                // ClearAllVehicles() kaldırıldı - araçlar yok edilmeden pürüzsüzce hayaletten katı moda geçecek
                LoggerService?.Log("[PixelFlow.VehicleSimulator] Simulation Phase started. All vehicles now transition to solid.");
            }
            else if (state == GameState.MainMenu || state == GameState.LevelCompleted || state == GameState.LevelFailed)
            {
                LoggerService?.Log($"[PixelFlow.VehicleSimulator] GameState set to {state}. Clearing all vehicles.");
                ClearAllVehicles();
            }
        }

        public void StartSimulationPhase()
        {
            GameStateModel.SetState(GameState.Simulating);
        }

        public void StopSimulationPhase()
        {
            GameStateModel.SetState(GameState.Playing);
        }

        public void ClearAllVehicles()
        {
            foreach (var v in _activeVehicles)
            {
                if (v.Visual != null)
                {
                    VehicleVisualFactory.RecycleVehicle(v.Visual);
                }
            }
            _activeVehicles.Clear();
            _spawnTimers.Clear();
            _cellOccupancy.Clear();
            _occupancyListPool.Clear();
            _movementService?.InvalidateSplineCache();
        }

        private bool IsVehiclePathStale(VehicleInstance v)
        {
            if (GridModel?.Paths == null) return true;
            if (!GridModel.Paths.TryGetValue(v.Color, out var currentPath) || currentPath == null)
                return true; // No path exists anymore

            if (v.Path == null || v.Path.Count != currentPath.Count)
                return true;

            for (int i = 0; i < v.Path.Count; i++)
            {
                if (v.Path[i] != currentPath[i])
                    return true;
            }

            return false;
        }

        public void Tick(float deltaTime)
        {
            var state = GameStateModel.CurrentState;
            if (state != GameState.Playing && state != GameState.Simulating)
                return;

            // Game timer uses real deltaTime (should track wall-clock time)
            if (state == GameState.Playing)
                GameSessionModel?.UpdateTime(Time.deltaTime);

            if (_fixedTimeStep <= 0f)
                CacheConfigValues();

            float step = _fixedTimeStep > 0f ? _fixedTimeStep : (1f / 60f);

            _fixedAccumulator += deltaTime;
            _fixedAccumulator = Mathf.Min(_fixedAccumulator, step * 5); // Cap: prevent spiral of death

            while (_fixedAccumulator >= step)
            {
                ObstacleService?.Tick(step);
                _movementService?.UpdateMovement(_activeVehicles, step);
                _fixedAccumulator -= step;
            }

            // Spawn timing, collision detection, and completion timer use real deltaTime
            // (these are timer-based, not physics-based)
            UpdateSpawning(deltaTime);

            // Remove vehicles whose path has been modified by the player
            int vehicleCount = _activeVehicles.Count;
            for (int i = vehicleCount - 1; i >= 0; i--)
            {
                if (IsVehiclePathStale(_activeVehicles[i]))
                {
                    var stale = _activeVehicles[i];
                    // Spline cache'i bu renk için temizle — eski path'in kontrol
                    // noktaları yeni araçlar tarafından kullanılmamalı
                    _movementService?.InvalidateSplineCache(stale.Color);
                    if (stale.Visual != null)
                        VehicleVisualFactory.RecycleVehicle(stale.Visual);
                    _activeVehicles.RemoveAt(i);
                }
            }
            
            if ((state == GameState.Playing || state == GameState.Simulating) && _activeVehicles.Count > 1)
            {
                UpdateCollisionDetection();
            }
            if (state == GameState.Simulating)
            {
                UpdateCompletionTimer(deltaTime);
            }
}

        private void UpdateSpawning(float deltaTime)
        {
            // Hiç aktif araç yoksa ve simülasyon çalışmıyorsa spawn kontrolünü seyrelt
            bool isSimulating = GameStateModel.CurrentState == GameState.Simulating;
            if (_activeVehicles.Count == 0 && !isSimulating)
            {
                _spawnSkipCounter++;
                if (_spawnSkipCounter < _spawnCheckInterval)
                    return;
            }
            _spawnSkipCounter = 0;

            // PERFORMANCE: Aktif renk cache'ini periyodik olarak yenile
            // Her frame tüm renkleri (7+) IsColorConnected ile kontrol etmek yerine,
            // sadece cache'teki renkler üzerinde spawn kontrolü yapılır.
            _activeColorCacheCounter++;
            if (_activeColorCacheCounter >= ActiveColorRefreshInterval)
            {
                _activeColorCacheCounter = 0;
                RefreshActiveColors();
            }

            foreach (var color in _activeConnectedColors)
            {
                float spawnInterval = _spawnInterval;

                // İlk spawn hemen olsun (timer 0'da başla)
                if (!_spawnTimers.ContainsKey(color))
                {
                    _spawnTimers[color] = spawnInterval;
                }

                _spawnTimers[color] += deltaTime;
                if (_spawnTimers[color] >= spawnInterval)
                {
                    _spawnTimers[color] = 0f;
                    SpawnVehicle(color);
                }
            }

            // Eskiden kullanılıp artık bağlı olmayan renklerin timer'larını temizle
            if (_spawnTimers.Count > _activeConnectedColors.Count * 2)
            {
                // Sadece ara sıra temizlik yap (GC koruması)
                var staleKeys = new List<ColorType>();
                foreach (var kvp in _spawnTimers)
                {
                    if (!_activeConnectedColors.Contains(kvp.Key))
                        staleKeys.Add(kvp.Key);
                }
                foreach (var key in staleKeys)
                    _spawnTimers.Remove(key);
            }
        }

        /// <summary>
        /// PERFORMANCE: Hangi renklerin bağlı (endpoint'leri eşleşen) path'e sahip
        /// olduğunu belirler ve cache'ler. Her ActiveColorRefreshInterval frame'de
        /// bir çağrılır — her frame AllColors + IsColorConnected döngüsü yerine
        /// sadece cache'teki renkler taranır.
        /// IsColorConnected çağrısı _cachedEndpoints'i de doldurur, böylece
        /// sonraki spawn'larda tekrar kontrol gerekmez.
        /// </summary>
        private void RefreshActiveColors()
        {
            _activeConnectedColors.Clear();
            if (GridModel?.Paths == null) return;

            foreach (var kvp in GridModel.Paths)
            {
                if (kvp.Key != ColorType.None && kvp.Value != null && kvp.Value.Count >= 2)
                {
                    // IsColorConnected endpoint eşleşmesini doğrular ve _cachedEndpoints'i doldurur
                    if (IsColorConnected(kvp.Key))
                    {
                        _activeConnectedColors.Add(kvp.Key);
                    }
                }
            }
        }

        private bool IsColorConnected(ColorType color)
        {
            if (!GridModel.Paths.TryGetValue(color, out var path) || path.Count < 2)
                return false;

            if (!_cachedEndpoints.TryGetValue(color, out var endpoints))
            {
                var currentLevel = LevelModel.CurrentLevel;
                if (currentLevel?.initialNodes == null) return false;

                int levelIndex = currentLevel.levelIndex;
                
                // Try to get from level cache first
                if (!_levelNodeCache.TryGetValue(levelIndex, out var levelCache))
                {
                    levelCache = new Dictionary<ColorType, (Vector2Int, Vector2Int)>();
                    _levelNodeCache[levelIndex] = levelCache;
                    
                    // Pre-compute all node pairs for this level
                    for (int i = 0; i < currentLevel.initialNodes.Count; i++)
                    {
                        var node = currentLevel.initialNodes[i];
                        if (node.color == ColorType.None) continue;
                        
                        if (!levelCache.TryGetValue(node.color, out var existing))
                        {
                            levelCache[node.color] = (node.position, new Vector2Int(-1, -1));
                        }
                        else if (existing.Item2 == new Vector2Int(-1, -1))
                        {
                            levelCache[node.color] = (existing.Item1, node.position);
                        }
                    }
                }
                
                if (!levelCache.TryGetValue(color, out endpoints) || endpoints.Item2 == new Vector2Int(-1, -1))
                    return false;
                
                _cachedEndpoints[color] = endpoints;
            }

            Vector2Int start = path[0];
            Vector2Int end = path[path.Count - 1];
            return (start == endpoints.Item1 && end == endpoints.Item2) || 
                   (start == endpoints.Item2 && end == endpoints.Item1);
        }

        private void SpawnVehicle(ColorType color)
        {
            if (!GridModel.Paths.TryGetValue(color, out var path) || path.Count < 2)
                return;

            if (ObstacleService != null && path.Count > 0)
            {
                Vector2Int startCell = path[0];
                if (ObstacleService.IsNarrowPass(startCell) && !ObstacleService.CanVehicleEnterNarrowPass(startCell, color))
                {
                    LoggerService?.Log($"[PixelFlow.VehicleSimulator] Spawn blocked: Narrow pass at start cell {startCell} is occupied for color {color}.");
                    return;
                }
            }

            if (_cachedGridView == null)
                _cachedGridView = GridViewProvider?.GridTransform;

            if (_vehicleContainer != null && _vehicleContainer.parent == null && _cachedGridView != null)
                _vehicleContainer.SetParent(_cachedGridView, false);
            Transform parentTransform = _cachedGridView != null ? _cachedGridView : _vehicleContainer;

            GameObject visual = new GameObject($"V_{color}");
            visual.transform.SetParent(parentTransform);

            VehicleStyle vehicleStyle = SettingsModel.CurrentVehicleStyle;
            if (InventoryModel == null)
                throw new DataValidationException("InventoryModel is null in VehicleSimulator!");

            if (DefaultSkinConfig == null)
                throw new DataValidationException("DefaultSkinIdsConfigAsset not injected in VehicleSimulator!");

            string defaultSkin = DefaultSkinConfig.DefaultVehicleSkinId;
            string equippedSkin = InventoryModel.GetEquippedSkin(color);
            if (string.IsNullOrEmpty(equippedSkin)) equippedSkin = defaultSkin;

            LoggerService?.Log($"[PixelFlow.VehicleSimulator] Spawning {vehicleStyle} with equipped skin '{equippedSkin}' for color {color}");
            
            Transform loco = null, wagon1 = null, wagon2 = null, coupler1 = null, coupler2 = null;
            List<Renderer> renderers = vehicleStyle == VehicleStyle.Train 
                ? VehicleVisualFactory.CreateTrain3D(visual, color, out loco, out wagon1, out wagon2, out coupler1, out coupler2) 
                : VehicleVisualFactory.CreateCar3D(visual, color);

            var inst = new VehicleInstance
            {
                Color = color,
                AnimationOffset = UnityEngine.Random.Range(0f, 100f),
                Style = vehicleStyle,
                // Path'in KOPYASINI al — referans değil!
                // Aksi halde kullanıcı çizimi değiştirince daha önce spawnlanmış
                // araçların Path'i de değişir (aynı List referansı) → teleportasyon
                Path = new List<Vector2Int>(path),
                SegmentIndex = 0,
                Progress = 0f,
                Visual = visual,
                CurrentPosition = new Vector3(path[0].x, path[0].y, GetZOffset(path[0], color)),
                Speed = _vehicleSpeed + UnityEngine.Random.Range(-_speedVariationRange, _speedVariationRange),
                CachedRenderers = renderers.ToArray(),
                LocoTransform = loco,
                Wagon1Transform = wagon1,
                Wagon2Transform = wagon2,
                Coupler1Transform = coupler1,
                Coupler2Transform = coupler2
            };

            LoggerService?.Log($"[PixelFlow.VehicleSimulator] Spawning vehicle of color {color} with style {vehicleStyle} and speed {inst.Speed}. Path points: {path.Count}");

            visual.transform.localPosition = vehicleStyle == VehicleStyle.Train ? Vector3.zero : inst.CurrentPosition;

            // Set initial vehicle color via MaterialPropertyBlock (shared materials don't have per-vehicle color baked in)
            VehicleVisualFactory.ApplyColorToRenderers(color, inst.CachedRenderers, inst.Mpb);

            _activeVehicles.Add(inst);
        }



        private float GetZOffset(Vector2Int gridPos, ColorType color)
        {
            if (gridPos.x >= 0 && gridPos.x < GridModel.Width && gridPos.y >= 0 && gridPos.y < GridModel.Height)
            {
                var cell = GridModel.Grid[gridPos.x, gridPos.y];
                if (cell.HasViaduct)
                {
                    if (cell.OverColor == color)
                    {
                        return _viaductOverZOffset;
                    }
                    if (cell.UnderColor == color)
                    {
                        return _viaductUnderZOffset;
                    }
                    if (cell.UnderColor == ColorType.None)
                    {
                        cell.UnderColor = color;
                        return _viaductUnderZOffset;
                    }
                    else
                    {
                        cell.OverColor = color;
                        return _viaductOverZOffset;
                    }
                }
            }
            return _normalZOffset;
        }

        /// <summary>
        /// Grid-based spatial partitioning collision detection.
        /// Vehicles register which cell they occupy. Collision is checked between
        /// vehicles on the same or adjacent cells (8-neighborhood), reducing
        /// complexity to O(n × avgDensity) in practice.
        /// </summary>
        private static readonly Vector2Int[] NeighborOffsets =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        private void UpdateCollisionDetection()
        {
            // Return all pooled lists before rebuilding
            foreach (var kvp in _cellOccupancy)
            {
                kvp.Value.Clear();
                _occupancyListPool.Add(kvp.Value);
            }
            _cellOccupancy.Clear();

            // Build fresh occupancy map from current vehicle positions
            for (int i = 0; i < _activeVehicles.Count; i++)
            {
                var v = _activeVehicles[i];
                Vector2Int gridPos = new Vector2Int(
                    Mathf.RoundToInt(v.CurrentPosition.x),
                    Mathf.RoundToInt(v.CurrentPosition.y));

                // Clamp to grid bounds
                gridPos.x = Mathf.Clamp(gridPos.x, 0, GridModel.Width - 1);
                gridPos.y = Mathf.Clamp(gridPos.y, 0, GridModel.Height - 1);

                if (!_cellOccupancy.TryGetValue(gridPos, out var list))
                {
                    // Reuse pooled list to avoid GC alloc
                    if (_occupancyListPool.Count > 0)
                    {
                        list = _occupancyListPool[_occupancyListPool.Count - 1];
                        _occupancyListPool.RemoveAt(_occupancyListPool.Count - 1);
                    }
                    else
                    {
                        list = new List<VehicleInstance>();
                    }
                    _cellOccupancy[gridPos] = list;
                }
                list.Add(v);
            }

            // Check collisions on cells with multiple vehicles + adjacent cells
            foreach (var kvp in _cellOccupancy)
            {
                var vehicles = kvp.Value;
                var cellPos = kvp.Key;

                // Same-cell collisions
                for (int i = 0; i < vehicles.Count; i++)
                {
                    for (int j = i + 1; j < vehicles.Count; j++)
                    {
                        if (CheckCollisionPair(vehicles[i], vehicles[j], cellPos))
                            return;
                    }
                }

                // Adjacent-cell collisions (only check positive offsets to avoid double-checking)
                for (int n = 0; n < 4; n++)
                {
                    Vector2Int neighborPos = cellPos + NeighborOffsets[n];
                    if (!_cellOccupancy.TryGetValue(neighborPos, out var neighborVehicles))
                        continue;

                    for (int i = 0; i < vehicles.Count; i++)
                    {
                        for (int j = 0; j < neighborVehicles.Count; j++)
                        {
                            if (CheckCollisionPair(vehicles[i], neighborVehicles[j], cellPos))
                                return;
                        }
                    }
                }
            }
        }

        private bool CheckCollisionPair(VehicleInstance v1, VehicleInstance v2, Vector2Int cellPos)
        {
            if (v1.Color == v2.Color) return false;

            var cell = GridModel.Grid[
                Mathf.Clamp(cellPos.x, 0, GridModel.Width - 1),
                Mathf.Clamp(cellPos.y, 0, GridModel.Height - 1)];

            if (cell.IsRainbowRoad || cell.HasViaduct)
            {
                return false; // Rainbow Road power-ups or Viaduct 3D bridges separate vehicle paths
            }

            Vector2Int gridPos1 = new Vector2Int(Mathf.RoundToInt(v1.CurrentPosition.x), Mathf.RoundToInt(v1.CurrentPosition.y));
            Vector2Int gridPos2 = new Vector2Int(Mathf.RoundToInt(v2.CurrentPosition.x), Mathf.RoundToInt(v2.CurrentPosition.y));

            bool sameCell = gridPos1 == gridPos2;
            float collisionDist = Config != null ? Config.CollisionDistance : throw new DataValidationException("[VehicleSimulator] GameConfig.CollisionDistance erişilemedi!");
            float sqrDist = (v1.CurrentPosition - v2.CurrentPosition).sqrMagnitude;

            if (sameCell || sqrDist < collisionDist * collisionDist)
            {
                TriggerCrash(cellPos, v1.Color, v2.Color);
                return true;
            }

            return false;
        }

        private void TriggerCrash(Vector2Int crashPos, ColorType colorA, ColorType colorB)
        {
            LoggerService?.Log($"[PixelFlow.VehicleSimulator] Bouncy collision at cell {crashPos} between {colorA} and {colorB}.");

            GridModel.LastCrashPosition.Value = crashPos;
            GridModel.CrashColorA.Value = colorA;
            GridModel.CrashColorB.Value = colorB;

            // Apply bouncy squash/stretch physics to vehicles at collision position
            for (int i = 0; i < _activeVehicles.Count; i++)
            {
                var v = _activeVehicles[i];
                if (v.Color == colorA || v.Color == colorB)
                {
                    Vector2Int vPos = new Vector2Int(Mathf.RoundToInt(v.CurrentPosition.x), Mathf.RoundToInt(v.CurrentPosition.y));
                    if (vPos == crashPos && v.Visual != null)
                    {
                        var level = LevelModel?.CurrentLevel;
                        BouncyPhysicsConfigAsset physicsAsset = null;

                        if (level?.bouncyPhysicsConfig != null)
                        {
                            physicsAsset = level.bouncyPhysicsConfig;
                        }
                        else if (BouncyPhysicsConfig != null)
                        {
                            physicsAsset = BouncyPhysicsConfig;
                        }

                        if (physicsAsset != null)
                        {
                            BouncyCollisionHandler.ApplyBouncyBounce(v.Visual, Vector3.up, physicsAsset);
                        }
                    }
                }
            }

            HapticService?.Vibrate(HapticType.Warning);
            AudioService?.PlaySfx(SfxType.Crash);

            SignalBus.Fire(new CrashDetectedSignal
            {
                Position = crashPos,
                ColorA = colorA,
                ColorB = colorB
            });

            // Transition state back to GameState.Playing so simulation stops, Toast is shown, and player can 1-tap Undo or edit path
            StopSimulationPhase();
        }

        private void UpdateCompletionTimer(float deltaTime)
        {
            _simulationPhaseTimer += deltaTime;
            
            // Maksimum güvenlik limiti (darboğaz durumlarında kilitlenmeyi önlemek için)
            float maxDuration = _maxSimulationSafetyDuration;
            float remaining = Mathf.Max(0f, maxDuration - _simulationPhaseTimer);
            GameSessionModel.SetSimulationTimer(remaining);

            // Flow Score kazanma kontrolü
            if (GameSessionModel != null && GameSessionModel.CurrentFlowScore >= GameSessionModel.TargetFlowScore)
            {
                LoggerService?.Log($"[PixelFlow.VehicleSimulator] Flow score threshold reached: {GameSessionModel.CurrentFlowScore} / {GameSessionModel.TargetFlowScore}. Completing level.");
                CompleteLevel();
            }
            else if (_simulationPhaseTimer >= maxDuration)
            {
                // Güvenlik zaman aşımı durumunda (kazasız ama akış yetersiz)
                LoggerService?.LogWarning($"[PixelFlow.VehicleSimulator] Simulation safety timeout reached ({maxDuration}s). Flow score achieved: {GameSessionModel.CurrentFlowScore}/{GameSessionModel.TargetFlowScore}. Returning to playing state due to grid congestion.");
                StopSimulationPhase();
            }
        }

        private void CompleteLevel()
        {
            LoggerService?.Log($"[PixelFlow.VehicleSimulator] Simulation completed successfully with no crashes! LEVEL COMPLETED! Target: {GameSessionModel.TargetFlowScore}, Flow achieved: {GameSessionModel.CurrentFlowScore}.");

            GameStateModel.SetState(GameState.LevelCompleted);
            HapticService?.Vibrate(HapticType.Success);
            AudioService?.PlaySfx(SfxType.LevelComplete);
            SignalBus.Fire(new LevelCompletedSignal());

            ClearAllVehicles();
        }


    }
}
