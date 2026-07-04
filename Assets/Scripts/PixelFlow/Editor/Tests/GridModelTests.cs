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
    public class GridModelTests
    {
        private NexusTestContext _ctx;
        private IGridModel _grid;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
            _grid = _ctx.GetModel<IGridModel>();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        [Test]
        public void Initialize_SetsDimensions()
        {
            _grid.Initialize(5, 7);
            Assert.AreEqual(5, _grid.Width);
            Assert.AreEqual(7, _grid.Height);
        }

        [Test]
        public void Initialize_AllCellsEmpty()
        {
            _grid.Initialize(3, 3);
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
            {
                Assert.AreEqual(CellState.Empty, _grid.Grid[x, y].State, $"({x},{y}) State");
                Assert.AreEqual(ColorType.None, _grid.Grid[x, y].Color, $"({x},{y}) Color");
                Assert.AreEqual(ObstacleType.None, _grid.Grid[x, y].ObstacleType, $"({x},{y}) ObstacleType");
            }
        }

        [Test]
        public void Initialize_DefaultPathColors_Empty()
        {
            _grid.Initialize(3, 3);
            Assert.AreEqual(0, _grid.Paths.Count);
        }

        [Test]
        public void Initialize_DefaultActiveColor_None()
        {
            _grid.Initialize(3, 3);
            Assert.AreEqual(ColorType.None, _grid.ActiveColor.Value);
            Assert.AreEqual(new Vector2Int(-1, -1), _grid.LastPosition.Value);
        }

        [Test]
        public void SetCell_StateAndColor_UpdatesCorrectly()
        {
            _grid.Initialize(3, 3);
            _grid.Grid[1, 1].State = CellState.Path;
            _grid.Grid[1, 1].Color = ColorType.Red;
            Assert.AreEqual(CellState.Path, _grid.Grid[1, 1].State);
            Assert.AreEqual(ColorType.Red, _grid.Grid[1, 1].Color);
        }

        [Test]
        public void ActiveColor_SetAndReset()
        {
            _grid.Initialize(5, 5);
            _grid.ActiveColor.Value = ColorType.Blue;
            Assert.AreEqual(ColorType.Blue, _grid.ActiveColor.Value);
            _grid.ActiveColor.Value = ColorType.None;
            Assert.AreEqual(ColorType.None, _grid.ActiveColor.Value);
        }

        [Test]
        public void LastPosition_SetAndReset()
        {
            _grid.Initialize(5, 5);
            _grid.LastPosition.Value = new Vector2Int(3, 4);
            Assert.AreEqual(new Vector2Int(3, 4), _grid.LastPosition.Value);
            _grid.LastPosition.Value = new Vector2Int(-1, -1);
            Assert.AreEqual(new Vector2Int(-1, -1), _grid.LastPosition.Value);
        }

        [Test]
        public void Paths_OverwriteExistingColor()
        {
            _grid.Initialize(5, 5);
            var firstPath = new List<Vector2Int> { new Vector2Int(0, 0) };
            _grid.Paths[ColorType.Red] = firstPath;
            var secondPath = new List<Vector2Int> { new Vector2Int(1, 1), new Vector2Int(2, 2) };
            _grid.Paths[ColorType.Red] = secondPath;
            Assert.AreEqual(2, _grid.Paths[ColorType.Red].Count);
            Assert.AreEqual(new Vector2Int(2, 2), _grid.Paths[ColorType.Red][1]);
        }

        [Test]
        public void PathCount_AfterPathDrawn()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            Assert.AreEqual(1, _grid.Paths.Count);
            Assert.IsTrue(_grid.Paths.ContainsKey(ColorType.Red));
        }

        [Test]
        public void MultipleColors_AddToPaths()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 4) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(0, 3) });
            Assert.AreEqual(2, _grid.Paths.Count);
            Assert.IsTrue(_grid.Paths.ContainsKey(ColorType.Red));
            Assert.IsTrue(_grid.Paths.ContainsKey(ColorType.Blue));
        }

        [Test]
        public void CellPathColors_AfterPathDrawn()
        {
            _grid.Initialize(5, 5);
            _grid.Grid[1, 1].State = CellState.Path;
            _grid.Grid[1, 1].Color = ColorType.Red;
            _grid.Grid[1, 1].AddPathColor(ColorType.Red);
            Assert.IsTrue(_grid.Grid[1, 1].HasPathColor(ColorType.Red));
        }
    }
}
