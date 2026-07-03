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
    public class CheckWinConditionCommandTests
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

        [Test]
        public void AfterLevelLoad_NotWin()
        {
            var level = CreateTestLevel();
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.Dispatch(new CheckWinConditionSignal());
            Assert.AreEqual(GameState.Playing, _state.CurrentState);
        }

        [Test]
        public void SingleColorConnected_OthersNot_DoesNotWin()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 2; level.height = 2;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(1, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(0, 1), color = ColorType.Green },
                new GridNode { position = new Vector2Int(1, 1), color = ColorType.Green },
            };
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });

            Assert.AreEqual(GameState.Playing, _state.CurrentState);
        }

        [Test]
        public void AllColorsConnected_AllCellsFilled_Wins()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3; level.height = 1;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });

            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(2, 0) });

            Assert.AreEqual(GameState.Simulating, _state.CurrentState);
        }

        [Test]
        public void AllColorsConnected_WithEmptyCells_requireFullGridCoverageFalse_Wins()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3; level.height = 2;
            level.requireFullGridCoverage = false;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(2, 0) });
            Assert.AreEqual(GameState.Simulating, _state.CurrentState);
        }

        [Test]
        public void AllColorsConnected_WithEmptyCells_requireFullGridCoverageTrue_DoesNotWin()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3; level.height = 2;
            level.requireFullGridCoverage = true;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(2, 0) });
            Assert.AreEqual(GameState.Playing, _state.CurrentState,
                "Should stay Playing when grid is not fully covered and requireFullGridCoverage is true");
        }

        [Test]
        public void AlreadySimulating_CheckWinCondition_NoOp()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelIndex = 0;
            level.width = 3; level.height = 1;
            level.initialNodes = new List<GridNode>
            {
                new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red },
                new GridNode { position = new Vector2Int(2, 0), color = ColorType.Red },
            };
            _ctx.Dispatch(new LoadLevelSignal { LevelToLoad = level });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.PointerDown, GridPosition = new Vector2Int(0, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(1, 0) });
            _ctx.Dispatch(new InputInteractionSignal { Type = InputType.Drag, GridPosition = new Vector2Int(2, 0) });
            Assert.AreEqual(GameState.Simulating, _state.CurrentState);
            _ctx.Dispatch(new CheckWinConditionSignal());
            Assert.AreEqual(GameState.Simulating, _state.CurrentState);
        }
    }
}
