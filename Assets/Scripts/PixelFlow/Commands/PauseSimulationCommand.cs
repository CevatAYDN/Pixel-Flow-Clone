using Nexus.Core;
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

        public void Execute(PixelFlow.Signals.PauseSimulationSignal signal)
        {
            switch (GameStateModel.CurrentState)
            {
                case GameState.Simulating:
                    VehicleSimulator.StopSimulationPhase();
                    break;
                case GameState.Playing:
                    GameStateModel.SetState(GameState.Paused);
                    break;
                case GameState.Paused:
                    GameStateModel.SetState(GameState.Playing);
                    break;
            }
        }

        public void Reset() { }
    }
}
