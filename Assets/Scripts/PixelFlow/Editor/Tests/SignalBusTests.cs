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
    public class SignalBusTests
    {
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
        public void ProcessInputDrag_FiresGridUpdatedSignal()
        {
            _ctx.Register<GridUpdatedSignal>();
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.ClearDispatchedSignals();

            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });

            int count = _ctx.GetDispatchedSignalCount<GridUpdatedSignal>();
            Assert.GreaterOrEqual(count, 1, "Drag should fire at least 1 GridUpdatedSignal (PointerDown no longer fires it)");
        }

        [Test]
        public void Hint_AppliesSolution_FiresGridUpdatedAndCheckWinCondition()
        {
            _ctx.Register<GridUpdatedSignal>();
            _ctx.Register<CheckWinConditionSignal>();

            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.ClearDispatchedSignals();

            _ctx.Dispatch(new RequestHintSignal());
            Assert.IsTrue(_ctx.SignalWasDispatched<GridUpdatedSignal>());
            Assert.IsTrue(_ctx.SignalWasDispatched<CheckWinConditionSignal>());
        }

        [Test]
        public void LevelCompleted_FiresProgressUpdatedSignal()
        {
            _ctx.Register<ProgressUpdatedSignal>();

            var level = CreateTestLevel(0);
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.ClearDispatchedSignals();

            _ctx.Dispatch(new LevelCompletedSignal());
            Assert.IsTrue(_ctx.SignalWasDispatched<ProgressUpdatedSignal>());
        }

        [Test]
        public void Undo_FiresGridUpdatedSignal()
        {
            _ctx.Register<GridUpdatedSignal>();

            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            _ctx.ClearDispatchedSignals();

            _ctx.Dispatch(new UndoSignal());
            Assert.IsTrue(_ctx.SignalWasDispatched<GridUpdatedSignal>());
        }
    }
}
