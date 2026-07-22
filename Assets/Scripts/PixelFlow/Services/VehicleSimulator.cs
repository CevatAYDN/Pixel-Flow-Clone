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
    /// Araç simülasyonunun çekirdek mantığı: spawn, movement, collision detection, timer.
    /// Görsel üretim VehicleVisualFactory'e, veri modeli VehicleInstance'e ayrıldı.
    /// </summary>
    public class VehicleSimulator : IVehicleSimulator, INexusService
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
        [Inject] public Data.GameConfig Config { get; set; }

        private ICrisisAdService _crisisAdService => CrisisAdService;

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
        private SimulationUpdater _updater;
        private Transform _vehicleContainer;
        private GridView _cachedGridView;
        private CameraController _cachedCameraController;
        private VehicleMovementService _movementService;

        // Grid-based spatial partitioning collision detection — List pool for GC alloc reduction
        private readonly Dictionary<Vector2Int, List<VehicleInstance>> _cellOccupancy = new Dictionary<Vector2Int, List<VehicleInstance>>();
        private readonly List<List<VehicleInstance>> _occupancyListPool = new List<List<VehicleInstance>>();

        private float _simulationPhaseTimer = 0f;
        private float ConfigVehicleSpeed => Config != null ? Config.VehicleSpeed : 3f;
        private float ConfigSpawnInterval => Config != null ? Config.SpawnInterval : 1.2f;
        private ISignalSubscription _undoSubscription;
        private ISignalSubscription _redoSubscription;
        private ISignalSubscription _levelFailedSubscription;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (Application.isPlaying)
            {
                GameObject updaterObj = new GameObject("[VehicleSimulatorUpdater]");
                updaterObj.hideFlags = HideFlags.DontSave;
                _updater = updaterObj.AddComponent<SimulationUpdater>();
                _updater.OnUpdate = Update;

                _vehicleContainer = new GameObject("[Vehicles]").transform;
                _vehicleContainer.gameObject.hideFlags = HideFlags.DontSave;

                // Parenting sırasını düzelt: GridView objesini bulup onun transform'u altında topla
                _cachedGridView = UnityEngine.Object.FindAnyObjectByType<GridView>();
                if (_cachedGridView != null)
                {
                    _vehicleContainer.SetParent(_cachedGridView.transform, false);
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
            if (_updater != null)
            {
                SafeDestroy(_updater.gameObject);
            }
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
            // Eğer duraklatılmış (Paused) durumdan çıkıyorsak (unpausing), araçları temizleme!
            if (GameStateModel.PreviousState == GameState.Paused)
            {
                return;
            }

            if (state == GameState.Playing)
            {
                _simulationPhaseTimer = 0f;
                _cachedEndpoints.Clear();
                ClearAllVehicles();
            }
            else if (state == GameState.Simulating)
            {
                _simulationPhaseTimer = 0f;
                _cachedEndpoints.Clear();
                // ClearAllVehicles() kaldırıldı - araçlar yok edilmeden pürüzsüzce hayaletten katı moda geçecek
                LoggerService?.Log("[VehicleSimulator] Simulation Phase started. All vehicles now transition to solid.");
            }
            else if (state == GameState.MainMenu || state == GameState.LevelCompleted || state == GameState.LevelFailed)
            {
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
        }

        private void Update()
        {
            if (GameStateModel == null) return;
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            var state = GameStateModel.CurrentState;
            if (state != GameState.Playing && state != GameState.Simulating)
                return;

            // Fire TimerTickSignal every frame so GameSessionModel.ElapsedTime stays accurate
            SignalBus.Fire(new PixelFlow.Signals.TimerTickSignal());

            if (ObstacleService != null)
            {
                ObstacleService.Tick(deltaTime);
            }

            UpdateSpawning(deltaTime);
            _movementService?.UpdateMovement(_activeVehicles, deltaTime);
            
            // Collision detection runs in BOTH Playing and Simulating states per GDD §2.4
            if (state == GameState.Playing || state == GameState.Simulating)
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
                    return;
            }

            if (_cachedGridView == null)
                _cachedGridView = UnityEngine.Object.FindAnyObjectByType<GridView>();

            if (_vehicleContainer != null && _vehicleContainer.parent == null && _cachedGridView != null)
                _vehicleContainer.SetParent(_cachedGridView.transform, false);
            Transform parentTransform = _cachedGridView != null ? _cachedGridView.transform : _vehicleContainer;

            GameObject visual = new GameObject($"V_{color}");
            visual.transform.SetParent(parentTransform);

            VehicleStyle vehicleStyle = SettingsModel.CurrentVehicleStyle;
            
            Transform loco = null, wagon1 = null, wagon2 = null, coupler1 = null, coupler2 = null;
            List<Renderer> renderers = vehicleStyle == VehicleStyle.Train 
                ? VehicleVisualFactory.CreateTrain3D(visual, color, out loco, out wagon1, out wagon2, out coupler1, out coupler2) 
                : VehicleVisualFactory.CreateCar3D(visual, color);

            var inst = new VehicleInstance
            {
                Color = color,
                Style = vehicleStyle,
                Path = path,
                SegmentIndex = 0,
                Progress = 0f,
                Visual = visual,
                CurrentPosition = new Vector3(path[0].x, path[0].y, GetZOffset(path[0], color)),
                Speed = ConfigVehicleSpeed + UnityEngine.Random.Range(-0.3f, 0.3f),
                CachedRenderers = renderers.ToArray(),
                LocoTransform = loco,
                Wagon1Transform = wagon1,
                Wagon2Transform = wagon2,
                Coupler1Transform = coupler1,
                Coupler2Transform = coupler2
            };

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
                    return -0.4f; // Over: Yükseltilmiş yol (GDD §4.4)
                }
                if (cell.HasViaduct && cell.UnderColor == color)
                {
                    return -0.1f; // Under: Alçaltılmış yol (GDD §4.4)
                }
            }
            return -0.2f; // Normal yol
        }

        /// <summary>
        /// Grid-based spatial partitioning collision detection.
        /// Instead of O(n²) brute-force distance checks, vehicles register which cell
        /// they occupy. Collision is checked only between vehicles on the same
        /// or adjacent cells, reducing complexity to O(n × avgDensity) in practice.
        /// </summary>
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

            // Check collisions only on cells with multiple vehicles
            foreach (var kvp in _cellOccupancy)
            {
                var vehicles = kvp.Value;
                if (vehicles.Count < 2) continue;

                for (int i = 0; i < vehicles.Count; i++)
                {
                    for (int j = i + 1; j < vehicles.Count; j++)
                    {
                        var v1 = vehicles[i];
                        var v2 = vehicles[j];
                        if (v1.Color == v2.Color) continue;

                        float sqrDist = (v1.CurrentPosition - v2.CurrentPosition).sqrMagnitude;
                        if (sqrDist < 0.2025f) // 0.45²
                        {
                            Vector2Int cellPos = kvp.Key;
                            var cell = GridModel.Grid[cellPos.x, cellPos.y];

                            if (cell.HasViaduct)
                            {
                                float zDiff = Mathf.Abs(v1.CurrentPosition.z - v2.CurrentPosition.z);
                                if (zDiff < 0.15f)
                                {
                                    TriggerCrash(cellPos, v1.Color, v2.Color);
                                    return;
                                }
                            }
                            else
                            {
                                TriggerCrash(cellPos, v1.Color, v2.Color);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void TriggerCrash(Vector2Int crashPos, ColorType colorA, ColorType colorB)
        {
            LoggerService?.LogError($"[VehicleSimulator] TRAFFIC CRASH detected at {crashPos} between {colorA} and {colorB}!");

            GridModel.LastCrashPosition.Value = crashPos;
            GridModel.CrashColorA.Value = colorA;
            GridModel.CrashColorB.Value = colorB;

            var camCtrl = _cachedCameraController;
            if (camCtrl != null)
            {
                camCtrl.FocusOnCrash(crashPos);
            }

            GameStateModel.SetState(GameState.Paused);

            HapticService?.Vibrate(HapticType.Warning);
            AudioService?.PlaySfx(SfxType.Crash);

            // GDD §2.4: 3 ardışık kaza denemesi → LevelFailed
            _crisisAdService?.RecordCrisisAttempt();

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
            
            // Maksimum 45 saniye güvenlik limiti (darboğaz durumlarında kilitlenmeyi önlemek için)
            float maxDuration = Config != null ? Config.MaxSimulationSafetyDuration : 45f;
            float remaining = Mathf.Max(0f, maxDuration - _simulationPhaseTimer);
            GameSessionModel.SetSimulationTimer(remaining);

            // Flow Score kazanma kontrolü
            if (GameSessionModel != null && GameSessionModel.CurrentFlowScore >= GameSessionModel.TargetFlowScore)
            {
                CompleteLevel();
            }
            else if (_simulationPhaseTimer >= maxDuration)
            {
                // Güvenlik zaman aşımı durumunda (kazasız ama akış yetersiz)
                LoggerService?.LogWarning("[VehicleSimulator] Simulation safety timeout reached. Returning to playing state due to grid congestion.");
                StopSimulationPhase();
            }
        }

        private void CompleteLevel()
        {
            LoggerService?.Log("[VehicleSimulator] Simulation completed successfully with no crashes! LEVEL COMPLETED!");

            GameStateModel.SetState(GameState.LevelCompleted);
            HapticService?.Vibrate(HapticType.Success);
            AudioService?.PlaySfx(SfxType.LevelComplete);
            SignalBus.Fire(new LevelCompletedSignal());

            ClearAllVehicles();
        }


    }
}
