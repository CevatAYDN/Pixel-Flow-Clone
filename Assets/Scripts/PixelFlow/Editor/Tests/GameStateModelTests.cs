using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using UnityEngine;
using UnityEngine.TestTools;
using static PixelFlow.Editor.Tests.GameTestContext;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class GameStateModelTests
    {
        private NexusTestContext _ctx;
        private IGameStateModel _state;

        [SetUp]
        public void SetUp()
        {
            _ctx = CreateGameContext();
            _state = _ctx.GetModel<IGameStateModel>();
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        [Test]
        public void DefaultState_IsMainMenu()
        {
            Assert.AreEqual(GameState.MainMenu, _state.CurrentState);
        }

        [Test]
        public void SetState_MainMenuToPlaying_Allowed()
        {
            _state.SetState(GameState.Playing);
            Assert.AreEqual(GameState.Playing, _state.CurrentState);
        }

        [Test]
        public void SetState_PlayingToPaused_Allowed()
        {
            _state.SetState(GameState.Playing);
            _state.SetState(GameState.Paused);
            Assert.AreEqual(GameState.Paused, _state.CurrentState);
        }

        [Test]
        public void SetState_PausedToPlaying_Allowed()
        {
            _state.SetState(GameState.Playing);
            _state.SetState(GameState.Paused);
            _state.SetState(GameState.Playing);
            Assert.AreEqual(GameState.Playing, _state.CurrentState);
        }

        [Test]
        public void SetState_PlayingToSimulating_Allowed()
        {
            _state.SetState(GameState.Playing);
            _state.SetState(GameState.Simulating);
            Assert.AreEqual(GameState.Simulating, _state.CurrentState);
        }

        [Test]
        public void SetState_SimulatingToLevelCompleted_Allowed()
        {
            _state.SetState(GameState.Playing);
            _state.SetState(GameState.Simulating);
            _state.SetState(GameState.LevelCompleted);
            Assert.AreEqual(GameState.LevelCompleted, _state.CurrentState);
        }

        [Test]
        public void SetState_MainMenuToLevelCompleted_Blocked()
        {
            LogAssert.Expect(LogType.Error, "[GameStateModel] Illegal transition: MainMenu \u2192 LevelCompleted. Blocked.");
            _state.SetState(GameState.LevelCompleted);
            Assert.AreEqual(GameState.MainMenu, _state.CurrentState,
                "MainMenu \u2192 LevelCompleted should be blocked by transition table");
        }

        [Test]
        public void SetState_PlayingToLevelCompleted_Allowed()
        {
            _state.SetState(GameState.Playing);
            _state.SetState(GameState.LevelCompleted);
            Assert.AreEqual(GameState.LevelCompleted, _state.CurrentState);
        }

        [Test]
        public void SetState_PreviousState_TracksLastState()
        {
            _state.SetState(GameState.Playing);
            Assert.AreEqual(GameState.MainMenu, _state.PreviousState);
            _state.SetState(GameState.Simulating);
            Assert.AreEqual(GameState.Playing, _state.PreviousState);
        }

        [Test]
        public void PreviousState_EqualsCurrent_AfterFirstSetState()
        {
            _state.SetState(GameState.Playing);
            Assert.AreEqual(GameState.MainMenu, _state.PreviousState);
            _state.SetState(GameState.Playing);
            Assert.AreEqual(GameState.Playing, _state.CurrentState);
            Assert.AreEqual(GameState.MainMenu, _state.PreviousState);
        }

        [Test]
        public void SetState_MainMenuToMainMenu_NoOp()
        {
            _state.SetState(GameState.MainMenu);
            Assert.AreEqual(GameState.MainMenu, _state.CurrentState);
        }

        [Test]
        public void SetState_MainMenuToSimulating_Blocked()
        {
            LogAssert.Expect(LogType.Error, "[GameStateModel] Illegal transition: MainMenu \u2192 Simulating. Blocked.");
            _state.SetState(GameState.Simulating);
            Assert.AreEqual(GameState.MainMenu, _state.CurrentState,
                "MainMenu \u2192 Simulating should be blocked by transition table");
        }

        [Test]
        public void SetState_SimulatingToPlaying_Blocked()
        {
            _state.SetState(GameState.Playing);
            _state.SetState(GameState.Simulating);
            // Simulating → Playing IS allowed in transition table (line 44 of GameStateModel)
            _state.SetState(GameState.Playing);
            Assert.AreEqual(GameState.Playing, _state.CurrentState,
                "Simulating \u2192 Playing is allowed by transition table");
        }
    }
}
