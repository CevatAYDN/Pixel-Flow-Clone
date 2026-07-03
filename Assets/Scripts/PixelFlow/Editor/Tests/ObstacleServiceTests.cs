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
    public class ObstacleServiceTests
    {
        private NexusTestContext _ctx;
        private IObstacleService _obstacle;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
            _obstacle = _ctx.Context.Container.Resolve<IObstacleService>();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        private LevelData CreateLevelWithObstacles(List<ObstacleData> obstacles)
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
            level.obstacles = obstacles;
            return level;
        }

        [Test]
        public void OneWay_DefaultDirection_IsRight()
        {
            var obstacles = new List<ObstacleData>
            {
                new ObstacleData { position = new Vector2Int(1, 1), type = ObstacleType.OneWay }
            };
            _obstacle.InitializeFromLevel(CreateLevelWithObstacles(obstacles));
            Assert.AreEqual(Vector2Int.right, _obstacle.GetOneWayDirection(new Vector2Int(1, 1)));
        }

        [Test]
        public void OneWay_WithDirection_StoresCorrectly()
        {
            var obstacles = new List<ObstacleData>
            {
                new ObstacleData { position = new Vector2Int(1, 1), type = ObstacleType.OneWay, oneWayDirection = Vector2Int.up }
            };
            _obstacle.InitializeFromLevel(CreateLevelWithObstacles(obstacles));
            Assert.AreEqual(Vector2Int.up, _obstacle.GetOneWayDirection(new Vector2Int(1, 1)));
        }

        [Test]
        public void OneWay_BlocksWrongDirection()
        {
            var obstacles = new List<ObstacleData>
            {
                new ObstacleData { position = new Vector2Int(1, 0), type = ObstacleType.OneWay, oneWayDirection = Vector2Int.right }
            };
            _obstacle.InitializeFromLevel(CreateLevelWithObstacles(obstacles));
            // Going left is blocked (wrong direction for right-only)
            Assert.IsTrue(_obstacle.IsOneWay(new Vector2Int(1, 0), ColorType.Red, Vector2Int.left));
            // Going right is allowed
            Assert.IsFalse(_obstacle.IsOneWay(new Vector2Int(1, 0), ColorType.Red, Vector2Int.right));
        }

        [Test]
        public void NarrowPass_FreeCell_AllowsAnyColor()
        {
            var obstacles = new List<ObstacleData>
            {
                new ObstacleData { position = new Vector2Int(1, 0), type = ObstacleType.NarrowPass }
            };
            _obstacle.InitializeFromLevel(CreateLevelWithObstacles(obstacles));
            Assert.IsTrue(_obstacle.IsNarrowPass(new Vector2Int(1, 0)));
            Assert.IsTrue(_obstacle.CanVehicleEnterNarrowPass(new Vector2Int(1, 0), ColorType.Red));
            Assert.IsTrue(_obstacle.CanVehicleEnterNarrowPass(new Vector2Int(1, 0), ColorType.Blue));
        }

        [Test]
        public void NarrowPass_OccupiedByColor_RejectsOtherColor()
        {
            var obstacles = new List<ObstacleData>
            {
                new ObstacleData { position = new Vector2Int(1, 0), type = ObstacleType.NarrowPass }
            };
            _obstacle.InitializeFromLevel(CreateLevelWithObstacles(obstacles));
            _obstacle.OnVehicleEnteredNarrowPass(new Vector2Int(1, 0), ColorType.Red);
            Assert.IsFalse(_obstacle.CanVehicleEnterNarrowPass(new Vector2Int(1, 0), ColorType.Blue));
            Assert.IsTrue(_obstacle.CanVehicleEnterNarrowPass(new Vector2Int(1, 0), ColorType.Red));
        }

        [Test]
        public void NarrowPass_VehicleLeaves_CellBecomesFree()
        {
            var obstacles = new List<ObstacleData>
            {
                new ObstacleData { position = new Vector2Int(1, 0), type = ObstacleType.NarrowPass }
            };
            _obstacle.InitializeFromLevel(CreateLevelWithObstacles(obstacles));
            _obstacle.OnVehicleEnteredNarrowPass(new Vector2Int(1, 0), ColorType.Red);
            _obstacle.OnVehicleLeftNarrowPass(new Vector2Int(1, 0), ColorType.Red);
            Assert.IsTrue(_obstacle.CanVehicleEnterNarrowPass(new Vector2Int(1, 0), ColorType.Blue));
        }

        [Test]
        public void NarrowPass_LeaveWithWrongColor_DoesNotFree()
        {
            var obstacles = new List<ObstacleData>
            {
                new ObstacleData { position = new Vector2Int(1, 0), type = ObstacleType.NarrowPass }
            };
            _obstacle.InitializeFromLevel(CreateLevelWithObstacles(obstacles));
            _obstacle.OnVehicleEnteredNarrowPass(new Vector2Int(1, 0), ColorType.Red);
            _obstacle.OnVehicleLeftNarrowPass(new Vector2Int(1, 0), ColorType.Blue);
            Assert.IsFalse(_obstacle.CanVehicleEnterNarrowPass(new Vector2Int(1, 0), ColorType.Blue));
        }

        [Test]
        public void NonObstacleCell_NotOneWay()
        {
            var obstacles = new List<ObstacleData>
            {
                new ObstacleData { position = new Vector2Int(1, 0), type = ObstacleType.Lake }
            };
            _obstacle.InitializeFromLevel(CreateLevelWithObstacles(obstacles));
            Assert.IsFalse(_obstacle.IsOneWay(new Vector2Int(1, 0), ColorType.Red, Vector2Int.left));
        }

        [Test]
        public void NonNarrowPassCell_ReturnsFalse()
        {
            _obstacle.InitializeFromLevel(CreateLevelWithObstacles(new List<ObstacleData>()));
            Assert.IsFalse(_obstacle.IsNarrowPass(new Vector2Int(0, 0)));
        }

        [Test]
        public void Ferry_InitialStateNotBlocked()
        {
            var obstacles = new List<ObstacleData>
            {
                new ObstacleData { position = new Vector2Int(1, 0), type = ObstacleType.Ferry }
            };
            _obstacle.InitializeFromLevel(CreateLevelWithObstacles(obstacles));
            Assert.IsFalse(_obstacle.IsFerryBlocked(new Vector2Int(1, 0)));
        }

        [Test]
        public void Tick_ChangesFerryBlockedState()
        {
            var obstacles = new List<ObstacleData>
            {
                new ObstacleData { position = new Vector2Int(1, 0), type = ObstacleType.Ferry }
            };
            _obstacle.InitializeFromLevel(CreateLevelWithObstacles(obstacles));
            _obstacle.Tick(11f); // > 10s period toggles ferry
            Assert.IsTrue(_obstacle.IsFerryBlocked(new Vector2Int(1, 0)));
        }
    }
}
