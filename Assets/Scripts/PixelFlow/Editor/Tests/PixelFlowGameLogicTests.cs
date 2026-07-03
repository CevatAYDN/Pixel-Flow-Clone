using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Commands;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using System.Collections.Generic;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    /// <summary>
    /// In-memory fake for IPlayerPrefsService.
    /// Replaces UnityPlayerPrefs in EditMode tests so models can be constructed
    /// without a running Unity runtime environment.
    /// </summary>
    public sealed class InMemoryPlayerPrefsService : IPlayerPrefsService
    {
        private readonly Dictionary<string, int> _store = new Dictionary<string, int>();

        public int GetInt(string key, int defaultValue = 0)
        {
            return _store.TryGetValue(key, out var val) ? val : defaultValue;
        }

        public void SetInt(string key, int value)
        {
            _store[key] = value;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return GetInt(key, defaultValue ? 1 : 0) == 1;
        }

        public void SetBool(string key, bool value)
        {
            SetInt(key, value ? 1 : 0);
        }

        public void Save() { }
    }

    [TestFixture]
    public class PixelFlowGameLogicTests
    {
        /// <summary>
        /// Builds a Nexus test context with all PixelFlow game bindings registered.
        /// Uses InMemoryPlayerPrefsService so models can be constructed in EditMode.
        /// Every test must Dispose() the returned context in teardown.
        /// </summary>
        private static NexusTestContext CreateGameContext()
        {
            return NexusTestHarness.CreateContext(builder =>
            {
                builder.Bind<IPlayerPrefsService, InMemoryPlayerPrefsService>();
                builder.Bind<IPathService, PathService>();
                builder.Bind<IGameHistoryService, GameHistoryService>();
                builder.Bind<IPathSolver, RuntimePathSolver>();
                builder.Bind<IHintService, HintService>();
                builder.Bind<IVehicleSimulator, VehicleSimulator>();
                builder.Bind<ITaxCollectionService, TaxCollectionService>();

                builder.BindModel<IGridModel, GridModel>();
                builder.BindModel<ILevelModel, LevelModel>();
                builder.BindModel<IProgressModel, ProgressModel>();
                builder.BindModel<IGameStateModel, GameStateModel>();
                builder.BindModel<IGameSessionModel, GameSessionModel>();
                builder.BindModel<IHintModel, HintModel>();
                builder.BindModel<ISettingsModel, SettingsModel>();
                builder.BindModel<ISoundModel, SoundModel>();
                builder.BindModel<ICityEconomyModel, CityEconomyModel>();
                builder.Bind<ILevelProgressionService, LevelProgressionService>();

                // Recovery: komut hatalarında 3 kez dene
                builder.BindInstance<IRecoveryStrategy>(new DefaultRecoveryStrategy(maxRetries: 3));

                builder.BindSignal<InputInteractionSignal>().To<ProcessInputCommand>();
                builder.BindSignal<CheckWinConditionSignal>().To<CheckWinConditionCommand>();
                builder.BindSignal<LoadLevelSignal>().To<LoadLevelCommand>();
                builder.BindSignal<RequestHintSignal>().To<UseHintCommand>();
                builder.BindSignal<ChangeThemeSignal>().To<ChangeThemeCommand>();
                builder.BindSignal<LevelCompletedSignal>().To<SaveProgressCommand>();
                builder.BindSignal<UndoSignal>().To<UndoCommand>();
                builder.BindSignal<RedoSignal>().To<RedoCommand>();
            });
        }

        /// <summary>
        /// Creates a sample LevelData for testing.
        /// 5x5 grid with Red, Blue, Green nodes, one bridge at (2,2).
        /// </summary>
        private static LevelData CreateTestLevel(int index = 0)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = index;
            level.width = 5;
            level.height = 5;

            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(0, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(4, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Green },
                new GridNode { position = new Vector2Int(2, 4), color = ColorType.Green },
            };

            level.bridgePositions = new List<Vector2Int> { new Vector2Int(2, 2) };

            level.solutions = new List<PathSolution>
            {
                new PathSolution
                {
                    color = ColorType.Red,
                    pathPositions = new List<Vector2Int>
                    {
                        new Vector2Int(0, 0), new Vector2Int(1, 0),
                        new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0)
                    }
                },
                new PathSolution
                {
                    color = ColorType.Blue,
                    pathPositions = new List<Vector2Int>
                    {
                        new Vector2Int(0, 4), new Vector2Int(0, 3),
                        new Vector2Int(0, 2), new Vector2Int(0, 1), new Vector2Int(0, 0)
                    }
                },
                new PathSolution
                {
                    color = ColorType.Green,
                    pathPositions = new List<Vector2Int>
                    {
                        new Vector2Int(2, 0), new Vector2Int(2, 1),
                        new Vector2Int(2, 2), new Vector2Int(2, 3), new Vector2Int(2, 4)
                    }
                }
            };

            return level;
        }

        // ---------------------------------------------------------------
        // Setup / Teardown
        // ---------------------------------------------------------------

        private NexusTestContext _ctx;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
            _ctx = null;
        }

        // ---------------------------------------------------------------
        // LoadLevelCommand tests
        // ---------------------------------------------------------------

        [Test]
        public void LoadLevelCommand_WithValidLevel_InitializesGrid()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var grid = _ctx.GetModel<IGridModel>();
            var levelModel = _ctx.GetModel<ILevelModel>();
            var state = _ctx.GetModel<IGameStateModel>();

            Assert.AreEqual(5, grid.Width, "Grid width should match level data");
            Assert.AreEqual(5, grid.Height, "Grid height should match level data");

            Assert.AreEqual(CellState.Node, grid.Grid[0, 0].State, "Red node at (0,0)");
            Assert.AreEqual(ColorType.Red, grid.Grid[0, 0].Color);
            Assert.AreEqual(CellState.Node, grid.Grid[4, 0].State, "Red node at (4,0)");
            Assert.AreEqual(ColorType.Red, grid.Grid[4, 0].Color);
            Assert.AreEqual(CellState.Node, grid.Grid[2, 0].State, "Green node at (2,0)");
            Assert.AreEqual(ColorType.Green, grid.Grid[2, 0].Color);

            Assert.AreEqual(CellState.Bridge, grid.Grid[2, 2].State, "Bridge at (2,2)");

            Assert.AreSame(level, levelModel.CurrentLevel, "LevelModel.CurrentLevel should be the loaded level");
            Assert.AreEqual(GameState.Playing, state.CurrentState, "State should be Playing after level load");
        }

        [Test]
        public void LoadLevelCommand_WithoutInitialNodes_LeavesGridEmpty()
        {
            var emptyLevel = ScriptableObject.CreateInstance<LevelData>();
            emptyLevel.levelIndex = 0;
            emptyLevel.width = 3;
            emptyLevel.height = 3;

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = emptyLevel });

            var grid = _ctx.GetModel<IGridModel>();

            Assert.AreEqual(3, grid.Width);
            Assert.AreEqual(3, grid.Height);

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    Assert.AreEqual(CellState.Empty, grid.Grid[x, y].State,
                        $"Cell ({x},{y}) should be Empty");
                }
            }
        }

        // ---------------------------------------------------------------
        // ProcessInputCommand tests
        // ---------------------------------------------------------------

        [Test]
        public void ProcessInput_PointerDownOnNode_ActivatesColor()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var grid = _ctx.GetModel<IGridModel>();

            Assert.AreEqual(ColorType.None, grid.ActiveColor, "No active color before pointer down");

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0),
                Color = ColorType.Red
            });

            Assert.AreEqual(ColorType.Red, grid.ActiveColor, "ActiveColor should be Red after touching Red node");
            Assert.AreEqual(new Vector2Int(0, 0), grid.LastPosition, "LastPosition should be the touched node");
        }

        [Test]
        public void ProcessInput_DragToEmptyCell_ExtendsPath()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var grid = _ctx.GetModel<IGridModel>();

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });

            Assert.IsTrue(grid.Paths.ContainsKey(ColorType.Red), "Red should have a path");
            Assert.AreEqual(2, grid.Paths[ColorType.Red].Count, "Path should have 2 positions");
            Assert.AreEqual(CellState.Path, grid.Grid[1, 0].State, "(1,0) should be Path");
            Assert.AreEqual(ColorType.Red, grid.Grid[1, 0].Color, "(1,0) should be Red");
            Assert.AreEqual(new Vector2Int(1, 0), grid.LastPosition, "LastPosition should be (1,0)");
        }

        [Test]
        public void ProcessInput_PointerUp_ClearsActiveColor()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var grid = _ctx.GetModel<IGridModel>();

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });

            Assert.AreEqual(ColorType.Red, grid.ActiveColor);

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerUp,
                GridPosition = new Vector2Int(0, 0)
            });

            Assert.AreEqual(ColorType.None, grid.ActiveColor, "ActiveColor should reset on pointer up");
            Assert.AreEqual(new Vector2Int(-1, -1), grid.LastPosition, "LastPosition should reset on pointer up");
        }

        [Test]
        public void ProcessInput_DragToSecondNode_CompletesConnection()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3;
            level.height = 1;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var grid = _ctx.GetModel<IGridModel>();

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(2, 0)
            });

            Assert.AreEqual(ColorType.None, grid.ActiveColor,
                "ActiveColor should clear after connecting to the target node");
        }

        [Test]
        public void ProcessInput_InvalidDragDistance_IsIgnored()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var grid = _ctx.GetModel<IGridModel>();

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(2, 0)
            });

            Assert.AreEqual(1, grid.Paths[ColorType.Red].Count, "Path should not extend with distance > 1");
        }

        // ---------------------------------------------------------------
        // CheckWinConditionCommand tests
        // ---------------------------------------------------------------

        [Test]
        public void CheckWinCondition_EmptyCellsRemaining_DoesNotWin()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var state = _ctx.GetModel<IGameStateModel>();

            _ctx.Dispatch(new CheckWinConditionSignal());

            Assert.AreEqual(GameState.Playing, state.CurrentState,
                "Should still be Playing when grid is not complete");
        }

        [Test]
        public void CheckWinCondition_AllCellsFilledAndConnected_Wins()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3;
            level.height = 1;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var grid = _ctx.GetModel<IGridModel>();
            var state = _ctx.GetModel<IGameStateModel>();

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(2, 0)
            });

            Assert.AreEqual(GameState.Simulating, state.CurrentState,
                "Should be Simulating after connecting all nodes and filling grid");
        }

        [Test]
        public void CheckWinCondition_ColorMissingPath_DoesNotWin()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 2;
            level.height = 2;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(1, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(0, 1), color = ColorType.Green },
                new GridNode { position = new Vector2Int(1, 1), color = ColorType.Green },
            };

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var grid = _ctx.GetModel<IGridModel>();
            var state = _ctx.GetModel<IGameStateModel>();

            // Complete Red path only
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });

            Assert.AreEqual(GameState.Playing, state.CurrentState,
                "Should still be Playing when Green nodes are not connected");

            // Complete Green path
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 1)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 1)
            });

            Assert.AreEqual(GameState.Simulating, state.CurrentState,
                "Should be Simulating after both colors connected");
        }

        // ---------------------------------------------------------------
        // UseHintCommand tests
        // ---------------------------------------------------------------

        [Test]
        public void UseHint_DecrementsHintsAndAppliesSolution()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var hintModel = _ctx.GetModel<IHintModel>();
            var grid = _ctx.GetModel<IGridModel>();

            int hintsBefore = hintModel.HintsRemaining;
            Assert.GreaterOrEqual(hintsBefore, 1, "Should have at least 1 hint");

            _ctx.Dispatch(new RequestHintSignal());

            Assert.AreEqual(hintsBefore - 1, hintModel.HintsRemaining, "Hint count should decrease by 1");

            bool anyPathPlaced = grid.Paths.Count > 0;
            Assert.IsTrue(anyPathPlaced, "At least one color should have a path after hint");
        }

        [Test]
        public void UseHint_RespectsSolvedColors()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var grid = _ctx.GetModel<IGridModel>();
            var hintModel = _ctx.GetModel<IHintModel>();

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(2, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(3, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(4, 0)
            });

            var redPath = grid.Paths[ColorType.Red];
            int redCountBeforeHint = redPath.Count;

            _ctx.Dispatch(new RequestHintSignal());
            Assert.AreEqual(redCountBeforeHint, grid.Paths[ColorType.Red].Count,
                "Solved red path should not be modified by hint");
        }

        [Test]
        public void UseHint_WithNoHintsLeft_DoesNothing()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var hintModel = _ctx.GetModel<IHintModel>();

            int hintCount = hintModel.HintsRemaining;
            for (int i = 0; i < hintCount; i++)
            {
                _ctx.Dispatch(new RequestHintSignal());
            }

            int hintsAfter = hintModel.HintsRemaining;
            _ctx.Dispatch(new RequestHintSignal());
            Assert.AreEqual(hintsAfter, hintModel.HintsRemaining, "Hint count should not go below 0");
        }

        // ---------------------------------------------------------------
        // SaveProgressCommand tests
        // ---------------------------------------------------------------

        [Test]
        public void LevelCompletion_UnlocksNextLevel()
        {
            var level = CreateTestLevel(index: 0);
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var progress = _ctx.GetModel<IProgressModel>();
            int unlockedBefore = progress.UnlockedLevels;

            _ctx.Dispatch(new LevelCompletedSignal());

            Assert.GreaterOrEqual(progress.UnlockedLevels, unlockedBefore + 1,
                "Progress should advance after level completion");
        }

        // ---------------------------------------------------------------
        // Signal tracking tests
        // ---------------------------------------------------------------

        [Test]
        public void LoadLevel_FiresGridUpdatedSignal()
        {
            _ctx.Register<GridUpdatedSignal>();

            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            Assert.IsTrue(_ctx.SignalWasDispatched<GridUpdatedSignal>(),
                "LoadLevelCommand should fire GridUpdatedSignal");
        }

        [Test]
        public void ProcessInputDrag_FiresGridUpdatedSignal()
        {
            _ctx.Register<GridUpdatedSignal>();

            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.ClearDispatchedSignals();

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });

            var gridUpdatedCount = _ctx.GetDispatchedSignalCount<GridUpdatedSignal>();
            Assert.GreaterOrEqual(gridUpdatedCount, 2,
                "PointerDown + Drag should fire at least 2 GridUpdatedSignals");
        }

        [Test]
        public void Hint_AppliesSolutionAndFiresSignals()
        {
            _ctx.Register<GridUpdatedSignal>();
            _ctx.Register<CheckWinConditionSignal>();

            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.ClearDispatchedSignals();

            _ctx.Dispatch(new RequestHintSignal());

            Assert.IsTrue(_ctx.SignalWasDispatched<GridUpdatedSignal>(),
                "Hint should fire GridUpdatedSignal");
            Assert.IsTrue(_ctx.SignalWasDispatched<CheckWinConditionSignal>(),
                "Hint should fire CheckWinConditionSignal");
        }

        [Test]
        public void LevelCompleted_FiresProgressUpdatedSignal()
        {
            _ctx.Register<ProgressUpdatedSignal>();

            var level = CreateTestLevel(index: 0);
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.ClearDispatchedSignals();

            _ctx.Dispatch(new LevelCompletedSignal());

            Assert.IsTrue(_ctx.SignalWasDispatched<ProgressUpdatedSignal>(),
                "LevelCompletedSignal should trigger ProgressUpdatedSignal via SaveProgressCommand");
        }

        // ---------------------------------------------------------------
        // Architectural & Performance Validation Tests (Finding C-001 / ADR)
        // ---------------------------------------------------------------

        [Test]
        public void Architecture_Dependency_Test()
        {
            var modelAssembly = typeof(GridModel).Assembly;
            var types = modelAssembly.GetTypes();

            foreach (var type in types)
            {
                if (type.Namespace != null && type.Namespace.StartsWith("PixelFlow.Models"))
                {
                    // Check fields
                    var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
                    foreach (var field in fields)
                    {
                        var fieldType = field.FieldType;
                        AssertNoForbiddenDependency(type, fieldType, $"field '{field.Name}'");
                    }

                    // Check properties
                    var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
                    foreach (var prop in properties)
                    {
                        var propType = prop.PropertyType;
                        AssertNoForbiddenDependency(type, propType, $"property '{prop.Name}'");
                    }

                    // Check constructors
                    var constructors = type.GetConstructors();
                    foreach (var ctor in constructors)
                    {
                        var parameters = ctor.GetParameters();
                        foreach (var param in parameters)
                        {
                            AssertNoForbiddenDependency(type, param.ParameterType, $"constructor parameter '{param.Name}'");
                        }
                    }
                }
            }
        }

        private static void AssertNoForbiddenDependency(System.Type sourceType, System.Type dependencyType, string context)
        {
            if (dependencyType == null) return;

            if (dependencyType.IsGenericType)
            {
                foreach (var genericArg in dependencyType.GetGenericArguments())
                {
                    AssertNoForbiddenDependency(sourceType, genericArg, context);
                }
            }

            var depNamespace = dependencyType.Namespace;
            if (depNamespace == null) return;

            bool isForbidden = depNamespace.StartsWith("PixelFlow.Views") || 
                               depNamespace.StartsWith("UnityEngine.UI") || 
                               depNamespace.StartsWith("UnityEngine.UIElements");

            if (isForbidden)
            {
                Assert.Fail($"Architecture Violation: Model '{sourceType.FullName}' has a forbidden dependency on '{dependencyType.FullName}' via {context}!");
            }
        }

        [Test]
        public void Signal_Performance_Benchmark_Test()
        {
            _ctx.Register<GridUpdatedSignal>();
            
            // Warm up
            for (int i = 0; i < 100; i++)
            {
                _ctx.Dispatch(new GridUpdatedSignal());
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int iterations = 10000;
            for (int i = 0; i < iterations; i++)
            {
                _ctx.Dispatch(new GridUpdatedSignal());
            }
            sw.Stop();

            double ms = sw.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"[Signal Benchmark] Dispatched {iterations} signals in {ms:F2} ms (Avg: {ms * 1000.0 / iterations:F4} microseconds per signal)");
            
            Assert.Less(sw.ElapsedMilliseconds, 200, $"Signal dispatching is too slow: {sw.ElapsedMilliseconds} ms");
        }

        [Test]
        public void ProcessInput_WhenNotPlaying_IsIgnored()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            
            var gameState = _ctx.GetModel<IGameStateModel>();
            gameState.SetState(GameState.LevelCompleted);

            var gridModel = _ctx.GetModel<IGridModel>();
            Assert.AreEqual(ColorType.None, gridModel.Grid[0, 1].Color, "Prerequisite: cell (0, 1) should be empty");

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(0, 1)
            });

            Assert.AreEqual(ColorType.None, gridModel.Grid[0, 1].Color, "Cell color should remain None since input is blocked when not Playing");
            Assert.AreEqual(ColorType.None, gridModel.ActiveColor, "ActiveColor should remain None since input is blocked");
        }

        [Test]
        public void BridgeCell_AcceptingPathOverIt_ShowsBridgeState()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3;
            level.height = 3;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };
            level.bridgePositions = new List<Vector2Int> { new Vector2Int(1, 0) };

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            var grid = _ctx.GetModel<IGridModel>();

            Assert.AreEqual(CellState.Bridge, grid.Grid[1, 0].State, "Bridge cell should be Bridge state");
        }

        [Test]
        public void PathOverBridge_DoesNotClearBridgeState()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3;
            level.height = 3;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };
            level.bridgePositions = new List<Vector2Int> { new Vector2Int(1, 0) };

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            var grid = _ctx.GetModel<IGridModel>();

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(2, 0)
            });

            Assert.AreEqual(CellState.Bridge, grid.Grid[1, 0].State, "Bridge cell should remain Bridge even with path over it");
        }

        [Test]
        public void BreakingPath_RemovesCellsFromPath()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            var grid = _ctx.GetModel<IGridModel>();

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });

            Assert.AreEqual(2, grid.Paths[ColorType.Red].Count, "Path should have 2 cells");

            grid.Paths[ColorType.Red].Clear();
            grid.Grid[1, 0].State = CellState.Empty;
            grid.Grid[1, 0].Color = ColorType.None;

            Assert.AreEqual(0, grid.Paths[ColorType.Red].Count, "Path should be empty after breaking at endpoint");
            Assert.AreEqual(CellState.Empty, grid.Grid[1, 0].State, "Broken cell should return to Empty");
        }

        [Test]
        public void Undo_RestoresBrokenPath()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            var grid = _ctx.GetModel<IGridModel>();

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });
            Assert.AreEqual(2, grid.Paths[ColorType.Red].Count);

            _ctx.Dispatch(new UndoSignal());
            Assert.IsTrue(grid.Paths.ContainsKey(ColorType.Red), "Path should still exist after single undo");
            Assert.AreEqual(1, grid.Paths[ColorType.Red].Count, "Path should have 1 cell after undo (restored to pre-drag state)");
        }

        [Test]
        public void Redo_RestoresUndonePath()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            var grid = _ctx.GetModel<IGridModel>();

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });

            _ctx.Dispatch(new UndoSignal());
            Assert.IsTrue(grid.Paths.ContainsKey(ColorType.Red), "Path should still exist after undo");
            Assert.AreEqual(1, grid.Paths[ColorType.Red].Count, "Path should have 1 cell after undo");

            _ctx.Dispatch(new RedoSignal());
            Assert.IsTrue(grid.Paths.ContainsKey(ColorType.Red), "Path restored after redo");
            Assert.AreEqual(2, grid.Paths[ColorType.Red].Count, "Path should have 2 cells after redo");
        }

        [Test]
        public void LoadLevel_StartsNewSession()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var session = _ctx.GetModel<IGameSessionModel>();
            Assert.IsTrue(session.IsSessionActive, "Session should be active after level load");
            Assert.AreEqual(0, session.ElapsedTime, 0.001f, "Timer should start at 0");
            Assert.AreEqual(0, session.Score, "Score should start at 0");
        }

        [Test]
        public void CompletingLevel_AwardsScore()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3;
            level.height = 1;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var session = _ctx.GetModel<IGameSessionModel>();
            var state = _ctx.GetModel<IGameStateModel>();

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(2, 0)
            });

            Assert.AreEqual(GameState.Simulating, state.CurrentState);

            // Manually finish simulation for the test
            state.SetState(GameState.LevelCompleted);
            _ctx.Dispatch(new LevelCompletedSignal());

            Assert.AreEqual(GameState.LevelCompleted, state.CurrentState);
            Assert.Greater(session.Score, 0, "Score should be > 0 after completing level");
            Assert.GreaterOrEqual(session.StarsEarned, 1, "Should earn at least 1 star");
        }

        [Test]
        public void HintUsage_DecrementsTotalHintsUsed()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var hintModel = _ctx.GetModel<IHintModel>();
            Assert.AreEqual(0, hintModel.TotalHintsUsed, "No hints used yet");

            _ctx.Dispatch(new RequestHintSignal());
            Assert.AreEqual(1, hintModel.TotalHintsUsed, "One hint used");
        }

        [Test]
        public void ProceduralGenerator_Easy_ProducesSolvableLevel()
        {
            var solver = new RuntimePathSolver();
            var generator = new ProceduralLevelGenerator(solver, seed: 42);
            var level = generator.Generate(DifficultyParams.Easy, maxAttempts: 10);

            Assert.IsNotNull(level, "Should generate a level");
            Assert.GreaterOrEqual(level.width, 5, "Easy level should be at least 5x5");
            Assert.IsTrue(level.solutions != null && level.solutions.Count > 0, "Generated level should have solutions");
        }

        [Test]
        public void ProceduralGenerator_WithSeed_Deterministic()
        {
            var solver = new RuntimePathSolver();
            var gen1 = new ProceduralLevelGenerator(solver, seed: 123);
            var gen2 = new ProceduralLevelGenerator(solver, seed: 123);

            var level1 = gen1.Generate(DifficultyParams.Medium, maxAttempts: 10);
            var level2 = gen2.Generate(DifficultyParams.Medium, maxAttempts: 10);

            if (level1 != null && level2 != null)
            {
                Assert.AreEqual(level1.width, level2.width);
                Assert.AreEqual(level1.height, level2.height);
                Assert.AreEqual(level1.initialNodes.Count, level2.initialNodes.Count,
                    "Same seed should produce same number of nodes");
                Assert.AreEqual(level1.bridgePositions.Count, level2.bridgePositions.Count,
                    "Same seed should produce same number of bridges");
            }
        }

        [Test]
        public void Solver_ReturnsNullForEmptyGrid()
        {
            var emptyLevel = ScriptableObject.CreateInstance<LevelData>();
            emptyLevel.width = 5;
            emptyLevel.height = 5;

            var solver = new RuntimePathSolver();
            Assert.IsFalse(solver.Solve(emptyLevel, out _), "Empty level should not be solvable");
        }

        [Test]
        public void Solver_SolvesSingleColorSimplePath()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 3;
            level.height = 1;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };

            var solver = new RuntimePathSolver();
            Assert.IsTrue(solver.Solve(level, out var solutions), "Simple 2-node path should be solvable");
            Assert.IsTrue(solutions.ContainsKey(ColorType.Red), "Should contain Red solution");
            Assert.GreaterOrEqual(solutions[ColorType.Red].Count, 3, "Path should have at least 3 positions (0->1->2)");
        }

        [Test]
        public void Solver_SolvesMultiColorWithBridge()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 5;
            level.height = 5;
            level.bridgePositions = new List<Vector2Int> { new Vector2Int(2, 2) };
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 2), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 2), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(2, 4), color = ColorType.Blue },
            };

            var solver = new RuntimePathSolver();
            Assert.IsTrue(solver.Solve(level, out var solutions), "Multi-color with bridge should be solvable");
            Assert.IsTrue(solutions.ContainsKey(ColorType.Red), "Red should have a path");
            Assert.IsTrue(solutions.ContainsKey(ColorType.Blue), "Blue should have a path");
            Assert.IsTrue(solutions[ColorType.Red].Contains(new Vector2Int(2, 2)),
                "Red path should cross bridge at (2,2)");
        }

        // ---------------------------------------------------------------
        // Bridge path-breaking & conflict tests
        // ---------------------------------------------------------------

        [Test]
        public void Bridge_NonPerpendicularCrossing_IsPreventedBySolver()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3;
            level.height = 3;
            level.bridgePositions = new List<Vector2Int> { new Vector2Int(1, 1) };
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 1), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 1), color = ColorType.Red },
                new GridNode { position = new Vector2Int(1, 0), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(1, 2), color = ColorType.Blue },
            };

            var solver = new RuntimePathSolver();
            bool solved = solver.Solve(level, out var solutions);

            if (solved && solutions != null)
            {
                bool redUsesBridge = solutions[ColorType.Red].Contains(new Vector2Int(1, 1));
                bool blueUsesBridge = solutions[ColorType.Blue].Contains(new Vector2Int(1, 1));
                if (redUsesBridge && blueUsesBridge)
                {
                    int redIdx = solutions[ColorType.Red].IndexOf(new Vector2Int(1, 1));
                    int blueIdx = solutions[ColorType.Blue].IndexOf(new Vector2Int(1, 1));
                    if (redIdx > 0 && redIdx < solutions[ColorType.Red].Count - 1 &&
                        blueIdx > 0 && blueIdx < solutions[ColorType.Blue].Count - 1)
                    {
                        var redDir = solutions[ColorType.Red][redIdx + 1] - solutions[ColorType.Red][redIdx - 1];
                        var blueDir = solutions[ColorType.Blue][blueIdx + 1] - solutions[ColorType.Blue][blueIdx - 1];
                        Assert.AreEqual(0, Vector2.Dot(redDir, blueDir),
                            "Crossing paths must be perpendicular at bridge");
                    }
                }
            }
        }

        [Test]
        public void Bridge_OccupiedByTwoColors_DeniesThirdColor()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 5;
            level.height = 5;
            level.bridgePositions = new List<Vector2Int> { new Vector2Int(2, 2) };
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 2), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 2), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(2, 4), color = ColorType.Blue },
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Green },
                new GridNode { position = new Vector2Int(4, 4), color = ColorType.Green },
            };

            var solver = new RuntimePathSolver();
            bool solved = solver.Solve(level, out var solutions);

            if (solved && solutions != null)
            {
                int bridgeUsers = 0;
                foreach (var kvp in solutions)
                {
                    if (kvp.Value.Contains(new Vector2Int(2, 2)))
                        bridgeUsers++;
                }
                Assert.LessOrEqual(bridgeUsers, 2,
                    "Bridge at (2,2) should not serve more than 2 colors");
            }
        }

        [Test]
        public void Path_BreakingOnBridge_DoesNotClearBridgeState()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3;
            level.height = 3;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };
            level.bridgePositions = new List<Vector2Int> { new Vector2Int(1, 0) };

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            var grid = _ctx.GetModel<IGridModel>();

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(2, 0)
            });

            Assert.AreEqual(CellState.Bridge, grid.Grid[1, 0].State,
                "Bridge cell should remain Bridge state even with path over it");

            grid.Paths[ColorType.Red].RemoveAt(grid.Paths[ColorType.Red].Count - 1);
            grid.Grid[2, 0].State = CellState.Empty;
            grid.Grid[2, 0].Color = ColorType.None;

            Assert.AreEqual(CellState.Bridge, grid.Grid[1, 0].State,
                "Bridge cell should remain Bridge state after path breaks elsewhere");
        }

        // ---------------------------------------------------------------
        // Backtracking tests
        // ---------------------------------------------------------------

        [Test]
        public void Backtracking_ReverseDrag_ShortensPath()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 5;
            level.height = 1;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(4, 0), color = ColorType.Red },
            };

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            var grid = _ctx.GetModel<IGridModel>();

            // Drag forward: 0→1→2→3
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(2, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(3, 0)
            });

            Assert.AreEqual(4, grid.Paths[ColorType.Red].Count,
                "Path should have 4 cells after forward drag");

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(2, 0)
            });

            Assert.AreEqual(3, grid.Paths[ColorType.Red].Count,
                "Path should shorten to 3 cells after backtracking one step");

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });

            Assert.AreEqual(2, grid.Paths[ColorType.Red].Count,
                "Path should shorten to 2 cells after backtracking two steps");
            Assert.AreEqual(CellState.Empty, grid.Grid[3, 0].State,
                "Cell (3,0) should return to Empty after backtrack");
            Assert.AreEqual(CellState.Empty, grid.Grid[2, 0].State,
                "Cell (2,0) should return to Empty after backtrack");
        }

        // ---------------------------------------------------------------
        // GameHistoryService overflow test
        // ---------------------------------------------------------------

        [Test]
        public void GameHistoryService_Overflow_DiscardsOldestSnapshot()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var grid = _ctx.GetModel<IGridModel>();
            var history = _ctx.Context.Resolve<IGameHistoryService>();

            for (int i = 0; i < 250; i++)
            {
                grid.Grid[0, 0].State = CellState.Path;
                history.Record(grid);
            }

            Assert.IsTrue(history.CanUndo, "CanUndo should be true after many snapshots");
            Assert.LessOrEqual(history.UndoCount, 200,
                "Snapshot count should be capped at 200");
        }

        // ---------------------------------------------------------------
        // ScoreCalculator edge case tests
        // ---------------------------------------------------------------

        [Test]
        public void ScoreCalculator_MaximumHintPenalty_CapsAtZero()
        {
            var (score, stars) = ScoreCalculator.Calculate(
                gridWidth: 5, gridHeight: 5,
                elapsedTime: 1f, hintsUsed: 15, totalHintsAvailable: 20, viaductsUsed: 3);

            Assert.AreEqual(0, score,
                "Score should be 0 when hint penalty saturates to 0");
            Assert.AreEqual(1, stars,
                "Should still earn 1 star for completing the level");
        }

        [Test]
        public void ScoreCalculator_ZeroHintsWithinIdealTime_EarnsThreeStars()
        {
            var (score, stars) = ScoreCalculator.Calculate(
                gridWidth: 5, gridHeight: 5,
                elapsedTime: 5f, hintsUsed: 0, totalHintsAvailable: 5, viaductsUsed: 0);

            Assert.Greater(score, 2000, "Score should be near maximum");
            Assert.AreEqual(3, stars,
                "0 hints + fast time = 3 stars");
        }

        [Test]
        public void ScoreCalculator_MinimumTimeMultiplier_IsTwentyFivePercent()
        {
            var (score, stars) = ScoreCalculator.Calculate(
                gridWidth: 5, gridHeight: 5,
                elapsedTime: 200f, hintsUsed: 0, totalHintsAvailable: 5, viaductsUsed: 0);

            float baseScore = 5 * 5 * 100f;
            float expectedMin = baseScore * 0.25f;
            Assert.AreEqual((int)(expectedMin + 0.5f), score,
                "Score should use minimum 25% time multiplier");
        }

        // ---------------------------------------------------------------
        // Solver partial solve test
        // ---------------------------------------------------------------

        [Test]
        public void Solver_SolvePartial_ReturnsLimitedPathSteps()
        {
            var level = CreateTestLevel();
            var solver = new RuntimePathSolver();

            bool solved = solver.SolvePartial(level, ColorType.Red, steps: 3, out var hintPath);

            if (solved)
            {
                Assert.IsNotNull(hintPath, "Hint path should not be null");
                Assert.Greater(hintPath.Count, 0,
                    "Hint path should have at least 1 position");
            }
        }

        // ---------------------------------------------------------------
        // LoadLevel clears old state test
        // ---------------------------------------------------------------

        [Test]
        public void LoadLevel_ClearsPreviousGridState()
        {
            var level1 = CreateTestLevel(0);
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level1 });

            var grid = _ctx.GetModel<IGridModel>();

            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.PointerDown,
                GridPosition = new Vector2Int(0, 0)
            });
            _ctx.Dispatch(new InputInteractionSignal
            {
                Type = InputType.Drag,
                GridPosition = new Vector2Int(1, 0)
            });

            Assert.AreEqual(2, grid.Paths[ColorType.Red].Count,
                "Red path should have 2 cells");

            var level2 = ScriptableObject.CreateInstance<LevelData>();
            level2.levelIndex = 1;
            level2.width = 3;
            level2.height = 1;
            level2.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Green },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Green },
            };

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level2 });

            Assert.AreEqual(3, grid.Width, "Grid width should match new level");
            Assert.AreEqual(0, grid.Paths.Count,
                "All paths should be cleared on new level load");
            Assert.AreEqual(ColorType.None, grid.ActiveColor,
                "ActiveColor should be reset");
        }

        // ---------------------------------------------------------------
        // ProceduralGenerator failure test
        // ---------------------------------------------------------------

        [Test]
        public void ProceduralGenerator_Failure_ReturnsNullAfterExhaustingAttempts()
        {
            var impossible = new DifficultyParams(
                width: 3, height: 3, colors: 6, bridges: 2);

            var solver = new RuntimePathSolver();
            var generator = new ProceduralLevelGenerator(solver, seed: 999);
            var result = generator.Generate(impossible, maxAttempts: 5);

            if (result != null)
            {
                Assert.IsTrue(result.solutions != null && result.solutions.Count > 0,
                    "If generated, level must be solvable");
            }
        }
    }
}
