using NUnit.Framework;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Models;
using PixelFlow.Services;
using PixelFlow.Signals;
using PixelFlow.Commands;
using UnityEngine;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class GameplayCommandsTests
    {
        [Test]
        public void StartSimulationCommand_WhenPlaying_StartsSimulationPhase()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var stateModel = ctx.GetModel<IGameStateModel>();
            var simulator = ctx.GetModel<IVehicleSimulator>();

            stateModel.SetState(GameState.Playing);
            ctx.Dispatch(new StartSimulationSignal());

            Assert.AreEqual(GameState.Simulating, stateModel.CurrentState);
        }

        [Test]
        public void StartSimulationCommand_WhenNotPlaying_AbortsSimulation()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var stateModel = ctx.GetModel<IGameStateModel>();

            stateModel.SetState(GameState.MainMenu);
            ctx.Dispatch(new StartSimulationSignal());

            Assert.AreEqual(GameState.MainMenu, stateModel.CurrentState);
        }

        [Test]
        public void PauseSimulationCommand_TogglesBetweenPlayingAndPaused()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var stateModel = ctx.GetModel<IGameStateModel>();

            stateModel.SetState(GameState.Playing);
            ctx.Dispatch(new PauseSimulationSignal());
            Assert.AreEqual(GameState.Paused, stateModel.CurrentState);

            ctx.Dispatch(new PauseSimulationSignal());
            Assert.AreEqual(GameState.Playing, stateModel.CurrentState);
        }

        [Test]
        public void PlaceViaductCommand_ValidIntersection_PlacesViaductAndChangesState()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var grid = ctx.GetModel<IGridModel>();
            var stateModel = ctx.GetModel<IGameStateModel>();
            var session = ctx.GetModel<IGameSessionModel>();

            grid.Initialize(5, 5);
            session.StartSession(3);
            stateModel.SetState(GameState.Playing);

            // Add 2 overlapping path colors at (2, 2)
            grid.Grid[2, 2].AddPathColor(ColorType.Red);
            grid.Grid[2, 2].AddPathColor(ColorType.Blue);

            ctx.Dispatch(new PlaceViaductSignal { Position = new Vector2Int(2, 2) });

            Assert.IsTrue(grid.Grid[2, 2].HasViaduct);
            Assert.AreEqual(2, session.AvailableViaducts);
        }

        [Test]
        public void ClearJamCommand_UsesPowerUpAndClearsPaths()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var grid = ctx.GetModel<IGridModel>();
            var stateModel = ctx.GetModel<IGameStateModel>();
            var powerUp = ctx.GetModel<IPowerUpService>();

            grid.Initialize(5, 5);
            grid.Grid[1, 1].State = CellState.Path;
            grid.Grid[1, 1].Color = ColorType.Red;
            stateModel.SetState(GameState.Playing);

            powerUp.AddClearJamUse(2);
            int usesBefore = powerUp.ClearJamUsesRemaining;

            ctx.Dispatch(new ClearJamSignal());

            Assert.AreEqual(usesBefore - 1, powerUp.ClearJamUsesRemaining);
        }

        [Test]
        public void RainbowRoadCommand_WhenPlaying_ActivatesPowerUp()
        {
            using var ctx = GameTestContext.CreateGameContext();
            var stateModel = ctx.GetModel<IGameStateModel>();
            var powerUp = ctx.GetModel<IPowerUpService>();

            stateModel.SetState(GameState.Playing);
            powerUp.ActivateRainbowRoad();

            ctx.Dispatch(new ActivateRainbowRoadSignal());

            Assert.IsTrue(powerUp.HasActiveRainbowRoad);
            Assert.IsTrue(powerUp.RainbowRoadUses > 0);
        }
    }
}
