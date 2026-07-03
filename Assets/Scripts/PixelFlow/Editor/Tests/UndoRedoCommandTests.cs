using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using UnityEngine;
using static PixelFlow.Editor.Tests.GameTestContext;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class UndoRedoCommandTests
    {
        private NexusTestContext _ctx;
        private IGridModel _grid;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
            _grid = _ctx.GetModel<IGridModel>();
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        [Test]
        public void Undo_AfterDrag_RemovesLastCell()
        {
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            Assert.AreEqual(2, _grid.Paths[ColorType.Red].Count);

            _ctx.Dispatch(new UndoSignal());
            Assert.IsTrue(_grid.Paths.ContainsKey(ColorType.Red));
            Assert.AreEqual(1, _grid.Paths[ColorType.Red].Count);
        }

        [Test]
        public void Undo_MultipleDrags_StepsBackOneAtATime()
        {
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 1) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 2) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 3) });
            Assert.AreEqual(4, _grid.Paths[ColorType.Red].Count);

            _ctx.Dispatch(new UndoSignal());
            Assert.AreEqual(3, _grid.Paths[ColorType.Red].Count);
            _ctx.Dispatch(new UndoSignal());
            Assert.AreEqual(2, _grid.Paths[ColorType.Red].Count);
        }

        [Test]
        public void Redo_RestoresUndoneCell()
        {
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            Assert.AreEqual(2, _grid.Paths[ColorType.Red].Count);

            _ctx.Dispatch(new UndoSignal());
            Assert.AreEqual(1, _grid.Paths[ColorType.Red].Count);

            _ctx.Dispatch(new RedoSignal());
            Assert.AreEqual(2, _grid.Paths[ColorType.Red].Count);
        }

        [Test]
        public void Undo_AtStart_DoesNothing()
        {
            _ctx.Dispatch(new UndoSignal());
            Assert.AreEqual(0, _grid.Paths.Count);
        }

        [Test]
        public void Redo_AtStart_DoesNothing()
        {
            _ctx.Dispatch(new RedoSignal());
            Assert.AreEqual(0, _grid.Paths.Count);
        }

        [Test]
        public void Undo_ThenDrag_ClearsRedoHistory()
        {
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            _ctx.Dispatch(new UndoSignal());
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });

            _ctx.Dispatch(new RedoSignal());
            Assert.AreEqual(2, _grid.Paths[ColorType.Red].Count,
                "Redo should do nothing after new action clears redo history");
        }

        [Test]
        public void Undo_RestoresCellState()
        {
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            Assert.AreEqual(CellState.Path, _grid.Grid[1, 0].State);
            Assert.AreEqual(ColorType.Red, _grid.Grid[1, 0].Color);

            _ctx.Dispatch(new UndoSignal());
            Assert.AreEqual(CellState.Empty, _grid.Grid[1, 0].State);
            Assert.AreEqual(ColorType.None, _grid.Grid[1, 0].Color);
        }
    }
}
