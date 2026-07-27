using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Models;
using PixelFlow.Services;

namespace PixelFlow.Commands
{
    /// <summary>
    /// GDD §8: Kaza veya pause butonu ile simülasyonu duraklatır.
    /// PauseSimulationSignal'i işler.
    /// </summary>
    public class PauseSimulationCommand : ICommand<PixelFlow.Signals.PauseSimulationSignal>, IResettable
    {
        [Inject] public IVehicleSimulator VehicleSimulator { get; set; }
        [Inject] public IGameStateModel GameStateModel { get; set; }
        [Inject] public ILoggerService LoggerService { get; set; }

        public void Execute(PixelFlow.Signals.PauseSimulationSignal signal)
        {
            var state = GameStateModel.CurrentState;
            LoggerService?.Log($"[PixelFlow.PauseSimulationCommand] PauseSimulationSignal received. Current state: {state}");

            switch (state)
            {
                case GameState.Simulating:
                    LoggerService?.Log("[PixelFlow.PauseSimulationCommand] Stopping simulation phase.");
                    VehicleSimulator.StopSimulationPhase();
                    break;
                case GameState.Playing:
                    LoggerService?.Log("[PixelFlow.PauseSimulationCommand] Pausing gameplay. State transition: Playing -> Paused.");
                    GameStateModel.SetState(GameState.Paused);
                    break;
                case GameState.Paused:
                    var prev = GameStateModel.PreviousState;
                    var next = prev == GameState.Simulating ? GameState.Simulating : GameState.Playing;
                    LoggerService?.Log($"[PixelFlow.PauseSimulationCommand] Unpausing gameplay. Reverting to previous state: {next}.");
                    GameStateModel.SetState(next);
                    break;
            }
        }

        public void Reset() { }
    }
}
