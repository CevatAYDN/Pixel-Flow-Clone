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
    public class FlowScoreAndOneWayTests
    {
        private NexusTestContext _ctx;
        private IGameSessionModel _session;
        private IObstacleService _obstacles;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
            _session = _ctx.GetModel<IGameSessionModel>();
            _obstacles = _ctx.Context.Container.Resolve<IObstacleService>();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        [Test]
        public void SessionModel_StartSession_SetsFlowScoreTargets()
        {
            _session.StartSession(maxViaducts: 3, targetFlowScore: 12);
            Assert.AreEqual(0, _session.CurrentFlowScore);
            Assert.AreEqual(12, _session.TargetFlowScore);
        }

        [Test]
        public void SessionModel_IncrementFlowScore_Works()
        {
            _session.StartSession(maxViaducts: 3, targetFlowScore: 5);
            _session.IncrementFlowScore();
            _session.IncrementFlowScore();
            Assert.AreEqual(2, _session.CurrentFlowScore);
        }

        [Test]
        public void ObstacleService_LoadsOneWayCells_Correctly()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 5;
            level.height = 5;
            level.oneWayCells = new List<OneWayCell>
            {
                new OneWayCell { position = new Vector2Int(2, 2), allowedDirection = Vector2Int.up }
            };

            _obstacles.InitializeFromLevel(level);

            // GDD §2.7: IsOneWay, moveDir izin verilen yönün tersi/farklısı ise restriction olarak true döner
            // Yukarı gitmek serbest (IsOneWay restricted = false), aşağı gitmek bloklu (IsOneWay restricted = true)
            Assert.IsFalse(_obstacles.IsOneWay(new Vector2Int(2, 2), ColorType.Red, Vector2Int.up));
            Assert.IsTrue(_obstacles.IsOneWay(new Vector2Int(2, 2), ColorType.Red, Vector2Int.down));
            
            Assert.AreEqual(Vector2Int.up, _obstacles.GetOneWayDirection(new Vector2Int(2, 2)));
            // OneWay olmayan normal hücrede restriction false dönmeli
            Assert.IsFalse(_obstacles.IsOneWay(new Vector2Int(0, 0), ColorType.Red, Vector2Int.up));
        }

        [Test]
        public void ObstacleService_OneWayDirectionValidation_AllowsCorrectAndBlocksIncorrect()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 5;
            level.height = 5;
            level.oneWayCells = new List<OneWayCell>
            {
                new OneWayCell { position = new Vector2Int(2, 2), allowedDirection = Vector2Int.right }
            };

            _obstacles.InitializeFromLevel(level);

            // (2, 2) hücresine sağa doğru (Vector2Int.right) hareket etmek kısıtlanmamalı (false dönmeli)
            Assert.IsFalse(_obstacles.IsOneWay(new Vector2Int(2, 2), ColorType.Red, Vector2Int.right));

            // (2, 2) hücresine sola doğru (Vector2Int.left) gitmeye çalışmak kısıtlanmalı (true dönmeli)
            Assert.IsTrue(_obstacles.IsOneWay(new Vector2Int(2, 2), ColorType.Red, Vector2Int.left));
        }
    }
}
