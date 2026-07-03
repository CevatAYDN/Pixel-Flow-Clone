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
        [Inject] public IHapticService HapticService { get; set; }
        [Inject] public IAudioService AudioService { get; set; }
        [Inject] public IObstacleService ObstacleService { get; set; }

        private class VehicleInstance
        {
            public ColorType Color;
            public List<Vector2Int> Path;
            public int SegmentIndex;
            public float Progress;
            public GameObject Visual;
            public Vector3 CurrentPosition;
            public float Speed;
            public Renderer[] CachedRenderers;
        }

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

        private float _simulationPhaseTimer = 0f;
        private const float SimulationPhaseDuration = 10f;
        private const float VehicleSpeed = 3f;
        private const float SpawnInterval = 1.2f;
        private ISignalSubscription _undoSubscription;
        private ISignalSubscription _redoSubscription;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
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
                _cachedEndpoints.Clear();
                ClearAllVehicles();
            }
            else if (state == GameState.Simulating)
            {
                _simulationPhaseTimer = 0f;
                _cachedEndpoints.Clear();
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
                    if (v.CachedRenderers != null)
                    {
                        for (int i = 0; i < v.CachedRenderers.Length; i++)
                        {
                            if (v.CachedRenderers[i]?.material != null)
                                UnityEngine.Object.Destroy(v.CachedRenderers[i].material);
                        }
                    }
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

            if (ObstacleService != null)
            {
                ObstacleService.Tick(Time.deltaTime);
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
            for (int i = 0; i < AllColors.Length; i++)
            {
                var color = AllColors[i];
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

            if (!_cachedEndpoints.TryGetValue(color, out var endpoints))
            {
                var currentLevel = LevelModel.CurrentLevel;
                if (currentLevel?.initialNodes == null) return false;

                Vector2Int n1 = new Vector2Int(-1, -1), n2 = new Vector2Int(-1, -1);
                int found = 0;
                for (int i = 0; i < currentLevel.initialNodes.Count && found < 2; i++)
                {
                    if (currentLevel.initialNodes[i].color == color)
                    {
                        if (found == 0) n1 = currentLevel.initialNodes[i].position;
                        else n2 = currentLevel.initialNodes[i].position;
                        found++;
                    }
                }
                if (found < 2) return false;
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

            if (_vehicleContainer != null && _vehicleContainer.parent == null)
            {
                if (_cachedGridView == null)
                    _cachedGridView = UnityEngine.Object.FindAnyObjectByType<GridView>();
                if (_cachedGridView != null)
                    _vehicleContainer.SetParent(_cachedGridView.transform, false);
            }

            GameObject visual = new GameObject($"V_{color}");
            visual.transform.SetParent(_vehicleContainer);

            List<Renderer> renderers = CreateProceduralVehicle3D(visual, color);

            var inst = new VehicleInstance
            {
                Color = color,
                Path = path,
                SegmentIndex = 0,
                Progress = 0f,
                Visual = visual,
                CurrentPosition = new Vector3(path[0].x, path[0].y, GetZOffset(path[0], color)),
                Speed = VehicleSpeed + UnityEngine.Random.Range(-0.3f, 0.3f),
                CachedRenderers = renderers.ToArray()
            };

            visual.transform.localPosition = inst.CurrentPosition;
            _activeVehicles.Add(inst);
        }

        private static List<Renderer> CreateProceduralVehicle3D(GameObject root, ColorType color)
        {
            var renderers = new List<Renderer>();
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");

            Color carColor = CellView.GetColor(color);

            // 1. Main Chassis / Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Chassis";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.44f, 0.26f, 0.16f);
            var rBody = body.GetComponent<Renderer>();
            if (rBody != null)
            {
                rBody.material = new Material(shader) { color = carColor };
                renderers.Add(rBody);
            }

            // 2. Cabin / Windshield
            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(root.transform, false);
            cabin.transform.localScale = new Vector3(0.24f, 0.20f, 0.12f);
            cabin.transform.localPosition = new Vector3(-0.03f, 0f, -0.12f);
            var rCabin = cabin.GetComponent<Renderer>();
            if (rCabin != null)
            {
                rCabin.material = new Material(shader) { color = new Color(0.15f, 0.2f, 0.3f, 0.9f) };
                renderers.Add(rCabin);
            }

            // 3. Headlights (Bright Cyan/White at front bumper +X)
            var headL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headL.name = "Headlights";
            headL.transform.SetParent(root.transform, false);
            headL.transform.localScale = new Vector3(0.04f, 0.20f, 0.06f);
            headL.transform.localPosition = new Vector3(0.22f, 0f, -0.02f);
            var rHead = headL.GetComponent<Renderer>();
            if (rHead != null)
            {
                rHead.material = new Material(shader) { color = new Color(0.9f, 1f, 1f, 1f) };
                renderers.Add(rHead);
            }

            // 4. Taillights (Red at rear bumper -X)
            var tailL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tailL.name = "Taillights";
            tailL.transform.SetParent(root.transform, false);
            tailL.transform.localScale = new Vector3(0.04f, 0.20f, 0.05f);
            tailL.transform.localPosition = new Vector3(-0.22f, 0f, -0.02f);
            var rTail = tailL.GetComponent<Renderer>();
            if (rTail != null)
            {
                rTail.material = new Material(shader) { color = new Color(1f, 0.15f, 0.15f, 1f) };
                renderers.Add(rTail);
            }

            // 5. 4 Wheels (Dark Cylinders)
            float[] wx = { -0.14f, 0.14f };
            float[] wy = { -0.12f, 0.12f };
            foreach (float x in wx)
            {
                foreach (float y in wy)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.name = "Wheel";
                    wheel.transform.SetParent(root.transform, false);
                    wheel.transform.localScale = new Vector3(0.09f, 0.02f, 0.09f);
                    wheel.transform.localPosition = new Vector3(x, y, 0.06f);
                    wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var rWheel = wheel.GetComponent<Renderer>();
                    if (rWheel != null)
                    {
                        rWheel.material = new Material(shader) { color = new Color(0.12f, 0.12f, 0.14f, 1f) };
                        renderers.Add(rWheel);
                    }
                }
            }

            return renderers;
        }

        private void UpdateMovement()
        {
            bool isPlaying = GameStateModel.CurrentState == GameState.Playing;
            float baseAlpha = isPlaying ? (0.45f + Mathf.Sin(Time.time * 6f) * 0.25f) : 1f;

            for (int i = _activeVehicles.Count - 1; i >= 0; i--)
            {
                var v = _activeVehicles[i];
                if (v.Visual == null)
                {
                    _activeVehicles.RemoveAt(i);
                    continue;
                }

                if (v.SegmentIndex > 0 && v.SegmentIndex - 1 < v.Path.Count && ObstacleService != null)
                {
                    Vector2Int prevCell = v.Path[v.SegmentIndex - 1];
                    if (ObstacleService.IsNarrowPass(prevCell))
                    {
                        ObstacleService.OnVehicleLeftNarrowPass(prevCell, v.Color);
                    }
                }
                if (v.SegmentIndex < v.Path.Count && ObstacleService != null)
                {
                    Vector2Int curCell = v.Path[v.SegmentIndex];
                    if (ObstacleService.IsNarrowPass(curCell) && v.Progress < 0.1f)
                    {
                        ObstacleService.OnVehicleEnteredNarrowPass(curCell, v.Color);
                    }
                }

                if (v.CachedRenderers != null)
                {
                    for (int ri = 0; ri < v.CachedRenderers.Length; ri++)
                    {
                        if (v.CachedRenderers[ri] == null) continue;
                        Color c = v.CachedRenderers[ri].material.color;
                        c.a = baseAlpha;
                        v.CachedRenderers[ri].material.color = c;
                    }
                }

                v.Progress += v.Speed * Time.deltaTime;
                if (v.Progress >= 1f)
                {
                    v.Progress = 0f;
                    v.SegmentIndex++;
                }

                if (v.SegmentIndex >= v.Path.Count - 1)
                {
                    if (ObstacleService != null && v.Path.Count > 0)
                    {
                        Vector2Int endCell = v.Path[v.Path.Count - 1];
                        if (ObstacleService.IsNarrowPass(endCell))
                        {
                            ObstacleService.OnVehicleLeftNarrowPass(endCell, v.Color);
                        }
                    }
                    if (v.CachedRenderers != null)
                    {
                        for (int ri = 0; ri < v.CachedRenderers.Length; ri++)
                        {
                            if (v.CachedRenderers[ri]?.material != null)
                                UnityEngine.Object.Destroy(v.CachedRenderers[ri].material);
                        }
                    }
                    UnityEngine.Object.Destroy(v.Visual);
                    _activeVehicles.RemoveAt(i);
                    continue;
                }

                Vector3 p0 = GetSplineControlPoint(v.Path, v.SegmentIndex - 1, v.Color);
                Vector3 p1 = GetSplineControlPoint(v.Path, v.SegmentIndex, v.Color);
                Vector3 p2 = GetSplineControlPoint(v.Path, v.SegmentIndex + 1, v.Color);
                Vector3 p3 = GetSplineControlPoint(v.Path, v.SegmentIndex + 2, v.Color);

                Vector3 basePos = CatmullRom(p0, p1, p2, p3, v.Progress);

                Vector3 tangent = CatmullRomTangent(p0, p1, p2, p3, v.Progress);
                Vector3 nextTangent = CatmullRomTangent(p0, p1, p2, p3, Mathf.Min(v.Progress + 0.1f, 1f));

                float baseAngle = tangent.sqrMagnitude > 0.001f ? Mathf.Atan2(tangent.y, tangent.x) : 0f;
                float nextAngle = nextTangent.sqrMagnitude > 0.001f ? Mathf.Atan2(nextTangent.y, nextTangent.x) : baseAngle;
                float deltaAngle = Mathf.DeltaAngle(baseAngle, nextAngle);

                float cornerProximity = Mathf.Min(v.Progress, 1f - v.Progress);
                bool isCorner = cornerProximity < 0.25f && Mathf.Abs(deltaAngle) > 10f;

                if (isCorner)
                {
                    float overshootT = cornerProximity < 0.12f
                        ? Mathf.Sin((cornerProximity / 0.12f) * Mathf.PI) * 0.18f
                        : 0f;

                    Vector3 perp = new Vector3(-tangent.y, tangent.x, 0f).normalized;
                    if (perp.sqrMagnitude > 0.001f)
                    {
                        float leanDir = deltaAngle > 0 ? -1f : 1f;
                        basePos += perp * overshootT * leanDir;
                    }

                    if (v.Progress < 0.12f)
                    {
                        float settleT = v.Progress / 0.12f;
                        Vector3 prevTangent = CatmullRomTangent(p0, p1, p2, p3, 0f);
                        Vector3 overshoot = prevTangent.normalized * 0.15f * (1f - settleT);
                        basePos += new Vector3(overshoot.x, overshoot.y, 0f);
                    }
                }

                v.CurrentPosition = basePos;

                float bobbing = Mathf.Sin(Time.time * 12f + v.GetHashCode()) * 0.02f;
                Vector3 finalPos = v.CurrentPosition;
                finalPos.z += bobbing;
                v.Visual.transform.localPosition = finalPos;

                Vector3 tangent2 = CatmullRomTangent(p0, p1, p2, p3, v.Progress);
                if (tangent2.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(tangent2.y, tangent2.x) * Mathf.Rad2Deg;
                    float rollBank = Mathf.Clamp(-deltaAngle * 0.5f, -20f, 20f);
                    v.Visual.transform.rotation = Quaternion.Euler(rollBank, 0f, angle);
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

            GridModel.LastCrashPosition = crashPos;
            GridModel.CrashColorA = colorA;
            GridModel.CrashColorB = colorB;

            var camCtrl = UnityEngine.Object.FindAnyObjectByType<CameraController>();
            if (camCtrl != null)
            {
                camCtrl.FocusOnCrash(crashPos);
            }

            GameStateModel.SetState(GameState.Paused);

            HapticService?.Vibrate(HapticType.Warning);
            AudioService?.PlaySfx(SfxType.Crash);

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
            float remaining = SimulationPhaseDuration - _simulationPhaseTimer;
            GameSessionModel.SetSimulationTimer(remaining);

            if (_simulationPhaseTimer >= SimulationPhaseDuration)
            {
                CompleteLevel();
            }
        }

        private void CompleteLevel()
        {
            Debug.Log("[VehicleSimulator] Simulation completed successfully with no crashes! LEVEL COMPLETED!");

            GameStateModel.SetState(GameState.LevelCompleted);
            HapticService?.Vibrate(HapticType.Success);
            AudioService?.PlaySfx(SfxType.LevelComplete);
            SignalBus.Fire(new LevelCompletedSignal());

            ClearAllVehicles();
        }

        private Vector3 GetSplineControlPoint(List<Vector2Int> path, int index, ColorType color)
        {
            int clamped = Mathf.Clamp(index, 0, path.Count - 1);
            Vector2Int pos = path[clamped];
            return new Vector3(pos.x, pos.y, GetZOffset(pos, color));
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * ((2f * p1) +
                           (-p0 + p2) * t +
                           (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private static Vector3 CatmullRomTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            return 0.5f * ((-p0 + p2) +
                           2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t +
                           3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2);
        }
    }
}
