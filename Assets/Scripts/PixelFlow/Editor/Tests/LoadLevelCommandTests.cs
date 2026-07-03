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
    public class LoadLevelCommandTests
    {
        private NexusTestContext _ctx;
        private IGridModel _grid;
        private ILevelModel _level;
        private IGameStateModel _state;
        private IGameSessionModel _session;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
            _grid = _ctx.GetModel<IGridModel>();
            _level = _ctx.GetModel<ILevelModel>();
            _state = _ctx.GetModel<IGameStateModel>();
            _session = _ctx.GetModel<IGameSessionModel>();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        [Test]
        public void ValidLevel_InitializesGridDimensions()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            Assert.AreEqual(5, _grid.Width);
            Assert.AreEqual(5, _grid.Height);
        }

        [Test]
        public void ValidLevel_SetsNodesCorrectly()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            Assert.AreEqual(CellState.Node, _grid.Grid[0, 0].State);
            Assert.AreEqual(ColorType.Red, _grid.Grid[0, 0].Color);
            Assert.AreEqual(CellState.Node, _grid.Grid[4, 0].State);
            Assert.AreEqual(ColorType.Red, _grid.Grid[4, 0].Color);
        }

        [Test]
        public void ValidLevel_SetsBridgesCorrectly()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            Assert.AreEqual(CellState.Bridge, _grid.Grid[2, 2].State);
        }

        [Test]
        public void ValidLevel_SetsObstaclesCorrectly()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3; level.height = 1;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };
            level.obstacles = new List<ObstacleData>
            {
                new ObstacleData { position = new Vector2Int(1, 0), type = ObstacleType.Lake }
            };
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            Assert.AreEqual(ObstacleType.Lake, _grid.Grid[1, 0].ObstacleType);
            Assert.AreEqual(CellState.Obstacle, _grid.Grid[1, 0].State);
        }

        [Test]
        public void ValidLevel_SetsLevelModel()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            Assert.AreSame(level, _level.CurrentLevel);
        }

        [Test]
        public void ValidLevel_TransitionsToPlaying()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            Assert.AreEqual(GameState.Playing, _state.CurrentState);
        }

        [Test]
        public void ValidLevel_StartsNewSession()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            Assert.IsTrue(_session.IsSessionActive);
            Assert.AreEqual(0f, _session.ElapsedTime, 0.001f);
            Assert.AreEqual(0, _session.Score);
        }

        [Test]
        public void EmptyLevel_AllCellsEmpty()
        {
            var emptyLevel = ScriptableObject.CreateInstance<LevelData>();
            emptyLevel.levelIndex = 0;
            emptyLevel.width = 3;
            emptyLevel.height = 3;
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = emptyLevel });
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                Assert.AreEqual(CellState.Empty, _grid.Grid[x, y].State, $"({x},{y})");
        }

        [Test]
        public void LoadSecondLevel_ClearsPreviousState()
        {
            var level1 = CreateTestLevel(0);
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level1 });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            Assert.AreEqual(2, _grid.Paths[ColorType.Red].Count);

            var level2 = ScriptableObject.CreateInstance<LevelData>();
            level2.levelIndex = 1;
            level2.width = 3; level2.height = 1;
            level2.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Green },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Green },
            };
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level2 });

            Assert.AreEqual(3, _grid.Width);
            Assert.AreEqual(0, _grid.Paths.Count);
            Assert.AreEqual(ColorType.None, _grid.ActiveColor.Value);
        }

        [Test]
        public void LoadLevel_FiresGridUpdatedSignal()
        {
            _ctx.Register<GridUpdatedSignal>();
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            Assert.IsTrue(_ctx.SignalWasDispatched<GridUpdatedSignal>());
        }

        [Test]
        public void LoadLevel_WithOneWayObstacle_PreservesDirection()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3; level.height = 1;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };
            level.obstacles = new List<ObstacleData>
            {
                new ObstacleData { position = new Vector2Int(1, 0), type = ObstacleType.OneWay, oneWayDirection = Vector2Int.right }
            };
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            var obstacle = _ctx.Context.Container.Resolve<IObstacleService>();
            Assert.AreEqual(Vector2Int.right, obstacle.GetOneWayDirection(new Vector2Int(1, 0)));
        }
    }
}
