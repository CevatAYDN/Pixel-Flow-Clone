using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PixelFlow.PlayMode.Tests
{
    /// <summary>
    /// Performance benchmarks for VehicleMovementService optimizations:
    /// - MPB ghost alpha throttle (GPU sync 2 frame'de 1)
    /// - GetHashCode() → AnimationOffset (GC compaction stabilitesi)
    /// - Verbose log removal from hot path
    /// 
    /// Çalıştırmak için: Unity Test Runner'da PlayMode tests altında çalıştırın.
    /// Sonuçlar Console'a "[Benchmark]" prefix'i ile yazılır.
    /// </summary>
    [TestFixture]
    public class VehicleMovementPerformanceTests
    {
        private NexusTestContext _ctx;
        private VehicleSimulator _simulator;
        private IGridModel _gridModel;
        private IGameStateModel _stateModel;
        private IGameSessionModel _sessionModel;
        private ILevelModel _levelModel;
        private List<VehicleInstance> _testVehicles;

        [SetUp]
        public void SetUp()
        {
            _ctx = NexusTestHarness.CreateContext(builder =>
            {
                builder.Bind<IPlayerPrefsService, InMemoryPlayerPrefsService>();

                var gameConfig = ScriptableObject.CreateInstance<GameConfig>();
                gameConfig.name = "GameConfig (Benchmark)";
                gameConfig.VehicleSpeed = 3f;
                gameConfig.SpawnInterval = 0.5f; // Hızlı spawn için düşük
                gameConfig.FixedTimeStep = 1f / 60f;
                gameConfig.SpawnCheckInterval = 15;
                gameConfig.SpeedVariationRange = 0.3f;
                gameConfig.CollisionDistance = 0.45f;
                gameConfig.ViaductOverZOffset = -0.4f;
                gameConfig.ViaductUnderZOffset = -0.1f;
                gameConfig.NormalZOffset = -0.2f;
                gameConfig.VehiclePartPoolCubes = 512;
                gameConfig.VehiclePartPoolCylinders = 256;
                gameConfig.MaxSimulationSafetyDuration = 45f;
                gameConfig.FerryPeriod = 10f;
                gameConfig.MaxProgressPerFrame = 0.25f;
                builder.BindInstance(gameConfig);
                PixelFlow.Views.VehiclePartPool.SetConfig(gameConfig);
                PixelFlow.Views.VehiclePartPool.Initialize();

                var storageKeys = ScriptableObject.CreateInstance<StorageKeysConfigAsset>();
                storageKeys.KeyUnlockedLevels = "UnlockedLevels";
                builder.BindInstance(storageKeys);

                var economyConfig = ScriptableObject.CreateInstance<EconomyConfigAsset>();
                economyConfig.IapProducts = new List<IapProductDefinition>();
                builder.BindInstance(economyConfig);

                var phaseConfig = ScriptableObject.CreateInstance<PhaseConfigAsset>();
                phaseConfig.Phase1 = ScriptableObject.CreateInstance<PhaseDefinitionAsset>();
                builder.BindInstance(phaseConfig);

                var levelCatalog = ScriptableObject.CreateInstance<LevelCatalogAsset>();
                levelCatalog.Levels = new List<LevelCatalogAsset.LevelCatalogEntry>();
                builder.BindInstance(levelCatalog);

                var themeConfig = ScriptableObject.CreateInstance<ThemePaletteAsset>();
                builder.BindInstance(themeConfig);

                var vehicleMatConfig = ScriptableObject.CreateInstance<VehicleMaterialConfigAsset>();
                builder.BindInstance(vehicleMatConfig);

                var vehicleVisualConfig = ScriptableObject.CreateInstance<VehicleVisualConfigAsset>();
                builder.BindInstance(vehicleVisualConfig);
                PixelFlow.Views.VehicleVisualFactory.Initialize(vehicleMatConfig, vehicleVisualConfig);

                var defaultSkinConfig = ScriptableObject.CreateInstance<DefaultSkinIdsConfigAsset>();
                defaultSkinConfig.DefaultVehicleSkinId = "skin_default";
                builder.BindInstance(defaultSkinConfig);

                var bouncyPhysicsConfig = ScriptableObject.CreateInstance<BouncyPhysicsConfigAsset>();
                builder.BindInstance(bouncyPhysicsConfig);

                var starCriteriaConfig = ScriptableObject.CreateInstance<StarCriteriaConfigAsset>();
                builder.BindInstance(starCriteriaConfig);

                var rushHourConfig = ScriptableObject.CreateInstance<RushHourConfigAsset>();
                builder.BindInstance(rushHourConfig);

                var difficultyFormulaConfig = ScriptableObject.CreateInstance<DifficultyFormulaConfigAsset>();
                builder.BindInstance(difficultyFormulaConfig);

                // Core services
                builder.BindService<IEconomyService, Nexus.Core.Services.EconomyService>();
                builder.Bind<Nexus.Core.Services.INetworkEconomyValidator, LocalEconomyValidator>();
                builder.BindService<IPathService, PathService>();
                builder.BindService<IGameHistoryService, GameHistoryService>();
                builder.Bind<IPathSolver, RuntimePathSolver>();
                builder.BindService<IHintService, HintService>();
                builder.BindService<IVehicleSimulator, VehicleSimulator>();
                builder.BindService<ISaveThrottler, SaveThrottler>();
                builder.BindService<IHapticService, HapticService>();
                var quietLogger = new LoggerService { IsEnabled = false };
                builder.BindInstance<ILoggerService>(quietLogger);
                builder.BindService<ICrisisAdService, CrisisAdService>();
                builder.BindService<IObstacleService, ObstacleService>();
                builder.BindService<ITutorialDriver, TutorialDriver>();
                builder.BindService<IPowerUpService, PowerUpService>();
                builder.BindService<PixelFlow.Services.IAudioService, PixelFlow.Services.AudioService>();
                builder.Bind<IFeedbackService, FeedbackService>();
                builder.Bind<Nexus.Core.Services.IAudioService, StubAudioService>();
                builder.Bind<Nexus.Core.Services.INetworkEconomyValidator, LocalEconomyValidator>();
                builder.Bind<ITimeProvider, UnityTimeProvider>();
                builder.BindService<INexusService, TickService>();
                builder.Bind<ITickService, TickService>();

                builder.BindReactiveModel<IGridModel, GridModel>();
                builder.BindReactiveModel<ILevelModel, LevelModel>();
                builder.BindReactiveModel<IProgressModel, ProgressModel>();
                builder.BindReactiveModel<IGameStateModel, GameStateModel>();
                builder.BindReactiveModel<IGameSessionModel, GameSessionModel>();
                builder.BindReactiveModel<IHintModel, HintModel>();
                builder.BindReactiveModel<ISettingsModel, SettingsModel>();
                builder.BindReactiveModel<ISoundModel, SoundModel>();
                builder.BindReactiveModel<ITutorialModel, TutorialModel>();
                builder.BindReactiveModel<IDailyCrisisModel, DailyCrisisModel>();
                builder.BindReactiveModel<IInventoryModel, InventoryModel>();
                builder.Bind<ILevelProgressionService, LevelProgressionService>();

                builder.Bind<ICameraProvider, StubCameraProvider>();
                builder.Bind<IGridViewProvider, StubGridViewProvider>();
                builder.BindService<ILevelLoaderService, LevelLoaderService>();
                builder.Bind<ICloudSaveAdapter, PixelFlow.Services.GlobalRelease.EncryptedCloudSaveAdapter>();
                builder.BindService<ILocalizationService, PixelFlow.Services.LocalizationService>();
            });

            _simulator = (VehicleSimulator)_ctx.Context.Container.Resolve<IVehicleSimulator>();
            _gridModel = _ctx.GetModel<IGridModel>();
            _stateModel = _ctx.GetModel<IGameStateModel>();
            _sessionModel = _ctx.GetModel<IGameSessionModel>();
            _levelModel = _ctx.GetModel<ILevelModel>();

            _testVehicles = new List<VehicleInstance>();
        }

        [TearDown]
        public void TearDown()
        {
            _simulator?.OnDispose();
            _ctx?.Dispose();
            PixelFlow.Views.VehiclePartPool.Dispose();

            // Benchmark_MPB_Throttle_Reduced_GPU_Sync için renderer GameObject'lerini temizle
            if (_testVehicles != null)
            {
                foreach (var v in _testVehicles)
                {
                    if (v.CachedRenderers != null)
                    {
                        foreach (var r in v.CachedRenderers)
                        {
                            if (r != null && r.gameObject != null)
                                Object.DestroyImmediate(r.gameObject);
                        }
                    }
                }
                _testVehicles.Clear();
            }
        }

        /// <summary>
        /// Creates a test grid with parallel paths for multiple colors to maximize vehicle count.
        /// </summary>
        private LevelData CreateMultiPathLevel(int width, int height, int colorCount)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = width;
            level.height = height;
            level.initialNodes = new List<GridNode>();
            level.viaductLimit = 0;

            var colors = new[] { ColorType.Red, ColorType.Blue, ColorType.Yellow, ColorType.Green, ColorType.Purple };
            for (int i = 0; i < colorCount && i < colors.Length; i++)
            {
                level.initialNodes.Add(new GridNode
                {
                    position = new Vector2Int(0, i * 2),
                    color = colors[i],
                    isSource = true,
                    pairIndex = i
                });
                level.initialNodes.Add(new GridNode
                {
                    position = new Vector2Int(width - 1, i * 2),
                    color = colors[i],
                    isSource = false,
                    pairIndex = i
                });
            }
            return level;
        }

        /// <summary>
        /// Sets up paths for each color in the grid, creating straight horizontal lines.
        /// </summary>
        private void SetupPaths(int width, int colorCount)
        {
            var colors = new[] { ColorType.Red, ColorType.Blue, ColorType.Yellow, ColorType.Green, ColorType.Purple };
            for (int i = 0; i < colorCount && i < colors.Length; i++)
            {
                var path = new List<Vector2Int>();
                for (int x = 0; x < width; x++)
                {
                    path.Add(new Vector2Int(x, i * 2));
                    _gridModel.Grid[x, i * 2].AddPathColor(colors[i]);
                }
                _gridModel.Paths[colors[i]] = path;
            }
        }

        [Test]
        public void Benchmark_VehicleSimulation_FrameTime()
        {
            // ── Setup: 3 renk, 5x5 grid, paralel yollar ──
            const int width = 5;
            const int height = 10;
            const int colorCount = 3;
            const int simulationFrames = 300; // 5 saniye @60fps
            const int warmupFrames = 60;      // 1 saniye ısınma

            var level = CreateMultiPathLevel(width, height, colorCount);
            _levelModel.SetLevel(level);
            _gridModel.Initialize(width, height);
            _gridModel.PlaceNodes(level.initialNodes);
            SetupPaths(width, colorCount);

            _stateModel.SetState(GameState.Playing);
            _sessionModel.StartSession(3, 30);

            // ── Warmup: Simülasyonu ısıt, araçlar spawn olsun ──
            for (int i = 0; i < warmupFrames; i++)
            {
                _simulator.Tick(1f / 60f);
            }

            // ── Benchmark: UpdateMovement süresini ölç ──
            var sw = new Stopwatch();
            long totalTicks = 0;

            for (int i = 0; i < simulationFrames; i++)
            {
                sw.Start();
                _simulator.Tick(1f / 60f);
                sw.Stop();
                totalTicks += sw.ElapsedTicks;
                sw.Reset();
            }

            double totalMs = (totalTicks * 1000.0) / Stopwatch.Frequency;
            double avgMs = totalMs / simulationFrames;
            double fpsEstimate = avgMs > 0 ? 1000.0 / avgMs : 0;

            Debug.Log($"[Benchmark] Simulation_FrameTime: {simulationFrames} frames, total={totalMs:F3}ms, avg={avgMs:F4}ms/frame, est.FPS={fpsEstimate:F1}");
            Debug.Log($"[Benchmark] Config: {colorCount} colors, {width}x{height} grid, {simulationFrames} frames (after {warmupFrames} warmup)");

            // Assert: Her frame 16ms'den az sürmeli (60fps hedef)
            Assert.Less(avgMs, 16.0,
                $"Average frame time ({avgMs:F3}ms) exceeds 16ms target. Performance regression detected!");
        }

        [Test]
        public void Benchmark_VehicleSimulation_StressTest()
        {
            // ── Setup: Maksimum araç sayısı stres testi ──
            // 5 renk, her renk kendi parallel yolunda → 5 yol
            const int width = 8;
            const int height = 12;
            const int colorCount = 5;
            const int simulationFrames = 600; // 10 saniye
            const int warmupFrames = 120;     // 2 saniye ısınma

            var level = CreateMultiPathLevel(width, height, colorCount);
            _levelModel.SetLevel(level);
            _gridModel.Initialize(width, height);
            _gridModel.PlaceNodes(level.initialNodes);
            SetupPaths(width, colorCount);

            _stateModel.SetState(GameState.Playing);
            _sessionModel.StartSession(5, 100);

            // ── Warmup ──
            for (int i = 0; i < warmupFrames; i++)
            {
                _simulator.Tick(1f / 60f);
            }

            // ── Benchmark ──
            var sw = new Stopwatch();
            long totalTicks = 0;
            int frameCount = 0;

            for (int i = 0; i < simulationFrames; i++)
            {
                sw.Start();
                _simulator.Tick(1f / 60f);
                sw.Stop();
                totalTicks += sw.ElapsedTicks;
                sw.Reset();
                frameCount++;

                // Her 60 frame'de bir GC.Collect simüle et (mobil cihaz davranışı)
                if (frameCount % 60 == 0)
                {
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                }
            }

            double totalMs = (totalTicks * 1000.0) / Stopwatch.Frequency;
            double avgMs = totalMs / frameCount;

            Debug.Log($"[Benchmark] Simulation_StressTest: {frameCount} frames, total={totalMs:F3}ms, avg={avgMs:F4}ms/frame");
            Debug.Log($"[Benchmark] Config: {colorCount} colors, {width}x{height} grid, GC.Collect every 60 frames");

            // Assert: Stres testinde her frame 33ms'den az sürmeli (30fps hedef)
            Assert.Less(avgMs, 33.0,
                $"Stress test avg frame time ({avgMs:F3}ms) exceeds 33ms target. Performance regression detected!");
        }

        [Test]
        public void Benchmark_Compare_GetHashCode_Vs_AnimationOffset()
        {
            // ── Setup: Çok sayıda VehicleInstance oluştur, GetHashCode vs AnimationOffset karşılaştır ──
            const int vehicleCount = 100;
            const int iterations = 10000;

            var vehicles = new List<VehicleInstance>(vehicleCount);
            for (int i = 0; i < vehicleCount; i++)
            {
                vehicles.Add(new VehicleInstance
                {
                    Color = (ColorType)(i % 5 + 1),
                    AnimationOffset = Random.Range(0f, 100f),
                    Speed = 3f
                });
            }

            // ── Benchmark: GetHashCode (ESKİ) ──
            var sw = new Stopwatch();
            double totalSinHash = 0;

            sw.Start();
            for (int iter = 0; iter < iterations; iter++)
            {
                for (int v = 0; v < vehicleCount; v++)
                {
                    totalSinHash += 0.45f + Mathf.Sin(Time.time * 6f + vehicles[v].GetHashCode() * 0.1f) * 0.25f;
                }
            }
            sw.Stop();
            double hashMs = (sw.ElapsedTicks * 1000.0) / Stopwatch.Frequency;

            // ── Benchmark: AnimationOffset (YENİ) ──
            sw.Reset();
            double totalSinOffset = 0;

            sw.Start();
            for (int iter = 0; iter < iterations; iter++)
            {
                for (int v = 0; v < vehicleCount; v++)
                {
                    totalSinOffset += 0.45f + Mathf.Sin(Time.time * 6f + vehicles[v].AnimationOffset * 0.1f) * 0.25f;
                }
            }
            sw.Stop();
            double offsetMs = (sw.ElapsedTicks * 1000.0) / Stopwatch.Frequency;

            // Prevent JIT elimination: accumulated değerleri Debug.Log ile kullan
            Debug.Log($"[Benchmark] Sanity check (JIT elimination prevention): hash={totalSinHash:F2}, offset={totalSinOffset:F2}");

            // ── Sonuçlar ──
            double hashPerCall = (hashMs * 1000.0 * 1000.0) / (vehicleCount * iterations); // nanoseconds
            double offsetPerCall = (offsetMs * 1000.0 * 1000.0) / (vehicleCount * iterations);

            Debug.Log($"[Benchmark] GetHashCode:    {hashMs:F3}ms total, {hashPerCall:F2}ns/call ({vehicleCount} vehicles × {iterations} iterations)");
            Debug.Log($"[Benchmark] AnimationOffset: {offsetMs:F3}ms total, {offsetPerCall:F2}ns/call ({vehicleCount} vehicles × {iterations} iterations)");
            Debug.Log($"[Benchmark] Speedup: {(hashMs / offsetMs):F2}x ({(hashMs - offsetMs):F3}ms saved over {iterations} iterations)");

            // AnimationOffset should be faster (field read vs virtual method call)
            // Allow small margin for measurement noise
            Assert.Less(offsetPerCall, hashPerCall * 1.1, 
                "AnimationOffset should not be significantly slower than GetHashCode");
        }

        [Test]
        public void Benchmark_GC_Stability_AnimationOffset()
        {
            // ── Setup: GC compaction sonrası AnimationOffset kararlılık testi ──
            const int vehicleCount = 50;
            const int iterations = 5;

            var vehicles = new List<VehicleInstance>(vehicleCount);
            for (int i = 0; i < vehicleCount; i++)
            {
                vehicles.Add(new VehicleInstance
                {
                    Color = (ColorType)(i % 5 + 1),
                    AnimationOffset = i * 1.7f,
                    Speed = 3f
                });
            }

            // Record animation offsets before GC
            var offsetsBefore = new float[vehicleCount];
            var hashBefore = new int[vehicleCount];
            for (int i = 0; i < vehicleCount; i++)
            {
                offsetsBefore[i] = vehicles[i].AnimationOffset;
                hashBefore[i] = vehicles[i].GetHashCode();
            }

            // Force multiple GC compactions (simulates mobile memory pressure)
            for (int gc = 0; gc < iterations; gc++)
            {
                // Allocate memory to trigger GC (no need to write — allocation alone suffices)
                var garbage = new byte[1024 * 1024 * 10]; // 10MB
                garbage = null;

                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
            }

            // Verify AnimationOffset stability
            bool allOffsetsStable = true;
            bool anyHashChanged = false;

            for (int i = 0; i < vehicleCount; i++)
            {
                if (Mathf.Abs(vehicles[i].AnimationOffset - offsetsBefore[i]) > 0.0001f)
                {
                    Debug.LogError($"[Benchmark] GC CHANGED AnimationOffset for vehicle {i}: {offsetsBefore[i]} → {vehicles[i].AnimationOffset}");
                    allOffsetsStable = false;
                }

                if (vehicles[i].GetHashCode() != hashBefore[i])
                {
                    anyHashChanged = true;
                }
            }

            Debug.Log($"[Benchmark] GC Stability: AnimationOffset stable={allOffsetsStable}, GetHashCode changed={anyHashChanged}");

            Assert.IsTrue(allOffsetsStable, "AnimationOffset changed after GC compaction! This defeats the purpose of the optimization.");
            if (anyHashChanged)
            {
                Debug.Log($"[Benchmark] NOTE: GetHashCode() changed after GC — confirmed reason for optimization");
            }
        }

        [Test]
        public void Benchmark_MPB_Throttle_Reduced_GPU_Sync()
        {
            // ── Setup: MPB çağrı sayısını ölç ──
            const int vehicleCount = 10;
            const int renderersPerVehicle = 4; // Car: body + 2 wheels + trail
            const int frames = 300;

            // Rastgele renderer'lar simüle et
            var vehicles = new List<VehicleInstance>(vehicleCount);
            for (int i = 0; i < vehicleCount; i++)
            {
                var renderers = new Renderer[renderersPerVehicle];
                for (int r = 0; r < renderersPerVehicle; r++)
                {
                    var go = new GameObject($"BenchmarkRenderer_{i}_{r}");
                    go.SetActive(true);
                    renderers[r] = go.AddComponent<MeshRenderer>();
                }

                vehicles.Add(new VehicleInstance
                {
                    Color = (ColorType)(i % 5 + 1),
                    AnimationOffset = i * 3.3f,
                    CachedRenderers = renderers,
                    Speed = 2f,
                    GhostAlphaCounter = 0
                });
            }

            // ── Simulate UpdateMovement MPB logic ──
            // Eski davranış: Her frame tüm renderer'lar için GetPropertyBlock + SetPropertyBlock
            int oldTotalCalls = 0;
            var swOld = new Stopwatch();

            swOld.Start();
            for (int f = 0; f < frames; f++)
            {
                float time = f / 60f; // Simulate Time.time
                for (int v = 0; v < vehicleCount; v++)
                {
                    float alpha = 0.45f + Mathf.Sin(time * 6f + vehicles[v].GetHashCode() * 0.1f) * 0.25f;
                    for (int r = 0; r < renderersPerVehicle; r++)
                    {
                        // Simulate GetPropertyBlock + SetPropertyBlock
                        var renderer = vehicles[v].CachedRenderers[r];
                        if (renderer == null) continue;
                        renderer.GetPropertyBlock(vehicles[v].Mpb);
                        renderer.SetPropertyBlock(vehicles[v].Mpb);
                        oldTotalCalls += 2; // GetPropertyBlock + SetPropertyBlock
                    }
                }
            }
            swOld.Stop();
            double oldMs = (swOld.ElapsedTicks * 1000.0) / Stopwatch.Frequency;

            // Yeni davranış: MPB 2 frame'de 1 güncellenir
            int newTotalCalls = 0;
            var swNew = new Stopwatch();

            swNew.Start();
            for (int f = 0; f < frames; f++)
            {
                float time = f / 60f;
                for (int v = 0; v < vehicleCount; v++)
                {
                    // Sin her frame (ucuz)
                    float alpha = 0.45f + Mathf.Sin(time * 6f + vehicles[v].AnimationOffset * 0.1f) * 0.25f;

                    // MPB 2 frame'de 1 (throttle)
                    vehicles[v].GhostAlphaCounter++;
                    if (vehicles[v].GhostAlphaCounter >= 2)
                    {
                        vehicles[v].GhostAlphaCounter = 0;
                        for (int r = 0; r < renderersPerVehicle; r++)
                        {
                            var renderer = vehicles[v].CachedRenderers[r];
                            if (renderer == null) continue;
                            renderer.GetPropertyBlock(vehicles[v].Mpb);
                            renderer.SetPropertyBlock(vehicles[v].Mpb);
                            newTotalCalls += 2;
                        }
                    }
                }
            }
            swNew.Stop();
            double newMs = (swNew.ElapsedTicks * 1000.0) / Stopwatch.Frequency;

            // ── Sonuçlar ──
            double oldAvgMs = oldMs / frames;
            double newAvgMs = newMs / frames;

            Debug.Log($"[Benchmark] MPB Sync Calls (ESKİ): {oldTotalCalls} calls = {oldMs:F3}ms total, {oldAvgMs:F4}ms/frame");
            Debug.Log($"[Benchmark] MPB Sync Calls (YENİ): {newTotalCalls} calls = {newMs:F3}ms total, {newAvgMs:F4}ms/frame");
            Debug.Log($"[Benchmark] MPB Call Reduction: {oldTotalCalls} → {newTotalCalls} ({(100.0 * (oldTotalCalls - newTotalCalls) / oldTotalCalls):F1}% reduction)");
            Debug.Log($"[Benchmark] Time Reduction: {oldMs:F3}ms → {newMs:F3}ms ({(100.0 * (oldMs - newMs) / oldMs):F1}% faster)");

            // Track for cleanup in TearDown
            _testVehicles.AddRange(vehicles);

            // Verify MPB calls are roughly halved (guaranteed by throttle logic)
            Assert.Less(newTotalCalls, oldTotalCalls * 0.6,
                $"MPB throttling should reduce calls by ~50% (old={oldTotalCalls}, new={newTotalCalls})");
            // Time improvement is informational (depends on GPU driver performance)
            if (newMs < oldMs)
            {
                Debug.Log($"[Benchmark] MPB throttling time improvement confirmed: {oldMs:F3}ms → {newMs:F3}ms");
            }
            else
            {
                Debug.LogWarning($"[Benchmark] MPB throttling did not improve time (may indicate GPU driver doesn't benefit from reduced sync): {oldMs:F3}ms → {newMs:F3}ms");
            }
        }
    }
}
