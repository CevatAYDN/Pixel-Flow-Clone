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

        private class VehicleInstance
        {
            public ColorType Color;
            public VehicleStyle Style;
            public List<Vector2Int> Path;
            public int SegmentIndex;
            public float Progress;
            public float TotalDistance;
            public GameObject Visual;
            public Vector3 CurrentPosition;
            public float Speed;
            public Renderer[] CachedRenderers;

            public Transform LocoTransform;
            public Transform Wagon1Transform;
            public Transform Wagon2Transform;
            public Transform Coupler1Transform;
            public Transform Coupler2Transform;

            public readonly MaterialPropertyBlock Mpb = new MaterialPropertyBlock();
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
        private const float MaxProgressPerFrame = 0.25f;
        private ISignalSubscription _undoSubscription;
        private ISignalSubscription _redoSubscription;

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
                var gridView = UnityEngine.Object.FindAnyObjectByType<GridView>();
                if (gridView != null)
                {
                    _vehicleContainer.SetParent(gridView.transform, false);
                }
            }

            if (GameStateModel != null)
                GameStateModel.OnStateChanged += HandleStateChanged;

            _undoSubscription = SignalBus.Subscribe<UndoSignal>(sig => ClearAllVehicles());
            _redoSubscription = SignalBus.Subscribe<RedoSignal>(sig => ClearAllVehicles());

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
                Debug.Log("[VehicleSimulator] Simulation Phase started. All vehicles now transition to solid.");
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
                    SafeDestroy(v.Visual);
                }
            }
            _activeVehicles.Clear();
            _spawnTimers.Clear();
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
            UpdateMovement(deltaTime);
            
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
                    if (!_spawnTimers.ContainsKey(color))
                    {
                        _spawnTimers[color] = SpawnInterval; // Spawn first vehicle immediately
                    }

                    _spawnTimers[color] += deltaTime;
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

            if (_vehicleContainer != null && _vehicleContainer.parent == null)
            {
                if (_cachedGridView == null)
                    _cachedGridView = UnityEngine.Object.FindAnyObjectByType<GridView>();
                if (_cachedGridView != null)
                    _vehicleContainer.SetParent(_cachedGridView.transform, false);
            }

            GameObject visual = new GameObject($"V_{color}");
            visual.transform.SetParent(_vehicleContainer);

            VehicleStyle vehicleStyle = SettingsModel != null ? SettingsModel.CurrentVehicleStyle : (VehicleStyle)PlayerPrefs.GetInt("VehicleStyle", 0);
            
            Transform loco = null, wagon1 = null, wagon2 = null, coupler1 = null, coupler2 = null;
            List<Renderer> renderers = vehicleStyle == VehicleStyle.Train 
                ? CreateProceduralTrain3D(visual, color, out loco, out wagon1, out wagon2, out coupler1, out coupler2) 
                : CreateProceduralVehicle3D(visual, color);

            var inst = new VehicleInstance
            {
                Color = color,
                Style = vehicleStyle,
                Path = path,
                SegmentIndex = 0,
                Progress = 0f,
                Visual = visual,
                CurrentPosition = new Vector3(path[0].x, path[0].y, GetZOffset(path[0], color)),
                Speed = VehicleSpeed + UnityEngine.Random.Range(-0.3f, 0.3f),
                CachedRenderers = renderers.ToArray(),
                LocoTransform = loco,
                Wagon1Transform = wagon1,
                Wagon2Transform = wagon2,
                Coupler1Transform = coupler1,
                Coupler2Transform = coupler2
            };

            visual.transform.localPosition = vehicleStyle == VehicleStyle.Train ? Vector3.zero : inst.CurrentPosition;

            // Set initial vehicle color via MaterialPropertyBlock (shared materials don't have per-vehicle color baked in)
            Color vehicleColor = CellView.GetColor(color);
            inst.Mpb.SetColor("_Color", new Color(vehicleColor.r, vehicleColor.g, vehicleColor.b, 1f));
            for (int ri = 0; ri < inst.CachedRenderers.Length; ri++)
            {
                if (inst.CachedRenderers[ri] != null)
                    inst.CachedRenderers[ri].SetPropertyBlock(inst.Mpb);
            }

            _activeVehicles.Add(inst);
        }

        // Shared materials for vehicle visuals — prevents new Material per-primitive (saved ~20+ allocs/vehicle)
        private static Material _sharedSpriteMat;
        private static Material _sharedMetalMat;
        private static Material _sharedWindowMat;
        private static Material _sharedHeadlightMat;
        private static Material _sharedWhiteMat;
        private static Material _sharedTailMat;

        private static Material GetSharedSpriteMat()
        {
            if (_sharedSpriteMat == null)
            {
                var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                _sharedSpriteMat = new Material(shader) { hideFlags = HideFlags.DontSave };
            }
            return _sharedSpriteMat;
        }

        private static void EnsureSharedMaterials()
        {
            if (_sharedSpriteMat != null) return;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            _sharedSpriteMat = new Material(shader) { hideFlags = HideFlags.DontSave };
            _sharedMetalMat = new Material(shader) { color = new Color(0.15f, 0.15f, 0.18f, 1f), hideFlags = HideFlags.DontSave };
            _sharedWindowMat = new Material(shader) { color = new Color(0.2f, 0.9f, 1f, 0.9f), hideFlags = HideFlags.DontSave };
            _sharedHeadlightMat = new Material(shader) { color = new Color(1f, 0.95f, 0.5f, 1f), hideFlags = HideFlags.DontSave };
            _sharedWhiteMat = new Material(shader) { color = Color.white, hideFlags = HideFlags.DontSave };
            _sharedTailMat = new Material(shader) { color = new Color(1f, 0.15f, 0.15f, 1f), hideFlags = HideFlags.DontSave };
        }

        private static List<Renderer> CreateProceduralTrain3D(GameObject root, ColorType color, out Transform loco, out Transform wagon1, out Transform wagon2, out Transform coupler1, out Transform coupler2)
        {
            var renderers = new List<Renderer>();
            loco = null; wagon1 = null; wagon2 = null; coupler1 = null; coupler2 = null;
            if (!Application.isPlaying) return renderers;
            EnsureSharedMaterials();

            Color trainColor = CellView.GetColor(color);

            // 1. LOCOMOTIVE ENGINE HEAD
            var locoObj = new GameObject("Locomotive");
            locoObj.transform.SetParent(root.transform, false);
            loco = locoObj.transform;

            var locoBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            locoBody.name = "EngineBody";
            locoBody.transform.SetParent(loco, false);
            locoBody.transform.localScale = new Vector3(0.38f, 0.22f, 0.18f);
            var rLoco = locoBody.GetComponent<Renderer>();
            if (rLoco != null) { rLoco.material = _sharedSpriteMat; rLoco.sortingOrder = 10; renderers.Add(rLoco); }

            var locoCab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            locoCab.name = "EngineCabin";
            locoCab.transform.SetParent(loco, false);
            locoCab.transform.localScale = new Vector3(0.18f, 0.20f, 0.16f);
            locoCab.transform.localPosition = new Vector3(-0.06f, 0f, -0.10f);
            var rCab = locoCab.GetComponent<Renderer>();
            if (rCab != null) { rCab.material = _sharedSpriteMat; rCab.sortingOrder = 10; renderers.Add(rCab); }

            var windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
            windshield.name = "Windshield";
            windshield.transform.SetParent(loco, false);
            windshield.transform.localScale = new Vector3(0.04f, 0.18f, 0.08f);
            windshield.transform.localPosition = new Vector3(0.19f, 0f, -0.06f);
            var rWin = windshield.GetComponent<Renderer>();
            if (rWin != null) { rWin.material = _sharedWindowMat; rWin.sortingOrder = 10; renderers.Add(rWin); }

            var headlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headlight.name = "TrainHeadlight";
            headlight.transform.SetParent(loco, false);
            headlight.transform.localScale = new Vector3(0.05f, 0.08f, 0.06f);
            headlight.transform.localPosition = new Vector3(0.20f, 0f, 0.02f);
            var rHead = headlight.GetComponent<Renderer>();
            if (rHead != null) { rHead.material = _sharedHeadlightMat; rHead.sortingOrder = 10; renderers.Add(rHead); }

            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "RoofStripe";
            stripe.transform.SetParent(loco, false);
            stripe.transform.localScale = new Vector3(0.36f, 0.06f, 0.04f);
            stripe.transform.localPosition = new Vector3(0f, 0f, -0.19f);
            var rStripe = stripe.GetComponent<Renderer>();
            if (rStripe != null) { rStripe.material = _sharedWhiteMat; rStripe.sortingOrder = 10; renderers.Add(rStripe); }

            float[] locoWheelX = { 0.10f, -0.10f };
            foreach (float x in locoWheelX)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.name = "Wheel";
                    wheel.transform.SetParent(loco, false);
                    wheel.transform.localScale = new Vector3(0.07f, 0.02f, 0.07f);
                    wheel.transform.localPosition = new Vector3(x, side * 0.09f, 0.05f);
                    wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var rWheel = wheel.GetComponent<Renderer>();
                    if (rWheel != null) { rWheel.material = _sharedMetalMat; rWheel.sortingOrder = 10; renderers.Add(rWheel); }
                }
            }

            // 2. COUPLER 1
            var c1Obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c1Obj.name = "Coupler1";
            c1Obj.transform.SetParent(root.transform, false);
            c1Obj.transform.localScale = new Vector3(0.10f, 0.06f, 0.06f);
            coupler1 = c1Obj.transform;
            var rC1 = c1Obj.GetComponent<Renderer>();
            if (rC1 != null) { rC1.material = _sharedMetalMat; rC1.sortingOrder = 10; renderers.Add(rC1); }

            // 3. WAGON 1
            var w1Obj = new GameObject("Wagon1");
            w1Obj.transform.SetParent(root.transform, false);
            wagon1 = w1Obj.transform;

            var w1Body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w1Body.name = "WagonBody";
            w1Body.transform.SetParent(wagon1, false);
            w1Body.transform.localScale = new Vector3(0.34f, 0.20f, 0.16f);
            var rW1 = w1Body.GetComponent<Renderer>();
            if (rW1 != null) { rW1.material = _sharedSpriteMat; rW1.sortingOrder = 10; renderers.Add(rW1); }

            for (int side = -1; side <= 1; side += 2)
            {
                var wWin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wWin.name = "WagonWindows";
                wWin.transform.SetParent(wagon1, false);
                wWin.transform.localScale = new Vector3(0.24f, 0.02f, 0.06f);
                wWin.transform.localPosition = new Vector3(0f, side * 0.10f, -0.03f);
                var rWWin = wWin.GetComponent<Renderer>();
                if (rWWin != null) { rWWin.material = _sharedWindowMat; rWWin.sortingOrder = 10; renderers.Add(rWWin); }
            }

            foreach (float x in locoWheelX)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.name = "Wheel";
                    wheel.transform.SetParent(wagon1, false);
                    wheel.transform.localScale = new Vector3(0.07f, 0.02f, 0.07f);
                    wheel.transform.localPosition = new Vector3(x, side * 0.09f, 0.05f);
                    wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var rWheel = wheel.GetComponent<Renderer>();
                    if (rWheel != null) { rWheel.material = _sharedMetalMat; rWheel.sortingOrder = 10; renderers.Add(rWheel); }
                }
            }

            // 4. COUPLER 2
            var c2Obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c2Obj.name = "Coupler2";
            c2Obj.transform.SetParent(root.transform, false);
            c2Obj.transform.localScale = new Vector3(0.10f, 0.06f, 0.06f);
            coupler2 = c2Obj.transform;
            var rC2 = c2Obj.GetComponent<Renderer>();
            if (rC2 != null) { rC2.material = _sharedMetalMat; rC2.sortingOrder = 10; renderers.Add(rC2); }

            // 5. WAGON 2
            var w2Obj = new GameObject("Wagon2");
            w2Obj.transform.SetParent(root.transform, false);
            wagon2 = w2Obj.transform;

            var w2Body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w2Body.name = "WagonBody";
            w2Body.transform.SetParent(wagon2, false);
            w2Body.transform.localScale = new Vector3(0.32f, 0.20f, 0.16f);
            var rW2 = w2Body.GetComponent<Renderer>();
            if (rW2 != null) { rW2.material = _sharedSpriteMat; rW2.sortingOrder = 10; renderers.Add(rW2); }

            foreach (float x in locoWheelX)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.name = "Wheel";
                    wheel.transform.SetParent(wagon2, false);
                    wheel.transform.localScale = new Vector3(0.07f, 0.02f, 0.07f);
                    wheel.transform.localPosition = new Vector3(x, side * 0.09f, 0.05f);
                    wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    var rWheel = wheel.GetComponent<Renderer>();
                    if (rWheel != null) { rWheel.material = _sharedMetalMat; rWheel.sortingOrder = 10; renderers.Add(rWheel); }
                }
            }

            return renderers;
        }

        private static List<Renderer> CreateProceduralVehicle3D(GameObject root, ColorType color)
        {
            var renderers = new List<Renderer>();
            if (!Application.isPlaying) return renderers;
            EnsureSharedMaterials();

            // 1. Main Chassis / Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Chassis";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.44f, 0.26f, 0.16f);
            var rBody = body.GetComponent<Renderer>();
            if (rBody != null)
            {
                rBody.material = _sharedSpriteMat;
                rBody.sortingOrder = 10;
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
                rCabin.material = _sharedWindowMat;
                rCabin.sortingOrder = 10;
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
                rHead.material = _sharedHeadlightMat;
                rHead.sortingOrder = 10;
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
                rTail.material = _sharedTailMat;
                rTail.sortingOrder = 10;
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
                        rWheel.material = _sharedMetalMat;
                        rWheel.sortingOrder = 10;
                        renderers.Add(rWheel);
                    }
                }
            }

            return renderers;
        }

        private void UpdateMovement(float deltaTime)
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
                    v.Mpb.SetColor("_Color", new Color(1f, 1f, 1f, baseAlpha));
                    for (int ri = 0; ri < v.CachedRenderers.Length; ri++)
                    {
                        if (v.CachedRenderers[ri] == null) continue;
                        v.CachedRenderers[ri].SetPropertyBlock(v.Mpb);
                    }
                }

                v.TotalDistance += Mathf.Min(v.Speed * deltaTime, MaxProgressPerFrame);
                float maxAllowedDist = (v.Style == VehicleStyle.Train) ? (v.Path.Count - 1) + 0.85f : (v.Path.Count - 1);

                if (v.TotalDistance >= maxAllowedDist)
                {
                    if (ObstacleService != null && v.Path.Count > 0)
                    {
                        Vector2Int endCell = v.Path[v.Path.Count - 1];
                        if (ObstacleService.IsNarrowPass(endCell))
                        {
                            ObstacleService.OnVehicleLeftNarrowPass(endCell, v.Color);
                        }
                    }

                    // GDD §2.8: Simülasyon fazında başarılı ulaşım tespiti ve Flow Score güncelleme
                    if (GameStateModel.CurrentState == GameState.Simulating && GameSessionModel != null)
                    {
                        GameSessionModel.IncrementFlowScore();
                        SignalBus?.Fire(new FlowScoreUpdatedSignal
                        {
                            CurrentScore = GameSessionModel.CurrentFlowScore,
                            TargetScore = GameSessionModel.TargetFlowScore
                        });
                        AudioService?.PlaySfx(SfxType.UIClick); // Hafif bir doğrulama sesi
                    }

                    SafeDestroy(v.Visual);
                    _activeVehicles.RemoveAt(i);
                    continue;
                }

                float locoDist = Mathf.Min(v.TotalDistance, v.Path.Count - 1);
                float zOffset = GetZOffset(v.Path[Mathf.Clamp(Mathf.FloorToInt(locoDist), 0, v.Path.Count - 1)], v.Color);

                Vector3 basePos = EvaluatePathPosition(v.Path, locoDist, v.Color, zOffset);
                Vector3 tangent = EvaluatePathTangent(v.Path, locoDist, v.Color);
                v.CurrentPosition = basePos;

                if (v.Style == VehicleStyle.Train)
                {
                    v.Visual.transform.localPosition = Vector3.zero;
                    float bobbing = Mathf.Sin(Time.time * 8f + v.GetHashCode()) * 0.01f;

                    // 1. Update Locomotive Engine
                    Vector3 locoPos = basePos;
                    locoPos.z = zOffset + bobbing;
                    if (v.LocoTransform != null)
                    {
                        v.LocoTransform.localPosition = locoPos;
                        if (tangent.sqrMagnitude > 0.001f)
                        {
                            float targetAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                            v.LocoTransform.localRotation = Quaternion.Slerp(
                                v.LocoTransform.localRotation,
                                Quaternion.Euler(0f, 0f, targetAngle),
                                deltaTime * 25f);
                        }
                    }

                    // 2. Wagon 1 (0.42f behind)
                    Vector3 w1Pos;
                    bool w1Active = UpdateCarriageTransform(v.Wagon1Transform, v.Path, v.TotalDistance - 0.42f, v.Color, zOffset, deltaTime, out w1Pos);

                    // 3. Coupler 1
                    if (v.Coupler1Transform != null)
                    {
                        if (v.LocoTransform != null && w1Active)
                        {
                            v.Coupler1Transform.gameObject.SetActive(true);
                            Vector3 c1Pos = (locoPos + w1Pos) * 0.5f;
                            v.Coupler1Transform.localPosition = c1Pos;
                            Vector3 dir1 = locoPos - w1Pos;
                            if (dir1.sqrMagnitude > 0.001f)
                            {
                                float c1Angle = Mathf.Atan2(dir1.y, dir1.x) * Mathf.Rad2Deg;
                                v.Coupler1Transform.localRotation = Quaternion.Euler(0f, 0f, c1Angle);
                            }
                        }
                        else
                        {
                            v.Coupler1Transform.gameObject.SetActive(false);
                        }
                    }

                    // 4. Wagon 2 (0.84f behind)
                    Vector3 w2Pos;
                    bool w2Active = UpdateCarriageTransform(v.Wagon2Transform, v.Path, v.TotalDistance - 0.84f, v.Color, zOffset, deltaTime, out w2Pos);

                    // 5. Coupler 2
                    if (v.Coupler2Transform != null)
                    {
                        if (w1Active && w2Active)
                        {
                            v.Coupler2Transform.gameObject.SetActive(true);
                            Vector3 c2Pos = (w1Pos + w2Pos) * 0.5f;
                            v.Coupler2Transform.localPosition = c2Pos;
                            Vector3 dir2 = w1Pos - w2Pos;
                            if (dir2.sqrMagnitude > 0.001f)
                            {
                                float c2Angle = Mathf.Atan2(dir2.y, dir2.x) * Mathf.Rad2Deg;
                                v.Coupler2Transform.localRotation = Quaternion.Euler(0f, 0f, c2Angle);
                            }
                        }
                        else
                        {
                            v.Coupler2Transform.gameObject.SetActive(false);
                        }
                    }
                }
                else
                {
                    // Regular Car movement
                    Vector3 nextTangent = EvaluatePathTangent(v.Path, Mathf.Min(locoDist + 0.1f, v.Path.Count - 1), v.Color);
                    float baseAngle = tangent.sqrMagnitude > 0.001f ? Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg : 0f;
                    float nextAngle = nextTangent.sqrMagnitude > 0.001f ? Mathf.Atan2(nextTangent.y, nextTangent.x) * Mathf.Rad2Deg : baseAngle;
                    float deltaAngle = Mathf.DeltaAngle(baseAngle, nextAngle);

                    float bobbing = Mathf.Sin(Time.time * 12f + v.GetHashCode()) * 0.02f;
                    Vector3 finalPos = v.CurrentPosition;
                    finalPos.z += bobbing;
                    v.Visual.transform.localPosition = finalPos;

                    if (tangent.sqrMagnitude > 0.001f)
                    {
                        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                        float rollBank = Mathf.Clamp(-deltaAngle * 0.3f, -15f, 15f);
                        v.Visual.transform.rotation = Quaternion.Slerp(
                            v.Visual.transform.rotation,
                            Quaternion.Euler(0f, 0f, angle + rollBank),
                            deltaTime * 25f);
                    }
                }
            }
        }

        private bool UpdateCarriageTransform(Transform tForm, List<Vector2Int> path, float targetDist, ColorType color, float zOffset, float deltaTime, out Vector3 worldPos)
        {
            worldPos = Vector3.zero;
            if (tForm == null) return false;

            if (targetDist < 0f)
            {
                tForm.gameObject.SetActive(false);
                return false;
            }

            tForm.gameObject.SetActive(true);
            float clampedDist = Mathf.Min(targetDist, path.Count - 1);
            Vector3 pos = EvaluatePathPosition(path, clampedDist, color, zOffset);
            Vector3 tangent = EvaluatePathTangent(path, clampedDist, color);
            worldPos = pos;

            tForm.localPosition = pos;
            if (tangent.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                tForm.localRotation = Quaternion.Slerp(
                    tForm.localRotation,
                    Quaternion.Euler(0f, 0f, targetAngle),
                    deltaTime * 25f);
            }

            return true;
        }

        private Vector3 EvaluatePathPosition(List<Vector2Int> path, float pathDistance, ColorType color, float zOffset)
        {
            if (path == null || path.Count == 0) return Vector3.zero;
            if (path.Count == 1) return new Vector3(path[0].x, path[0].y, zOffset);

            float clampedDist = Mathf.Clamp(pathDistance, 0f, path.Count - 1);
            int seg = Mathf.FloorToInt(clampedDist);
            if (seg >= path.Count - 1)
            {
                seg = path.Count - 2;
                clampedDist = path.Count - 1;
            }
            float t = clampedDist - seg;

            Vector3 p0 = GetSplineControlPoint(path, seg - 1, color);
            Vector3 p1 = GetSplineControlPoint(path, seg, color);
            Vector3 p2 = GetSplineControlPoint(path, seg + 1, color);
            Vector3 p3 = GetSplineControlPoint(path, seg + 2, color);

            Vector3 pos = CatmullRom(p0, p1, p2, p3, t);
            pos.z = zOffset;
            return pos;
        }

        private Vector3 EvaluatePathTangent(List<Vector2Int> path, float pathDistance, ColorType color)
        {
            if (path == null || path.Count < 2) return Vector3.right;

            float clampedDist = Mathf.Clamp(pathDistance, 0f, path.Count - 1);
            int seg = Mathf.FloorToInt(clampedDist);
            if (seg >= path.Count - 1)
            {
                seg = path.Count - 2;
                clampedDist = path.Count - 1;
            }
            float t = clampedDist - seg;

            Vector3 p0 = GetSplineControlPoint(path, seg - 1, color);
            Vector3 p1 = GetSplineControlPoint(path, seg, color);
            Vector3 p2 = GetSplineControlPoint(path, seg + 1, color);
            Vector3 p3 = GetSplineControlPoint(path, seg + 2, color);

            return CatmullRomTangent(p0, p1, p2, p3, t);
        }

        private float GetZOffset(Vector2Int gridPos, ColorType color)
        {
            if (gridPos.x >= 0 && gridPos.x < GridModel.Width && gridPos.y >= 0 && gridPos.y < GridModel.Height)
            {
                var cell = GridModel.Grid[gridPos.x, gridPos.y];
                if (cell.HasViaduct && cell.OverColor == color)
                {
                    return -0.5f; // Yükseltilmiş yol
                }
            }
            return -0.2f; // Normal yol
        }

        // Shared buffer for collision point queries — avoids per-pair allocation
        private static readonly List<Vector3> _bufferA = new List<Vector3>(8);
        private static readonly List<Vector3> _bufferB = new List<Vector3>(8);

        private void UpdateCollisionDetection()
        {
            for (int i = 0; i < _activeVehicles.Count; i++)
            {
                var v1 = _activeVehicles[i];
                PopulateCollisionPoints(v1, _bufferA);
                if (_bufferA.Count == 0) continue;

                for (int j = i + 1; j < _activeVehicles.Count; j++)
                {
                    var v2 = _activeVehicles[j];
                    if (v1.Color == v2.Color) continue;

                    PopulateCollisionPoints(v2, _bufferB);
                    if (_bufferB.Count == 0) continue;

                    foreach (var p1 in _bufferA)
                    {
                        foreach (var p2 in _bufferB)
                        {
                            float dist = Vector3.Distance(p1, p2);
                            const float collisionThreshold = 0.45f;

                            if (dist < collisionThreshold)
                            {
                                Vector3 midPos = (p1 + p2) * 0.5f;
                                var cellPos = new Vector2Int(
                                    Mathf.RoundToInt(midPos.x),
                                    Mathf.RoundToInt(midPos.y));

                                if (cellPos.x >= 0 && cellPos.x < GridModel.Width &&
                                    cellPos.y >= 0 && cellPos.y < GridModel.Height)
                                {
                                    var cell = GridModel.Grid[cellPos.x, cellPos.y];
                                    if (cell.HasViaduct)
                                    {
                                        float zDiff = Mathf.Abs(p1.z - p2.z);
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
        }

        private static void PopulateCollisionPoints(VehicleInstance v, List<Vector3> buffer)
        {
            buffer.Clear();
            if (v.Style == VehicleStyle.Train)
            {
                if (v.LocoTransform != null && v.LocoTransform.gameObject.activeSelf) buffer.Add(v.LocoTransform.localPosition);
                if (v.Wagon1Transform != null && v.Wagon1Transform.gameObject.activeSelf) buffer.Add(v.Wagon1Transform.localPosition);
                if (v.Wagon2Transform != null && v.Wagon2Transform.gameObject.activeSelf) buffer.Add(v.Wagon2Transform.localPosition);
                if (buffer.Count == 0) buffer.Add(v.CurrentPosition);
            }
            else
            {
                buffer.Add(v.CurrentPosition);
            }
        }

        private void TriggerCrash(Vector2Int crashPos, ColorType colorA, ColorType colorB)
        {
            LoggerService?.LogError($"[VehicleSimulator] TRAFFIC CRASH detected at {crashPos} between {colorA} and {colorB}!");

            GridModel.LastCrashPosition.Value = crashPos;
            GridModel.CrashColorA.Value = colorA;
            GridModel.CrashColorB.Value = colorB;

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

        private void UpdateCompletionTimer(float deltaTime)
        {
            _simulationPhaseTimer += deltaTime;
            
            // Maksimum 45 saniye güvenlik limiti (darboğaz durumlarında kilitlenmeyi önlemek için)
            const float maxSimulationSafetyDuration = 45f;
            float remaining = Mathf.Max(0f, maxSimulationSafetyDuration - _simulationPhaseTimer);
            GameSessionModel.SetSimulationTimer(remaining);

            // Flow Score kazanma kontrolü
            if (GameSessionModel != null && GameSessionModel.CurrentFlowScore >= GameSessionModel.TargetFlowScore)
            {
                CompleteLevel();
            }
            else if (_simulationPhaseTimer >= maxSimulationSafetyDuration)
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

        private Vector3 GetSplineControlPoint(List<Vector2Int> path, int index, ColorType color)
        {
            if (path == null || path.Count == 0) return Vector3.zero;
            if (path.Count == 1)
            {
                Vector2Int p = path[0];
                return new Vector3(p.x, p.y, GetZOffset(p, color));
            }

            if (index < 0)
            {
                Vector2Int pos0 = path[0];
                Vector2Int pos1 = path[1];
                Vector3 v0 = new Vector3(pos0.x, pos0.y, GetZOffset(pos0, color));
                Vector3 v1 = new Vector3(pos1.x, pos1.y, GetZOffset(pos1, color));
                return v0 + (v0 - v1) * Mathf.Abs(index);
            }
            if (index >= path.Count)
            {
                Vector2Int posEnd = path[path.Count - 1];
                Vector2Int posPrev = path[path.Count - 2];
                Vector3 vEnd = new Vector3(posEnd.x, posEnd.y, GetZOffset(posEnd, color));
                Vector3 vPrev = new Vector3(posPrev.x, posPrev.y, GetZOffset(posPrev, color));
                return vEnd + (vEnd - vPrev) * (index - path.Count + 1);
            }

            Vector2Int gridPos = path[index];
            return new Vector3(gridPos.x, gridPos.y, GetZOffset(gridPos, color));
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
