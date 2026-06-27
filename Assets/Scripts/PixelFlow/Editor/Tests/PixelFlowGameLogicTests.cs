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

                builder.BindModel<IGridModel, GridModel>();
                builder.BindModel<ILevelModel, LevelModel>();
                builder.BindModel<IProgressModel, ProgressModel>();
                builder.BindModel<IGameStateModel, GameStateModel>();
                builder.BindModel<IHintModel, HintModel>();
                builder.BindModel<ISettingsModel, SettingsModel>();
                builder.BindModel<ISoundModel, SoundModel>();

                builder.BindSignal<InputInteractionSignal>().To<ProcessInputCommand>();
                builder.BindSignal<CheckWinConditionSignal>().To<CheckWinConditionCommand>();
                builder.BindSignal<LoadLevelSignal>().To<LoadLevelCommand>();
                builder.BindSignal<RequestHintSignal>().To<UseHintCommand>();
                builder.BindSignal<ChangeThemeSignal>().To<ChangeThemeCommand>();
                builder.BindSignal<LevelCompletedSignal>().To<SaveProgressCommand>();
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

            Assert.AreEqual(GameState.LevelCompleted, state.CurrentState,
                "Should be LevelCompleted after connecting all nodes and filling grid");
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

            Assert.AreEqual(GameState.LevelCompleted, state.CurrentState,
                "Should be LevelCompleted after both colors connected");
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
            Assert.IsTrue(grid.Paths.ContainsKey(ColorType.Red), "Red should have a path after hint");
            Assert.IsTrue(grid.LockedColors.Contains(ColorType.Red), "Red should be locked after hint");
        }

        [Test]
        public void UseHint_OnlyAppliesToUnsolvedColor()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            var grid = _ctx.GetModel<IGridModel>();
            var hintModel = _ctx.GetModel<IHintModel>();

            _ctx.Dispatch(new RequestHintSignal());
            Assert.AreEqual(1, grid.LockedColors.Count, "One color should be locked");

            _ctx.Dispatch(new RequestHintSignal());
            Assert.AreEqual(2, grid.LockedColors.Count, "Two colors should be locked");

            _ctx.Dispatch(new RequestHintSignal());
            Assert.AreEqual(3, grid.LockedColors.Count, "All three colors should be locked");
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
    }
}
