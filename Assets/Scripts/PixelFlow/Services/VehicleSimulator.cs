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

namespace PixelFlow.Services
{
    public interface IVehicleSimulator
    {
        void StartSimulationPhase();
        void StopSimulationPhase();
        void ClearAllVehicles();
    }

    public class VehicleSimulator : IVehicleSimulator, INexusService
    {
        [Inject] public IGridModel GridModel { get; set; }
        [Inject] public ILevelModel LevelModel { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public IGameSessionModel GameSessionModel { get; set; }
        [Inject] public ISignalBus SignalBus { get; set; }
        [Inject] public IHintModel HintModel { get; set; }

        private class VehicleInstance
        {
            public ColorType Color;
            public List<Vector2Int> Path;
            public int SegmentIndex;
            public float Progress; // 0 to 1
            public GameObject Visual;
            public Vector3 CurrentPosition;
            public float Speed;
        }

        private readonly List<VehicleInstance> _activeVehicles = new List<VehicleInstance>();
        private readonly Dictionary<ColorType, float> _spawnTimers = new Dictionary<ColorType, float>();
        private SimulationUpdater _updater;
        private Transform _vehicleContainer;

        private float _simulationPhaseTimer = 0f;
        private const float SimulationPhaseDuration = 10f; // Win after 10s of no crashes
        private const float VehicleSpeed = 3f; // Cells per second
        private const float SpawnInterval = 1.2f; // Seconds between spawns

        private static Sprite _arrowSprite;
        private ISignalSubscription _undoSubscription;
        private ISignalSubscription _redoSubscription;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            // Create a hidden updater game object to receive Unity's Update loop
            GameObject updaterObj = new GameObject("[VehicleSimulatorUpdater]");
            updaterObj.hideFlags = HideFlags.DontSave;
            _updater = updaterObj.AddComponent<SimulationUpdater>();
            _updater.OnUpdate = Update;

            _vehicleContainer = new GameObject("[Vehicles]").transform;
            _vehicleContainer.gameObject.hideFlags = HideFlags.DontSave;

            GameStateModel.OnStateChanged += HandleStateChanged;

            _undoSubscription = SignalBus.Subscribe<UndoSignal>(sig => ClearAllVehicles());
            _redoSubscription = SignalBus.Subscribe<RedoSignal>(sig => ClearAllVehicles());

            return default;
        }

        public void OnDispose()
        {
            if (_updater != null)
            {
                UnityEngine.Object.Destroy(_updater.gameObject);
            }
            if (_vehicleContainer != null)
            {
                UnityEngine.Object.Destroy(_vehicleContainer.gameObject);
            }
            GameStateModel.OnStateChanged -= HandleStateChanged;
            _undoSubscription?.Dispose();
            _redoSubscription?.Dispose();
            ClearAllVehicles();
        }

        private void HandleStateChanged(GameState state)
        {
            // Eğer duraklatılmış (Paused) durumdan çıkıyorsak (unpausing), araçları temizleme!
            if (GameStateModel.PreviousState == GameState.Paused)
            {
                if (state == GameState.Playing)
                {
                    _simulationPhaseTimer = 0f;
                }
                return;
            }

            if (state == GameState.Playing)
            {
                _simulationPhaseTimer = 0f;
                ClearAllVehicles();
            }
            else if (state == GameState.Simulating)
            {
                _simulationPhaseTimer = 0f;
                ClearAllVehicles();
                Debug.Log("[VehicleSimulator] Simulation Phase started. All vehicles now solid.");
            }
            else if (state == GameState.MainMenu || state == GameState.LevelCompleted)
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
                    UnityEngine.Object.Destroy(v.Visual);
                }
            }
            _activeVehicles.Clear();
            _spawnTimers.Clear();
        }

        private void Update()
        {
            var state = GameStateModel.CurrentState;
            if (state != GameState.Playing && state != GameState.Simulating)
            {
                return;
            }

            UpdateSpawning();
            UpdateMovement();
            
            UpdateCollisionDetection();
            
            if (state == GameState.Simulating)
            {
                UpdateCompletionTimer();
            }
        }

        private void UpdateSpawning()
        {
            foreach (ColorType color in Enum.GetValues(typeof(ColorType)))
            {
                if (color == ColorType.None) continue;

                if (IsColorConnected(color))
                {
                    if (!_spawnTimers.ContainsKey(color))
                    {
                        _spawnTimers[color] = SpawnInterval; // Spawn first vehicle immediately
                    }

                    _spawnTimers[color] += Time.deltaTime;
                    if (_spawnTimers[color] >= SpawnInterval)
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

            var currentLevel = LevelModel.CurrentLevel;
            if (currentLevel == null || currentLevel.initialNodes == null)
                return false;

            // Find endpoints for this color in LevelData
            Vector2Int node1 = new Vector2Int(-1, -1);
            Vector2Int node2 = new Vector2Int(-1, -1);
            int count = 0;
            foreach (var n in currentLevel.initialNodes)
            {
                if (n.color == color)
                {
                    if (count == 0) node1 = n.position;
                    else if (count == 1) node2 = n.position;
                    count++;
                }
            }

            if (count < 2) return false;

            Vector2Int start = path[0];
            Vector2Int end = path[path.Count - 1];

            return (start == node1 && end == node2) || (start == node2 && end == node1);
        }

        private void SpawnVehicle(ColorType color)
        {
            if (!GridModel.Paths.TryGetValue(color, out var path) || path.Count < 2)
                return;

            // Align the vehicles container with the grid's local coordinate system
            if (_vehicleContainer != null && _vehicleContainer.parent == null)
            {
                var gridView = UnityEngine.Object.FindAnyObjectByType<GridView>();
                if (gridView != null)
                {
                    _vehicleContainer.SetParent(gridView.transform, false);
                }
            }

            GameObject visual = new GameObject($"Vehicle_{color}");
            visual.transform.SetParent(_vehicleContainer);
            
            // Create a small 3D blocky vehicle for visibility in both 2D and 3D camera angles
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(visual.transform, false);
            var r = body.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = CellView.GetColor(color);
            }

            body.transform.localScale = new Vector3(0.4f, 0.4f, 0.25f);
            Debug.Log($"[VehicleSimulator] Spawned vehicle of color {color} at start node: {path[0]}");

            var inst = new VehicleInstance
            {
                Color = color,
                Path = path,
                SegmentIndex = 0,
                Progress = 0f,
                Visual = visual,
                CurrentPosition = new Vector3(path[0].x, path[0].y, GetZOffset(path[0], color)),
                Speed = VehicleSpeed + UnityEngine.Random.Range(-0.4f, 0.4f)
            };

            visual.transform.localPosition = inst.CurrentPosition;
            _activeVehicles.Add(inst);
        }

        private void UpdateMovement()
        {
            float alpha = GameStateModel.CurrentState == GameState.Playing ? 0.6f : 1f;

            for (int i = _activeVehicles.Count - 1; i >= 0; i--)
            {
                var v = _activeVehicles[i];
                if (v.Visual == null)
                {
                    _activeVehicles.RemoveAt(i);
                    continue;
                }

                // Update opacity
                var sr = v.Visual.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = alpha;
                    sr.color = c;
                }

                // Advance segment progress
                v.Progress += v.Speed * Time.deltaTime;
                if (v.Progress >= 1f)
                {
                    v.Progress = 0f;
                    v.SegmentIndex++;
                }

                if (v.SegmentIndex >= v.Path.Count - 1)
                {
                    // Reached destination, despawn
                    UnityEngine.Object.Destroy(v.Visual);
                    _activeVehicles.RemoveAt(i);
                    continue;
                }

                // Calculate position along segment
                Vector2Int p1 = v.Path[v.SegmentIndex];
                Vector2Int p2 = v.Path[v.SegmentIndex + 1];

                Vector3 startPos = new Vector3(p1.x, p1.y, GetZOffset(p1, v.Color));
                Vector3 endPos = new Vector3(p2.x, p2.y, GetZOffset(p2, v.Color));

                v.CurrentPosition = Vector3.Lerp(startPos, endPos, v.Progress);
                v.Visual.transform.localPosition = v.CurrentPosition;

                // Point in direction of movement
                Vector3 dir = endPos - startPos;
                if (dir.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    v.Visual.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
            }
        }

        private float GetZOffset(Vector2Int gridPos, ColorType color)
        {
            if (gridPos.x >= 0 && gridPos.x < GridModel.Width && gridPos.y >= 0 && gridPos.y < GridModel.Height)
            {
                var cell = GridModel.Grid[gridPos.x, gridPos.y];
                if (cell.HasViaduct && cell.OverColor == color)
                {
                    return -0.4f; // Yükseltilmiş yol
                }
            }
            return -0.1f; // Normal yol
        }

        private void UpdateCollisionDetection()
        {
            // Check for physical distance collisions between vehicles (distance < 0.5f)
            for (int i = 0; i < _activeVehicles.Count; i++)
            {
                var v1 = _activeVehicles[i];

                for (int j = i + 1; j < _activeVehicles.Count; j++)
                {
                    var v2 = _activeVehicles[j];
                    if (v1.Color == v2.Color) continue;

                    float dist = Vector3.Distance(v1.CurrentPosition, v2.CurrentPosition);
                    const float collisionThreshold = 0.5f;

                    if (dist < collisionThreshold)
                    {
                        Vector2Int cellPos = new Vector2Int(
                            Mathf.RoundToInt(v1.CurrentPosition.x),
                            Mathf.RoundToInt(v1.CurrentPosition.y));

                        if (cellPos.x >= 0 && cellPos.x < GridModel.Width &&
                            cellPos.y >= 0 && cellPos.y < GridModel.Height)
                        {
                            var cell = GridModel.Grid[cellPos.x, cellPos.y];
                            if (!cell.HasViaduct)
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

        private void TriggerCrash(Vector2Int crashPos, ColorType colorA, ColorType colorB)
        {
            Debug.LogError($"[VehicleSimulator] TRAFFIC CRASH detected at {crashPos} between {colorA} and {colorB}!");
            
            // Simülasyonu durdur
            GameStateModel.SetState(GameState.Paused);

            // Crash sinyalini ateşle (UI bunu dinleyip Kriz Ekranı açacak)
            SignalBus.Fire(new CrashDetectedSignal
            {
                Position = crashPos,
                ColorA = colorA,
                ColorB = colorB
            });
        }

        private void UpdateCompletionTimer()
        {
            _simulationPhaseTimer += Time.deltaTime;
            if (_simulationPhaseTimer >= SimulationPhaseDuration)
            {
                CompleteLevel();
            }
        }

        private void CompleteLevel()
        {
            Debug.Log("[VehicleSimulator] Simulation completed successfully with no crashes! LEVEL COMPLETED!");
            
            GameStateModel.SetState(GameState.LevelCompleted);
            SignalBus.Fire(new LevelCompletedSignal());
            
            ClearAllVehicles();
        }

        private static Sprite GetArrowSprite()
        {
            if (_arrowSprite == null)
            {
                int size = 64;
                Texture2D tex = new Texture2D(size, size);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        tex.SetPixel(x, y, Color.clear);

                // Draw arrowhead pointing right (0 degrees)
                for (int x = 16; x < 48; x++)
                {
                    int halfHeight = Mathf.RoundToInt((x - 16) * 0.5f);
                    for (int y = 32 - halfHeight; y <= 32 + halfHeight; y++)
                    {
                        if (y >= 0 && y < size)
                        {
                            tex.SetPixel(x, y, Color.white);
                        }
                    }
                }

                tex.Apply();
                tex.hideFlags = HideFlags.DontSave;
                _arrowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                _arrowSprite.hideFlags = HideFlags.DontSave;
            }
            return _arrowSprite;
        }
    }
}
