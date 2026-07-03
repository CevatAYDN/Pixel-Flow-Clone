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
    public class HintServiceTests
    {
        private NexusTestContext _ctx;
        private IHintModel _hintModel;
        private IGridModel _grid;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
            _hintModel = _ctx.GetModel<IHintModel>();
            _grid = _ctx.GetModel<IGridModel>();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        [Test]
        public void UseHint_DecrementsHintsRemaining()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            int hintsBefore = _hintModel.HintsRemaining;
            Assert.GreaterOrEqual(hintsBefore, 1);

            _ctx.Dispatch(new RequestHintSignal());
            Assert.AreEqual(hintsBefore - 1, _hintModel.HintsRemaining);
        }

        [Test]
        public void UseHint_IncrementsTotalHintsUsed()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            Assert.AreEqual(0, _hintModel.TotalHintsUsed);

            _ctx.Dispatch(new RequestHintSignal());
            Assert.AreEqual(1, _hintModel.TotalHintsUsed);
        }

        [Test]
        public void UseHint_AppliesPath()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.Dispatch(new RequestHintSignal());
            Assert.Greater(_grid.Paths.Count, 0, "At least one path should exist after hint");
        }

        [Test]
        public void UseHint_RespectsSolvedColors()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            // Draw a partial Red path (2 cells)
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(2, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(3, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(4, 0) });

            int redPathCount = _grid.Paths[ColorType.Red].Count;

            _ctx.Dispatch(new RequestHintSignal());
            Assert.AreEqual(redPathCount, _grid.Paths[ColorType.Red].Count,
                "Solved red path should not be modified by hint");
        }

        [Test]
        public void UseHint_WithNoHintsLeft_DoesNothing()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            int hintCount = _hintModel.HintsRemaining;
            for (int i = 0; i < hintCount; i++)
                _ctx.Dispatch(new RequestHintSignal());

            int hintsAfter = _hintModel.HintsRemaining;
            _ctx.Dispatch(new RequestHintSignal());
            Assert.AreEqual(hintsAfter, _hintModel.HintsRemaining,
                "Hint count should not go below 0");
        }
    }
}
