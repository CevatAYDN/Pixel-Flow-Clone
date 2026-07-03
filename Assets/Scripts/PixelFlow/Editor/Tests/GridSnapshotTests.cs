using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using System.Collections.Generic;
using UnityEngine;
using static PixelFlow.Editor.Tests.GameTestContext;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class GridSnapshotTests
    {
        private NexusTestContext _ctx;
        private IGridModel _grid;
        private IGameHistoryService _history;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
            _grid = _ctx.GetModel<IGridModel>();
            _history = _ctx.Context.Container.Resolve<IGameHistoryService>();
            _grid.Initialize(3, 3);
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        [Test]
        public void Record_CapturesCellState()
        {
            _history.Record(_grid);
            _grid.Grid[1, 0].State = CellState.Path;
            _grid.Grid[1, 0].Color = ColorType.Red;

            Assert.AreEqual(CellState.Path, _grid.Grid[1, 0].State);

            _history.Undo(_grid);
            Assert.AreEqual(CellState.Empty, _grid.Grid[1, 0].State,
                "Undo should restore cell to pre-record state");
        }

        [Test]
        public void Record_CapturesCellColor()
        {
            _grid.Grid[1, 0].State = CellState.Path;
            _grid.Grid[1, 0].Color = ColorType.Red;
            _history.Record(_grid);

            _grid.Grid[1, 0].Color = ColorType.Blue;
            _history.Undo(_grid);

            Assert.AreEqual(ColorType.Red, _grid.Grid[1, 0].Color,
                "Undo should restore cell color");
        }

        [Test]
        public void Record_CapturesObstacleType()
        {
            _grid.Grid[1, 0].ObstacleType = ObstacleType.Lake;
            _history.Record(_grid);

            _grid.Grid[1, 0].ObstacleType = ObstacleType.Ferry;
            _history.Undo(_grid);

            Assert.AreEqual(ObstacleType.Lake, _grid.Grid[1, 0].ObstacleType,
                "Undo should restore ObstacleType");
        }

        [Test]
        public void Record_CapturesPaths()
        {
            _grid.Paths[ColorType.Red] = new List<Vector2Int> { new Vector2Int(0, 0) };
            _history.Record(_grid);

            _grid.Paths.Remove(ColorType.Red);
            _history.Undo(_grid);

            Assert.IsTrue(_grid.Paths.ContainsKey(ColorType.Red),
                "Undo should restore Paths dictionary");
        }

        [Test]
        public void Record_CapturesActiveColor()
        {
            _history.Record(_grid);
            _grid.ActiveColor = ColorType.Red;

            _history.Undo(_grid);
            Assert.AreEqual(ColorType.None, _grid.ActiveColor,
                "Undo should restore ActiveColor");
        }

        [Test]
        public void Record_CapturesLastPosition()
        {
            _history.Record(_grid);
            _grid.LastPosition = new Vector2Int(2, 2);

            _history.Undo(_grid);
            Assert.AreEqual(new Vector2Int(-1, -1), _grid.LastPosition,
                "Undo should restore LastPosition");
        }

        [Test]
        public void HistoryOverflow_DiscardsOldest()
        {
            for (int i = 0; i < 250; i++)
            {
                _grid.Grid[0, 0].State = CellState.Path;
                _history.Record(_grid);
            }

            Assert.IsTrue(_history.CanUndo, "CanUndo should be true after many snapshots");
            Assert.LessOrEqual(_history.UndoCount, 200,
                "Snapshot count should be capped at 200");
        }

        [Test]
        public void CanUndo_WhenEmpty_ReturnsFalse()
        {
            Assert.IsFalse(_history.CanUndo);
        }

        [Test]
        public void Undo_WhenEmpty_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _history.Undo(_grid));
        }
    }
}
