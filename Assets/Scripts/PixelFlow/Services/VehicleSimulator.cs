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
        [Inject, OptionalInject] public IInventoryModel InventoryModel { get; set; }

        private static readonly ColorType[] AllColors;

        static VehicleSimulator()
        {
            var values = System.Enum.GetValues(typeof(ColorType));
            AllColors = new ColorType[values.Length];
            for (int i = 0; i < values.Length; i++)
                AllColors[i] = (ColorType)values.GetValue(i);
        }

        private readonly List<VehicleInstance> _activeVehicles = new List<VehicleInstance>();
        private readonly Dictionary<ColorType, float> _spawnTimers = new Dictionary<ColorType, float>();
        private readonly Dictionary<ColorType, (Vector2Int, Vector2Int)> _cachedEndpoints = new Dictionary<ColorType, (Vector2Int, Vector2Int)>();
        private Transform _vehicleContainer;
        private Transform _cachedGridView;
        private CameraController _cachedCameraController;
        private VehicleMovementService _movementService;

        // Grid-based spatial partitioning collision detection — List pool for GC alloc reduction
        private readonly Dictionary<Vector2Int, List<VehicleInstance>> _cellOccupancy = new Dictionary<Vector2Int, List<VehicleInstance>>();
        private readonly List<List<VehicleInstance>> _occupancyListPool = new List<List<VehicleInstance>>();

        private float _simulationPhaseTimer = 0f;
        private float _fixedAccumulator = 0f;
        private float FixedTimeStep => Config != null ? Config.FixedTimeStep : throw new Data.DataValidationException("GameConfig.FixedTimeStep erişilemedi!");
        private float ConfigVehicleSpeed => Config != null ? Config.VehicleSpeed : throw new Data.DataValidationException("GameConfig.VehicleSpeed erişilemedi!");
        private float ConfigSpawnInterval => Config != null ? Config.SpawnInterval : throw new Data.DataValidationException("GameConfig.SpawnInterval erişilemedi!");
        private int ConfigSpawnCheckInterval => Config != null ? Config.SpawnCheckInterval : throw new Data.DataValidationException("GameConfig.SpawnCheckInterval erişilemedi!");
        private ISignalSubscription _undoSubscription;
        private ISignalSubscription _redoSubscription;
        private ISignalSubscription _levelFailedSubscription;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (Application.isPlaying)
            {
                TickService?.RegisterTickable(this);

                _vehicleContainer = new GameObject("[Vehicles]").transform;
                _vehicleContainer.gameObject.hideFlags = HideFlags.DontSave;

                // Parenting sırasını düzelt: GridView objesini DI provider üzerinden bul
                var gridTransform = GridViewProvider?.GridTransform;
                if (gridTransform != null)
                {
                    _vehicleContainer.SetParent(gridTransform, false);
                }

                // CameraController'ı önbellekle (TriggerCrash'te GetComponent çağırmamak için)
                _cachedCameraController = CamProvider?.MainCamera?.GetComponent<CameraController>();

                // Hareket servisini oluştur (VehicleSimulator'da kalmak yerine ayrı bir servis)
                _movementService = new VehicleMovementService(
                    GridModel, GameStateModel, GameSessionModel,
                    SignalBus, AudioService, ObstacleService, Config);
            }

            if (GameStateModel != null)
                GameStateModel.OnStateChanged += HandleStateChanged;

            _undoSubscription = SignalBus.Subscribe<UndoSignal>(sig => ClearAllVehicles());
            _redoSubscription = SignalBus.Subscribe<RedoSignal>(sig => ClearAllVehicles());

            // GDD §8: LevelFailed sinyalini dinle — state geçişi ve temizlik
            _levelFailedSubscription = SignalBus.Subscribe<LevelFailedSignal>(sig =>
            {
                ClearAllVehicles();
                GameStateModel.SetState(GameState.LevelFailed);
            });

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

            // ── Fixed timestep accumulation ──
            // Obstacle ticks and vehicle movement run at a fixed 60Hz rate
            // regardless of frame rate. This ensures consistent simulation
            // behavior on mobile devices with variable FPS.
            _fixedAccumulator += deltaTime;
            _fixedAccumulator = Mathf.Min(_fixedAccumulator, FixedTimeStep * 5); // Cap: prevent spiral of death

            while (_fixedAccumulator >= FixedTimeStep)
            {
                ObstacleService?.Tick(FixedTimeStep);
                _movementService?.UpdateMovement(_activeVehicles, FixedTimeStep);
                _fixedAccumulator -= FixedTimeStep;
            }

            // Spawn timing, collision detection, and completion timer use real deltaTime
            // (these are timer-based, not physics-based)
            UpdateSpawning(deltaTime);

            // Remove vehicles whose path has been modified by the player
            for (int i = _activeVehicles.Count - 1; i >= 0; i--)
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
            
            if (state == GameState.Playing || state == GameState.Simulating)
            {
                UpdateCollisionDetection();
            }
            if (state == GameState.Simulating)
            {
                UpdateCompletionTimer(deltaTime);
            }
        }

        // Frame skip counter: boş frame'lerde spawn kontrolünü atla
        private int _spawnSkipCounter;

        private void UpdateSpawning(float deltaTime)
        {
            // Hiç aktif araç yoksa ve simülasyon çalışmıyorsa spawn kontrolünü seyrelt
            bool isSimulating = GameStateModel.CurrentState == GameState.Simulating;
            if (_activeVehicles.Count == 0 && !isSimulating)
            {
                _spawnSkipCounter++;
                if (_spawnSkipCounter < ConfigSpawnCheckInterval)
                    return;
            }
            _spawnSkipCounter = 0;

            for (int i = 0; i < AllColors.Length; i++)
            {
                var color = AllColors[i];
                if (color == ColorType.None) continue;

                if (IsColorConnected(color))
                {
                    float spawnInterval = ConfigSpawnInterval;
                    if (!_spawnTimers.ContainsKey(color))
                    {
                        _spawnTimers[color] = spawnInterval; // Spawn first vehicle immediately
                    }

                    _spawnTimers[color] += deltaTime;
                    if (_spawnTimers[color] >= spawnInterval)
                    {
                        _spawnTimers[color] = 0f;
                        SpawnVehicle(color);
                    }
                }
                else
                {
                    _spawnTimers.Remove(color);
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

                Vector2Int n1 = new Vector2Int(-1, -1), n2 = new Vector2Int(-1, -1);
                int found = 0;
                for (int i = 0; i < currentLevel.initialNodes.Count; i++)
                {
                    if (currentLevel.initialNodes[i].color == color)
                    {
                        if (found == 0) n1 = currentLevel.initialNodes[i].position;
                        else if (found == 1) n2 = currentLevel.initialNodes[i].position;
                        found++;
                    }
                }
                if (found != 2) return false;
                endpoints = (n1, n2);
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
            string equippedSkin = InventoryModel?.GetEquippedSkin(color) ?? "skin_default";
            LoggerService?.Log($"[PixelFlow.VehicleSimulator] Spawning {vehicleStyle} with equipped skin '{equippedSkin}' for color {color}");
            
            Transform loco = null, wagon1 = null, wagon2 = null, coupler1 = null, coupler2 = null;
            List<Renderer> renderers = vehicleStyle == VehicleStyle.Train 
                ? VehicleVisualFactory.CreateTrain3D(visual, color, out loco, out wagon1, out wagon2, out coupler1, out coupler2) 
                : VehicleVisualFactory.CreateCar3D(visual, color);

            var inst = new VehicleInstance
            {
                Color = color,
                Style = vehicleStyle,
                // Path'in KOPYASINI al — referans değil!
                // Aksi halde kullanıcı çizimi değiştirince daha önce spawnlanmış
                // araçların Path'i de değişir (aynı List referansı) → teleportasyon
                Path = new List<Vector2Int>(path),
                SegmentIndex = 0,
                Progress = 0f,
                Visual = visual,
                CurrentPosition = new Vector3(path[0].x, path[0].y, GetZOffset(path[0], color)),
                Speed = ConfigVehicleSpeed + UnityEngine.Random.Range(-Config.SpeedVariationRange, Config.SpeedVariationRange),
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
                if (cell.HasViaduct && cell.OverColor == color)
                {
                    return Config.ViaductOverZOffset;
                }
                if (cell.HasViaduct && cell.UnderColor == color)
                {
                    return Config.ViaductUnderZOffset;
                }
            }
            return Config.NormalZOffset;
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

            float collisionDist = Config.CollisionDistance;
            float sqrDist = (v1.CurrentPosition - v2.CurrentPosition).sqrMagnitude;
            if (sqrDist >= collisionDist * collisionDist) return false;

            var cell = GridModel.Grid[
                Mathf.Clamp(cellPos.x, 0, GridModel.Width - 1),
                Mathf.Clamp(cellPos.y, 0, GridModel.Height - 1)];

            if (cell.HasViaduct)
            {
                float zDiff = Mathf.Abs(v1.CurrentPosition.z - v2.CurrentPosition.z);
                if (zDiff >= Config.ViaductZDiffThreshold) return false;
            }

            TriggerCrash(cellPos, v1.Color, v2.Color);
            return true;
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
                        var physics = LevelModel?.CurrentLevel != null
                            ? LevelModel.CurrentLevel.bouncyPhysics
                            : PixelFlow.Data.BouncyPhysicsConfig.Default;
                        BouncyCollisionHandler.ApplyBouncyBounce(v.Visual, Vector3.up, physics);
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
        }

        private void UpdateCompletionTimer(float deltaTime)
        {
            _simulationPhaseTimer += deltaTime;
            
            // Maksimum güvenlik limiti (darboğaz durumlarında kilitlenmeyi önlemek için)
            float maxDuration = Config != null ? Config.MaxSimulationSafetyDuration : throw new Data.DataValidationException("GameConfig.MaxSimulationSafetyDuration erişilemedi!");
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
