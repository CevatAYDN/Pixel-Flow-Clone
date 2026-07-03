using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using System.Collections.Generic;
using UnityEngine;
using static PixelFlow.Editor.Tests.GameTestContext;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class PathServiceTests
    {
        private NexusTestContext _ctx;
        private IPathService _pathService;
        private IGridModel _grid;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
            _pathService = _ctx.Context.Container.Resolve<IPathService>();
            _grid = _ctx.GetModel<IGridModel>();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        private void LoadAndDrawPath()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            // Red path vertically: (0,0) → (0,1) → (0,2) → (0,3) to avoid Green node at (2,0)
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 1) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 2) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 3) });
        }

        [Test]
        public void Backtrack_ReverseDrag_ShortensPath()
        {
            LoadAndDrawPath();
            // 4 cells: (0,0), (0,1), (0,2), (0,3)
            Assert.AreEqual(4, _grid.Paths[ColorType.Red].Count);

            // Backtrack to (0,2) — removes (0,3), so path becomes 3 cells
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 2) });
            Assert.AreEqual(3, _grid.Paths[ColorType.Red].Count,
                "Path should shorten to 3 after backtracking one step");

            // Backtrack to (0,1) — removes (0,2), so path becomes 2 cells
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 1) });
            Assert.AreEqual(2, _grid.Paths[ColorType.Red].Count,
                "Path should shorten to 2 after backtracking two steps");
        }

        [Test]
        public void Backtrack_ClearsCellState()
        {
            LoadAndDrawPath();
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 2) });

            Assert.AreEqual(CellState.Empty, _grid.Grid[0, 3].State,
                "Backtracked cell should return to Empty");
            Assert.AreEqual(ColorType.None, _grid.Grid[0, 3].Color,
                "Backtracked cell should lose its color");
        }

        [Test]
        public void BacktrackToNode_LeavesNodeIntact()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });

            // Backtrack to starting node (0,0)
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 0) });

            Assert.AreEqual(1, _grid.Paths[ColorType.Red].Count,
                "Path should have only the starting node");
            Assert.AreEqual(CellState.Node, _grid.Grid[0, 0].State,
                "Starting node should remain Node state");
            Assert.AreEqual(ColorType.Red, _grid.Grid[0, 0].Color,
                "Starting node should keep its color");
        }

        [Test]
        public void PathOnBridge_DoesNotClearBridgeState()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3; level.height = 3;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };
            level.bridgePositions = new List<Vector2Int> { new Vector2Int(1, 0) };

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });

            Assert.AreEqual(CellState.Bridge, _grid.Grid[1, 0].State,
                "Bridge cell should remain Bridge even with path over it");
        }

        [Test]
        public void PathBreakingOnBridge_KeepsBridge()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3; level.height = 3;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };
            level.bridgePositions = new List<Vector2Int> { new Vector2Int(1, 0) };

            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(2, 0) });

            // Manually break path at end
            _grid.Paths[ColorType.Red].RemoveAt(_grid.Paths[ColorType.Red].Count - 1);
            _grid.Grid[2, 0].State = CellState.Empty;
            _grid.Grid[2, 0].Color = ColorType.None;

            Assert.AreEqual(CellState.Bridge, _grid.Grid[1, 0].State,
                "Bridge cell should remain Bridge after path breaks");
        }
    }
}
