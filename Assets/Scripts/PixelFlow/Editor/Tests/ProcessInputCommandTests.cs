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
    public class ProcessInputCommandTests
    {
        private NexusTestContext _ctx;
        private IGridModel _grid;
        private IGameStateModel _state;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
            _grid = _ctx.GetModel<IGridModel>();
            _state = _ctx.GetModel<IGameStateModel>();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        private void LoadLevel()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
        }

        [Test]
        public void PointerDownOnNode_ActivatesColor()
        {
            LoadLevel();
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            Assert.AreEqual(ColorType.Red, _grid.ActiveColor.Value);
            Assert.AreEqual(new Vector2Int(0, 0), _grid.LastPosition.Value);
        }

        [Test]
        public void PointerDownOnEmptyCell_DoesNothing()
        {
            LoadLevel();
            _grid.Grid[3, 3].State = CellState.Empty;
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(3, 3) });
            Assert.AreEqual(ColorType.None, _grid.ActiveColor.Value);
        }

        [Test]
        public void DragToAdjacentCell_ExtendsPath()
        {
            LoadLevel();
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            Assert.IsTrue(_grid.Paths.ContainsKey(ColorType.Red));
            Assert.AreEqual(2, _grid.Paths[ColorType.Red].Count);
            Assert.AreEqual(CellState.Path, _grid.Grid[1, 0].State);
            Assert.AreEqual(ColorType.Red, _grid.Grid[1, 0].Color);
        }

        [Test]
        public void DragToNonAdjacentCell_Ignored()
        {
            LoadLevel();
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(2, 0) });
            Assert.AreEqual(1, _grid.Paths[ColorType.Red].Count);
        }

        [Test]
        public void PointerUp_ResetsState()
        {
            LoadLevel();
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            Assert.AreEqual(ColorType.Red, _grid.ActiveColor.Value);
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerUp, GridPosition = new Vector2Int(0, 0) });
            Assert.AreEqual(ColorType.None, _grid.ActiveColor.Value);
            Assert.AreEqual(new Vector2Int(-1, -1), _grid.LastPosition.Value);
        }

        [Test]
        public void DragToSecondNode_CompletesConnection()
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

            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(2, 0) });
            Assert.AreEqual(ColorType.None, _grid.ActiveColor.Value,
                "ActiveColor should clear after connecting to target node");
        }

        [Test]
        public void InputInNonPlayingState_Ignored()
        {
            LoadLevel();
            _state.SetState(GameState.LevelCompleted);

            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            Assert.AreEqual(ColorType.None, _grid.ActiveColor.Value,
                "Input should be blocked when state is not Playing");
        }

        [Test]
        public void PointerDownOnOccupiedByOtherColor_DoesNotActivate()
        {
            LoadLevel();
            // Set cell (1,0) as occupied by Blue
            _grid.Grid[1, 0].State = CellState.Path;
            _grid.Grid[1, 0].Color = ColorType.Blue;

            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(1, 0) });
            Assert.AreEqual(ColorType.Blue, _grid.ActiveColor.Value,
                "Should activate Blue when touching a cell occupied by Blue path");
        }

        [Test]
        public void DragToOccupiedCell_MaxTwoPathsLimit()
        {
            LoadLevel();
            // Occupy (2,0) with 2 colors (Blue and Green) before Red reaches it
            _grid.Grid[2, 0].State = CellState.Path;
            _grid.Grid[2, 0].AddPathColor(ColorType.Blue);
            _grid.Grid[2, 0].AddPathColor(ColorType.Green);

            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(2, 0) });

            Assert.AreEqual(2, _grid.Paths[ColorType.Red].Count,
                "Red path should not extend into cell occupied by 2 other colors (Max 2 paths limit)");
        }

        [Test]
        public void DragBackToAnyCellInPath_SmoothBacktracks()
        {
            LoadLevel();
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 1) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 2) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 3) });
            Assert.AreEqual(4, _grid.Paths[ColorType.Red].Count);

            // Drag back: first to (0,2)
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 2) });
            // Then to (0,1)
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 1) });

            Assert.AreEqual(2, _grid.Paths[ColorType.Red].Count, "Path must shorten to 2 cells");
            Assert.AreEqual(new Vector2Int(0, 1), _grid.LastPosition.Value);
        }
    }
}
