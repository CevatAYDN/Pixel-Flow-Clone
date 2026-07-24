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
            // Default state is now Boot (per game plan). Tests needing MainMenu call GoToMainMenu().
        }

        private void GoToMainMenu()
        {
            if (_state.CurrentState == GameState.Boot)
                _state.SetState(GameState.MainMenu);
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
        }

        [Test]
        public void DefaultState_IsBoot()
        {
            Assert.AreEqual(GameState.Boot, _state.CurrentState);
        }

        [Test]
        public void SetState_BootToMainMenu_Allowed()
        {
            _state.SetState(GameState.MainMenu);
            Assert.AreEqual(GameState.MainMenu, _state.CurrentState);
        }

        [Test]
        public void SetState_MainMenuToPlaying_Allowed()
        {
            GoToMainMenu();
            _state.SetState(GameState.Playing);
            Assert.AreEqual(GameState.Playing, _state.CurrentState);
        }

        [Test]
        public void SetState_PlayingToPaused_Allowed()
        {
            GoToMainMenu();
            _state.SetState(GameState.Playing);
            _state.SetState(GameState.Paused);
            Assert.AreEqual(GameState.Paused, _state.CurrentState);
        }

        [Test]
        public void SetState_PausedToPlaying_Allowed()
        {
            GoToMainMenu();
            _state.SetState(GameState.Playing);
            _state.SetState(GameState.Paused);
            _state.SetState(GameState.Playing);
            Assert.AreEqual(GameState.Playing, _state.CurrentState);
        }

        [Test]
        public void SetState_PlayingToSimulating_Allowed()
        {
            GoToMainMenu();
            _state.SetState(GameState.Playing);
            _state.SetState(GameState.Simulating);
            Assert.AreEqual(GameState.Simulating, _state.CurrentState);
        }

        [Test]
        public void SetState_SimulatingToLevelCompleted_Allowed()
        {
            GoToMainMenu();
            _state.SetState(GameState.Playing);
            _state.SetState(GameState.Simulating);
            _state.SetState(GameState.LevelCompleted);
            Assert.AreEqual(GameState.LevelCompleted, _state.CurrentState);
        }

        [Test]
        public void SetState_MainMenuToLevelCompleted_Blocked()
        {
            GoToMainMenu();
            LogAssert.Expect(LogType.Error, "[GameStateModel] Illegal transition: MainMenu \u2192 LevelCompleted. Blocked.");
            _state.SetState(GameState.LevelCompleted);
            Assert.AreEqual(GameState.MainMenu, _state.CurrentState,
                "MainMenu \u2192 LevelCompleted should be blocked by transition table");
        }

        [Test]
        public void SetState_PlayingToLevelCompleted_Allowed()
        {
            GoToMainMenu();
            _state.SetState(GameState.Playing);
            _state.SetState(GameState.LevelCompleted);
            Assert.AreEqual(GameState.LevelCompleted, _state.CurrentState);
        }

        [Test]
        public void SetState_PreviousState_TracksLastState()
        {
            GoToMainMenu();
            _state.SetState(GameState.Playing);
            Assert.AreEqual(GameState.MainMenu, _state.PreviousState);
            _state.SetState(GameState.Simulating);
            Assert.AreEqual(GameState.Playing, _state.PreviousState);
        }

        [Test]
        public void PreviousState_EqualsCurrent_AfterFirstSetState()
        {
            _state.SetState(GameState.MainMenu);
            Assert.AreEqual(GameState.Boot, _state.PreviousState);
            _state.SetState(GameState.Playing);
            Assert.AreEqual(GameState.Playing, _state.CurrentState);
            Assert.AreEqual(GameState.MainMenu, _state.PreviousState);
        }

        [Test]
        public void SetState_MainMenuToMainMenu_NoOp()
        {
            GoToMainMenu();
            _state.SetState(GameState.MainMenu);
            Assert.AreEqual(GameState.MainMenu, _state.CurrentState);
        }

        [Test]
        public void SetState_MainMenuToSimulating_Blocked()
        {
            GoToMainMenu();
            LogAssert.Expect(LogType.Error, "[GameStateModel] Illegal transition: MainMenu \u2192 Simulating. Blocked.");
            _state.SetState(GameState.Simulating);
            Assert.AreEqual(GameState.MainMenu, _state.CurrentState,
                "MainMenu \u2192 Simulating should be blocked by transition table");
        }

        [Test]
        public void SetState_SimulatingToPlaying_Allowed()
        {
            _state.SetState(GameState.Playing);
            _state.SetState(GameState.Simulating);
            // Simulating → Playing IS allowed in transition table (line 44 of GameStateModel)
            _state.SetState(GameState.Playing);
            Assert.AreEqual(GameState.Playing, _state.CurrentState,
                "Simulating → Playing is allowed by transition table");
        }

        // === LevelSelect geçişleri (settings-levels.html "SEVİYE SEÇİMİ") ===

        [Test]
        public void SetState_MainMenuToLevelSelect_Allowed()
        {
            GoToMainMenu();
            _state.SetState(GameState.LevelSelect);
            Assert.AreEqual(GameState.LevelSelect, _state.CurrentState);
        }

        [Test]
        public void SetState_LevelSelectToPlaying_Allowed()
        {
            GoToMainMenu();
            _state.SetState(GameState.LevelSelect);
            _state.SetState(GameState.Playing);
            Assert.AreEqual(GameState.Playing, _state.CurrentState,
                "Bir seviye seçilince LevelSelect → Playing izinli olmalı");
        }

        [Test]
        public void SetState_LevelSelectToMainMenu_Allowed()
        {
            GoToMainMenu();
            _state.SetState(GameState.LevelSelect);
            _state.SetState(GameState.MainMenu);
            Assert.AreEqual(GameState.MainMenu, _state.CurrentState,
                "Geri butonu LevelSelect → MainMenu izinli olmalı");
        }

        [Test]
        public void SetState_LevelSelectToPaused_Blocked()
        {
            GoToMainMenu();
            _state.SetState(GameState.LevelSelect);
            LogAssert.Expect(LogType.Error, "[GameStateModel] Illegal transition: LevelSelect \u2192 Paused. Blocked.");
            _state.SetState(GameState.Paused);
            Assert.AreEqual(GameState.LevelSelect, _state.CurrentState,
                "LevelSelect → Paused transition tablosunda yok, bloklanmalı");
        }
    }
}
